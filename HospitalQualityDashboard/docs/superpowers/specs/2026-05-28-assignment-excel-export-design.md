# Assignment Excel Export Design

## Context

The Assignment page lets admins review and manage indicator-to-department assignments. Mentors need an Excel file to confirm whether the system assigned indicators to the correct departments.

Current exports go through `ExportController`, `ExportService`, and `ExcelImportExportService`, but they produce CSV files. The project already reads `.xlsx` files with `ZipArchive`, so the Excel export should use the same low-dependency style instead of adding a new package.

## User Experience

Add an `Xuat Excel` action near the current assignment list on `Views/Assignment/Index.cshtml`.

When clicked, the page opens a column-selection modal. The modal shows checkboxes for these Vietnamese columns, all selected by default:

- `Ten Chi so`
- `Tan suat bao cao`
- `Phuong phap tinh`
- `Tu so`
- `Mau so`
- `Thu thap va tong hop so lieu`
- `Khoa/Phong duoc phan cong`

The downloaded workbook must display the column headers in Vietnamese with accents:

- `Tên Chỉ số`
- `Tần suất báo cáo`
- `Phương pháp tính`
- `Tử số`
- `Mẫu số`
- `Thu thập và tổng hợp số liệu`
- `Khoa/Phòng được phân công`

If the admin selects only some columns, the workbook contains only those columns in the fixed display order above.

## Data Scope

The export uses the filters currently applied on the Assignment page:

- `khoaPhongId`
- `chiSoId`
- `trangThai`
- `trangThaiPhanCong`
- `search`
- `viewMode`

The export is not limited to the current page size. It exports every matching assigned row.

Each row represents one assignment between one quality indicator and one department. If an indicator is assigned to multiple departments, it appears on multiple rows, one row per assigned department.

The export only includes assignments that exist in `PhanCongChiSo`. It does not export unassigned indicators, even when the current page is in "Theo chi so" view and can display unassigned indicator groups.

## Server Design

Add an Assignment export endpoint that requires admin access.

The endpoint accepts selected column keys and the current Assignment filters. It validates the selected columns against a server-side allowlist. If no valid columns are selected, it falls back to the default full column set.

Add an assignment export service method that queries assignment rows joined with indicator and department data:

- indicator name
- report frequency
- calculation method
- numerator description
- denominator description
- collection and aggregation department/source text
- assigned department name

Use existing formatter methods for report frequency text where possible.

Add a reusable `.xlsx` creation method to `ExcelImportExportService`. The method should create a minimal OpenXML workbook with one worksheet, shared strings or inline strings, escaped XML content, and UTF-8 text support.

## Error Handling

Invalid or unknown column keys are ignored.

If the filtered result is empty, still return a valid workbook with the selected header row and no data rows.

The action returns an Excel MIME type and a Vietnamese filename such as `phan-cong-chi-so.xlsx`.

## Verification

Verification should cover:

- The project builds.
- The export endpoint returns an `.xlsx` file.
- The workbook contains only the selected columns.
- Headers are Vietnamese with accents.
- Filters are preserved from the Assignment page.
- Empty result exports still open as a valid workbook.
