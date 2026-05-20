using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;
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

                var headers = rawRows[0].OrderBy(k => ColumnIndex(k.Key)).Select(k => k.Value).ToList();
                foreach (var rawRow in rawRows.Skip(1))
                {
                    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < headers.Count; i++)
                    {
                        var columnName = ColumnName(i + 1);
                        row[headers[i]] = rawRow.ContainsKey(columnName) ? rawRow[columnName] : string.Empty;
                    }

                    if (row.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                    {
                        rows.Add(row);
                    }
                }
            }

            return rows;
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
            var valueElement = cell.Element(ns + "v");
            if (valueElement == null)
            {
                return string.Empty;
            }

            var value = valueElement.Value;
            var type = Convert.ToString(cell.Attribute("t"));
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

        private static string ColumnName(int index)
        {
            var name = string.Empty;
            while (index > 0)
            {
                var modulo = (index - 1) % 26;
                name = Convert.ToChar('A' + modulo) + name;
                index = (index - modulo) / 26;
            }

            return name;
        }
    }
}
