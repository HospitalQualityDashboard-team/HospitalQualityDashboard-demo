# Thiết kế Việt hóa giao diện và tài liệu chính

## Mục tiêu

Chuẩn hóa toàn bộ nội dung người dùng nhìn thấy sang tiếng Việt có dấu, đồng thời cập nhật các tài liệu dự án chính bằng tiếng Việt có dấu rõ ràng. Không thay đổi business logic, route, tên class/method, schema SQL hoặc identifier kỹ thuật.

## Phạm vi

### Có sửa

- Text hiển thị trong `.cshtml`: tiêu đề, label, nút, placeholder, bảng, alert, badge, empty state.
- Text người dùng có thể thấy trong `.cs`: `TempData`, `ModelState.AddModelError`, `HttpStatusCodeResult`, thông báo lỗi/thành công.
- Tài liệu chính:
  - `README.md`
  - `HospitalQualityDashboard/PROJECT_CONTEXT.md`
  - `HospitalQualityDashboard/TAI_LIEU_NGHIEP_VU.md`
  - `HospitalQualityDashboard/implementation-notes.md`
  - `HospitalQualityDashboard/AGENTS.md` nếu có nội dung hướng dẫn dự án.

### Không sửa

- `packages/`, `bin/`, `obj/`.
- Identifier kỹ thuật: namespace, class, method, enum name, route action, table/column SQL.
- Tài liệu lịch sử trong `docs/superpowers/` và `HospitalQualityDashboard/docs/superpowers/`, trừ plan/spec mới của công việc này.
- Dữ liệu mẫu kỹ thuật hoặc tên file nếu việc dịch có thể làm hỏng import/export.

## Phương pháp

Dùng phương pháp regex-driven + review thủ công:

1. Scan các cụm tiếng Anh/không dấu phổ biến trong `.cshtml`, `.cs`, `.md` chính.
2. Sửa theo nhóm để dễ kiểm soát:
   - Layout, login, profile, home.
   - Admin views.
   - User views.
   - Controller/service messages.
   - Tài liệu chính.
3. Không dịch máy hàng loạt trên toàn repo; mỗi cụm được sửa theo ngữ cảnh.

## Quy tắc ngôn ngữ

- Dùng tiếng Việt có dấu tự nhiên, nhất quán.
- Dịch thuật ngữ giao diện thường gặp:
  - `Dashboard` → `Tổng quan`
  - `Report` → `Báo cáo`
  - `Notification` → `Thông báo`
  - `Profile` → `Hồ sơ`
  - `Logout` → `Đăng xuất`
  - `Unauthorized` → `Không có quyền truy cập`
  - `Approval workflow is disabled.` → `Quy trình duyệt báo cáo hiện không được sử dụng.`
- Trong docs kỹ thuật, có thể giữ thuật ngữ như Area, controller, service, DTO, build, route, nhưng câu mô tả phải bằng tiếng Việt có dấu.

## Kiểm thử

Sau khi sửa:

- Build Debug bằng MSBuild.
- Build Razor view với `MvcBuildViews=true`.
- Chạy `VerifyAreasAndDtos.ps1`.
- Chạy `VerifySecurityHardening.ps1`.
- Grep lại các cụm tiếng Anh/không dấu nổi bật trong phạm vi đã chọn để phát hiện phần còn sót.

## Rủi ro và kiểm soát

- Không đổi identifier kỹ thuật để tránh compile/runtime lỗi.
- Không đổi tên route/action trong `Url.Action`, `Html.ActionLink`, `BeginForm`.
- Không đổi nội dung file package hoặc docs lịch sử không thuộc phạm vi.
- Nếu `VerifySecurityHardening.ps1` cho phép `debug=true` theo môi trường dev, giữ đúng quyết định hiện tại của người dùng.
