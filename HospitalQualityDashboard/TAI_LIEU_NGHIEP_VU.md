# Tài Liệu Business Và Nghiệp Vụ Dự Án HospitalQualityDashboard

## 1. Mục Đích Tài Liệu

Tài liệu này mô tả chi tiết bài toán business, quy trình nghiệp vụ, vai trò người dùng, dữ liệu quản lý và các quy tắc vận hành của hệ thống `HospitalQualityDashboard`.

Đối tượng sử dụng tài liệu:

- Người quản lý dự án hoặc người hướng dẫn thực tập cần hiểu mục tiêu hệ thống.
- Lập trình viên cần nắm nghiệp vụ trước khi sửa code.
- Người kiểm thử cần xác định kịch bản kiểm thử.
- Người dùng phía bệnh viện cần hiểu luồng vận hành tổng thể.
- Sinh viên cần dùng nội dung để viết báo cáo phân tích, thiết kế và triển khai.

## 2. Bối Cảnh Business

Trong hoạt động quản lý chất lượng bệnh viện, các khoa/phòng phải định kỳ thu thập, tổng hợp và báo cáo nhiều chỉ số chất lượng. Các chỉ số này có thể liên quan đến chuyên môn khám chữa bệnh, an toàn người bệnh, nhân sự, điều dưỡng, kiểm soát nhiễm khuẩn, kế hoạch tổng hợp, tài chính hoặc các lĩnh vực quản trị khác.

Nếu quản lý bằng file rời, email hoặc giấy tờ, bệnh viện thường gặp các vấn đề:

- Khó biết khoa/phòng nào đã gửi báo cáo, khoa/phòng nào còn thiếu.
- Khó kiểm soát hạn nộp theo tháng, quý, 3 tháng, 6 tháng, 9 tháng hoặc năm.
- Một chỉ số có thể liên quan nhiều khoa/phòng nhưng file quản lý dễ chỉ gán nhầm cho một đơn vị.
- Những chỉ số có nhiều khoa quản lý sẽ được gắn đúng với khoa đó.
Ví dụ: Chỉ số: Tỷ lệ Bác sĩ có chứng chỉ tạo hình thẩm mỹ/ tổng số phẫu thuật viên
được Thu thập và tổng hợp số liệu: P. KHTH thu thập số liệu và P. TCCB tổng hợp số liệu
thì ở phân chia chỉ số Chỉ số đó sẽ được gắn là Phòng Kế hoạch tổng hợp thu thập số liệu và Phòng Tổ chức cán bộ tổng hợp số liệu.
- Dữ liệu chỉ số có nhiều cách tính, dễ nhập sai tử số, mẫu số hoặc giá trị trực tiếp.
- Admin khó tổng hợp tiến độ toàn viện.
- User khoa/phòng khó biết chính xác mình cần báo cáo chỉ số nào trong kỳ nào.
- Thông báo nhắc việc dễ bị thất lạc.
- Danh mục chỉ số ban đầu thường nằm trong Word/Excel nên nhập tay mất thời gian và dễ sai.

`HospitalQualityDashboard` được xây dựng để giải quyết các vấn đề trên bằng một hệ thống web tập trung.

## 3. Mục Tiêu Business

Hệ thống hướng đến các mục tiêu chính:

1. Quản lý tập trung danh mục chỉ số chất lượng bệnh viện.
2. Quản lý danh mục khoa/phòng và nhân viên.
3. Phân công trách nhiệm báo cáo rõ ràng cho từng khoa/phòng.
4. Hỗ trợ quan hệ nhiều-nhiều giữa chỉ số và khoa/phòng.
5. Hỗ trợ nhiều tần suất báo cáo cho một chỉ số.
6. Cho phép User khoa/phòng nhập và gửi số liệu trực tuyến.
7. Tự động tính kết quả theo công thức chỉ số.
8. Tự động so sánh kết quả với mục tiêu nếu chỉ số có mục tiêu.
9. Giúp Admin theo dõi tiến độ báo cáo toàn viện.
10. Giảm lỗi nhập liệu bằng import từ Excel/Word.
11. Bảo đảm người dùng chỉ xem và thao tác dữ liệu đúng phạm vi quyền hạn.
12. Cung cấp dashboard để theo dõi số đã gửi, còn thiếu và quá hạn.

## 4. Phạm Vi Hệ Thống

### 4.1. Trong phạm vi

- Đăng nhập riêng cho Admin và User.
- Đổi mật khẩu.
- Quản lý khoa/phòng.
- Import khoa/phòng từ Excel.
- Quản lý nhân viên.
- Import nhân viên từ Excel.
- Tạo tài khoản User cho nhân viên.
- Quản lý chỉ số chất lượng.
- Import chỉ số từ Excel/Word.
- Nhận diện tần suất báo cáo từ dữ liệu nguồn.
- Nhận diện khoa/phòng phụ trách từ trường thu thập/tổng hợp số liệu.
- Phân công chỉ số cho một hoặc nhiều khoa/phòng.
- Đồng bộ phân công từ dữ liệu chỉ số.
- Quản lý kỳ báo cáo.
- User nhập, lưu nháp và gửi báo cáo.
- Admin xem, khóa, xóa và xuất báo cáo.
- Dashboard theo quyền.
- Thông báo và đánh dấu đã đọc.

### 4.2. Ngoài phạm vi hiện tại

- Quy trình phê duyệt nhiều cấp.
- Tích hợp chữ ký số.
- Tích hợp HIS/LIS/EMR.
- Import dữ liệu tự động từ hệ thống bệnh viện khác.
- Cảnh báo tự động qua SMS, Zalo, email.
- Phân quyền chi tiết theo từng chức năng con.
- Báo cáo phân tích nâng cao theo biểu đồ chuyên sâu.

Các phần ngoài phạm vi có thể phát triển thêm ở giai đoạn sau.

## 5. Vai Trò Người Dùng

### 5.1. Admin

Admin là người quản trị hệ thống, thường thuộc phòng quản lý chất lượng, phòng kế hoạch tổng hợp hoặc nhóm phụ trách triển khai.

Admin có quyền:

- Đăng nhập qua trang Admin.
- Quản lý khoa/phòng.
- Quản lý nhân viên.
- Tạo tài khoản User cho nhân viên. (có thể thêm chức năng Import dữ liệu nhân viên và khi Import tự động tạo tài khoản User cho nhân viên tài khoản mật khẩu được tạo theo mã nhân viên và nhân viên đó sẽ được phân theo Khoa/Phòng dựa trên cột khoa Phòng)
- Quản lý danh mục chỉ số.
- Import chỉ số từ file nguồn.
- Phân công chỉ số cho khoa/phòng (Thêm chức năng phân công bằng tay thay vì dựa vào dữ liệu Import các chỉ số vì các phân công dễ bị sai xót và lỗi.)
- Đồng bộ phân công từ trường thu thập/tổng hợp của chỉ số.
- Quản lý kỳ báo cáo (Thêm chức năng CRUD kì báo cáo).
- Theo dõi báo cáo toàn viện.
- Khóa báo cáo đã gửi.
- Xóa báo cáo khi cần xử lý dữ liệu sai trong giai đoạn vận hành.
- Xem dashboard toàn viện.
- Gửi thông báo.
- Xuất dữ liệu.

### 5.2. User khoa/phòng

User là tài khoản của một khoa/phòng cụ thể. Mỗi User bắt buộc gắn với một `KhoaPhongId`.

User có quyền:

- Đăng nhập qua trang User.
- Xem dashboard của khoa/phòng mình.
- Xem các chỉ số được phân công cho khoa/phòng mình.
- Nhập số liệu báo cáo theo kỳ.
- Lưu nháp báo cáo.
- Gửi báo cáo.
- Xem thông báo được gửi cho mình.
- Đánh dấu thông báo đã đọc.
- Đổi mật khẩu.

User không có quyền:

- Xem danh mục chỉ số toàn hệ thống.
- Quản lý khoa/phòng.
- Quản lý nhân viên.
- Quản lý phân công.
- Quản lý kỳ báo cáo.
- Xuất dữ liệu toàn viện.
- Khóa hoặc xóa báo cáo.
- Gửi thông báo cho khoa/phòng khác.

## 6. Danh Mục Nghiệp Vụ

### 6.1. Khoa/phòng

Khoa/phòng là đơn vị tổ chức trong bệnh viện. Mỗi khoa/phòng có thể được phân công nhiều chỉ số chất lượng.

Thông tin chính:

- Mã khoa/phòng nguồn.
- Tên khoa/phòng.
- Trạng thái đang sử dụng hay ngừng sử dụng.

Quy tắc:

- Không nên xóa khoa/phòng đã phát sinh nhân viên, tài khoản, phân công hoặc báo cáo.
- Khi khoa/phòng không còn sử dụng, nên chuyển trạng thái thay vì xóa.
- Tên khoa/phòng cần đủ rõ để parser nhận diện từ dữ liệu chỉ số.

### 6.2. Nhân viên

Nhân viên là người thuộc một khoa/phòng. Nhân viên có thể được tạo tài khoản User để nhập báo cáo cho khoa/phòng.

Thông tin chính:

- Mã nhân viên.
- Họ tên.
- Ngày sinh.
- Giới tính.
- Chức vụ.
- Email.
- Số điện thoại.
- Khoa/phòng.
- Trạng thái sử dụng.

Quy tắc:

- Mã nhân viên nên là duy nhất.
- Nhân viên phải thuộc một khoa/phòng.
- Một nhân viên có thể được tạo tài khoản đăng nhập nếu cần.

### 6.3. Tài khoản

Tài khoản dùng để đăng nhập hệ thống.

Loại tài khoản:

- Admin.
- User.

Quy tắc:

- Tên đăng nhập là duy nhất.
- Mật khẩu được hash, không lưu plain text.
- User bắt buộc có khoa/phòng.
- Admin có thể không gắn khoa/phòng.
- Tài khoản ngừng hoạt động không được đăng nhập.

### 6.4. Chỉ số chất lượng

Chỉ số chất lượng là đối tượng nghiệp vụ trung tâm của hệ thống.

Thông tin chính:

- Mã chỉ số.
- Số thứ tự.
- Tên chỉ số.
- Định nghĩa.
- Lĩnh vực áp dụng.
- Khía cạnh chất lượng.
- Thành tố chất lượng.
- Lý do lựa chọn.
- Phương pháp tính.
- Tử số.
- Mẫu số.
- Nguồn số liệu.
- Thu thập và tổng hợp số liệu.
- Giá trị của số liệu.
- Tần suất báo cáo.
- Loại công thức.
- Đơn vị tính.
- Mục tiêu.
- Trạng thái hoạt động.

Quy tắc:

- Một chỉ số có thể có nhiều tần suất báo cáo.
- Một chỉ số có thể liên quan nhiều khoa/phòng.
- Chỉ số đã có báo cáo không nên xóa cứng.
- Nếu chỉ số không còn áp dụng, nên ngừng hoạt động.
- Thông tin `Thu thập và tổng hợp số liệu` là nguồn quan trọng để tự động phân công khoa/phòng.
- Khi import từ file Word/Excel mà không có cột `Đơn vị tính`, hệ thống phải tự suy luận `DonViTinh` từ tên chỉ số, loại công thức, tử số, mẫu số và phương pháp tính.

## 7. Quan Hệ Chỉ Số Và Khoa/Phòng

### 7.1. Bản chất nghiệp vụ

Trong thực tế, một chỉ số chất lượng có thể không chỉ thuộc một khoa/phòng duy nhất. Ví dụ:

```text
Thu thập và tổng hợp số liệu:
P. KHTH thu thập số liệu
P. TCCB tổng hợp số liệu
```

Trường hợp này có 2 đơn vị liên quan:

- Phòng Kế hoạch tổng hợp.
- Phòng Tổ chức cán bộ.

Nếu hệ thống chỉ cho phép một chỉ số gắn với một khoa/phòng, dữ liệu phân công sẽ sai nghiệp vụ. Vì vậy quan hệ đúng là nhiều-nhiều.

### 7.2. Mô hình nghiệp vụ

```text
Một chỉ số -> có thể phân công cho nhiều khoa/phòng
Một khoa/phòng -> có thể được phân công nhiều chỉ số
```

Mô hình dữ liệu:

```text
ChiSoChatLuong 1-n PhanCongChiSo n-1 KhoaPhong
```

`PhanCongChiSo` là bảng trung gian, lưu từng cặp chỉ số - khoa/phòng.

### 7.3. Quy tắc chống trùng

Mỗi cặp `ChiSoChatLuongId` và `KhoaPhongId` chỉ nên tồn tại một lần. Vì vậy database có constraint:

```text
UQ_PhanCongChiSo_ChiSo_KhoaPhong
```

Mục tiêu:

- Tránh tạo trùng phân công.
- Tránh User thấy cùng một chỉ số lặp nhiều lần.
- Tránh báo cáo bị sinh dư.

## 8. Tần Suất Báo Cáo

### 8.1. Nhu cầu nghiệp vụ

Chỉ số chất lượng có thể được báo cáo theo nhiều chu kỳ:

- Hằng ngày.
- Hằng tuần.
- Hằng tháng.
- Hằng quý.
- 3 tháng.
- 6 tháng.
- 9 tháng.
- 12 tháng.
- Hằng năm.
- Khi phát sinh.
- Trước và sau khi thực hiện
- Khi có xảy ra phản ứng có hại, thường xuyên.
- 6 lần truyền thông (tại bệnh nhân, tại khoa).

Một số chỉ số có mô tả tần suất kết hợp, ví dụ:

```text
Mỗi quý, 6 tháng, 12 tháng.
```

Vì vậy hệ thống cần lưu được nhiều tần suất cho cùng một chỉ số.

### 8.2. Các biến thể tần suất quý

Trong file nguồn, tần suất quý có thể được ghi bằng nhiều cách:

- `Quý`
- `Hàng quý`
- `Mỗi quý`
- `Theo quý`
- `3 tháng`
- `Ba tháng`

Parser cần hiểu các biến thể này đều tương ứng với `HangQuy`.

### 8.3. Ảnh hưởng đến nhập báo cáo

Khi User chọn một kỳ báo cáo, hệ thống chỉ hiển thị các chỉ số:

- Được phân công cho khoa/phòng của User.
- Có tần suất phù hợp với loại kỳ báo cáo.
- Đang hoạt động.

Ví dụ:

- Kỳ quý chỉ hiển thị chỉ số có tần suất quý.
- Kỳ 6 tháng chỉ hiển thị chỉ số có tần suất 6 tháng.
- Kỳ năm chỉ hiển thị chỉ số có tần suất năm.

## 9. Phân Công Chỉ Số

### 9.1. Phân công thủ công

Admin có thể chọn:

- Một hoặc nhiều khoa/phòng.
- Một hoặc nhiều chỉ số.

Hệ thống tạo phân công cho từng cặp được chọn.

Ví dụ Admin chọn:

- Khoa/phòng: P. KHTH, P. TCCB.
- Chỉ số: CS001, CS002.

Hệ thống tạo 4 phân công:

- CS001 - P. KHTH.
- CS001 - P. TCCB.
- CS002 - P. KHTH.
- CS002 - P. TCCB.

### 9.2. Đồng bộ từ dữ liệu chỉ số

Admin có thể bấm đồng bộ để hệ thống tự phân công từ trường `ThuThapTongHop`.

Luồng xử lý:

1. Lấy danh sách chỉ số đang hoạt động.
2. Đọc trường `ThuThapTongHop`.
3. Chuẩn hóa text tiếng Việt.
4. So khớp với danh mục khoa/phòng.
5. Tạo phân công còn thiếu.
6. Không tạo trùng nếu phân công đã tồn tại.

### 9.3. Lý do cần đồng bộ

Dữ liệu import có thể chứa nhiều đơn vị phụ trách trong một ô hoặc một dòng. Nếu chỉ map đơn vị đầu tiên, phân công sẽ thiếu. Chức năng đồng bộ giúp sửa các phân công chưa gắn phù hợp sau khi import hoặc sau khi chỉnh sửa danh mục khoa/phòng.

## 10. Kỳ Báo Cáo

Kỳ báo cáo xác định khoảng thời gian User cần nộp số liệu.

Thông tin chính:

- Tên kỳ báo cáo.
- Loại kỳ báo cáo.
- Ngày bắt đầu.
- Ngày kết thúc.
- Hạn nộp.
- Trạng thái.

Trạng thái kỳ báo cáo:

- `Nhap`: đang chuẩn bị.
- `Mo`: mở cho nhập liệu.
- `Khoa`: khóa kỳ báo cáo.

Quy tắc:

- User chỉ nên nhập báo cáo cho kỳ đang phù hợp.
- Hạn nộp được dùng để tính báo cáo quá hạn.
- Kỳ báo cáo liên kết với tần suất của chỉ số để lọc danh sách cần nhập.

## 11. Báo Cáo Chỉ Số

### 11.1. Bản ghi báo cáo

Một báo cáo được xác định bởi:

- Kỳ báo cáo.
- Khoa/phòng.
- Chỉ số.
- Phân công chỉ số.

Nói cách khác, báo cáo là dữ liệu mà một khoa/phòng nộp cho một chỉ số trong một kỳ cụ thể.

### 11.2. Trạng thái báo cáo

Các trạng thái hiện tại:

- `Nhap = 1`: báo cáo đang nháp.
- `DaGui = 2`: User đã gửi.
- `QuaHan = 3`: User đã gửi báo cáo nhưng gửi sau hạn nộp của kỳ báo cáo.
- `DaKhoa = 4`: báo cáo đã khóa.

Lưu ý nghiệp vụ quan trọng:

- Một báo cáo được xem là **đã báo cáo** khi có trạng thái `DaGui`, `QuaHan` hoặc `DaKhoa`.
- `QuaHan` trong bảng `BaoCao` không đại diện cho slot chưa nộp. Nó là báo cáo đã được gửi, nhưng thời điểm gửi trễ hơn `KyBaoCao.HanNop`.
- Trường hợp kỳ đã qua hạn nhưng khoa/phòng chưa gửi chỉ số nào thì được xem là **quá hạn chưa nộp** ở dashboard/thông báo, nhưng chưa có bản ghi `BaoCao` trạng thái `QuaHan`.

### 11.3. Luồng nhập liệu của User

1. User đăng nhập.
2. Vào `Báo cáo của tôi`.
3. Chọn kỳ báo cáo.
4. Hệ thống hiển thị danh sách chỉ số cần nhập.
5. User chọn một chỉ số.
6. User nhập dữ liệu.
7. Hệ thống tính kết quả.
8. User lưu nháp.
9. User kiểm tra lại và gửi.
10. Khi User bấm gửi, hệ thống so sánh thời điểm gửi với `KyBaoCao.HanNop`.
11. Nếu gửi đúng hạn hoặc trước hạn, trạng thái chuyển sang `DaGui`.
12. Nếu gửi sau hạn, trạng thái chuyển sang `QuaHan`.

### 11.4. Luồng xử lý của Admin

1. Admin vào trang báo cáo.
2. Lọc theo kỳ, khoa/phòng hoặc chỉ số.
3. Admin chỉ nhìn thấy các báo cáo đã gửi, gửi trễ hoặc đã khóa (`DaGui`, `QuaHan`, `DaKhoa`), không nhìn thấy báo cáo `Nhap`.
4. Admin xem chi tiết số liệu ở chế độ chỉ đọc.
5. Khóa báo cáo `DaGui` hoặc `QuaHan` nếu đã chốt dữ liệu.
6. Xóa báo cáo nếu cần xử lý dữ liệu sai trong giai đoạn vận hành.
7. Xuất dữ liệu khi cần tổng hợp.

## 12. Công Thức Tính Chỉ Số

Hệ thống hỗ trợ nhiều loại công thức:

- `TyLe`: nhập tử số và mẫu số, tính tỷ lệ.
- `SoLuong`: nhập giá trị số lượng.
- `ThoiGianTrungBinh`: nhập giá trị thời gian trung bình.
- `DiemTrungBinh`: nhập điểm trung bình.
- `GiaTriTrucTiep`: nhập trực tiếp kết quả.
- `TySo`: nhập tử số và mẫu số, tính tỷ số.

Quy tắc:

- Nếu công thức cần tử số/mẫu số, mẫu số không được bằng 0.
- Nếu công thức là giá trị trực tiếp, không yêu cầu tử số/mẫu số.
- Kết quả được lưu vào `BaoCaoChiTiet.KetQua`.
- Nếu có mục tiêu, hệ thống xác định `DatMucTieu`.

## 13. Mục Tiêu Chỉ Số

Mục tiêu chỉ số dùng để đánh giá kết quả đạt hay không đạt.

Thông tin:

- Năm mục tiêu.
- Toán tử so sánh.
- Giá trị mục tiêu.
- Mô tả mục tiêu.

Ví dụ:

- Kết quả >= 90%.
- Kết quả <= 5%.
- Kết quả = 100%.

Quy tắc:

- Không phải chỉ số nào cũng có mục tiêu.
- Nếu chỉ số có mục tiêu và dữ liệu nhập hợp lệ, hệ thống tự đánh giá đạt/không đạt.
- Mục tiêu có thể thay đổi theo năm.

## 14. Dashboard

Dashboard giúp theo dõi tiến độ báo cáo.

### 14.1. Dashboard Admin

Admin thấy:

- Tổng chỉ số.
- Số báo cáo đã gửi.
- Số báo cáo còn thiếu.
- Số báo cáo quá hạn.
- Tiến độ theo khoa/phòng.

Mục tiêu:

- Biết toàn viện đang hoàn thành tới đâu.
- Phát hiện khoa/phòng còn thiếu báo cáo.
- Theo dõi báo cáo quá hạn.

### 14.2. Dashboard User

User thấy:

- Số chỉ số được phân công cho khoa/phòng mình.
- Số báo cáo đã gửi.
- Số còn thiếu.
- Số quá hạn.
- Tiến độ của khoa/phòng mình.
- Cảnh báo ngay trên dashboard về các chỉ số chưa báo cáo, gần đến hạn hoặc đã quá hạn chưa nộp.

Mục tiêu:

- User biết mình còn phải nhập bao nhiêu báo cáo.
- User nhìn thấy ngay các chỉ số cần xử lý sau khi đăng nhập, không phải tự vào từng kỳ để kiểm tra.
- User không nhìn thấy dữ liệu của khoa/phòng khác.

## 15. Thông Báo

Thông báo dùng để nhắc việc hoặc truyền đạt thông tin cho khoa/phòng.

Admin có thể gửi thông báo đến một hoặc nhiều khoa/phòng. User chỉ thấy thông báo gửi đến tài khoản của mình.

Thông tin chính:

- Tiêu đề.
- Nội dung.
- Loại thông báo.
- Người tạo.
- Ngày tạo.
- Người nhận.
- Trạng thái đã đọc.

Quy tắc:

- Chỉ Admin được tạo thông báo.
- User được đánh dấu thông báo là đã đọc.
- User không được gửi thông báo.
- Thông báo tự động được tạo theo kỳ báo cáo: kỳ mở, nhắc hạn, quá hạn chưa nộp và tổng hợp tiến độ cho Admin.
- Hệ thống có cơ chế chống gửi trùng bằng khóa dedup trong bảng `ThongBaoTuDongLog`.
- Khi User bấm vào một thông báo có gắn `KyBaoCaoId`, hệ thống mở trang chi tiết thông báo.
- Với thông báo `QuaHan`, trang chi tiết hiển thị danh sách chỉ số thuộc kỳ đó mà khoa/phòng chưa nộp và đã quá hạn.
- Với thông báo nhắc hạn hoặc kỳ mở, trang chi tiết hiển thị danh sách chỉ số còn thiếu của kỳ tương ứng để User biết cần nhập báo cáo nào.
- Khi User mở trang chi tiết thông báo, thông báo được đánh dấu là đã đọc.

## 16. Import Dữ Liệu

### 16.1. Import khoa/phòng

Mục tiêu: tạo nhanh danh mục khoa/phòng từ Excel.

Yêu cầu:

- Đọc đúng mã khoa/phòng nguồn.
- Đọc đúng tên khoa/phòng.
- Bỏ qua hoặc báo lỗi dòng thiếu dữ liệu bắt buộc.
- Cập nhật hoặc thêm mới theo quy tắc hiện tại của service.

### 16.2. Import nhân viên

Mục tiêu: tạo nhanh danh sách nhân viên.

Yêu cầu:

- Đọc đúng mã nhân viên.
- Gắn đúng khoa/phòng.
- Báo lỗi nếu khoa/phòng không tồn tại.
- Không làm hỏng dữ liệu đã có.

### 16.3. Import chỉ số

Mục tiêu: đưa danh mục chỉ số từ file nghiệp vụ vào hệ thống.

Yêu cầu:

- Đọc được file Excel/Word.
- Nhận diện đúng các trường trong bảng chỉ số.
- Nhận diện tần suất báo cáo.
- Nhận diện nhiều khoa/phòng trong trường thu thập/tổng hợp.
- Nhận diện loại công thức từ tên chỉ số, tử số, mẫu số và phương pháp tính nếu file không khai báo rõ.
- Tự gán đơn vị tính nếu file không có cột `DonViTinh`.
- Lưu chỉ số, mục tiêu, tần suất và phân công.
- Ghi lại lỗi import để Admin kiểm tra.

Quy tắc suy luận `DonViTinh` khi file nguồn không có đơn vị:

- Nhóm `Tỷ lệ`, `Tỷ suất`, `Công suất`, `Hiệu suất`: đơn vị `%`.
- Nhóm `Tỷ số`: dùng đơn vị cụ thể theo tên chỉ số, ví dụ `bác sĩ/giường bệnh`, `điều dưỡng/giường bệnh`, `bác sĩ/điều dưỡng`, `dược sĩ/giường bệnh`, `nhân viên dinh dưỡng/giường bệnh`, `bác sĩ có chứng chỉ/phẫu thuật viên`.
- Nhóm thời gian: `giờ`, `phút` hoặc `ngày`.
- Nhóm số lượng: `người`, `báo cáo`, `ca`, `lượt`, `buồng`, `điểm tiếp nối`, `cầu thang`.
- Chỉ số `Vi tính hóa quản lý trang thiết bị y tế khối nội`: đơn vị `mức độ`.
- Nếu file import có sẵn `DonViTinh`, giá trị trong file được ưu tiên và không bị override.

Lưu ý với file `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx`:

- File này không có cột đơn vị tính riêng, nhưng hệ thống vẫn phải import đủ `DonViTinh`.
- Tên chỉ số có số thứ tự đầu dòng như `8.` hoặc `10.` phải được bỏ số thứ tự trước khi suy luận.
- Chỉ số `Số lượng các điểm tiếp nối vật lý để vận chuyển người bệnh` phải được phân loại là `SoLuong`, không phải `DiemTrungBinh`.
- Sau khi thay đổi logic import, dữ liệu đã import trước đó cần import lại hoặc cập nhật lại để điền đơn vị tính cho các bản ghi cũ.

### 16.4. Vấn đề kỹ thuật của Excel

Excel có thể lược bỏ ô trống trong XML hoặc lưu chuỗi dưới dạng `inlineStr`. Parser đã được xử lý để tránh lệch cột khi gặp các trường hợp này.

## 17. Phân Quyền Giao Diện

Giao diện cần phản ánh đúng quyền người dùng.

### 17.1. Admin

Menu Admin gồm:

- Tổng quan.
- Khoa/phòng.
- Nhân viên.
- Chỉ số.
- Phân công.
- Kỳ báo cáo.
- Báo cáo.
- Thông báo.
- Đổi mật khẩu.
- Đăng xuất.

### 17.2. User

Menu User gồm:

- Tổng quan.
- Báo cáo của tôi.
- Thông báo.
- Đổi mật khẩu.
- Đăng xuất.

User không được thấy các menu Admin-only. Nếu User truy cập trực tiếp URL Admin, controller vẫn trả về lỗi không có quyền.

## 18. Quy Tắc Bảo Mật Và Dữ Liệu

- Không lưu mật khẩu plain text.
- Kiểm tra quyền ở server.
- User chỉ thao tác dữ liệu thuộc khoa/phòng của mình.
- Admin thao tác dữ liệu toàn hệ thống.
- Tên đăng nhập không được trùng.
- User phải có khoa/phòng.
- Báo cáo phải gắn kỳ, khoa/phòng, chỉ số và phân công.
- Không tạo trùng phân công cùng chỉ số và khoa/phòng.
- Không hiển thị chức năng Admin-only cho User.

## 19. Tình Huống Nghiệp Vụ Đặc Biệt

### 19.1. Chỉ số có nhiều khoa/phòng

Nếu một chỉ số có nhiều phòng ban trong trường thu thập/tổng hợp, hệ thống phải tạo nhiều phân công. Đây là yêu cầu quan trọng đã được cập nhật.

### 19.2. Chỉ số có nhiều tần suất

Nếu tần suất ghi là “Mỗi quý, 6 tháng, 12 tháng”, hệ thống phải lưu cả quý, 6 tháng và năm.

### 19.3. File chỉ số không có cột đơn vị tính

Một số file nguồn Word chỉ mô tả tên chỉ số, phương pháp tính, tử số, mẫu số, nguồn số liệu và tần suất nhưng không có cột `Đơn vị tính`. Trong trường hợp này, hệ thống không yêu cầu sửa file Word nguồn mà tự suy luận đơn vị theo quy tắc import.

### 19.4. User không có dữ liệu

Nếu User đăng nhập nhưng không thấy dữ liệu báo cáo, nguyên nhân có thể là:

- Khoa/phòng chưa được phân công chỉ số.
- Chưa tạo kỳ báo cáo phù hợp.
- Tần suất chỉ số không khớp kỳ báo cáo.
- Chỉ số bị ngừng hoạt động.
- Tài khoản User gắn sai khoa/phòng.

### 19.5. User thấy link Admin

Đây là lỗi giao diện phân quyền. Menu đã được cập nhật để phân biệt Admin/User. Controller vẫn là lớp bảo vệ chính.

### 19.6. Báo cáo quá hạn

Trong hệ thống hiện tại cần phân biệt 2 khái niệm:

- **Báo cáo trạng thái `QuaHan`**: đã có bản ghi báo cáo và User đã bấm gửi, nhưng gửi sau `KyBaoCao.HanNop`.
- **Quá hạn chưa nộp**: kỳ báo cáo đã qua hạn nhưng khoa/phòng vẫn chưa có báo cáo ở trạng thái `DaGui`, `QuaHan` hoặc `DaKhoa` cho chỉ số đó. Đây là dữ liệu dùng để cảnh báo dashboard và thông báo tự động, không phải trạng thái của một bản ghi `BaoCao`.

Vì vậy, báo cáo được xem là **đã báo cáo** nếu trạng thái là `DaGui`, `QuaHan` hoặc `DaKhoa`. Chỉ số chưa có một trong ba trạng thái này vẫn được tính là còn thiếu.

## 20. Tiêu Chí Thành Công

Hệ thống được xem là đáp ứng nghiệp vụ khi:

- Admin import được danh mục khoa/phòng, nhân viên và chỉ số.
- Parser nhận diện đúng các biến thể tần suất quý.
- Parser import chỉ số nhận diện đúng loại công thức và đơn vị tính, kể cả file DOCX không có cột `DonViTinh`.
- Chỉ số có nhiều khoa/phòng được phân công đủ.
- User chỉ thấy menu và dữ liệu đúng quyền.
- User nhập được báo cáo theo kỳ.
- Kết quả chỉ số được tính đúng theo công thức.
- Dashboard Admin và User hiển thị đúng phạm vi.
- Các thao tác không đúng quyền bị chặn ở controller.
- Tài liệu dự án phản ánh đúng hiện trạng.

## 21. Rủi Ro Và Lưu Ý

- File nguồn Word/Excel có thể thay đổi format, khiến parser cần cập nhật.
- Tên khoa/phòng trong file nguồn có thể viết tắt hoặc viết không thống nhất.
- Một chỉ số có nhiều tần suất cần kiểm tra kỹ khi tạo kỳ báo cáo.
- Nếu dữ liệu phân công sai, User sẽ không thấy đúng chỉ số cần nhập.
- Nếu tài khoản User gắn sai khoa/phòng, dashboard và báo cáo sẽ sai phạm vi.
- Các thao tác xóa báo cáo nên được kiểm soát chặt khi chuyển sang vận hành thật.

## 22. Hướng Phát Triển (chưa cần)

Các hướng có thể phát triển tiếp:

- Quy trình duyệt/trả lại báo cáo.
- Lý do trả lại báo cáo và lịch sử phản hồi.
- Nhật ký thao tác đầy đủ cho dữ liệu nhạy cảm.
- Phân quyền chi tiết theo chức năng.
- Tích hợp gửi thông báo ra kênh ngoài hệ thống như email, SMS hoặc Zalo.
- Dashboard phân tích xu hướng theo thời gian.
- Export Excel định dạng đẹp hơn.
- API tích hợp dữ liệu từ hệ thống bệnh viện.
- Test tự động cho service và parser.

---

## 23. Các Cải Tiến Nghiệp Vụ Mới Nhất (Ngày 23/05/2026)

Hệ thống đã được bổ sung và nâng cấp toàn diện 3 tính năng nghiệp vụ cốt lõi nhằm tối ưu hóa giao diện quản lý và hạn chế tối đa các sai sót nhập liệu:

### 23.1. Cải tiến toàn diện nghiệp vụ Phân công chỉ số
- **Cấu trúc thống kê động (5 Metric Cards)**: Cung cấp số liệu tổng quan toàn diện về: *Tổng số chỉ số*, *Số chỉ số đã phân công*, *Số chỉ số chưa phân công*, *Tổng số phân công*, và *Số lượng phân công đang tạm dừng*. Card "Chưa phân công" đóng vai trò là điểm nhấn giúp Admin phát hiện ngay các chỉ số bị bỏ quên.
- **Biểu mẫu phân công cải tiến kèm AJAX Preview**: Hỗ trợ việc kéo chọn nhiều chỉ số và nhiều khoa/phòng để tạo phân công hàng loạt. Đi kèm bảng xem trước preview thông minh hiển thị trạng thái `✅ Mới` (sẽ được tạo) và `⚠️ Đã tồn tại` (sẽ bỏ qua) để tránh việc phân công trùng lắp.
- **Xử lý trạng thái Tạm dừng (Deactivate) chuẩn nghiệp vụ**: Sửa lỗi ẩn mất khoa/phòng khi bấm "Tạm dừng" phân công. Giờ đây, chỉ số chỉ được đánh dấu là "Chưa phân công" khi nó hoàn toàn chưa được phân bổ cho bất kỳ khoa/phòng nào. Các phân công bị "Tạm dừng" vẫn sẽ hiển thị bình thường trong bảng phân bổ với trạng thái badge cam rõ ràng và cung cấp hành động **"Kích hoạt"** màu xanh để dễ dàng kích hoạt lại.
- **Thanh thao tác nhanh hàng loạt (Bulk Action Bar)**: Hỗ trợ tích chọn hàng loạt checkbox phân công để Tạm dừng, Kích hoạt hoặc Xóa nhiều bản ghi cùng một lúc thông qua thanh Floating Bar trượt mượt mà.
- **Duy trì bộ lọc và trang (State Preservation)**: Đảm bảo khi Admin bấm Tạm dừng, Kích hoạt, hay Xóa một phân công đơn lẻ, hệ thống vẫn giữ nguyên bộ lọc (Khoa/phòng, Chỉ số, Trạng thái, Từ khóa tìm kiếm), trang phân trang và chế độ xem hiện tại để mang lại trải nghiệm liền mạch.

### 23.2. Ràng buộc & Hướng dẫn nhập liệu chặt chẽ trên Biểu mẫu Tạo/Sửa chỉ số
- **Phân tách phân khu trực quan**: Biểu mẫu `Indicator/Create` và `Indicator/Edit` được quy hoạch thành 4 phân khu chính: *1. Thông tin cơ bản*, *2. Cấu hình báo cáo*, *3. Mô tả định nghĩa & Phương pháp tính*, và *4. Giá trị mục tiêu*.
- **Định danh bắt buộc và tùy chọn (Required vs Optional)**: Các trường bắt buộc nhập được in đậm và đánh dấu bằng dấu sao đỏ `*` kế bên tiêu đề (Mã chỉ số, Tên chỉ số, Tần suất báo cáo, Loại công thức). Các trường tùy chọn được đánh dấu nhãn phụ màu xám `(Không bắt buộc)` ở bên phải để điều hướng admin thuận tiện.
- **Xác thực dữ liệu đa lớp**:
  - *Client-side Validation (Thời gian thực)*: Sử dụng thư viện `jquery.validate.unobtrusive` để tự động kiểm tra và hiển thị ngay thông báo lỗi màu đỏ phía dưới ô nhập khi admin bỏ trống trường bắt buộc mà không cần reload trang.
  - *Server-side Validation (Dữ liệu gửi lên)*: Thêm hộp thoại thông báo màu đỏ Bootstrap nổi bật ở đầu trang liệt kê tất cả các lỗi nếu biểu mẫu gửi lên không hợp lệ. Ràng buộc thêm thuộc tính `SelectedTanSuatBaoCaoValues` phải có ít nhất một giá trị được chọn.

### 23.3. Cơ chế Lọc thông minh Kỳ báo cáo theo Tần suất của Khoa/Phòng (User Level)
- Nhằm tránh trường hợp tài khoản người dùng cấp khoa/phòng (User Role) gặp bối rối hoặc thao tác nhầm khi thấy quá nhiều kỳ báo cáo mở trên toàn viện (Daily, Monthly, Quarterly...) trong khi khoa mình không có chỉ số nào để báo cáo trong các kỳ đó, hệ thống đã bổ sung bộ lọc thông minh:
  - Khi một User thuộc một khoa phòng đăng nhập vào trang Báo cáo, hệ thống sẽ kiểm tra danh sách chỉ số đang được giao hoạt động cho khoa đó.
  - Từ danh sách chỉ số đó, hệ thống trích xuất tất cả các tần suất báo cáo liên quan (kết hợp tần suất mặc định và bảng tần suất chi tiết `dbo.ChiSoTanSuatBaoCao`).
  - Phần hiển thị **"Kỳ báo cáo đang mở"** sẽ được lọc tự động để **chỉ hiển thị những kỳ báo cáo có tần suất tương thích với tần suất chỉ số của khoa đó**. Các kỳ báo cáo có tần suất không liên quan sẽ tự động ẩn đi hoàn toàn.
  - *Ví dụ*: Khoa CNTT chỉ phụ trách các chỉ số có tần suất là *Hàng ngày*, khi họ đăng nhập sẽ chỉ nhìn thấy duy nhất thẻ kỳ báo cáo *Hàng ngày*, các thẻ Hàng tháng, Hàng quý hay 6 tháng sẽ tự động ẩn đi. Admin vẫn sẽ nhìn thấy đầy đủ các kỳ báo cáo để phục vụ mục tiêu giám sát.

## 24. Cập Nhật Quy Trình Báo Cáo Và Thông Báo (Ngày 26/05/2026)

### 24.1. Chốt lại phạm vi nghiệp vụ hiện tại

Dự án hiện tại tập trung vào quản lý chỉ số và tiến độ nộp báo cáo. Vì vậy hệ thống chưa dùng luồng kiểm tra đúng/sai số liệu bởi khoa/phòng hay luồng Admin trả lại báo cáo.

Các trạng thái `DaDuyet` và `TraLai` có thể vẫn tồn tại trong enum/schema để tránh phá dữ liệu cũ, nhưng không còn là luồng thao tác chính trên giao diện và route xử lý duyệt/trả lại đã bị chặn.

### 24.2. Quy tắc hiển thị và thao tác báo cáo

- User được sửa báo cáo khi báo cáo còn `Nhap`.
- Khi User gửi báo cáo, hệ thống tự xác định `DaGui` hoặc `QuaHan` dựa trên hạn nộp.
- Admin không nhìn thấy báo cáo nháp của khoa/phòng.
- Admin chỉ xem báo cáo `DaGui`, `QuaHan`, `DaKhoa`.
- Admin chỉ xem chi tiết, khóa hoặc xóa báo cáo; không sửa số liệu, không gửi thay User, không duyệt và không trả lại.
- Nút trên danh sách báo cáo được điều chỉnh theo vai trò: User có thể `Sửa` khi nháp, còn Admin chỉ `Xem`.

### 24.3. Dashboard và cảnh báo sau đăng nhập

Khi User đăng nhập vào dashboard, hệ thống hiển thị cảnh báo nếu khoa/phòng còn chỉ số chưa báo cáo. Cảnh báo này nêu số chỉ số gần đến hạn và số chỉ số đã quá hạn chưa nộp. Khi mở chi tiết, User thấy danh sách kỳ báo cáo, hạn nộp, mã chỉ số, tên chỉ số và nút nhập báo cáo nhanh.

Dashboard và thống kê dùng quy tắc:

```text
Đã báo cáo = DaGui + QuaHan + DaKhoa
```

Slot chưa nộp sau hạn vẫn là dữ liệu thiếu báo cáo, không tự sinh báo cáo `QuaHan`.

### 24.4. Thông báo tự động và trang chi tiết thông báo

Hệ thống đã bổ sung thông báo tự động nội bộ:

- `KyBaoCaoMo`: kỳ báo cáo đã mở.
- `NhacHan`: nhắc còn 7 ngày, 3 ngày, 1 ngày hoặc đúng ngày hạn.
- `QuaHan`: cảnh báo khoa/phòng còn chỉ số quá hạn chưa nộp.
- `TongHopAdmin`: tổng hợp tiến độ hằng ngày cho Admin.

Thông báo tự động có log chống trùng `ThongBaoTuDongLog`, nên service có thể chạy nhiều lần mà không tạo trùng thông báo.

Khi User click vào một thông báo:

- Hệ thống mở `Notification/Details/{id}`.
- Nếu thông báo có `KyBaoCaoId`, trang chi tiết truy vấn các chỉ số còn thiếu của khoa/phòng trong kỳ đó.
- Nếu là thông báo `QuaHan`, danh sách chỉ hiển thị các chỉ số đã quá hạn chưa nộp.
- Thông báo được tự động đánh dấu đã đọc khi User mở chi tiết.

## 25. Cập Nhật Import Công Thức Và Đơn Vị Tính Chỉ Số (Ngày 28/05/2026)

### 25.1. Vấn đề nghiệp vụ

File `Phân chia các chỉ số dựa theo đơn vị thu thập và tổng hợp.docx` là nguồn dữ liệu quan trọng để khởi tạo danh mục chỉ số. File này có đầy đủ thông tin tên chỉ số, phương pháp tính, tử số, mẫu số, nguồn số liệu, thu thập/tổng hợp và tần suất, nhưng không có nhãn/cột riêng cho `Đơn vị tính`.

Nếu hệ thống không tự suy luận đơn vị, nhiều chỉ số sau import sẽ thiếu `DonViTinh`, làm giảm độ rõ ràng khi User nhập báo cáo và khi Admin kiểm tra cấu hình chỉ số.

### 25.2. Cách xử lý hiện tại

- Không yêu cầu sửa file Word nguồn.
- Không đổi schema vì bảng `ChiSoChatLuong` đã có trường `DonViTinh`.
- Khi import, hệ thống ưu tiên giá trị `DonViTinh` nếu file có sẵn.
- Nếu file không có `DonViTinh`, hệ thống tự suy luận từ tên chỉ số, loại công thức, tử số, mẫu số và phương pháp tính.
- Logic suy luận chạy sau bước suy luận `LoaiCongThuc`.

### 25.3. Kết quả mong đợi

- Import DOCX vẫn gán đủ đơn vị cho 55 chỉ số.
- Các chỉ số tỷ lệ/tỷ suất/công suất/hiệu suất dùng `%`.
- Các chỉ số tỷ số dùng đúng đơn vị tỷ số nghiệp vụ.
- Các chỉ số thời gian dùng đúng `giờ`, `phút`, `ngày`.
- Các chỉ số số lượng dùng đúng `người`, `báo cáo`, `ca`, `lượt`, `buồng`, `điểm tiếp nối`, `cầu thang`.
- Dữ liệu đã import trước khi cập nhật cần import lại hoặc cập nhật bổ sung để được điền `DonViTinh`.

## 26. Cập Nhật Nghiệp Vụ Tạo Lịch Kỳ Báo Cáo Tự Động (Ngày 30/05/2026)

### 26.1. Lý do bổ sung chức năng

Trước đây Admin có thể tạo kỳ báo cáo thủ công, ví dụ tạo một kỳ tháng, một kỳ quý hoặc một kỳ năm. Cách này phù hợp khi số kỳ ít, nhưng không thuận tiện khi bệnh viện cần vận hành báo cáo xuyên suốt cả năm với nhiều tần suất khác nhau.

Chức năng **Tạo lịch tự động** được bổ sung để Admin có thể tạo hàng loạt kỳ báo cáo theo năm. Hệ thống hỗ trợ tạo lịch trước, tự mở kỳ đúng ngày bắt đầu và vẫn đảm bảo User chỉ nhìn thấy kỳ phù hợp với trách nhiệm báo cáo của khoa/phòng mình.

Mô hình được chọn là:

```text
Admin tạo lịch hàng loạt -> Hệ thống tự mở kỳ khi tới ngày -> User nhập/gửi báo cáo khi có kỳ Mở phù hợp.
```

Không chọn mô hình tự sinh toàn bộ báo cáo rỗng cho User vì dễ tạo dữ liệu dư, khó xử lý khi phân công chỉ số thay đổi và làm tăng số dòng trong database không cần thiết.

### 26.2. Đối tượng sử dụng

| Vai trò | Quyền trong chức năng |
|---|---|
| Admin | Tạo lịch tự động, xem preview, tạo các kỳ chưa tồn tại, xem toàn bộ kỳ. |
| User | Không tạo lịch, chỉ thấy kỳ đang Mở và khớp tần suất chỉ số được phân công. |

### 26.3. Luồng nghiệp vụ Admin

1. Admin đăng nhập hệ thống.
2. Admin vào menu **Kỳ báo cáo**.
3. Admin bấm nút **Tạo lịch tự động**.
4. Admin nhập năm cần tạo lịch, ví dụ `2026`.
5. Admin chọn một hoặc nhiều loại kỳ báo cáo.
6. Admin bấm **Xem trước**.
7. Hệ thống hiển thị danh sách kỳ dự kiến, gồm tên kỳ, loại kỳ, ngày mở, ngày đóng, hạn nộp, trạng thái dự kiến và kết quả kiểm tra trùng.
8. Admin kiểm tra các kỳ **Sẽ tạo mới** và **Đã tồn tại**.
9. Admin bấm **Tạo các kỳ chưa tồn tại**.
10. Hệ thống lưu các kỳ mới, bỏ qua kỳ đã tồn tại và quay về danh sách kỳ báo cáo.

### 26.4. Loại kỳ được tạo tự động

| Loại kỳ | Số kỳ dự kiến trong năm đầy đủ | Cách đặt tên | Khoảng thời gian |
|---|---:|---|---|
| Hàng ngày | 365 hoặc 366 | Ngày dd/MM/yyyy | Từ 00:00 đến 23:59 cùng ngày. |
| Hàng tháng | 12 | Tháng MM/yyyy | Từ ngày đầu tháng đến ngày cuối tháng. |
| Hàng quý | 4 | Quý I/yyyy, Quý II/yyyy... | Từ ngày đầu quý đến ngày cuối quý. |
| 6 tháng | 2 | 6 tháng đầu năm yyyy, 6 tháng cuối năm yyyy | 01/01-30/06 và 01/07-31/12. |
| 9 tháng | 1 | 9 tháng năm yyyy | 01/01-30/09. |
| Hàng năm | 1 | Năm yyyy | 01/01-31/12. |

Không tạo tự động cho **Khi phát sinh** và **Trước/sau khi thực hiện** vì hai loại này phụ thuộc sự kiện nghiệp vụ thực tế, không phù hợp để tạo sẵn theo lịch năm.

### 26.5. Quy tắc ngày giờ mở, đóng và hạn nộp

Hệ thống thống nhất cách hiểu như sau:

```text
Ngày bắt đầu kỳ = mở lúc 00:00.
Ngày kết thúc kỳ = đóng lúc 23:59.
Hạn nộp = 23:59 của ngày kết thúc kỳ.
```

Ví dụ chi tiết:

| Trường hợp | Mở kỳ | Đóng kỳ | Hạn nộp cuối cùng |
|---|---|---|---|
| Ngày 30/05/2026 | 30/05/2026 00:00 | 30/05/2026 23:59 | 30/05/2026 23:59 |
| Tháng 06/2026 | 01/06/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| Quý II/2026 | 01/04/2026 00:00 | 30/06/2026 23:59 | 30/06/2026 23:59 |
| 6 tháng cuối năm 2026 | 01/07/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |
| 9 tháng năm 2026 | 01/01/2026 00:00 | 30/09/2026 23:59 | 30/09/2026 23:59 |
| Năm 2026 | 01/01/2026 00:00 | 31/12/2026 23:59 | 31/12/2026 23:59 |

Lưu ý kỹ thuật: database hiện lưu ngày, không lưu giờ cho `KyBaoCao.HanNop`. Vì vậy `23:59` là quy ước nghiệp vụ và quy ước hiển thị. Khi User gửi báo cáo trong đúng ngày hạn nộp, báo cáo vẫn được xem là đúng hạn. Báo cáo chỉ bị đánh `QuaHan` khi ngày gửi lớn hơn ngày hạn nộp.

### 26.6. Quy tắc tạo kỳ khi năm đã đi qua một phần

Khi Admin tạo lịch cho năm hiện tại, hệ thống không tạo lại những kỳ đã kết thúc trước ngày hiện tại, vì các kỳ đó không còn giá trị vận hành.

Ví dụ ngày hiện tại là **30/05/2026**:

| Loại kỳ | Kỳ bị bỏ qua | Kỳ còn được preview |
|---|---|---|
| Hàng ngày | 01/01/2026 đến 29/05/2026 | Từ 30/05/2026 đến 31/12/2026 |
| Hàng tháng | Tháng 01, 02, 03, 04/2026 | Tháng 05 đến Tháng 12/2026 |
| Hàng quý | Quý I/2026 | Quý II, III, IV/2026 |
| 6 tháng | Không bỏ kỳ 6 tháng đầu nếu ngày hiện tại còn trong kỳ | 6 tháng đầu năm, 6 tháng cuối năm |
| 9 tháng | Không bỏ nếu ngày hiện tại trước hoặc bằng 30/09/2026 | 9 tháng năm 2026 |
| Hàng năm | Không bỏ nếu ngày hiện tại trước hoặc bằng 31/12/2026 | Năm 2026 |

Với ví dụ Tháng 05/2026, dù kỳ đã mở từ ngày 01/05/2026, ngày 30/05/2026 vẫn nằm trong kỳ nên kỳ này vẫn được tạo ở trạng thái **Mở** nếu chưa tồn tại.

### 26.7. Quy tắc trạng thái kỳ báo cáo

| Trạng thái code | Hiển thị | Khi nào dùng |
|---|---|---|
| `Nhap` | Nhập | Kỳ tương lai, chưa tới ngày bắt đầu. |
| `Mo` | Mở | Kỳ đã tới ngày bắt đầu, User được nhập và gửi báo cáo. |
| `Khoa` | Khóa | Kỳ đã khóa, User không tiếp tục nhập/sửa báo cáo. |

Khi tạo lịch:

- Kỳ có `TuNgay <= hôm nay` được tạo là **Mở**.
- Kỳ có `TuNgay > hôm nay` được tạo là **Nhập**.
- Kỳ **Nhập** sẽ tự chuyển thành **Mở** khi tới ngày bắt đầu.

Không tự chuyển kỳ sang **Khóa** sau hạn nộp trong giai đoạn này, vì hệ thống vẫn cho User gửi trễ và tự đánh báo cáo là `QuaHan`.

### 26.8. Quy tắc chống trùng kỳ

Một kỳ được xem là trùng nếu có cùng:

```text
Loại kỳ báo cáo + Từ ngày + Đến ngày
```

Ví dụ: đã có `HangThang`, `01/06/2026`, `30/06/2026`. Khi tạo lịch tháng năm 2026 lần nữa, dòng Tháng 06/2026 phải hiện **Đã tồn tại** và hệ thống bỏ qua khi tạo.

Không dùng tên kỳ làm khóa chính để tránh lỗi do đổi cách đặt tên hiển thị.

### 26.9. Quy tắc User nhìn thấy kỳ báo cáo

User không nhìn thấy tất cả kỳ đang có trong hệ thống. User chỉ thấy kỳ khi đồng thời thỏa mãn:

- kỳ đang ở trạng thái **Mở**;
- loại kỳ khớp với tần suất báo cáo của ít nhất một chỉ số được phân công cho khoa/phòng User;
- phân công chỉ số còn hoạt động;
- chỉ số còn hoạt động.

Ví dụ: Khoa A chỉ có chỉ số tần suất **Hàng ngày**. Hệ thống đang mở kỳ Hàng ngày, Hàng tháng và Hàng quý. Khoa A chỉ thấy kỳ Hàng ngày, còn Admin vẫn thấy toàn bộ kỳ để quản lý.

### 26.10. Quy tắc không tạo báo cáo rỗng

Khi Admin tạo lịch tự động, hệ thống chỉ thêm dòng vào bảng `KyBaoCao`. Hệ thống không tạo trước các dòng `BaoCao` theo từng `KhoaPhong + ChiSo + KyBaoCao`.

Lý do:

- tránh phình dữ liệu khi có nhiều khoa/phòng và nhiều chỉ số;
- tránh phải đồng bộ lại nếu Admin thay đổi phân công;
- tránh tạo các bản ghi nháp không có ý nghĩa nghiệp vụ;
- đảm bảo báo cáo chỉ tồn tại khi User thật sự lưu nháp hoặc gửi.

Danh sách “còn thiếu báo cáo” trên Dashboard/Thông báo được tính động, không dựa trên báo cáo rỗng.

### 26.11. Tiêu chí nghiệm thu

| STT | Tiêu chí | Kết quả mong đợi |
|---:|---|---|
| 1 | Admin tạo lịch hàng ngày | Có các kỳ từ hôm nay trở đi, kỳ hôm nay là Mở. |
| 2 | Admin tạo lịch hàng tháng ngày 30/05/2026 | Không tạo Tháng 01-04/2026, có Tháng 05-12/2026. |
| 3 | Admin tạo lịch quý | Sinh đúng các quý còn hiệu lực. |
| 4 | Admin tạo lịch 6 tháng | Sinh đúng kỳ 6 tháng còn hiệu lực. |
| 5 | Admin tạo lịch 9 tháng | Sinh kỳ 01/01-30/09 nếu kỳ chưa kết thúc. |
| 6 | Admin tạo lịch năm | Sinh kỳ 01/01-31/12 nếu kỳ chưa kết thúc. |
| 7 | Chạy tạo lịch lần hai | Không tạo trùng. |
| 8 | Kỳ tương lai | Trạng thái Nhập. |
| 9 | Kỳ tới ngày bắt đầu | Trạng thái Mở. |
| 10 | User vào Báo cáo | Chỉ thấy kỳ Mở khớp tần suất chỉ số được phân công. |
| 11 | User gửi trong ngày hạn nộp | Không bị đánh QuaHan. |
| 12 | User gửi sau ngày hạn nộp | Bị đánh QuaHan. |

### 26.12. Ghi chú vận hành

- Admin nên tạo lịch đầu năm hoặc trước khi bắt đầu triển khai báo cáo chính thức.
- Nếu triển khai giữa năm, hệ thống bỏ qua các kỳ đã kết thúc để tránh tạo kỳ cũ không còn cần nhập.
- Nếu bệnh viện muốn nhập bù dữ liệu các tháng cũ, Admin vẫn có thể dùng chức năng tạo kỳ thủ công hoặc chỉnh quy tắc tạo lịch trong tương lai.
- Hạn nộp mặc định của lịch tự động hiện bằng ngày kết thúc kỳ, hiểu là 23:59 của ngày đó.
- Khi cần thay đổi chính sách hạn nộp, nên thống nhất lại nghiệp vụ trước vì sẽ ảnh hưởng dashboard, thông báo và trạng thái `QuaHan`.

## 27. Cập Nhật Tài Liệu Và Chú Thích Code (Ngày 06/06/2026)

### 27.1. Mục tiêu cập nhật

Đợt cập nhật này chuẩn hóa tài liệu và chú thích code để người đọc dự án dễ hiểu hơn khi kiểm tra, bảo trì hoặc trình bày báo cáo thực tập.

Các file code tự viết đã có chú thích đầu file bằng tiếng Việt có dấu theo mẫu `Mục đích:`. Chú thích này nêu vai trò chính của từng controller, service, model, filter, Razor view hoặc file cấu hình MVC.

### 27.2. Phạm vi chú thích

Chú thích áp dụng cho:

- controller xử lý luồng nghiệp vụ;
- service chứa nghiệp vụ và truy cập dữ liệu;
- entity, enum và view model;
- filter và cấu hình MVC;
- Razor view tự viết trong hệ thống.

Không áp dụng cho thư viện bên thứ ba như Bootstrap, jQuery, Modernizr hoặc file minified, vì các file này không thuộc phần nghiệp vụ của dự án.

### 27.3. Kiểm tra session bằng trình duyệt

Khi kiểm thử đăng nhập, người kiểm thử có thể dùng DevTools để kiểm tra session:

1. Mở ứng dụng local, thường là `https://localhost:44387/`.
2. Nhấn `F12` hoặc `Ctrl + Shift + I`.
3. Vào tab `Application`.
4. Chọn `Storage` > `Cookies` > domain local của ứng dụng.
5. Kiểm tra cookie `ASP.NET_SessionId`.

Cookie này chỉ là mã phiên. Các thông tin nghiệp vụ như `TaiKhoanId`, `LoaiTaiKhoan`, `NhanVienId`, `KhoaPhongId` và `TenKhoaPhong` được lưu ở server-side `Session`, không nằm trong `localStorage` hoặc `sessionStorage`.

Hiện `Web.config` chưa cấu hình timeout session riêng, nên ASP.NET dùng timeout mặc định khoảng 20 phút không hoạt động. Sau khi logout, hệ thống gọi `Session.Clear()` và `Session.Abandon()`, vì vậy truy cập lại trang cần đăng nhập phải quay về màn hình login.
