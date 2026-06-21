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
        private const int MaxImportBytes = 5 * 1024 * 1024;
        private const int MaxImportRows = 10000;
        private const int MaxCsvLineLength = 1024 * 1024;
        private const long MaxZipEntryBytes = 10 * 1024 * 1024;
        private const int MaxSharedStrings = 50000;
        private const int MaxZipExpansionRatio = 100;

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho nhập và xuất dữ liệu Excel.
        public IList<IDictionary<string, string>> ReadWorksheet(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                throw new InvalidOperationException("Vui lòng chọn file import.");
            }

            ValidateImportSize(file);

            var extension = Path.GetExtension(file.FileName);
            if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ReadCsv(file.InputStream);
            }

            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chỉ hỗ trợ file .xlsx hoặc .csv.");
            }

            return ReadXlsx(file.InputStream);
        }

        // Chuyển dữ liệu nguồn sang cấu trúc dùng cho nhập và xuất dữ liệu Excel.
        public IList<IDictionary<string, string>> ReadIndicatorDocxTables(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                throw new InvalidOperationException("Vui lòng chọn file import.");
            }

            ValidateImportSize(file);

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chỉ hỗ trợ file .docx cho import từ định nghĩa chỉ số.");
            }

            return ReadIndicatorDocxTables(file.InputStream);
        }

        // Tạo cấu trúc dữ liệu phục vụ nhập và xuất dữ liệu Excel.
        public byte[] CreateCsv<T>(IEnumerable<T> items, IList<KeyValuePair<string, Func<T, object>>> columns)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Join(",", columns.Select(c => Escape(c.Key))));

            foreach (var item in items)
            {
                builder.AppendLine(string.Join(",", columns.Select(c => Escape(NeutralizeCsvFormula(Convert.ToString(c.Value(item)))))));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        }

        // Tạo cấu trúc dữ liệu phục vụ nhập và xuất dữ liệu Excel.
        public byte[] CreateXlsx<T>(IEnumerable<T> items, IList<KeyValuePair<string, Func<T, object>>> columns)
        {
            return CreateXlsxWorkbook(new List<ExcelWorksheetExport>
            {
                ExcelWorksheetExport.From("Sheet1", items, columns)
            });
        }

        // Tạo cấu trúc dữ liệu phục vụ nhập và xuất dữ liệu Excel.
        public byte[] CreateXlsxWorkbook(IList<ExcelWorksheetExport> worksheets)
        {
            worksheets = NormalizeWorksheets(worksheets);

            using (var stream = new MemoryStream())
            {
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
                {
                    AddTextEntry(archive, "[Content_Types].xml", BuildContentTypesXml(worksheets.Count));
                    AddTextEntry(archive, "_rels/.rels", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>");
                    AddTextEntry(archive, "xl/workbook.xml", BuildWorkbookXml(worksheets));
                    AddTextEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationshipsXml(worksheets.Count));

                    for (var i = 0; i < worksheets.Count; i++)
                    {
                        WriteWorksheetEntry(archive, string.Format(CultureInfo.InvariantCulture, "xl/worksheets/sheet{0}.xml", i + 1), worksheets[i].Items, worksheets[i].Columns);
                    }
                }

                return stream.ToArray();
            }
        }

        // Chuẩn hóa dữ liệu đầu vào trước khi dùng cho nhập và xuất dữ liệu Excel.
    }
}
