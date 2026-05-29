$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$servicePath = Join-Path $projectRoot 'Services\ExcelImportExportService.cs'

$references = @(
    'System.dll',
    'System.Core.dll',
    'System.Web.dll',
    'System.Xml.dll',
    'System.Xml.Linq.dll',
    'System.IO.Compression.dll',
    'System.IO.Compression.FileSystem.dll'
)

$tempAssembly = Join-Path ([System.IO.Path]::GetTempPath()) ('HospitalQualityDashboard.ExcelImportExportService.' + [System.Guid]::NewGuid().ToString('N') + '.dll')
Add-Type -ReferencedAssemblies $references -Path $servicePath -OutputAssembly $tempAssembly
[void][System.Reflection.Assembly]::LoadFrom($tempAssembly)
$harnessReferences = $references + $tempAssembly

Add-Type -ReferencedAssemblies $harnessReferences -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using HospitalQualityDashboard.Services;

public class AssignmentExcelExportVerifier
{
    public static void Run()
    {
        var service = new ExcelImportExportService();
        var rows = new List<TestAssignmentExportRow>
        {
            new TestAssignmentExportRow
            {
                TenChiSo = "T\u1ef7 l\u1ec7 h\u00e0i l\u00f2ng ng\u01b0\u1eddi b\u1ec7nh",
                TanSuatBaoCao = "H\u00e0ng th\u00e1ng",
                KhoaPhongDuocPhanCong = "Khoa Kh\u00e1m b\u1ec7nh"
            }
        };

        var columns = new List<KeyValuePair<string, Func<TestAssignmentExportRow, object>>>
        {
            new KeyValuePair<string, Func<TestAssignmentExportRow, object>>("T\u00ean Ch\u1ec9 s\u1ed1", x => x.TenChiSo),
            new KeyValuePair<string, Func<TestAssignmentExportRow, object>>("T\u1ea7n su\u1ea5t b\u00e1o c\u00e1o", x => x.TanSuatBaoCao),
            new KeyValuePair<string, Func<TestAssignmentExportRow, object>>("Khoa/Ph\u00f2ng \u0111\u01b0\u1ee3c ph\u00e2n c\u00f4ng", x => x.KhoaPhongDuocPhanCong)
        };

        var bytes = service.CreateXlsx(rows, columns);
        if (bytes == null || bytes.Length == 0)
        {
            throw new Exception("Expected non-empty XLSX bytes.");
        }

        using (var stream = new MemoryStream(bytes))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            AssertEntry(archive, "[Content_Types].xml");
            AssertEntry(archive, "_rels/.rels");
            AssertEntry(archive, "xl/workbook.xml");
            AssertEntry(archive, "xl/_rels/workbook.xml.rels");
            var sheetEntry = AssertEntry(archive, "xl/worksheets/sheet1.xml");

            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XDocument sheet;
            using (var sheetStream = sheetEntry.Open())
            {
                sheet = XDocument.Load(sheetStream);
            }

            var texts = sheet.Descendants(ns + "t").Select(x => x.Value).ToList();
            AssertContains(texts, "T\u00ean Ch\u1ec9 s\u1ed1");
            AssertContains(texts, "T\u1ea7n su\u1ea5t b\u00e1o c\u00e1o");
            AssertContains(texts, "Khoa/Ph\u00f2ng \u0111\u01b0\u1ee3c ph\u00e2n c\u00f4ng");
            AssertContains(texts, "T\u1ef7 l\u1ec7 h\u00e0i l\u00f2ng ng\u01b0\u1eddi b\u1ec7nh");
            AssertContains(texts, "H\u00e0ng th\u00e1ng");
            AssertContains(texts, "Khoa Kh\u00e1m b\u1ec7nh");

            if (texts.Contains("T\u1eed s\u1ed1"))
            {
                throw new Exception("Workbook should not contain an unselected numerator column.");
            }
        }

        var postedFile = new MemoryPostedFile(new MemoryStream(bytes), "phan-cong-chi-so.xlsx");
        var parsedRows = service.ReadWorksheet(postedFile);
        if (parsedRows.Count != 1)
        {
            throw new Exception("Expected one parsed data row, got " + parsedRows.Count + ".");
        }

        var parsed = parsedRows[0];
        if (parsed.Count != 3)
        {
            throw new Exception("Expected three selected columns, got " + parsed.Count + ".");
        }

        if (parsed["T\u00ean Ch\u1ec9 s\u1ed1"] != "T\u1ef7 l\u1ec7 h\u00e0i l\u00f2ng ng\u01b0\u1eddi b\u1ec7nh")
        {
            throw new Exception("Parsed indicator name did not round-trip.");
        }
    }

    private static ZipArchiveEntry AssertEntry(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        if (entry == null)
        {
            throw new Exception("Expected XLSX entry " + name + ".");
        }

        return entry;
    }

    private static void AssertContains(IList<string> values, string expected)
    {
        if (!values.Contains(expected))
        {
            throw new Exception("Expected workbook text '" + expected + "'.");
        }
    }

    private class TestAssignmentExportRow
    {
        public string TenChiSo { get; set; }
        public string TanSuatBaoCao { get; set; }
        public string KhoaPhongDuocPhanCong { get; set; }
    }

    private class MemoryPostedFile : HttpPostedFileBase
    {
        private readonly Stream inputStream;
        private readonly string fileName;

        public MemoryPostedFile(Stream inputStream, string fileName)
        {
            this.inputStream = inputStream;
            this.fileName = fileName;
        }

        public override int ContentLength { get { return (int)inputStream.Length; } }
        public override string FileName { get { return fileName; } }
        public override Stream InputStream { get { return inputStream; } }
    }
}
'@

[AssignmentExcelExportVerifier]::Run()
Write-Host 'Assignment Excel export verification passed.'
