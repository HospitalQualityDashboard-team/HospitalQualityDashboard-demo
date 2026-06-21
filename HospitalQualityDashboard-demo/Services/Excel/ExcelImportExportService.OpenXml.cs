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
        private static IList<ExcelWorksheetExport> NormalizeWorksheets(IList<ExcelWorksheetExport> worksheets)
        {
            if (worksheets == null || worksheets.Count == 0)
            {
                return new List<ExcelWorksheetExport>
                {
                    ExcelWorksheetExport.From("Sheet1", Enumerable.Empty<object>(), new List<KeyValuePair<string, Func<object, object>>>())
                };
            }

            for (var i = 0; i < worksheets.Count; i++)
            {
                if (worksheets[i] == null)
                {
                    worksheets[i] = ExcelWorksheetExport.From("Sheet" + (i + 1).ToString(CultureInfo.InvariantCulture), Enumerable.Empty<object>(), new List<KeyValuePair<string, Func<object, object>>>());
                    continue;
                }

                worksheets[i].Name = SanitizeWorksheetName(worksheets[i].Name, i + 1);
                worksheets[i].Items = worksheets[i].Items ?? Enumerable.Empty<object>();
                worksheets[i].Columns = worksheets[i].Columns ?? new List<KeyValuePair<string, Func<object, object>>>();
            }

            return worksheets;
        }

        // Tạo cấu trúc dữ liệu phục vụ nhập và xuất dữ liệu Excel.
        private static string BuildContentTypesXml(int sheetCount)
        {
            var builder = new StringBuilder();
            builder.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>");
            for (var i = 1; i <= sheetCount; i++)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, @"
  <Override PartName=""/xl/worksheets/sheet{0}.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>", i);
            }

            builder.Append(@"
</Types>");
            return builder.ToString();
        }

        // Tạo cấu trúc dữ liệu phục vụ nhập và xuất dữ liệu Excel.
        private static string BuildWorkbookXml(IList<ExcelWorksheetExport> worksheets)
        {
            var builder = new StringBuilder();
            builder.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>");
            for (var i = 0; i < worksheets.Count; i++)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, @"
    <sheet name=""{0}"" sheetId=""{1}"" r:id=""rId{1}""/>", EscapeXmlAttribute(worksheets[i].Name), i + 1);
            }

            builder.Append(@"
  </sheets>
</workbook>");
            return builder.ToString();
        }

        // Tạo cấu trúc dữ liệu phục vụ nhập và xuất dữ liệu Excel.
        private static string BuildWorkbookRelationshipsXml(int sheetCount)
        {
            var builder = new StringBuilder();
            builder.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">");
            for (var i = 1; i <= sheetCount; i++)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, @"
  <Relationship Id=""rId{0}"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet{0}.xml""/>", i);
            }

            builder.Append(@"
</Relationships>");
            return builder.ToString();
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho nhập và xuất dữ liệu Excel.
        private static string SanitizeWorksheetName(string name, int fallbackIndex)
        {
            var value = string.IsNullOrWhiteSpace(name) ? "Sheet" + fallbackIndex.ToString(CultureInfo.InvariantCulture) : name.Trim();
            var invalidChars = new[] { '[', ']', ':', '*', '?', '/', '\\' };
            foreach (var ch in invalidChars)
            {
                value = value.Replace(ch, ' ');
            }

            value = Regex.Replace(value, @"\s+", " ").Trim(' ', '\'');
            if (string.IsNullOrWhiteSpace(value))
            {
                value = "Sheet" + fallbackIndex.ToString(CultureInfo.InvariantCulture);
            }

            return value.Length > 31 ? value.Substring(0, 31) : value;
        }

        // Thoát và bao giá trị để tạo đầu ra an toàn, đúng định dạng.
        private static string EscapeXmlAttribute(string value)
        {
            return SecurityElement.Escape(SanitizeXmlText(value)) ?? string.Empty;
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho nhập và xuất dữ liệu Excel.
        private static void AddTextEntry(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        // Ghi dữ liệu đã chuẩn hóa vào đầu ra của nhập và xuất dữ liệu Excel.
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

        // Ghi dữ liệu đã chuẩn hóa vào đầu ra của nhập và xuất dữ liệu Excel.
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

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho nhập và xuất dữ liệu Excel.
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

        // Truy vấn nhập và xuất dữ liệu Excel theo điều kiện được cung cấp.
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

        // Truy vấn nhập và xuất dữ liệu Excel theo điều kiện được cung cấp.
        private static string GetColumnName(string cellReference)
        {
            return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
        }

        // Chuyển tên cột Excel thành chỉ số cột tương ứng.
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
