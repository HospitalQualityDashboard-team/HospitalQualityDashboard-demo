# Badge thông báo chưa đọc cho User

## Mục tiêu

Khi User nhận thông báo, mục `Thông báo` trên sidebar hiển thị số lượng thông báo chưa đọc. User có thể đánh dấu từng thông báo đã đọc ngay tại danh sách mà không cần mở trang chi tiết.

## Phạm vi

- Áp dụng cho sidebar và danh sách thông báo trong khu vực User.
- Không hiển thị badge cho Admin.
- Không thay đổi quy trình Admin tạo hoặc gửi thông báo.
- Giữ nguyên thao tác đánh dấu đã đọc tại trang chi tiết.

## Quy tắc đếm

- Chỉ đếm các dòng `ThongBaoNguoiNhan` thuộc tài khoản User hiện tại và có `DaDoc = 0`.
- Không tải danh sách thông báo để đếm; dịch vụ dùng truy vấn `COUNT` riêng.
- Mỗi request trang thuộc khu vực User lấy lại số lượng để phản ánh thông báo mới hoặc thao tác vừa hoàn tất.
- Badge hiển thị số nguyên chính xác và tự ẩn khi số lượng bằng `0`.
- Tài khoản không được xem số lượng của User khác.

## Giao diện sidebar

- Thay link chữ đơn hiện tại bằng link có hai phần: nhãn `Thông báo` và badge số chưa đọc.
- Link giữ nguyên trạng thái active, hover, focus và vùng bấm trên toàn chiều rộng sidebar.
- Badge dùng màu nổi bật, kích thước gọn và có nhãn hỗ trợ đọc màn hình.
- Khi không có thông báo chưa đọc, layout không render badge.

## Đánh dấu đã đọc tại danh sách

- Mỗi dòng chưa đọc hiển thị nút `Đánh dấu đã đọc` bên cạnh nút `Xem chi tiết`.
- Dòng đã đọc không hiển thị nút đánh dấu.
- Nút gửi `POST`, kèm anti-forgery token và ID thông báo.
- Controller chỉ cập nhật dòng nhận thông báo thuộc tài khoản đang đăng nhập; không cho phép đánh dấu thay tài khoản khác.
- Sau khi hoàn tất, controller quay lại đúng trang danh sách đã gửi lên. Số trang nhỏ hơn `1` được chuẩn hóa thành `1`.
- Khi trang tải lại, trạng thái dòng đổi thành `Đã đọc`, nút đánh dấu biến mất và badge giảm một đơn vị.

## Kiến trúc và luồng dữ liệu

1. `NotificationService` cung cấp phương thức đếm thông báo chưa đọc theo `accountId` bằng `COUNT` trên bảng người nhận.
2. `UserBaseController` gọi phương thức đếm sau khi xác thực User và gán kết quả vào `ViewBag` cho layout dùng chung.
3. Layout chỉ render badge trong nhánh menu User khi giá trị lớn hơn `0`.
4. Danh sách thông báo render form đánh dấu đã đọc cho từng item có `DaDoc = false`.
5. `NotificationController.MarkAsRead` nhận `id` và `page`, gọi dịch vụ hiện có rồi redirect về `Index` với đúng `page`.

## Xử lý lỗi và an toàn

- Nếu thông báo không thuộc tài khoản hiện tại, câu lệnh cập nhật hiện có không thay đổi bản ghi nào.
- Endpoint tiếp tục yêu cầu đăng nhập User, `POST` và anti-forgery token.
- Không lưu số chưa đọc trong Session để tránh badge cũ khi Admin vừa gửi thông báo.
- Các trang và thông báo cũ tiếp tục hoạt động; thay đổi không yêu cầu migration database.

## Kiểm thử

- Dịch vụ đếm đúng các thông báo chưa đọc của một tài khoản và loại trừ thông báo đã đọc.
- Truy vấn đếm luôn ràng buộc `TaiKhoanId` và `DaDoc = 0`.
- Layout User có badge khi số lượng lớn hơn `0`, ẩn badge khi bằng `0`, và Admin không nhận badge này.
- Danh sách chỉ render nút `Đánh dấu đã đọc` cho item chưa đọc.
- Form có anti-forgery token và gửi đúng ID thông báo.
- Sau thao tác, controller redirect về đúng trang đã gửi lên.
- Build C# và Razor với `MvcBuildViews=true` thành công.
