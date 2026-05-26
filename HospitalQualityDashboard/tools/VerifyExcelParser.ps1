$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$servicePath = Join-Path $projectRoot 'Services\ExcelImportExportService.cs'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$references = @(
    'System.dll',
    'System.Core.dll',
    'System.Web.dll',
    'System.Xml.dll',
    'System.Xml.Linq.dll',
    'System.IO.Compression.dll',
    'System.IO.Compression.FileSystem.dll'
)

Add-Type -ReferencedAssemblies $references -Path $servicePath

Add-Type -ReferencedAssemblies @('System.dll', 'System.Web.dll') -TypeDefinition @'
using System.IO;
using System.Web;

public class MemoryPostedFile : HttpPostedFileBase
{
    private readonly Stream inputStream;
    private readonly string fileName;
    private readonly int contentLength;

    public MemoryPostedFile(Stream inputStream, string fileName)
    {
        this.inputStream = inputStream;
        this.fileName = fileName;
        this.contentLength = (int)inputStream.Length;
    }

    public override int ContentLength { get { return contentLength; } }
    public override string FileName { get { return fileName; } }
    public override Stream InputStream { get { return inputStream; } }
}
'@

function Add-ZipEntry {
    param(
        [System.IO.Compression.ZipArchive] $Archive,
        [string] $Name,
        [string] $Content
    )

    $entry = $Archive.CreateEntry($Name)
    $writer = New-Object System.IO.StreamWriter($entry.Open(), [System.Text.Encoding]::UTF8)
    try {
        $writer.Write($Content)
    }
    finally {
        $writer.Dispose()
    }
}

function New-MinimalWorkbook {
    $stream = New-Object System.IO.MemoryStream
    $archive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        Add-ZipEntry $archive 'xl/worksheets/sheet1.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
    <row r="1">
      <c r="A1" t="inlineStr"><is><t>ID</t></is></c>
      <c r="C1" t="inlineStr"><is><t>TENKHOAPHONG</t></is></c>
    </row>
    <row r="2">
      <c r="A2"><v>1</v></c>
      <c r="C2" t="inlineStr"><is><t>Phong Tai chinh</t></is></c>
    </row>
  </sheetData>
</worksheet>
'@
    }
    finally {
        $archive.Dispose()
    }

    $stream.Position = 0
    return $stream
}

function New-MinimalDocx {
    $stream = New-Object System.IO.MemoryStream
    $archive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        Add-ZipEntry $archive 'word/document.xml' @'
<?xml version="1.0" encoding="UTF-8"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    <w:tbl>
      <w:tr>
        <w:tc><w:p><w:r><w:t>T&#234;n ch&#7881; s&#7889;</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Ti le thanh toan vien phi truc tuyen</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>&#272;&#7883;nh ngh&#297;a ch&#7881; s&#7889;</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Nguoi benh thanh toan truc tuyen qua QR code</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Ph&#432;&#417;ng ph&#225;p t&#237;nh</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t></w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>T&#7917; s&#7889;</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>So luot thanh toan bang QR code</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>M&#7851;u s&#7889;</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Tong so luot thanh toan vien phi</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Thu th&#7853;p v&#224; t&#7893;ng h&#7907;p s&#7889; li&#7879;u</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Phong Tai chinh ke toan thu thap va tong hop so lieu</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>T&#7847;n su&#7845;t b&#225;o c&#225;o</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>3 thang, 6 thang, 9 thang, 12 thang</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>M&#7909;c ti&#234;u &#273;&#7841;t &#273;&#432;&#7907;c trong n&#259;m 2020</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>100%</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>
  </w:body>
</w:document>
'@
    }
    finally {
        $archive.Dispose()
    }

    $stream.Position = 0
    return $stream
}

$workbook = New-MinimalWorkbook
$file = New-Object MemoryPostedFile($workbook, 'test.xlsx')
$service = New-Object HospitalQualityDashboard.Services.ExcelImportExportService
$rows = $service.ReadWorksheet($file)

if ($rows.Count -ne 1) {
    throw "Expected 1 data row, got $($rows.Count)."
}

$row = $rows[0]
if (-not $row.ContainsKey('TENKHOAPHONG')) {
    throw 'Expected row to contain TENKHOAPHONG header.'
}

if ($row['TENKHOAPHONG'] -ne 'Phong Tai chinh') {
    throw "Expected TENKHOAPHONG from column C, got '$($row['TENKHOAPHONG'])'."
}

$departmentFilePath = Join-Path $projectRoot 'Tai_Lieu\DM_KHOA_PHONG.xlsx'
if (Test-Path $departmentFilePath) {
    $departmentStream = [System.IO.File]::OpenRead($departmentFilePath)
    try {
        $departmentFile = New-Object MemoryPostedFile($departmentStream, 'DM_KHOA_PHONG.xlsx')
        $departmentRows = $service.ReadWorksheet($departmentFile)
        if ($departmentRows.Count -ne 44) {
            throw "Expected 44 department rows, got $($departmentRows.Count)."
        }

        if (-not $departmentRows[0].ContainsKey('IDKHOAPHONG') -or -not $departmentRows[0].ContainsKey('TENKHOAPHONG')) {
            throw 'Expected DM_KHOA_PHONG rows to contain IDKHOAPHONG and TENKHOAPHONG.'
        }

        if ([string]::IsNullOrWhiteSpace($departmentRows[0]['TENKHOAPHONG'])) {
            throw 'Expected first DM_KHOA_PHONG row to contain a department name.'
        }
    }
    finally {
        $departmentStream.Dispose()
    }
}

$docx = New-MinimalDocx
$docxFile = New-Object MemoryPostedFile($docx, 'indicator.docx')
$docxRows = $service.ReadIndicatorDocxTables($docxFile)
if ($docxRows.Count -ne 1) {
    throw "Expected 1 DOCX indicator row, got $($docxRows.Count)."
}

$docxRow = $docxRows[0]
if ($docxRow['TenChiSo'] -ne 'Ti le thanh toan vien phi truc tuyen') {
    throw "Expected DOCX TenChiSo to be parsed, got '$($docxRow['TenChiSo'])'."
}

if ($docxRow['TuSoMoTa'] -ne 'So luot thanh toan bang QR code') {
    throw "Expected DOCX TuSoMoTa to be parsed, got '$($docxRow['TuSoMoTa'])'."
}

if ($docxRow['TanSuatBaoCao'] -ne '3 thang, 6 thang, 9 thang, 12 thang') {
    throw "Expected DOCX TanSuatBaoCao to be parsed, got '$($docxRow['TanSuatBaoCao'])'."
}

if ($docxRow['NamMucTieu'] -ne '2020' -or $docxRow['MucTieuDatDuoc'] -ne '100%') {
    throw 'Expected DOCX target year and target text to be parsed.'
}

$definitionDocx = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Tai_Lieu') -Filter '*.docx' |
    Where-Object { $_.Name -like '*55*' -and $_.Name -notlike '~$*' } |
    Select-Object -First 1
if ($definitionDocx) {
    $definitionStream = [System.IO.File]::OpenRead($definitionDocx.FullName)
    try {
        $definitionFile = New-Object MemoryPostedFile($definitionStream, $definitionDocx.Name)
        $definitionRows = $service.ReadIndicatorDocxTables($definitionFile)
        if ($definitionRows.Count -lt 50) {
            throw "Expected at least 50 indicators from definition DOCX, got $($definitionRows.Count)."
        }

        if ([string]::IsNullOrWhiteSpace($definitionRows[0]['TenChiSo'])) {
            throw 'Expected first definition DOCX row to contain TenChiSo.'
        }
    }
    finally {
        $definitionStream.Dispose()
    }
}

Write-Host 'Import parser verification passed.'
