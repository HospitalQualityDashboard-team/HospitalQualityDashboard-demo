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
    public class ExcelWorksheetExport
    {
        public string Name { get; set; }
        public IEnumerable<object> Items { get; set; }
        public IList<KeyValuePair<string, Func<object, object>>> Columns { get; set; }

        // Tạo mô tả worksheet từ tập dữ liệu và danh sách cột được cung cấp.
        public static ExcelWorksheetExport From<T>(string name, IEnumerable<T> items, IList<KeyValuePair<string, Func<T, object>>> columns)
        {
            return new ExcelWorksheetExport
            {
                Name = name,
                Items = (items ?? Enumerable.Empty<T>()).Cast<object>(),
                Columns = (columns ?? new List<KeyValuePair<string, Func<T, object>>>())
                    .Select(column => new KeyValuePair<string, Func<object, object>>(column.Key, item => column.Value((T)item)))
                    .ToList()
            };
        }
    }
}
