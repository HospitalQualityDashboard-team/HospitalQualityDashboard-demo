# Danh sách chức năng của Admin và User

## 1. Phạm vi tài khoản hiện tại

Hệ thống hiện tại chỉ cần **2 loại tài khoản**:

| Loại tài khoản | Mô tả |
|---|---|
| **Admin** | Người quản trị hệ thống, có quyền quản lý toàn bộ dữ liệu và chức năng. |
| **User** | Người dùng thuộc khoa/phòng, chỉ được xem và nhập báo cáo các chỉ số được phân công cho khoa/phòng của mình. |

> Ghi chú: Hiện tại **chưa cần xây dựng vai trò Kiểm duyệt, Ban giám đốc hoặc phân quyền chi tiết theo bảng VaiTro/Quyen**. Có thể mở rộng sau nếu cần.

---

## 2. Chức năng của Admin

Admin là tài khoản quản trị toàn bộ hệ thống. Admin có quyền quản lý dữ liệu nền, phân công chỉ số, quản lý báo cáo, thông báo, dashboard và xuất dữ liệu.

---

### 2.1. Quản lý tài khoản người dùng

Admin có thể quản lý các tài khoản đăng nhập vào hệ thống.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách tài khoản | Hiển thị toàn bộ tài khoản Admin và User trong hệ thống. |
| 2 | Thêm tài khoản | Tạo tài khoản mới cho nhân viên hoặc khoa/phòng. |
| 3 | Sửa tài khoản | Cập nhật thông tin tài khoản. |
| 4 | Khóa tài khoản | Ngăn tài khoản không được đăng nhập vào hệ thống. |
| 5 | Mở khóa tài khoản | Cho phép tài khoản hoạt động trở lại. |
| 6 | Reset mật khẩu | Cấp lại mật khẩu cho người dùng. |
| 7 | Gán nhân viên cho tài khoản | Liên kết tài khoản với một nhân viên trong bệnh viện. |
| 8 | Gán khoa/phòng cho tài khoản | Xác định tài khoản thuộc khoa/phòng nào. |
| 9 | Chọn loại tài khoản | Chọn tài khoản là Admin hoặc User. |

#### Gợi ý dữ liệu cần quản lý

| Trường dữ liệu | Mô tả |
|---|---|
| Tên đăng nhập | Tên dùng để đăng nhập hệ thống. |
| Mật khẩu | Mật khẩu đăng nhập. Nên mã hóa khi lưu. |
| Loại tài khoản | Admin hoặc User. |
| Nhân viên | Nhân viên được liên kết với tài khoản. |
| Khoa/phòng | Khoa/phòng của tài khoản. |
| Trạng thái | Đang hoạt động hoặc bị khóa. |

---

### 2.2. Quản lý khoa/phòng

Admin quản lý danh sách các khoa/phòng trong bệnh viện.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách khoa/phòng | Hiển thị toàn bộ khoa/phòng trong bệnh viện. |
| 2 | Thêm khoa/phòng | Thêm mới một khoa/phòng. |
| 3 | Sửa khoa/phòng | Cập nhật thông tin khoa/phòng. |
| 4 | Khóa khoa/phòng | Ngưng sử dụng khoa/phòng trong hệ thống. |
| 5 | Mở khóa khoa/phòng | Cho phép khoa/phòng hoạt động trở lại. |
| 6 | Import danh sách khoa/phòng | Import file Excel chứa danh sách khoa/phòng vào hệ thống. |
| 7 | Xuất danh sách khoa/phòng | Xuất danh sách khoa/phòng ra Excel/PDF nếu cần. |

#### Mẫu file import khoa/phòng

| ID | IDKHOAPHONG | TENKHOAPHONG | USED |
|---|---|---|---|
| 1 | 1 | Ban giám đốc | 1 |
| 6 | 6 | Công nghệ thông tin | 1 |
| 19 | 31 | Tài chính - Kế toán | 1 |

#### Quy tắc kiểm tra khi import khoa/phòng

| Điều kiện kiểm tra | Cách xử lý |
|---|---|
| File không có đủ 4 cột `ID`, `IDKHOAPHONG`, `TENKHOAPHONG`, `USED` | Báo lỗi định dạng file và không import. |
| `IDKHOAPHONG` trống | Báo lỗi dòng dữ liệu vì đây là mã khoa/phòng dùng để đối chiếu. |
| `IDKHOAPHONG` không phải số nguyên dương | Báo lỗi dòng dữ liệu. |
| `TENKHOAPHONG` trống | Báo lỗi dòng dữ liệu. |
| `USED` trống hoặc không thuộc giá trị `0`, `1` | Báo lỗi dòng dữ liệu. |
| `IDKHOAPHONG` bị trùng trong file import | Báo lỗi các dòng bị trùng và không import các dòng đó. |
| `IDKHOAPHONG` đã tồn tại trong bảng `KhoaPhong` | Cập nhật `TENKHOAPHONG` và trạng thái sử dụng theo `USED`. |
| `IDKHOAPHONG` chưa tồn tại trong bảng `KhoaPhong` | Thêm mới khoa/phòng. |
| `USED = 1` | Đánh dấu khoa/phòng đang hoạt động. |
| `USED = 0` | Đánh dấu khoa/phòng ngưng sử dụng, không xóa dữ liệu cũ. |
| Dữ liệu hợp lệ | Lưu hoặc cập nhật vào bảng `KhoaPhong`, trong đó `IDKHOAPHONG` là mã duy nhất. |

---

### 2.3. Quản lý nhân viên

Admin quản lý danh sách nhân viên của bệnh viện.

Hệ thống chỉ cần **một bảng nhân viên duy nhất**. Danh sách nhân viên toàn viện và danh sách nhân viên theo khoa/phòng được tách bằng cách lọc theo `KhoaPhongId`.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách nhân viên toàn viện | Hiển thị toàn bộ nhân viên của bệnh viện. |
| 2 | Xem nhân viên theo khoa/phòng | Lọc danh sách nhân viên theo từng khoa/phòng. |
| 3 | Thêm nhân viên | Thêm mới một nhân viên. |
| 4 | Sửa nhân viên | Cập nhật thông tin nhân viên. |
| 5 | Khóa nhân viên | Ngưng sử dụng nhân viên trong hệ thống. |
| 6 | Mở khóa nhân viên | Cho phép nhân viên hoạt động trở lại. |
| 7 | Import nhân viên toàn viện | Import file Excel chứa danh sách nhân viên toàn bệnh viện, có cột khoa/phòng. |
| 8 | Import nhân viên theo khoa/phòng | Admin chọn khoa/phòng trước, sau đó import danh sách nhân viên thuộc khoa/phòng đó. |
| 9 | Xuất danh sách nhân viên | Xuất danh sách nhân viên ra Excel/PDF nếu cần. |
| 10 | Tạo tài khoản từ nhân viên | Chọn nhân viên và tạo tài khoản đăng nhập cho nhân viên đó. |

#### Mẫu file import nhân viên toàn viện

| Mã nhân viên | Họ tên | Ngày sinh | Giới tính | Chức vụ | Email | Số điện thoại | Mã khoa/phòng | Tên khoa/phòng |
|---|---|---|---|---|---|---|---|---|
| NV001 | Nguyễn Văn A | 01/01/1990 | Nam | Kế toán | a@example.com | 0900000001 | TC | Phòng Tài chính |
| NV002 | Trần Thị B | 02/02/1991 | Nữ | Dược sĩ | b@example.com | 0900000002 | DUOC | Khoa Dược |

#### Mẫu file import nhân viên theo khoa/phòng

Trường hợp này Admin chọn khoa/phòng trước khi import, nên file có thể không cần cột `Mã khoa/phòng`.

| Mã nhân viên | Họ tên | Ngày sinh | Giới tính | Chức vụ | Email | Số điện thoại |
|---|---|---|---|---|---|---|
| NV001 | Nguyễn Văn A | 01/01/1990 | Nam | Kế toán | a@example.com | 0900000001 |
| NV005 | Lê Thị E | 05/05/1992 | Nữ | Kế toán viên | e@example.com | 0900000005 |

#### Quy tắc kiểm tra khi import nhân viên

| Điều kiện kiểm tra | Cách xử lý |
|---|---|
| Mã nhân viên trống | Báo lỗi dòng dữ liệu. |
| Họ tên trống | Báo lỗi dòng dữ liệu. |
| Mã khoa/phòng không tồn tại | Báo lỗi dòng dữ liệu khi import toàn viện. |
| Mã nhân viên đã tồn tại | Có thể cập nhật hoặc bỏ qua tùy thiết kế. |
| Dữ liệu hợp lệ | Lưu vào bảng `NhanVien`. |

---

### 2.4. Quản lý danh mục chỉ số chất lượng

Admin quản lý toàn bộ danh mục chỉ số chất lượng của bệnh viện.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách chỉ số | Hiển thị toàn bộ danh mục chỉ số chất lượng. |
| 2 | Thêm chỉ số | Thêm mới một chỉ số chất lượng. |
| 3 | Sửa chỉ số | Cập nhật thông tin chỉ số. |
| 4 | Khóa chỉ số | Ngưng sử dụng chỉ số. |
| 5 | Xem chi tiết chỉ số | Xem định nghĩa, phương pháp tính, tử số, mẫu số, nguồn số liệu. |
| 6 | Cấu hình phương pháp tính | Thiết lập chỉ số tính theo tỷ lệ, số lượng, thời gian, điểm trung bình... |
| 7 | Cấu hình tần suất báo cáo | Thiết lập báo cáo theo tháng, quý, năm. |
| 8 | Cấu hình mục tiêu năm | Nhập mục tiêu cần đạt của chỉ số trong năm. |

#### Thông tin một chỉ số cần có

| Trường dữ liệu | Mô tả |
|---|---|
| Mã chỉ số | Mã định danh của chỉ số. |
| Tên chỉ số | Tên đầy đủ của chỉ số chất lượng. |
| Định nghĩa | Mô tả ý nghĩa chỉ số. |
| Lĩnh vực áp dụng | Lĩnh vực sử dụng chỉ số. |
| Khía cạnh chất lượng | Khía cạnh chất lượng liên quan. |
| Thành tố chất lượng | Thành tố chất lượng liên quan. |
| Lý do lựa chọn | Lý do cần theo dõi chỉ số. |
| Phương pháp tính | Công thức hoặc cách tính chỉ số. |
| Tử số | Mô tả tử số nếu có. |
| Mẫu số | Mô tả mẫu số nếu có. |
| Nguồn số liệu | Nơi phát sinh dữ liệu. |
| Thu thập và tổng hợp số liệu | Cách thu thập, tổng hợp dữ liệu. |
| Giá trị của số liệu | Dạng giá trị cần ghi nhận. |
| Tần suất báo cáo | Tháng/quý/năm. |
| Mục tiêu đạt được trong năm | Giá trị mục tiêu. |

---

### 2.5. Phân công khoa/phòng phụ trách chỉ số

Admin phân công khoa/phòng phụ trách từng chỉ số chất lượng.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách phân công | Hiển thị khoa/phòng nào đang phụ trách chỉ số nào. |
| 2 | Phân công chỉ số cho khoa/phòng | Gán một hoặc nhiều chỉ số cho một khoa/phòng. |
| 3 | Sửa phân công | Thay đổi khoa/phòng phụ trách chỉ số. |
| 4 | Hủy phân công | Ngưng phân công chỉ số cho khoa/phòng. |
| 5 | Xem chỉ số theo khoa/phòng | Xem từng khoa/phòng đang phụ trách các chỉ số nào. |

#### Ví dụ phân công

| Khoa/phòng | Chỉ số được phân công |
|---|---|
| Phòng Tài chính | Tỷ lệ người bệnh được cung cấp hóa đơn ngay khi NB thanh toán viện phí trên kiosk |
| Phòng Tài chính | Tỷ lệ người bệnh thanh toán viện phí trực tuyến bằng QR code |
| Khoa Dược | Tỷ lệ phản ứng có hại của thuốc được báo cáo |

> Khi User thuộc Phòng Tài chính đăng nhập, hệ thống chỉ hiển thị 2 chỉ số của Phòng Tài chính và chỉ cho phép nhập báo cáo các chỉ số đó.

---

### 2.6. Quản lý kỳ báo cáo

Admin tạo và quản lý các kỳ báo cáo.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách kỳ báo cáo | Hiển thị các kỳ báo cáo đã tạo. |
| 2 | Tạo kỳ báo cáo | Tạo kỳ báo cáo tháng, quý, năm. |
| 3 | Sửa kỳ báo cáo | Cập nhật thời gian và hạn nộp. |
| 4 | Mở kỳ báo cáo | Cho phép User nhập báo cáo. |
| 5 | Khóa kỳ báo cáo | Không cho User nhập hoặc sửa báo cáo. |
| 6 | Cấu hình hạn nộp | Thiết lập ngày hết hạn báo cáo. |
| 7 | Theo dõi tiến độ kỳ báo cáo | Xem khoa/phòng nào đã nộp, chưa nộp, quá hạn. |

#### Ví dụ kỳ báo cáo

| Kỳ báo cáo | Từ ngày | Đến ngày | Hạn nộp |
|---|---|---|---|
| Tháng 01/2026 | 01/01/2026 | 31/01/2026 | 10/02/2026 |
| Quý I/2026 | 01/01/2026 | 31/03/2026 | 10/04/2026 |

---

### 2.7. Quản lý báo cáo

Admin quản lý báo cáo do User khoa/phòng gửi lên.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách báo cáo | Xem báo cáo theo kỳ, khoa/phòng, chỉ số, trạng thái. |
| 2 | Xem chi tiết báo cáo | Xem tử số, mẫu số, kết quả, ghi chú, file minh chứng. |
| 3 | Duyệt báo cáo | Xác nhận báo cáo hợp lệ. |
| 4 | Trả lại báo cáo | Yêu cầu User chỉnh sửa báo cáo. |
| 5 | Khóa báo cáo | Không cho chỉnh sửa sau khi đã duyệt hoặc hết kỳ. |
| 6 | Theo dõi báo cáo quá hạn | Xem khoa/phòng chưa nộp hoặc nộp trễ. |
| 7 | Lọc báo cáo | Lọc theo kỳ, chỉ số, khoa/phòng, trạng thái. |

#### Trạng thái báo cáo

| Trạng thái | Ý nghĩa |
|---|---|
| Nháp | User đang nhập, chưa gửi. |
| Đã gửi | User đã gửi báo cáo cho Admin. |
| Đã duyệt | Admin đã duyệt báo cáo. |
| Trả lại | Admin yêu cầu User chỉnh sửa. |
| Quá hạn | Đã quá hạn nhưng chưa nộp. |
| Đã khóa | Báo cáo không còn được chỉnh sửa. |

---

### 2.8. Quản lý thông báo

Admin gửi và quản lý thông báo trong hệ thống.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách thông báo | Hiển thị các thông báo đã tạo. |
| 2 | Gửi thông báo thủ công | Admin gửi thông báo đến một hoặc nhiều khoa/phòng. |
| 3 | Tự động cảnh báo trước hạn | Hệ thống cảnh báo trước hạn báo cáo 2 tuần. |
| 4 | Nhắc báo cáo quá hạn | Gửi nhắc nhở cho khoa/phòng chưa nộp báo cáo. |
| 5 | Xem trạng thái đọc | Biết User đã đọc thông báo hay chưa. |

#### Ví dụ thông báo

```text
Kỳ báo cáo tháng 05/2026 sắp đến hạn nộp. Vui lòng hoàn thành báo cáo trước ngày 10/06/2026.
```

---

### 2.9. Dashboard và thống kê

Admin xem tổng quan tình hình báo cáo và kết quả chỉ số.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem tổng số chỉ số | Tổng số chỉ số đang quản lý. |
| 2 | Xem tiến độ báo cáo | Số báo cáo đã nộp, chưa nộp, quá hạn. |
| 3 | Thống kê theo khoa/phòng | Theo dõi từng khoa/phòng đã hoàn thành báo cáo hay chưa. |
| 4 | Thống kê theo chỉ số | Xem kết quả từng chỉ số theo kỳ. |
| 5 | Biểu đồ dashboard | Hiển thị biểu đồ tổng hợp. |
| 6 | Lọc dashboard | Lọc theo kỳ, khoa/phòng, chỉ số. |

---

### 2.10. Xuất Excel/PDF

Admin có thể xuất dữ liệu phục vụ báo cáo và lưu trữ.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xuất danh sách khoa/phòng | Xuất dữ liệu khoa/phòng. |
| 2 | Xuất danh sách nhân viên | Xuất dữ liệu nhân viên. |
| 3 | Xuất danh mục chỉ số | Xuất toàn bộ danh mục chỉ số chất lượng. |
| 4 | Xuất báo cáo theo kỳ | Xuất báo cáo của một kỳ báo cáo. |
| 5 | Xuất báo cáo theo khoa/phòng | Xuất báo cáo của một khoa/phòng. |
| 6 | Xuất báo cáo tổng hợp | Xuất báo cáo toàn viện. |
| 7 | Xuất dashboard | Xuất thống kê dashboard ra Excel/PDF. |

---

## 3. Chức năng của User

User là tài khoản thuộc một khoa/phòng cụ thể. User chỉ được thao tác trong phạm vi khoa/phòng của mình.

Ví dụ:

| Tài khoản | Khoa/phòng | Loại tài khoản |
|---|---|---|
| taichinh01 | Phòng Tài chính | User |

Nếu Phòng Tài chính được phân công 2 chỉ số:

1. Tỷ lệ người bệnh được cung cấp hóa đơn ngay khi NB thanh toán viện phí trên kiosk.
2. Tỷ lệ người bệnh thanh toán viện phí trực tuyến bằng QR code.

Thì User thuộc Phòng Tài chính chỉ thấy và nhập báo cáo cho 2 chỉ số này.

---

### 3.1. Đăng nhập hệ thống

User đăng nhập bằng tài khoản do Admin cấp.

Sau khi đăng nhập, hệ thống lưu các thông tin cần thiết vào Session:

| Thông tin Session | Mô tả |
|---|---|
| TaiKhoanId | Mã tài khoản đăng nhập. |
| TenDangNhap | Tên đăng nhập. |
| LoaiTaiKhoan | Admin hoặc User. |
| NhanVienId | Nhân viên liên kết với tài khoản. |
| KhoaPhongId | Khoa/phòng của User. |
| TenKhoaPhong | Tên khoa/phòng của User. |

---

### 3.2. Xem chỉ số được phân công

User chỉ được xem danh sách chỉ số được phân công cho khoa/phòng của mình.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem danh sách chỉ số được phân công | Hiển thị các chỉ số mà khoa/phòng của User phụ trách. |
| 2 | Xem chi tiết chỉ số | Xem định nghĩa, phương pháp tính, tử số, mẫu số, nguồn số liệu. |
| 3 | Lọc chỉ số | Lọc theo kỳ báo cáo hoặc trạng thái báo cáo. |

---

### 3.3. Nhập số liệu báo cáo

User nhập số liệu cho các chỉ số được phân công.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Chọn kỳ báo cáo | Chọn kỳ cần nhập số liệu. |
| 2 | Nhập tử số | Nhập giá trị tử số nếu chỉ số có tử số. |
| 3 | Nhập mẫu số | Nhập giá trị mẫu số nếu chỉ số có mẫu số. |
| 4 | Nhập giá trị | Nhập giá trị trực tiếp nếu chỉ số không dùng tử số/mẫu số. |
| 5 | Tự động tính kết quả | Hệ thống tính kết quả dựa trên công thức chỉ số. |
| 6 | Nhập ghi chú | User nhập giải thích hoặc nhận xét. |
| 7 | Đính kèm file minh chứng | Upload file Excel, PDF, hình ảnh hoặc tài liệu minh chứng nếu cần. |

---

### 3.4. Lưu nháp báo cáo

User có thể lưu báo cáo ở trạng thái nháp nếu chưa hoàn thành.

| Chức năng | Mô tả |
|---|---|
| Lưu nháp | Lưu dữ liệu nhưng chưa gửi Admin. |
| Sửa nháp | Cho phép chỉnh sửa báo cáo đang ở trạng thái nháp. |

Trạng thái báo cáo:

```text
Nháp
```

---

### 3.5. Gửi báo cáo

Sau khi nhập đầy đủ số liệu, User gửi báo cáo cho Admin.

| Chức năng | Mô tả |
|---|---|
| Gửi báo cáo | Chuyển báo cáo từ Nháp sang Đã gửi. |
| Xác nhận gửi | Hệ thống yêu cầu xác nhận trước khi gửi. |

Trạng thái báo cáo sau khi gửi:

```text
Đã gửi
```

Sau khi gửi, User không nên được sửa báo cáo trừ khi Admin trả lại.

---

### 3.6. Xem trạng thái báo cáo

User có thể theo dõi trạng thái báo cáo của khoa/phòng mình.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem báo cáo đã gửi | Xem các báo cáo đã gửi cho Admin. |
| 2 | Xem báo cáo đã duyệt | Xem các báo cáo đã được Admin duyệt. |
| 3 | Xem báo cáo bị trả lại | Xem báo cáo cần chỉnh sửa. |
| 4 | Xem báo cáo quá hạn | Xem các báo cáo chưa nộp đúng hạn. |
| 5 | Lọc báo cáo | Lọc theo kỳ, chỉ số, trạng thái. |

---

### 3.7. Sửa báo cáo bị trả lại

Nếu Admin trả lại báo cáo, User được phép chỉnh sửa và gửi lại.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xem lý do trả lại | Xem ghi chú của Admin. |
| 2 | Chỉnh sửa số liệu | Sửa tử số, mẫu số, giá trị, ghi chú hoặc file minh chứng. |
| 3 | Gửi lại báo cáo | Gửi lại báo cáo sau khi chỉnh sửa. |

---

### 3.8. Xem thông báo

User xem thông báo từ Admin hoặc hệ thống.

#### Loại thông báo User có thể nhận

| Loại thông báo | Mô tả |
|---|---|
| Sắp tới hạn báo cáo | Hệ thống hoặc Admin nhắc trước hạn nộp 2 tuần. |
| Quá hạn báo cáo | Nhắc User khi chưa nộp đúng hạn. |
| Báo cáo bị trả lại | Thông báo báo cáo cần chỉnh sửa. |
| Báo cáo đã được duyệt | Thông báo báo cáo đã hợp lệ. |

---

### 3.9. Xuất báo cáo của khoa/phòng mình

User chỉ được xuất dữ liệu thuộc khoa/phòng của mình.

#### Chức năng chính

| STT | Chức năng | Mô tả |
|---:|---|---|
| 1 | Xuất báo cáo theo kỳ | Xuất báo cáo của khoa/phòng trong một kỳ. |
| 2 | Xuất báo cáo theo chỉ số | Xuất báo cáo của một chỉ số được phân công. |
| 3 | Xuất Excel | Xuất dữ liệu dạng Excel. |
| 4 | Xuất PDF | Xuất báo cáo dạng PDF nếu cần. |

User không được xuất báo cáo toàn viện hoặc báo cáo của khoa/phòng khác.

---

### 3.10. Đổi mật khẩu

User có thể đổi mật khẩu tài khoản của mình.

| Chức năng | Mô tả |
|---|---|
| Đổi mật khẩu | User nhập mật khẩu cũ và mật khẩu mới để cập nhật. |

---

## 4. Bảng chức năng tổng hợp

| Nhóm chức năng | Admin | User |
|---|---:|---:|
| Quản lý tài khoản | Có | Không |
| Quản lý khoa/phòng | Có | Không |
| Import khoa/phòng | Có | Không |
| Quản lý nhân viên | Có | Không |
| Import nhân viên toàn viện | Có | Không |
| Import nhân viên theo khoa/phòng | Có | Không |
| Quản lý danh mục chỉ số | Có | Không |
| Phân công chỉ số cho khoa/phòng | Có | Không |
| Quản lý kỳ báo cáo | Có | Không |
| Xem chỉ số được phân công | Có | Có, chỉ trong khoa/phòng mình |
| Nhập số liệu báo cáo | Có | Có, chỉ với chỉ số được phân công |
| Lưu nháp báo cáo | Có | Có |
| Gửi báo cáo | Có | Có |
| Duyệt báo cáo | Có | Không |
| Trả lại báo cáo | Có | Không |
| Xem báo cáo toàn viện | Có | Không |
| Xem báo cáo khoa/phòng mình | Có | Có |
| Gửi thông báo | Có | Không |
| Xem thông báo | Có | Có |
| Dashboard toàn viện | Có | Không |
| Dashboard khoa/phòng | Có | Có, nếu cần |
| Xuất báo cáo toàn viện | Có | Không |
| Xuất báo cáo khoa/phòng mình | Có | Có |
| Đổi mật khẩu | Có | Có |

---

## 5. Quy tắc phân quyền hiện tại

Hệ thống nên áp dụng 2 nguyên tắc:

```text
LoaiTaiKhoan quyết định người dùng là Admin hay User.
KhoaPhongId quyết định User được xem và nhập dữ liệu thuộc khoa/phòng nào.
```

Cụ thể:

```text
Admin:
- Được truy cập toàn bộ chức năng.
- Được xem toàn bộ khoa/phòng, nhân viên, chỉ số, báo cáo.
- Được import dữ liệu và phân công chỉ số.

User:
- Chỉ được truy cập chức năng nhập và theo dõi báo cáo.
- Chỉ thấy chỉ số được phân công cho khoa/phòng của mình.
- Chỉ được nhập báo cáo cho chỉ số được phân công.
- Không được xem hoặc sửa dữ liệu của khoa/phòng khác.
```

---

## 6. Các bảng database liên quan

Các bảng chính nên có trong giai đoạn hiện tại:

| Bảng | Mục đích |
|---|---|
| `TaiKhoan` | Lưu thông tin tài khoản đăng nhập. |
| `KhoaPhong` | Lưu danh sách khoa/phòng. |
| `NhanVien` | Lưu danh sách nhân viên bệnh viện. |
| `ChiSoChatLuong` | Lưu danh mục chỉ số chất lượng. |
| `PhanCongChiSo` | Lưu phân công khoa/phòng phụ trách chỉ số. |
| `KyBaoCao` | Lưu kỳ báo cáo. |
| `BaoCao` | Lưu thông tin báo cáo chính. |
| `BaoCaoChiTiet` | Lưu số liệu chi tiết của báo cáo. |
| `ThongBao` | Lưu thông báo hệ thống. |
| `LichSuImport` | Lưu lịch sử import khoa/phòng và nhân viên. |

---

## 7. Kết luận

Ở giai đoạn hiện tại, hệ thống nên được thiết kế đơn giản với 2 loại tài khoản:

```text
Admin và User
```

Admin quản lý toàn bộ hệ thống, bao gồm tài khoản, khoa/phòng, nhân viên, import dữ liệu, chỉ số chất lượng, phân công chỉ số, kỳ báo cáo, báo cáo, thông báo, dashboard và xuất Excel/PDF.

User chỉ được xem và nhập báo cáo cho các chỉ số được phân công theo khoa/phòng của mình.

Thiết kế này phù hợp với dự án sử dụng **ASP.NET MVC4**, dễ triển khai, dễ kiểm soát Session bằng `PageController`, đồng thời vẫn có thể mở rộng phân quyền chi tiết trong tương lai nếu cần.
