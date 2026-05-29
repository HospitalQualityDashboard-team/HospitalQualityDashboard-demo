$ErrorActionPreference = 'Stop'

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$packageRoot = Resolve-Path (Join-Path $projectRoot '..\packages')

$references = @(
    'System.dll',
    'System.Core.dll',
    'System.Data.dll',
    'System.Configuration.dll',
    'System.ComponentModel.DataAnnotations.dll',
    'System.Web.dll',
    'System.Xml.dll',
    'System.Xml.Linq.dll',
    'System.IO.Compression.dll',
    'System.IO.Compression.FileSystem.dll',
    (Join-Path $packageRoot 'Microsoft.AspNet.Mvc.4.0.40804\lib\net40\System.Web.Mvc.dll')
)

$appAssembly = if ([string]::IsNullOrWhiteSpace($env:HQD_APP_ASSEMBLY)) {
    Join-Path $projectRoot 'bin\HospitalQualityDashboard.dll'
} else {
    $env:HQD_APP_ASSEMBLY
}
if (-not (Test-Path $appAssembly)) {
    throw "Build the project before running this verifier. Missing $appAssembly."
}

[void][System.Reflection.Assembly]::LoadFrom($appAssembly)

Add-Type -ReferencedAssemblies ($references + $appAssembly) -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;
using HospitalQualityDashboard.Models.Enums;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

public class IndicatorFormulaImportVerifier
{
    public static void Run()
    {
        VerifyDocxLabelAliases();
        VerifyFormulaInference();
        VerifyUnitInference();
    }

    private static void VerifyDocxLabelAliases()
    {
        var service = new ExcelImportExportService();
        using (var stream = NewMinimalDocxWithAliasLabels())
        {
            var rows = service.ReadIndicatorDocxTables(new MemoryPostedFile(stream, "indicator.docx"));
            if (rows.Count != 1)
            {
                throw new Exception("Expected one DOCX indicator row, got " + rows.Count + ".");
            }

            var row = rows[0];
            AssertValue(row, "LyDoLuaChon", "\u0110\u1ea3m b\u1ea3o ch\u1ea5t l\u01b0\u1ee3ng kh\u00e1m ch\u1eefa b\u1ec7nh");
            AssertValue(row, "ThuThapTongHop", "Ph\u00f2ng TCCB thu nh\u1eadp t\u1eeb Ph\u00f2ng KHTH v\u00e0 t\u1ed5ng h\u1ee3p s\u1ed1 li\u1ec7u n\u00e0y");
        }
    }

    private static void VerifyFormulaInference()
    {
        AssertFormula("T\u1ef7 s\u1ed1 B\u00e1c s\u0129/ gi\u01b0\u1eddng b\u1ec7nh c\u1ee7a to\u00e0n b\u1ec7nh vi\u1ec7n", "S\u1ed1 b\u00e1c s\u0129 trong b\u1ec7nh vi\u1ec7n", "S\u1ed1 gi\u01b0\u1eddng b\u1ec7nh th\u1ef1c k\u00ea", null, null, LoaiCongThuc.TySo);
        AssertFormula("S\u1ed1 l\u01b0\u1ee3ng b\u00e1c s\u0129 tham d\u1ef1 c\u00e1c bu\u1ed5i sinh ho\u1ea1t chuy\u00ean \u0111\u1ec1", null, null, null, null, LoaiCongThuc.SoLuong);
        AssertFormula("Th\u1eddi gian x\u1eed l\u00fd s\u1ef1 c\u1ed1 h\u1ec7 th\u1ed1ng m\u1ea1ng v\u00e0 m\u00e1y ch\u1ee7", null, null, null, null, LoaiCongThuc.ThoiGianTrungBinh);
        AssertFormula("\u0110i\u1ec3m trung b\u00ecnh h\u00e0i l\u00f2ng ng\u01b0\u1eddi b\u1ec7nh", null, null, null, null, LoaiCongThuc.DiemTrungBinh);
        AssertFormula("T\u1ef7 l\u1ec7 h\u1ed3 s\u01a1 BHYT chuy\u1ec3n c\u1ed5ng gi\u00e1m \u0111\u1ecbnh \u0111\u00fang th\u1eddi gian", "S\u1ed1 h\u1ed3 s\u01a1 chuy\u1ec3n \u0111\u00fang th\u1eddi gian", "T\u1ed5ng s\u1ed1 h\u1ed3 s\u01a1", null, null, LoaiCongThuc.TyLe);
        AssertFormula("Ch\u1ec9 s\u1ed1 c\u00f3 override", "T\u1eed s\u1ed1", "M\u1eabu s\u1ed1", null, "TySo", LoaiCongThuc.TySo);
        AssertFormula("8. T\u1ef7 l\u1ec7 \u0110i\u1ec1u d\u01b0\u1ee1ng tham gia \u0111\u00e0o t\u1ea1o li\u00ean t\u1ee5c.", null, null, null, null, LoaiCongThuc.TyLe);
        AssertFormula("S\u1ed1 l\u01b0\u1ee3ng c\u00e1c \u0111i\u1ec3m ti\u1ebfp n\u1ed1i v\u1eadt l\u00fd \u0111\u1ec3 v\u1eadn chuy\u1ec3n ng\u01b0\u1eddi b\u1ec7nh", null, null, null, null, LoaiCongThuc.SoLuong);
    }

    private static void VerifyUnitInference()
    {
        AssertUnit("T\u1ef7 l\u1ec7 h\u1ed3 s\u01a1 BHYT chuy\u1ec3n c\u1ed5ng gi\u00e1m \u0111\u1ecbnh \u0111\u00fang th\u1eddi gian", "S\u1ed1 h\u1ed3 s\u01a1 chuy\u1ec3n \u0111\u00fang th\u1eddi gian", "T\u1ed5ng s\u1ed1 h\u1ed3 s\u01a1", null, null, "%");
        AssertUnit("T\u1ef7 s\u1ed1 B\u00e1c s\u0129/ gi\u01b0\u1eddng b\u1ec7nh c\u1ee7a to\u00e0n b\u1ec7nh vi\u1ec7n", "S\u1ed1 b\u00e1c s\u0129 trong b\u1ec7nh vi\u1ec7n", "S\u1ed1 gi\u01b0\u1eddng b\u1ec7nh th\u1ef1c k\u00ea", null, null, "b\u00e1c s\u0129/gi\u01b0\u1eddng b\u1ec7nh");
        AssertUnit("T\u1ef7 s\u1ed1 \u0110i\u1ec1u d\u01b0\u1ee1ng/ gi\u01b0\u1eddng b\u1ec7nh c\u1ee7a to\u00e0n b\u1ec7nh vi\u1ec7n", "S\u1ed1 \u0110i\u1ec1u d\u01b0\u1ee1ng trong b\u1ec7nh vi\u1ec7n", "S\u1ed1 gi\u01b0\u1eddng b\u1ec7nh th\u1ef1c k\u00ea", null, null, "\u0111i\u1ec1u d\u01b0\u1ee1ng/gi\u01b0\u1eddng b\u1ec7nh");
        AssertUnit("T\u1ef7 s\u1ed1 B\u00e1c s\u0129/ \u0110i\u1ec1u d\u01b0\u1ee1ng c\u1ee7a to\u00e0n b\u1ec7nh vi\u1ec7n", "S\u1ed1 B\u00e1c s\u0129 c\u1ee7a b\u1ec7nh vi\u1ec7n", "S\u1ed1 \u0110i\u1ec1u d\u01b0\u1ee1ng c\u1ee7a b\u1ec7nh vi\u1ec7n", null, null, "b\u00e1c s\u0129/\u0111i\u1ec1u d\u01b0\u1ee1ng");
        AssertUnit("T\u1ef7 s\u1ed1 D\u01b0\u1ee3c s\u0129/ gi\u01b0\u1eddng b\u1ec7nh c\u1ee7a to\u00e0n b\u1ec7nh vi\u1ec7n", null, null, null, null, "d\u01b0\u1ee3c s\u0129/gi\u01b0\u1eddng b\u1ec7nh");
        AssertUnit("T\u1ef7 s\u1ed1 Nh\u00e2n vi\u00ean dinh d\u01b0\u1ee1ng/ gi\u01b0\u1eddng b\u1ec7nh c\u1ee7a to\u00e0n b\u1ec7nh vi\u1ec7n", null, null, null, null, "nh\u00e2n vi\u00ean dinh d\u01b0\u1ee1ng/gi\u01b0\u1eddng b\u1ec7nh");
        AssertUnit("T\u1ef7 s\u1ed1 B\u00e1c s\u0129 c\u00f3 ch\u1ee9ng ch\u1ec9 ph\u1eabu thu\u1eadt n\u1ed9i soi/ t\u1ed5ng s\u1ed1 ph\u1eabu thu\u1eadt vi\u00ean", null, null, null, null, "b\u00e1c s\u0129 c\u00f3 ch\u1ee9ng ch\u1ec9/ph\u1eabu thu\u1eadt vi\u00ean");
        AssertUnit("Th\u1eddi gian x\u1eed l\u00fd s\u1ef1 c\u1ed1 h\u1ec7 th\u1ed1ng m\u1ea1ng v\u00e0 m\u00e1y ch\u1ee7", null, null, null, null, "gi\u1edd");
        AssertUnit("Th\u1eddi gian kh\u00e1m b\u1ec7nh trung b\u00ecnh c\u1ee7a ng\u01b0\u1eddi b\u1ec7nh", null, null, null, null, "ph\u00fat");
        AssertUnit("Th\u1eddi gian n\u1eb1m vi\u1ec7n trung b\u00ecnh (t\u1ea5t c\u1ea3 c\u00e1c b\u1ec7nh)", null, null, null, null, "ng\u00e0y");
        AssertUnit("S\u1ed1 l\u01b0\u1ee3ng b\u00e1c s\u0129 tham d\u1ef1 c\u00e1c bu\u1ed5i sinh ho\u1ea1t chuy\u00ean \u0111\u1ec1", null, null, null, null, "ng\u01b0\u1eddi");
        AssertUnit("S\u1ed1 l\u01b0\u1ee3ng b\u00e1o c\u00e1o ph\u1ea3n \u1ee9ng c\u00f3 h\u1ea1i c\u1ee7a thu\u1ed1c g\u1eedi v\u1ec1 khoa D\u01b0\u1ee3c", null, null, null, null, "b\u00e1o c\u00e1o");
        AssertUnit("S\u1ed1 ca ph\u1eabu thu\u1eadt", null, null, null, null, "ca");
        AssertUnit("S\u1ed1 l\u01b0\u1ee3t kh\u00e1m b\u1ec7nh", null, null, null, null, "l\u01b0\u1ee3t");
        AssertUnit("S\u1ed1 l\u01b0\u1ee3ng bu\u1ed3ng v\u1ec7 sinh ng\u01b0\u1eddi b\u1ec7nh c\u00f3 trang thi\u1ebft b\u1ecb \u0111\u1ea7y \u0111\u1ee7 v\u00e0 s\u1ea1ch s\u1ebd", null, null, null, null, "bu\u1ed3ng");
        AssertUnit("S\u1ed1 l\u01b0\u1ee3ng c\u00e1c \u0111i\u1ec3m ti\u1ebfp n\u1ed1i v\u1eadt l\u00fd \u0111\u1ec3 v\u1eadn chuy\u1ec3n ng\u01b0\u1eddi b\u1ec7nh", null, null, null, null, "\u0111i\u1ec3m ti\u1ebfp n\u1ed1i");
        AssertUnit("S\u1ed1 l\u01b0\u1ee3ng c\u1ea7u thang b\u1ed9 \u0111\u01b0\u1ee3c d\u00e1n d\u1ea5u c\u1ea3n quang", null, null, null, null, "c\u1ea7u thang");
        AssertUnit("Vi t\u00ednh h\u00f3a qu\u1ea3n l\u00fd trang thi\u1ebft b\u1ecb y t\u1ebf kh\u1ed1i n\u1ed9i", null, null, null, null, "m\u1ee9c \u0111\u1ed9");
        AssertUnit("Ch\u1ec9 s\u1ed1 c\u00f3 \u0111\u01a1n v\u1ecb t\u00ednh s\u1eb5n", null, null, null, "ng\u00e0y c\u00f4ng", "ng\u00e0y c\u00f4ng");
    }

    private static void AssertFormula(string name, string numerator, string denominator, string method, string explicitFormula, LoaiCongThuc expected)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        row["TenChiSo"] = name;
        row["TanSuatBaoCao"] = "H\u00e0ng th\u00e1ng";
        if (numerator != null) row["TuSoMoTa"] = numerator;
        if (denominator != null) row["MauSoMoTa"] = denominator;
        if (method != null) row["PhuongPhapTinh"] = method;
        if (explicitFormula != null) row["LoaiCongThuc"] = explicitFormula;

        var model = BuildIndicator(row);
        if (model.LoaiCongThuc != expected)
        {
            throw new Exception("Expected '" + name + "' to infer " + expected + ", got " + model.LoaiCongThuc + ".");
        }
    }

    private static void AssertUnit(string name, string numerator, string denominator, string method, string explicitUnit, string expected)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        row["TenChiSo"] = name;
        row["TanSuatBaoCao"] = "H\u00e0ng th\u00e1ng";
        if (numerator != null) row["TuSoMoTa"] = numerator;
        if (denominator != null) row["MauSoMoTa"] = denominator;
        if (method != null) row["PhuongPhapTinh"] = method;
        if (explicitUnit != null) row["DonViTinh"] = explicitUnit;

        var model = BuildIndicator(row);
        if (model.DonViTinh != expected)
        {
            throw new Exception("Expected '" + name + "' to infer unit '" + expected + "', got '" + model.DonViTinh + "'.");
        }
    }

    private static ChiSoViewModel BuildIndicator(IDictionary<string, string> row)
    {
        var serviceType = typeof(IndicatorService);
        var method = serviceType.GetMethod("BuildIndicatorFromRow", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new Exception("Could not find BuildIndicatorFromRow.");
        }

        var service = FormatterServices.GetUninitializedObject(serviceType);
        return (ChiSoViewModel)method.Invoke(service, new object[] { row });
    }

    private static void AssertValue(IDictionary<string, string> row, string key, string expected)
    {
        string actual;
        if (!row.TryGetValue(key, out actual))
        {
            throw new Exception("Expected DOCX parser to include key " + key + ".");
        }

        if (actual != expected)
        {
            throw new Exception("Expected " + key + " to be '" + expected + "', got '" + actual + "'.");
        }
    }

    private static MemoryStream NewMinimalDocxWithAliasLabels()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddZipEntry(archive, "word/document.xml", @"<?xml version=""1.0"" encoding=""UTF-8""?>
<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main"">
  <w:body>
    <w:tbl>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Ch&#7881; s&#7889;</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>T&#7927; s&#7889; B&#225;c s&#297;/ gi&#432;&#7901;ng b&#7879;nh c&#7911;a to&#224;n b&#7879;nh vi&#7879;n</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>L&#253; do ch&#7885;n l&#7921;a</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>&#272;&#7843;m b&#7843;o ch&#7845;t l&#432;&#7907;ng kh&#225;m ch&#7919;a b&#7879;nh</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Thu nh&#7853;p v&#224; t&#7893;ng h&#7907;p s&#7889; li&#7879;u</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Ph&#242;ng TCCB thu nh&#7853;p t&#7915; Ph&#242;ng KHTH v&#224; t&#7893;ng h&#7907;p s&#7889; li&#7879;u n&#224;y</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>T&#7847;n su&#7845;t b&#225;o c&#225;o</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>H&#7857;ng n&#259;m</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>
  </w:body>
</w:document>");
        }

        stream.Position = 0;
        return stream;
    }

    private static void AddZipEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using (var writer = new StreamWriter(entry.Open()))
        {
            writer.Write(content);
        }
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

[IndicatorFormulaImportVerifier]::Run()
Write-Host 'Indicator formula import verification passed.'
