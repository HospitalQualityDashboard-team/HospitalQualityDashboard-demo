# Backend Tasks — Hệ thống QLQTDT

> **39 tasks · 5 Dev · ~104 APIs**\
> Dùng để gán backlog Trello\
> **Ký hiệu phụ thuộc:** `← Task X` = cần Task X hoàn thành trước. `Entity: X` = cần entity X có trong DB. `(độc lập)` = không phụ thuộc task khác.

## Done (đã có trong DB/code)

* Database 19 tables (schema đã apply)

* VaiTro CRUD + 7 roles seeded (ADMIN, KHOA_PHONG, BCN_KHOA_PHONG, KE_TOAN, PHONG_QLDT, VIEN_TRUONG, NHA_THAU)

* NguoiDung CRUD + Login/Register (7 users)

* NguoiDung_KhoaPhong_VaiTro (gán user→role, thiếu KhoaPhongId)

* BaseService<T>, BaseController<T>, ExceptionMiddleware, AuditInterceptor

* JWT pipeline (HttpOnly cookie)

* NhatKyKiemToan entity + interceptor tự ghi audit

* Task 1: Seed dữ liệu Quyen (~30 quyền)

* Task 2: CRUD API Quyen (GET/POST/PUT/DELETE)

* Task 7: Fix NguoiDung_KhoaPhong_VaiTro — hỗ trợ KhoaPhongId

* Task 11: CRUD Hình thức đấu thầu (GET/POST/PUT/DELETE + seed 6 hình thức)
* Task 4: Update Login — inject permissions vào JWT claims
* Task 19: CRUD Gói thầu
* Task 24: CRUD Nhà thầu

> **Migration reminder:** Mỗi task tạo bảng mới cần chạy `dotnet ef migrations Add TenMigration && dotnet ef database update`. Bảng mới gồm: DeXuat, HinhThucDauThau, BuocQuyTrinh, GioThau, TaiLieuHoSo, NhaThau, HoSoDuThau, HopDong, PhuLuc, NghiemThu, QuyetToan, ThongBao.

## PHASE 1: RBAC (Dev #1 — 8 tasks)

### Task 1: Seed dữ liệu Quyen ✅

**Phụ thuộc:** (độc lập) — chỉ cần entity `Quyen` + `AppDbContext`

**Mô tả:** Insert danh sách quyền mặc định vào bảng `Quyen`

**AC:**

* ~30 quyền được seed, chia theo module

* Migration script hoặc `Data/SeedData.cs`

**Done:** ~30 quyền seeded trong `DbInitializer.SeedAsync()`

**Danh sách quyền:**

```text
DeXuat: Create, View, Edit, Delete, Submit, Approve, Reject
GoiThau: Create, View, Edit, Delete, StartWorkflow
HoSoDuThau: View, Create, Evaluate, Award
HopDong: Create, View, Edit, Delete, NghiemThu, QuyetToan
NhaThau: Create, View, Edit
Workflow: Config, Process, Rollback, Reassign
User: Create, View, Edit, Lock
Role: Create, View, Edit, Delete
Report: View, Export
Audit: View
```

### Task 2: CRUD API Quyen ✅

**Phụ thuộc:** ← Task 1 (cần seed data để test CRUD)

**Mô tả:** Controller + Service cho bảng Quyen, kế thừa BaseController/BaseService

**API:**

```text
GET    /api/quyen?page=1&pageSize=20&search=DeXuat
POST   /api/quyen          → { maQuyen, tenQuyen }
PUT    /api/quyen/{id}     → { tenQuyen }
DELETE /api/quyen/{id}
```

**Logic:** Validate MaQuyen unique, soft delete (DaXoa), chỉ ADMIN CRUD.

**Done:** QuyenService + QuyenController, kế thừa BaseService/BaseController.

### Task 3: CRUD VaiTro_Quyen + Seed mapping

**Phụ thuộc:** ← Task 1 + Task 2 (cần Quyen entity + CRUD API)

**API:**

```text
POST /api/vai-tro/{id}/quyen  → { permissionIds: [1, 2, 5, 8] }
GET  /api/vai-tro/{id}/quyen  → [{ id, maQuyen, tenQuyen }]
```

**Seed mapping:**

| Role           | Quyền                                                     |
| -------------- | --------------------------------------------------------- |
| ADMIN          | ALL                                                       |
| KHOA_PHONG     | DeXuat.Create, View, Edit, Delete, Submit                 |
| BCN_KHOA_PHONG | DeXuat.View, Approve, Reject                              |
| KE_TOAN        | HopDong.View, QuyetToan; Report.View                      |
| PHONG_QLDT     | GoiThau.*, HoSoDuThau.*, Workflow.*, NhaThau.*, HopDong.* |
| VIEN_TRUONG    | Report.View, Export                                       |
| NHA_THAU       | HoSoDuThau.Create, View; NhaThau.View                     |

**Logic:** Bulk replace — xoá hết cũ → insert mới trong 1 transaction. Validate quyền tồn tại.

### Task 4: Update Login — inject permissions vào JWT claims ✅

**Phụ thuộc:** ← Task 3 (cần VaiTro_Quyen mapping để query permissions khi login)
**Dev:** Công

**Trước:** JWT chỉ có `NameIdentifier`\
**Sau:**

```text
ClaimTypes.NameIdentifier = "1"
"permissions" = "DeXuat.Create,DeXuat.View,GoiThau.Create,..."
```

**Cách lấy:** Query NguoiDung_KhoaPhong_VaiTro → VaiTro_Quyen → Quyen.MaQuyen

**File:** Sửa `AuthService.LoginAsync()`

**Done:** Code done + fix conflict. Đang chờ test.

### Task 5: Permission check attribute

**Phụ thuộc:** ← Task 4 (cần permission claims trong JWT để check)
**Dev:** Khoa (đang chờ test)

**Cách dùng:**

```csharp
[HasPermission("DeXuat.Approve")]
[HttpPost("{id}/approve")]
public async Task<IActionResult> Approve(int id) { ... }
```

**Logic:**

* Đọc claim "permissions" từ JWT token

* So sánh với permission yêu cầu

* Không có → throw 403 FORBIDDEN

* Cache permissions trong request

**File:** `Middleware/HasPermissionAttribute.cs` + `Services/PermissionService.cs`

### Task 6: Update /api/auth/me — trả permissions

**Phụ thuộc:** ← Task 4 (cần permission claims/query để trả về)
**Dev:** Khoa (đang chờ test)

**Trước:**

```json
{ "data": { "id": 1, "hoTen": "...", "vaiTro": ["PHONG_QLDT"] } }
```

**Sau:**

```json
{ "data": { "id": 1, "hoTen": "...", "vaiTro": ["PHONG_QLDT"], "quyen": ["DeXuat.View", "GoiThau.Create"] } }
```

**File:** Sửa `AuthService.GetCurrentUserAsync()`

### Task 7: Fix NguoiDung_KhoaPhong_VaiTro — hỗ trợ KhoaPhongId ✅

**Phụ thuộc:** (độc lập) — chỉ cần entity `NguoiDungKhoaPhongVaiTro` + `KhoaPhong` đã có

**Mô tả:** Hiện tại KhoaPhongId = NULL. Sửa API assign role bắt buộc nhập KhoaPhongId.

**API:**

```text
POST /api/users/{id}/assign-role → { khoaPhongId: 5, vaiTroId: 2 }
```

**Logic:** Validate KhoaPhongId, VaiTroId tồn tại. 1 user có thể có nhiều role ở nhiều khoa/phòng.

**File:** Sửa `UserService.AssignRoleAsync()`

**Done:** UserService.AssignRoleAsync() cập nhật, validate KhoaPhongId + VaiTroId.

### Task 8: Frontend permissions API

**Phụ thuộc:** ← Task 6 (dùng lại `/api/auth/me` response)

**Mô tả:** API cho frontend lấy permissions để render menu/button

Dùng lại `/api/auth/me` (Task 6). Nếu cần endpoint riêng:

```text
GET /api/auth/permissions → ["DeXuat.Create", "GoiThau.View", ...]
```

## PHASE 2: Procurement (Dev #2 — 3 tasks)

### Task 9: CRUD Đề xuất mua sắm

**Phụ thuộc:** ← Task 7 (cần assign role để phân quyền người tạo đề xuất). Entity mới: `DeXuat`, `DeXuatChiTiet`

**Mô tả:** Controller + Service cho bảng DeXuat + DeXuatChiTiet

**API:**

```text
GET    /api/de-xuat?page=1&trangThai=DRAFT&khoaPhongId=5
POST   /api/de-xuat          → { tieuDe, moTa, khoaPhongId, tongDuToan, chiTiet: [...] }
GET    /api/de-xuat/{id}
PUT    /api/de-xuat/{id}     → chỉ khi DRAFT
DELETE /api/de-xuat/{id}     → chỉ khi DRAFT
GET    /api/de-xuat/{id}/chi-tiet
```

**Business logic:**

* Tự sinh MaDeXuat = DX-{year}-{seq} (SELECT MAX + 1 trong năm, pad 4 số, Helper/CodeGenerator.cs)

* Tính ThanhTien = SoLuong * DonGiaDuToan

* Tổng du toán = SUM(ThanhTien)

* Chỉ sửa/xoá khi DRAFT, chỉ người tạo mới sửa được

**Models:** DeXuat, DeXuatChiTiet

### Task 10: State transitions — Submit, Approve, Reject

**Phụ thuộc:** ← Task 9 (cần CRUD đề xuất trước). Approve/Reject cần Task 5 (permission check)

**Mô tả:** 3 endpoint chuyển trạng thái đề xuất

**Submit (DRAFT → PENDING):**

```text
POST /api/de-xuat/{id}/submit
```

Validate: có chi tiết, TrangThai = DRAFT. Set PENDING. Tạo thông báo.

**Approve (PENDING → APPROVED):**

```text
POST /api/de-xuat/{id}/approve → { ghiChu }
```

Validate: quyền DeXuat.Approve. Set APPROVED.

**Reject (PENDING → REJECTED):**

```text
POST /api/de-xuat/{id}/reject → { lyDo }
```

Validate: bắt buộc lyDo. Set REJECTED.

## PHASE 3: Workflow (Dev #3 — 8 tasks)

> **⚠️ Lưu ý tên bảng thực tế trong DB:** `BuocQuyTrinh` (thay `BuocWorkflow`), `TheoDoiQuyTrinh` (thay `WorkflowInstance`), `GioThau` (thay `GoiThau`). Confirm với team trước khi code.

### Task 11: CRUD Hình thức đấu thầu ✅

**Phụ thuộc:** (độc lập) — entity mới `HinhThucDauThau`, không phụ thuộc task nào

**Mô tả:** Danh mục hình thức mua sắm

**API:**

```text
GET    /api/hinh-thuc-dau-thau
POST   /api/hinh-thuc-dau-thau  → { tenHinhThuc, moTa, giaTriToiDa }
PUT    /api/hinh-thuc-dau-thau/{id}
DELETE /api/hinh-thuc-dau-thau/{id}
```

**Seed:** Chỉ định thầu, Chào hàng cạnh tranh, Đấu thầu rộng rãi, Mua sắm trực tiếp, Chào giá trực tuyến, Đặt hàng

**Done:** HinhThucDauThauService (override GetByIdAsync check TrangThaiHoatDong) + Controller.

### Task 12: CRUD Workflow template

**Phụ thuộc:** ← Task 11 (có `hinhThucDauThauId` FK). Entity mới: `Workflow`

**Mô tả:** Template workflow (khung quy trình)

**API:**

```text
GET    /api/workflows
POST   /api/workflows      → { tenWorkflow, moTa, hinhThucDauThauId }
PUT    /api/workflows/{id}
DELETE /api/workflows/{id}  → không xoá nếu có instance ACTIVE
```

### Task 13: CRUD Bước workflow + Transition

**Phụ thuộc:** ← Task 12 (cần Workflow template). Entity mới: `BuocQuyTrinh`, `ChuyenTiepQuyTrinh`

**Mô tả:** BuocWorkflow + ChuyenTiepWorkflow

**Steps:**

```text
GET    /api/workflows/{id}/steps
POST   /api/workflows/{id}/steps       → { tenBuoc, soThuTu, vaiTroXuLyId, choPhepBoQua, soNgaySLA }
PUT    /api/workflows/steps/{id}
DELETE /api/workflows/steps/{id}
```

**Transitions:**

```text
GET    /api/workflows/{id}/transitions
POST   /api/workflows/{id}/transitions → { tuBuocId, denBuocId, tenHanhDong, dieuKien }
```

### Task 14: Workflow Engine — StartWorkflow

**Phụ thuộc:** ← Task 13 (cần steps/transitions). Cần entity `GioThau` (Task 19). Entity mới: `TheoDoiQuyTrinh`, `BuocTheoDoi`, `PhanCongBuoc`

**Mô tả:** Khởi tạo workflow instance cho gói thầu

```text
POST /api/goi-thau/{id}/start-workflow → { workflowId }
```

**Luồng:**

1. Validate gói thầu, TrangThai = DU_THAO

2. Tìm Workflow template

3. Tạo WorkflowInstance (ACTIVE)

4. Tạo WorkflowStepInstance + WorkflowAssignment cho bước đầu

5. Ghi ActionHistory, tạo ThongBao

6. Update GoiThau.TrangThai = DANG_XU_LY

**File:** `Services/WorkflowEngine.cs` — `StartWorkflowAsync()`

### Task 15: Workflow Engine — ProcessStep

**Phụ thuộc:** ← Task 14 (cần workflow instance + assignments để xử lý)

**Mô tả:** Xử lý bước hiện tại (approve/reject)

```text
POST /api/workflow/{instanceId}/process → { hanhDong: "APPROVE"|"REJECT", ghiChu }
```

**Approve:** Validate assignment + transition → complete step hiện tại → tạo step mới (hoặc complete instance) → ghi log + thông báo

**Reject:** Set step REJECTED → instance REJECTED → thông báo

**File:** `WorkflowEngine.ProcessStepAsync()`

### Task 16: Workflow Engine — Rollback, Skip, Reassign

**Phụ thuộc:** ← Task 15 (cùng engine, thêm 3 operations)

**Rollback:**

```text
POST /api/workflow/{instanceId}/rollback → { lyDo }
```

Set step ROLLED_BACK → reactivate step trước → tạo assignment mới

**Skip:**

```text
POST /api/workflow/{instanceId}/skip
```

Chỉ khi ChoPhepBoQua = true

**Reassign:**

```text
POST /api/workflow/{instanceId}/reassign → { nguoiDuocGiaoMoiId, lyDo }
```

Set assignment cũ done → tạo assignment mới

### Task 17: Workflow Engine — CheckOverdue + PendingTasks

**Phụ thuộc:** ← Task 15 (cần instance + steps để query)

**Pending tasks:**

```text
GET /api/workflow/pending
```

→ Steps PENDING của user hiện tại + tính quaHan

**Overdue:**

```text
GET /api/workflow/overdue
```

→ Steps có NgayBatDau + SoNgaySLA < now và vẫn PENDING

### Task 18: Seed data workflow templates

**Phụ thuộc:** ← Task 11 + Task 12 + Task 13 (cần entity HinhThucDauThau, Workflow, BuocQuyTrinh)

**Mô tả:** Tạo 6 workflows cho từng hình thức đấu thầu

**Ví dụ — Chỉ định thầu:**

| Bước | Tên              | Role           | SLA    |
| ---- | ---------------- | -------------- | ------ |
| 1    | Lập đề xuất      | KHOA_PHONG     | 2 ngày |
| 2    | Duyệt khoa/phòng | BCN_KHOA_PHONG | 2 ngày |
| 3    | Kiểm tra QLĐT    | PHONG_QLDT     | 3 ngày |
| 4    | Phê duyệt BGĐ    | VIEN_TRUONG    | 5 ngày |
| 5    | Ký hợp đồng      | PHONG_QLDT     | 3 ngày |

## PHASE 4a: Tender + Document (Dev #4a — 4 tasks)

### Task 19: CRUD Gói thầu ✅

**Phụ thuộc:** ← Task 9 (có thể tạo từ DeXuatId). Entity mới: `GioThau`, `GoiThauChiTiet`
**Dev:** Thắng

**API:**

```text
GET    /api/goi-thau?page=1&trangThai=DU_THAO
POST   /api/goi-thau          → tạo từ DeXuatId hoặc nhập tay
GET    /api/goi-thau/{id}
PUT    /api/goi-thau/{id}     → chỉ khi DU_THAO
DELETE /api/goi-thau/{id}     → chỉ khi DU_THAO
GET    /api/goi-thau/{id}/chi-tiet
```

**Logic:** MaGoiThau = GT-{year}-{seq}. TrangThai mặc định = DU_THAO.

### Task 20: Tích hợp Workflow

**Phụ thuộc:** ← Task 14 (cần WorkflowEngine.StartWorkflow) + Task 19 (cần GoiThau entity)

```text
POST /api/goi-thau/{id}/start-workflow → { workflowId }
GET  /api/goi-thau/{id}/workflow        → instance + steps + assignments
```

Gọi WorkflowEngine. Update TrangThai GoiThau.

### Task 21: FTP Service

**Phụ thuộc:** (độc lập) — utility service, không phụ thuộc task nào. Cấu hình từ `.env`

**Mô tả:** Upload/download/delete file qua FTP passive mode

```text
FtpService.UploadAsync(stream, remotePath)   → remotePath
FtpService.DownloadAsync(remotePath)          → stream
FtpService.DeleteAsync(remotePath)
```

**Config:** Server, Port, User, Password từ .env → FtpConfig

### Task 22: CRUD Tài liệu

**Phụ thuộc:** ← Task 21 (cần FTP để upload file) + Task 19 (có `goiThauId` FK)

```text
POST /api/files/upload  (multipart) → [{ id, fileName, size }]
GET  /api/files/{id}                → download file
DELETE /api/files/{id}
GET  /api/files?goiThauId=1&loaiTaiLieu=HOSO_DUTHAU
```

**Logic:** Upload → FTP → lưu metadata vào TaiLieuHoSo

## PHASE 4b: Dashboard (Dev #4b — 1 task)

### Task 23: Dashboard 4 APIs

**Phụ thuộc:** ← Task 19 (GoiThau thống kê) + Task 17 (pending tasks). Có thể làm độc lập nếu aggregate tay

```text
GET /api/dashboard/summary
```

→ Tổng gói thầu theo trạng thái, giá trị, tỷ lệ tiết kiệm

```text
GET /api/dashboard/statistics?nam=2026&quy=2
```

→ Theo tháng, khoa phòng, hình thức đấu thầu

```text
GET /api/dashboard/pending
```

→ Đếm việc pending của user

```text
GET /api/dashboard/export?loai=excel&tuNgay=...&denNgay=...
```

→ Export Excel/PDF

**NuGet:** `ClosedXML` (Excel), `QuestPDF` (PDF)

## PHASE 5a: Vendor (Dev #5a — 3 tasks)

### Task 24: CRUD Nhà thầu

**Phụ thuộc:** (độc lập) — entity `NhaThau` đã có trong DB. Không phụ thuộc task RBAC nào
**Dev:** Công (đang chờ test)

```text
GET    /api/nha-thau?search=xyz
POST   /api/nha-thau  → { tenNhaThau, maSoThue, nguoiDaiDien, email, soDienThoai, diaChi, linhVuc, soTaiKhoan, tenNganHang }
PUT    /api/nha-thau/{id}
GET    /api/nha-thau/{id}
```

**Validate:** MaSoThue unique

### Task 25: Hồ sơ năng lực nhà thầu

**Phụ thuộc:** ← Task 24 (cần NhaThau entity). Cần Task 21 (FTP) để upload file

```text
GET    /api/nha-thau/{id}/ho-so-nang-luc
POST   /api/nha-thau/{id}/ho-so-nang-luc (multipart) → { tenTaiLieu, loai, ngayHetHan, file }
DELETE /api/nha-thau/{id}/ho-so-nang-luc/{fileId}
```

Loại hồ sơ: GPKD, CHUNGCHI, ISO, etc.

### Task 26: Lịch sử đấu thầu

**Phụ thuộc:** ← Task 24 + Task 27 (cần HoSoDuThau để aggregate)

```text
GET /api/nha-thau/{id}/ls-du-thau
```

Aggregate từ HoSoDuThau: gói thầu, kết quả, giá trị trúng thầu.

## PHASE 5b: Bidding (Dev #5b — 3 tasks)

### Task 27: Nộp hồ sơ dự thầu

**Phụ thuộc:** ← Task 19 (cần GoiThau) + Task 24 (cần NhaThau). Entity mới: `HoSoDuThau`

```text
POST /api/ho-so-du-thau → { goiThauId, nhaThauId, giaDuThau, thoiGianBaoHanh, thoiGianGiaoHang, dinhKemFileIds }
GET  /api/ho-so-du-thau?goiThauId=1
GET  /api/ho-so-du-thau/{id}
```

**Logic:** MaHoSo = HSDT-{year}-{seq}. 1 nhà thầu 1 hồ sơ/gói thầu. TrangThai mặc định = DANG_XET.

### Task 28: Đánh giá hồ sơ

**Phụ thuộc:** ← Task 27 (cần hồ sơ để đánh giá). Cần Task 5 (permission HoSoDuThau.Evaluate)

```text
POST /api/ho-so-du-thau/{id}/evaluate → { ketQua, diemDanhGia, nhanXet }
```

**Logic:** Chỉ user có quyền HoSoDuThau.Evaluate.

### Task 29: Chọn nhà thầu trúng thầu

**Phụ thuộc:** ← Task 27 (cần HSDT) + Task 19 (cần GoiThau để update trạng thái)

```text
POST /api/goi-thau/{id}/award → { hoSoDuThauId, giaTrungThau, lyDo }
```

**Logic:**

1. Validate GoiThau.TrangThai = DANG_XET_HO_SO

2. Set 1 hoSo TRUNG_THAU, còn lại KHONG_TRUNG

3. Update GoiThau.TrangThai = DA_CHON_NHA_THAU

4. (Hợp đồng tạo thủ công qua Task 30 — không auto-create để tránh coupling giữa Bidding và Contract)

## PHASE 5c: Contract (Dev #5c — 5 tasks)

### Task 30: CRUD Hợp đồng

**Phụ thuộc:** ← Task 29 (cần nhà thầu trúng thầu). Entity mới: `HopDong`

```text
GET    /api/hop-dong?goiThauId=1
POST   /api/hop-dong  → { goiThauId, nhaThauId, tenHopDong, giaTri, ngayKy, ngayHieuLuc, ngayHetHan, dieuKhoanThanhToan, dinhKemFileIds }
GET    /api/hop-dong/{id}
PUT    /api/hop-dong/{id}  → chỉ khi NHAP
```

**Logic:** SoHopDong = HD-{year}-{seq}. TrangThai mặc định = NHAP.

### Task 31: Phụ lục hợp đồng

**Phụ thuộc:** ← Task 30 (cần HopDong entity)

```text
POST /api/hop-dong/{id}/phu-luc → { soPhuLuc, noiDung, giaTriDieuChinh, ngayKy, dinhKemFileIds }
GET  /api/hop-dong/{id}/phu-luc
```

Phụ lục làm thay đổi tổng giá trị hợp đồng.

### Task 32: Nghiệm thu

**Phụ thuộc:** ← Task 30 (cần HopDong entity)

```text
POST /api/hop-dong/{id}/nghiem-thu → { lan, noiDung, ngayNghiemThu, ketQua, nhanXet, dinhKemFileIds }
GET  /api/hop-dong/{id}/nghiem-thu
```

1 hợp đồng có thể có nhiều lần nghiệm thu.

### Task 33: Quyết toán

**Phụ thuộc:** ← Task 32 (cần nghiệm thu trước)

```text
POST /api/hop-dong/{id}/quyet-toan → { soQuyetToan, giaTriThucTe, noiDung, ngayQuyetToan, dinhKemFileIds }
GET  /api/hop-dong/{id}/quyet-toan
```

1 hợp đồng → 1 quyết toán. Tính chênh lệch vs giá trị hợp đồng.

### Task 34: State machine hợp đồng

**Phụ thuộc:** ← Task 30 (cần trạng thái HopDong để chuyển)

```text
PATCH /api/hop-dong/{id}/status → { trangThai, ghiChu }
```

**State machine:**

```text
NHAP → DA_KY → DANG_THUC_HIEN → DA_NGHIEM_THU → DA_QUYET_TOAN → DA_TAT_TOAN
                 ↓                    ↓
            TAM_DUNG              HUY
```

Validate transition. Ghi audit log.

## PHASE 5d: Notification + Integration (Dev #5d — 2 tasks)

### Task 35: Thông báo

**Phụ thuộc:** ← Task 14 + Task 10 (tạo notification tự động từ workflow/procurement events)

```text
GET    /api/thong-bao?page=1&daDoc=false
PATCH  /api/thong-bao/{id}/read
POST   /api/thong-bao/read-all
```

**Tự động tạo khi:**

* WorkflowAssignment mới

* Đề xuất/hồ sơ được approve/reject

* Kết quả đấu thầu

* Step quá hạn

### Task 36: Đồng bộ dữ liệu

**Phụ thuộc:** (độc lập) — kết nối hệ thống ngoài HIS/kho, không phụ thuộc task nội bộ

```text
POST /api/integration/sync        → { heThong: "HIS", loai: "NHAN_SU", ngayTu }
GET  /api/integration/logs
POST /api/integration/retry/{id}
```

Kết nối hệ thống ngoài (HIS/kho). Ghi log kết quả.

## Cross-cutting (ai rảnh — 3 tasks)

### Task 37: Audit Log API

**Phụ thuộc:** (độc lập) — entity `NhatKyKiemToan` đã có + interceptor tự ghi. Chỉ cần Controller + Service

```text
GET /api/audit-log?page=1&bang=GoiThau&hanhDong=UPDATE
GET /api/audit-log/goi-thau/{id}
```

Đọc từ NhatKyKiemToan (đã có interceptor tự ghi). Chỉ ADMIN + PHONG_QLDT xem được.

### Task 38: Review + fix response format

**Phụ thuộc:** ← tất cả task có API response (nên làm cuối cùng sau khi các API hoàn thiện)

Rà soát toàn bộ API → đảm bảo format chuẩn `ApiResponse<T>`.

### Task 39: Tổng hợp docs API cuối phase

**Phụ thuộc:** ← Task 38 (nên review trước rồi mới tổng hợp docs)

Cập nhật `docs/API.md` và `docs/TASKS.md` sau mỗi phase.

## Tổng hợp

| Phase               | Tasks  | Dev | APIs     |
| ------------------- | ------ | --- | -------- |
| RBAC                | 1-8    | #1  | 14       |
| Procurement         | 9-10   | #2  | 9        |
| Workflow            | 11-18  | #3  | 20       |
| Tender + Doc        | 19-22  | #4a | 12       |
| Dashboard           | 23     | #4b | 4        |
| Vendor              | 24-26  | #5a | 7        |
| Bidding             | 27-29  | #5b | 6        |
| Contract            | 30-34  | #5c | 11       |
| Notif + Integration | 35-36  | #5d | 6        |
| Cross               | 37-39  | any | 2+       |
| **Total**           | **39** |     | **~104** |

## Timeline thực thi — 4 Dev × 2 Tuần

> Deadline hard: 10 ngày. Mỗi ngày 1 dev làm ~2-4 APIs.
>
> **Critical path:** `T5` → block `T10`+`T28`. `T19` → block `T14`+`T20`+`T22`+`T27`.

### Ký hiệu

| Ký hiệu | Ý nghĩa |
|:-------:|---------|
| 🟢 | Độc lập, không phụ thuộc |
| 🔗 T1 | Phụ thuộc Task 1 |
| ⬜ T1 | Phụ thuộc Task 1 (dev khác) |
| → | Tuần tự | ＋ | Song song |

### Tuần 1 — RBAC + Workflow + Core CRUD

| Done | Task | Tên | Phụ thuộc |
|:----:|:----:|-----|:---------:|
| ☑ | T1 | Seed Quyền | 🟢 |
| ☑ | T2 | CRUD Quyền | 🔗 T1 |
| ☑ | T7 | Fix NKV Role — KhoaPhongId | 🟢 |
| ☑ | T11 | CRUD Hình thức đấu thầu | 🟢 |
| ☑ | T3 | VaiTrò_Quyền + Seed mapping | 🔗 T1, T2 |
| ☑ | T4 | Login — inject JWT permissions | 🔗 T3 |
| ⏳ | T5 | HasPermission attribute | 🔗 T4 (đang chờ test) |
| ⏳ | T6 | /api/auth/me — trả permissions | 🔗 T4 (đang chờ test) |
| ☑ | T9 | CRUD Đề xuất | 🔗 T7 |
| ☑ | T12 | CRUD Workflow template | 🔗 T11 |
| ☐ | T13 | CRUD Bước + Transition | 🔗 T12 |
| ☐ | T14 | Workflow Engine — StartWorkflow | 🔗 T13, ⬜ T19 |
| ☐ | T15 | ProcessStep | 🔗 T14 |
| ☑ | T19 | CRUD Gói thầu | 🔗 T9 |
| ☑ | T21 | FTP Service | 🟢 |
| ☑ | T24 | CRUD Nhà thầu | 🟢 |
| ☐ | T27 | Nộp hồ sơ dự thầu | 🔗 T19, T24 |
| ☐ | T28 | Đánh giá hồ sơ | 🔗 T27, ⬜ T5 |
| ☐ | T29 | Chọn trúng thầu | 🔗 T27, T19 |
| ☑ | T36 | Đồng bộ (HIS/kho) | 🟢 |
| ☑ | T37 | Audit Log API | 🟢 |

**Schedule:**

| Ngày | Dev A (Minh) | Dev B (Khoa) | Dev C (Thắng) | Dev D (Công) |
|:----:|:------|:------|:------|:------|
| D1 | T1 → T2 | T11 | T7 | T24 |
| D2 | T3 | T12 | T21 | T36, T37 |
| D3 | T3 → T4 | T13 | T9 | T37, review |
| D4 | T5 | T14 (⬜T19) | T9 → T19 | T27 |
| D5 | T6 | T15 | T19 + migration | T28 (⬜T5) → T29 |

### Current Assignment (sau khi các task độc lập xong)

| Dev | Task hiện tại | Task tiếp theo | Ghi chú |
|-----|:------------:|:-------------:|---------|
| **Minh** | T13: Bước + Transition | T14 → T15 | Workflow engine chain |
| **Khoa** | Test T5, T6 | T8 → support T16/T17 | Chờ test xong |
| **Thắng** | T22: CRUD Tài liệu | T23 → T25 | T19✅ + T21✅ |
| **Công** | Test T24 | T27 → T25 | Chờ test xong |

### Tuần 2 — Tích hợp + Notification + Docs

| Done | Task | Tên | Phụ thuộc |
|:----:|:----:|-----|:---------:|
| ☐ | T8 | Frontend permissions API | 🔗 T6 |
| ☐ | T10 | State trans. (Submit/Approve/Reject) | 🔗 T9, ⬜ T5 |
| ☐ | T16 | Rollback, Skip, Reassign | 🔗 T15 |
| ☐ | T17 | CheckOverdue + PendingTasks | 🔗 T15 |
| ☐ | T18 | Seed workflow templates | 🔗 T11, T12, T13 |
| ☐ | T20 | Tích hợp Workflow | 🔗 T14, T19 |
| ☐ | T22 | CRUD Tài liệu (FTP) | 🔗 T21, T19 |
| ☐ | T23 | Dashboard | 🔗 T19, ⬜ T17 |
| ☐ | T25 | Hồ sơ năng lực | 🔗 T24, T21 |
| ☐ | T26 | Lịch sử đấu thầu | 🔗 T24, T27 |
| ☐ | T30 | CRUD Hợp đồng | 🔗 T29 |
| ☐ | T31 | Phụ lục hợp đồng | 🔗 T30 |
| ☐ | T32 | Nghiệm thu | 🔗 T30 |
| ☐ | T33 | Quyết toán | 🔗 T32 |
| ☐ | T34 | State machine hợp đồng | 🔗 T30 |
| ☐ | T35 | Thông báo | 🔗 T14, T10 |
| ☐ | T38 | Review response format | 🔗 all |
| ☐ | T39 | Tổng hợp docs | 🔗 T38 |

**Schedule:**

| Ngày | Dev A | Dev B | Dev C | Dev D |
|:----:|:------|:------|:------|:------|
| D6 | T10 (⬜T5) | T16 | T20 | T30 |
| D7 | T10 → T8 | T17 | T22 | T31, T32 |
| D8 | T37, review | T18 | T23 (1/2) | T33, T34 |
| D9 | T35 | T35 (trigger) | T23 (2/2) | T25, T26 |
| D10 | T38 | T38 | T39 | T39 |

### Priority scope

| Mức    | Tasks                                       | APIs | Bắt buộc?              |
| ------ | ------------------------------------------- | ---- | ---------------------- |
| **P0** | T1-7, T9, T11-15, T19, T21, T24             | ~52  | ❗ Bắt buộc             |
| **P1** | T8, T10, T16-18, T20, T22, T27-32, T35, T37 | ~30  | ⚡ Cố gắng              |
| **P2** | T23-26, T33-34, T36, T38-39                 | ~18  | 🟢 Có thể bỏ nếu cuống |

### Handoff rules

* **Contract-first:** Dev A define interface → deadline D2. Các dev còn lại dùng mock đến khi có implement thật

* **Critical path:** Dev A T5 (HasPermission) block T10 + T28. Nếu chậm, Dev B/C/D tự mock attribute

* **Overflow:** Dev nào xong sớm → support dev chậm hoặc làm P2

* **Daily check:** Đầu mỗi ngày verify dependency đã pass chưa. Chưa → reassign

## PR Template (dùng cho tất cả task)

> Theo quy định tại `docs/Quy Định & Hướng Dẫn Dành Cho Dev Team.md` mục 1.3

### Tiêu đề PR

```text
[TASK-ID] Mô tả ngắn gọn
```

Ví dụ: `[T2] CRUD API Quyền`, `[T9] CRUD Đề xuất mua sắm`

### Template nội dung PR

```markdown
## Mô tả

Mô tả ngắn gọn về thay đổi — mục đích, scope, vấn đề giải quyết.

## Loại thay đổi

- [ ] Bug fix
- [x] New feature
- [ ] Breaking change
- [ ] Documentation update

## Checklist

- [x] Code đã được test
- [ ] Đã viết/update test cases
- [ ] Đã update documentation
- [x] Code tuân thủ convention
- [x] Không có warning/error

## API endpoints (nếu có)
```

GET /api/...\
POST /api/... → { req body }

```text

## Cách test

1. Step-by-step
2. HTTP status kỳ vọng
3. Edge cases
```

> **Chú ý:** API endpoints + cách test bắt buộc cho task có HTTP APIs. Bỏ nếu task chỉ là seed data/config.
