// Mục đích: chuyển dữ liệu Excel đầu vào thành model nghiệp vụ và gom lỗi import.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace HospitalQualityDashboardDemo.Services
{
    public partial class ExcelImportExportService
    {
        private static IList<IDictionary<string, string>> ReadCsv(Stream stream)
        {
            var rows = new List<IDictionary<string, string>>();
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                var headerLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    return rows;
                }

                if (headerLine.Length > MaxCsvLineLength)
                {
                    throw new InvalidOperationException("Dòng CSV vượt quá giới hạn cho phép.");
                }

                var headers = SplitCsv(headerLine).ToArray();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length > MaxCsvLineLength)
                    {
                        throw new InvalidOperationException("Dòng CSV vượt quá giới hạn cho phép.");
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var values = SplitCsv(line).ToArray();
                    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < headers.Length; i++)
                    {
                        row[headers[i]] = i < values.Length ? values[i] : string.Empty;
                    }

                    rows.Add(row);
                    if (rows.Count > MaxImportRows)
                    {
                        throw new InvalidOperationException("File import vượt quá số dòng tối đa cho phép.");
                    }
                }
            }

            return rows;
        }

        // Đọc nội dung XLSX bằng OpenXml và chuẩn hóa thành bảng chuỗi để các luồng import dùng chung.
        private static IList<IDictionary<string, string>> ReadXlsx(Stream stream)
        {
            var rows = new List<IDictionary<string, string>>();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheet == null)
                {
                    throw new InvalidOperationException("Không tìm thấy sheet đầu tiên trong file Excel.");
                }

                ValidateZipEntry(sheet);

                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XDocument document;
                using (var sheetStream = sheet.Open())
                {
                    document = XDocument.Load(sheetStream);
                }

                var rawRows = document.Descendants(ns + "row")
                    .Select(r => r.Elements(ns + "c").ToDictionary(
                        c => GetColumnName(Convert.ToString(c.Attribute("r").Value)),
                        c => ReadCellValue(c, ns, sharedStrings),
                        StringComparer.OrdinalIgnoreCase))
                    .Take(MaxImportRows + 2)
                    .ToList();

                if (rawRows.Count == 0)
                {
                    return rows;
                }

                if (rawRows.Count > MaxImportRows + 1)
                {
                    throw new InvalidOperationException("File import vượt quá số dòng tối đa cho phép.");
                }

                var headerColumns = new List<KeyValuePair<string, string>>();
                var seenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var header in rawRows[0].OrderBy(k => ColumnIndex(k.Key)))
                {
                    var headerName = (header.Value ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(headerName) || !seenHeaders.Add(headerName))
                    {
                        continue;
                    }

                    headerColumns.Add(new KeyValuePair<string, string>(header.Key, headerName));
                }

                foreach (var rawRow in rawRows.Skip(1))
                {
                    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var header in headerColumns)
                    {
                        string value;
                        row[header.Value] = rawRow.TryGetValue(header.Key, out value) ? value : string.Empty;
                    }

                    if (row.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                    {
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        // Đọc bảng chỉ số từ file Word để hỗ trợ import danh mục chỉ số từ tài liệu nghiệp vụ.
        private static IList<IDictionary<string, string>> ReadIndicatorDocxTables(Stream stream)
        {
            var rows = new List<IDictionary<string, string>>();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                var documentEntry = archive.GetEntry("word/document.xml");
                if (documentEntry == null)
                {
                    throw new InvalidOperationException("Không tìm thấy nội dung document.xml trong file Word.");
                }

                ValidateZipEntry(documentEntry);

                XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                XDocument document;
                using (var documentStream = documentEntry.Open())
                {
                    document = XDocument.Load(documentStream);
                }

                foreach (var table in document.Descendants(w + "tbl"))
                {
                    var row = ReadIndicatorTable(table, w);
                    string name;
                    if (row.TryGetValue("TenChiSo", out name) && !string.IsNullOrWhiteSpace(name))
                    {
                        rows.Add(row);
                        if (rows.Count > MaxImportRows)
                        {
                            throw new InvalidOperationException("File import vượt quá số dòng tối đa cho phép.");
                        }
                    }
                }
            }

            return rows;
        }

        // Chuyển một bảng chỉ số trong tài liệu Word thành các dòng import có thể kiểm tra lỗi.
        private static IDictionary<string, string> ReadIndicatorTable(XElement table, XNamespace w)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tableRow in table.Elements(w + "tr"))
            {
                var cells = tableRow.Elements(w + "tc").Select(c => ReadWordCellText(c, w)).ToList();
                if (cells.Count < 2)
                {
                    continue;
                }

                var label = cells[0];
                var value = string.Join("\n", cells.Skip(1).Where(c => !string.IsNullOrWhiteSpace(c))).Trim();
                if (cells.Count >= 3 && IsDetailLabel(cells[1]) && IsMethodOrBlank(label))
                {
                    label = cells[1];
                    value = string.Join("\n", cells.Skip(2).Where(c => !string.IsNullOrWhiteSpace(c))).Trim();
                }

                var key = MapIndicatorDocxLabel(label);
                if (key == null)
                {
                    continue;
                }

                if (string.Equals(key, "MucTieuDatDuoc", StringComparison.OrdinalIgnoreCase))
                {
                    var year = FindYear(label);
                    if (year.HasValue)
                    {
                        result["NamMucTieu"] = year.Value.ToString(CultureInfo.InvariantCulture);
                    }
                }

                AddOrAppend(result, key, value);
            }

            return result;
        }

        // Gom text trong một ô Word, giữ thứ tự đoạn để không mất ý nghĩa định nghĩa chỉ số.
        private static string ReadWordCellText(XElement cell, XNamespace w)
        {
            var paragraphs = cell.Elements(w + "p")
                .Select(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)).Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (paragraphs.Count > 0)
            {
                return string.Join("\n", paragraphs);
            }

            return string.Concat(cell.Descendants(w + "t").Select(t => t.Value)).Trim();
        }

        // Ánh xạ nhãn trong tài liệu Word sang tên cột chuẩn để import chỉ số không phụ thuộc cách viết tiêu đề.
        private static string MapIndicatorDocxLabel(string label)
        {
            var normalized = NormalizeLabel(label);
            if (normalized == "ten chi so") return "TenChiSo";
            if (normalized == "chi so") return "TenChiSo";
            if (normalized == "dinh nghia chi so") return "DinhNghia";
            if (normalized == "linh vuc ap dung") return "LinhVucApDung";
            if (normalized == "khia canh chat luong") return "KhiaCanhChatLuong";
            if (normalized == "thanh to chat luong") return "ThanhToChatLuong";
            if (normalized == "ly do lua chon") return "LyDoLuaChon";
            if (normalized == "ly do chon lua") return "LyDoLuaChon";
            if (normalized == "phuong phap tinh") return "PhuongPhapTinh";
            if (normalized == "tu so") return "TuSoMoTa";
            if (normalized == "mau so") return "MauSoMoTa";
            if (normalized == "nguon so lieu") return "NguonSoLieu";
            if (normalized == "thu thap va tong hop so lieu") return "ThuThapTongHop";
            if (normalized == "thu nhap va tong hop so lieu") return "ThuThapTongHop";
            if (normalized == "gia tri cua so lieu") return "GiaTriSoLieu";
            if (normalized == "tan suat bao cao") return "TanSuatBaoCao";
            if (normalized.StartsWith("muc tieu dat duoc")) return "MucTieuDatDuoc";
            return null;
        }

        // Nhận diện nhãn đặc biệt trong bảng định nghĩa chỉ số để ghép đúng phần mô tả công thức.
        private static bool IsDetailLabel(string label)
        {
            var normalized = NormalizeLabel(label);
            return normalized == "tu so" || normalized == "mau so";
        }

        // Bỏ qua dòng nhãn phương pháp tính hoặc dòng trống khi đọc bảng định nghĩa chỉ số từ Word.
        private static bool IsMethodOrBlank(string label)
        {
            var normalized = NormalizeLabel(label);
            return string.IsNullOrWhiteSpace(normalized) || normalized == "phuong phap tinh";
        }

        // Gộp nhiều mảnh mô tả cùng khóa để không mất nội dung dài khi đọc từ tài liệu nguồn.
        private static void AddOrAppend(IDictionary<string, string> row, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string existing;
            if (row.TryGetValue(key, out existing) && !string.IsNullOrWhiteSpace(existing))
            {
                row[key] = existing + "\n" + value;
                return;
            }

            row[key] = value;
        }

        // Tìm năm trong nội dung import để gán mục tiêu hoặc kỳ áp dụng của chỉ số khi nguồn có nhiều năm.
        private static int? FindYear(string value)
        {
            var match = Regex.Match(value ?? string.Empty, @"(19|20)\d{2}");
            int year;
            return match.Success && int.TryParse(match.Value, out year) ? year : (int?)null;
        }

        // Chuẩn hóa nhãn import về dạng không dấu, chữ thường để nhận diện cột ổn định hơn.
        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (ch == '\u0111')
                {
                    builder.Append('d');
                    continue;
                }

                builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            }

            return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        }

        // Đọc shared string của workbook Excel để giải mã giá trị cell kiểu chuỗi.
        private static IList<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
            {
                return new List<string>();
            }

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            using (var stream = entry.Open())
            {
                ValidateZipEntry(entry);
                var values = XDocument.Load(stream).Descendants(ns + "si")
                    .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
                    .Take(MaxSharedStrings + 1)
                    .ToList();
                if (values.Count > MaxSharedStrings)
                {
                    throw new InvalidOperationException("File Excel vượt quá số shared strings tối đa cho phép.");
                }

                return values;
            }
        }

        // Đọc giá trị cell Excel theo kiểu dữ liệu, bao gồm shared string và giá trị trực tiếp.
        private static string ReadCellValue(XElement cell, XNamespace ns, IList<string> sharedStrings)
        {
            var type = (string)cell.Attribute("t");
            if (type == "inlineStr")
            {
                return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));
            }

            var valueElement = cell.Element(ns + "v");
            if (valueElement == null)
            {
                return string.Empty;
            }

            var value = valueElement.Value;
            if (type == "s")
            {
                int index;
                return int.TryParse(value, out index) && index >= 0 && index < sharedStrings.Count ? sharedStrings[index] : string.Empty;
            }

            return value;
        }

        // Tách dòng CSV thủ công có xử lý dấu nháy để không làm vỡ dữ liệu có dấu phẩy trong ô.
        private static IEnumerable<string> SplitCsv(string line)
        {
            var values = new List<string>();
            var builder = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        builder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    values.Add(builder.ToString());
                    builder.Length = 0;
                }
                else
                {
                    builder.Append(ch);
                }
            }

            values.Add(builder.ToString());
            return values;
        }

        // Escape giá trị khi xuất CSV để tránh sai định dạng và giữ nguyên nội dung người dùng nhập.
        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // Vô hiệu hóa tiền tố công thức để ngăn Excel thực thi nội dung không tin cậy.
        private static string NeutralizeCsvFormula(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var trimmed = value.TrimStart();
            if (StartsWithFormulaPrefix(value) || StartsWithFormulaPrefix(trimmed))
            {
                return "'" + value;
            }

            return value;
        }

        // Kiểm tra nội dung có chứa mẫu cần nhận diện hay không.
        private static bool StartsWithFormulaPrefix(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            switch (value[0])
            {
                case '=':
                case '+':
                case '-':
                case '@':
                case '\t':
                case '\r':
                case '\n':
                    return true;
                default:
                    return false;
            }
        }

        // Giới hạn dung lượng file import để tránh xử lý file quá lớn gây treo request hoặc tốn bộ nhớ.
        private static void ValidateImportSize(HttpPostedFileBase file)
        {
            if (file.ContentLength > MaxImportBytes)
            {
                throw new InvalidOperationException("File import vượt quá dung lượng tối đa cho phép.");
            }
        }

        // Chặn entry trong file Office nén quá lớn để giảm rủi ro zip bomb khi đọc Excel/Word.
        private static void ValidateZipEntry(ZipArchiveEntry entry)
        {
            if (entry.Length > MaxZipEntryBytes)
            {
                throw new InvalidOperationException("Nội dung file Office vượt quá giới hạn cho phép.");
            }

            if (entry.CompressedLength > 0 && entry.Length / entry.CompressedLength > MaxZipExpansionRatio)
            {
                throw new InvalidOperationException("Tỉ lệ giải nén file Office vượt quá giới hạn cho phép.");
            }
        }

        // Bổ sung dữ liệu mới phục vụ nhập và xuất dữ liệu Excel.
    }
}
