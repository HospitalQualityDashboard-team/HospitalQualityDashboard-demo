// Mục đích: đọc Excel/CSV import và tạo file export theo định dạng an toàn.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace HospitalQualityDashboard.Services
{
    public class ExcelImportExportService
    {
        public IList<IDictionary<string, string>> ReadWorksheet(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                throw new InvalidOperationException("Vui long chon file import.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ReadCsv(file.InputStream);
            }

            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chi ho tro file .xlsx hoac .csv.");
            }

            return ReadXlsx(file.InputStream);
        }

        public IList<IDictionary<string, string>> ReadIndicatorDocxTables(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                throw new InvalidOperationException("Vui long chon file import.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chi ho tro file .docx cho import tu dinh nghia chi so.");
            }

            return ReadIndicatorDocxTables(file.InputStream);
        }

        public byte[] CreateCsv<T>(IEnumerable<T> items, IList<KeyValuePair<string, Func<T, object>>> columns)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", columns.Select(c => Escape(c.Key))));

            foreach (var item in items)
            {
                builder.AppendLine(string.Join(",", columns.Select(c => Escape(Convert.ToString(c.Value(item))))));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        }

        public byte[] CreateXlsx<T>(IEnumerable<T> items, IList<KeyValuePair<string, Func<T, object>>> columns)
        {
            items = items ?? Enumerable.Empty<T>();
            columns = columns ?? new List<KeyValuePair<string, Func<T, object>>>();

            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    AddTextEntry(archive, "[Content_Types].xml", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
</Types>");

                    AddTextEntry(archive, "_rels/.rels", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");

                    AddTextEntry(archive, "xl/workbook.xml", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""Sheet1"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>");

                    AddTextEntry(archive, "xl/_rels/workbook.xml.rels", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>
</Relationships>");

                    WriteWorksheetEntry(archive, "xl/worksheets/sheet1.xml", items, columns);
                }

                return stream.ToArray();
            }
        }

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

                var headers = SplitCsv(headerLine).ToArray();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
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
                }
            }

            return rows;
        }

        private static IList<IDictionary<string, string>> ReadXlsx(Stream stream)
        {
            var rows = new List<IDictionary<string, string>>();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                var sharedStrings = ReadSharedStrings(archive);
                var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
                if (sheet == null)
                {
                    throw new InvalidOperationException("Khong tim thay sheet dau tien trong file Excel.");
                }

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
                    .ToList();

                if (rawRows.Count == 0)
                {
                    return rows;
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

        private static IList<IDictionary<string, string>> ReadIndicatorDocxTables(Stream stream)
        {
            var rows = new List<IDictionary<string, string>>();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                var documentEntry = archive.GetEntry("word/document.xml");
                if (documentEntry == null)
                {
                    throw new InvalidOperationException("Khong tim thay noi dung document.xml trong file Word.");
                }

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
                    }
                }
            }

            return rows;
        }

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

        private static bool IsDetailLabel(string label)
        {
            var normalized = NormalizeLabel(label);
            return normalized == "tu so" || normalized == "mau so";
        }

        private static bool IsMethodOrBlank(string label)
        {
            var normalized = NormalizeLabel(label);
            return string.IsNullOrWhiteSpace(normalized) || normalized == "phuong phap tinh";
        }

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

        private static int? FindYear(string value)
        {
            var match = Regex.Match(value ?? string.Empty, @"(19|20)\d{2}");
            int year;
            return match.Success && int.TryParse(match.Value, out year) ? year : (int?)null;
        }

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
                return XDocument.Load(stream).Descendants(ns + "si")
                    .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
                    .ToList();
            }
        }

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

        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static void AddTextEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static void WriteWorksheetEntry<T>(
            ZipArchive archive,
            string name,
            IEnumerable<T> items,
            IList<KeyValuePair<string, Func<T, object>>> columns)
        {
            const string spreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false,
                Indent = false
            };

            var entry = archive.CreateEntry(name);
            using (var entryStream = entry.Open())
            using (var writer = XmlWriter.Create(entryStream, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("worksheet", spreadsheetNamespace);
                writer.WriteStartElement("sheetData", spreadsheetNamespace);

                var rowNumber = 1;
                writer.WriteStartElement("row", spreadsheetNamespace);
                writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
                for (var i = 0; i < columns.Count; i++)
                {
                    WriteInlineStringCell(writer, rowNumber, i + 1, columns[i].Key);
                }
                writer.WriteEndElement();

                foreach (var item in items)
                {
                    rowNumber++;
                    writer.WriteStartElement("row", spreadsheetNamespace);
                    writer.WriteAttributeString("r", rowNumber.ToString(CultureInfo.InvariantCulture));
                    for (var i = 0; i < columns.Count; i++)
                    {
                        WriteInlineStringCell(writer, rowNumber, i + 1, Convert.ToString(columns[i].Value(item)));
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private static void WriteInlineStringCell(XmlWriter writer, int rowNumber, int columnNumber, string value)
        {
            const string spreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            writer.WriteStartElement("c", spreadsheetNamespace);
            writer.WriteAttributeString("r", GetExcelColumnName(columnNumber) + rowNumber.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("t", "inlineStr");
            writer.WriteStartElement("is", spreadsheetNamespace);
            writer.WriteStartElement("t", spreadsheetNamespace);
            writer.WriteString(SanitizeXmlText(value));
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static string SanitizeXmlText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                builder.Append(XmlConvert.IsXmlChar(ch) ? ch : ' ');
            }

            return builder.ToString();
        }

        private static string GetExcelColumnName(int columnNumber)
        {
            var builder = new StringBuilder();
            while (columnNumber > 0)
            {
                columnNumber--;
                builder.Insert(0, (char)('A' + (columnNumber % 26)));
                columnNumber /= 26;
            }

            return builder.ToString();
        }

        private static string GetColumnName(string cellReference)
        {
            return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        }

        private static int ColumnIndex(string columnName)
        {
            var index = 0;
            foreach (var ch in columnName.ToUpperInvariant())
            {
                index = index * 26 + (ch - 'A' + 1);
            }

            return index;
        }

    }
}
