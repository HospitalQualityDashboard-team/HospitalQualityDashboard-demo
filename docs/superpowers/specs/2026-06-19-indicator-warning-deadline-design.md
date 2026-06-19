# Cảnh báo hạn nộp theo chỉ số

## Mục tiêu

Khi Admin bấm `Cảnh báo` cho một chỉ số chưa nộp, hệ thống tạo thông báo cho các User thuộc khoa/phòng phụ trách. Thông báo phải nêu mã chỉ số, tên chỉ số, số ngày trước hoặc sau hạn nộp và có màu thể hiện mức độ khẩn cấp.

## Phạm vi

- Chỉ thay đổi cảnh báo thủ công được gửi từ Dashboard Admin cho từng chỉ số.
- Giữ nguyên người nhận, liên kết đến chỉ số cần nhập và quy tắc mỗi chỉ số chỉ được cảnh báo tối đa một lần trong một ngày.
- Không thay đổi nội dung hoặc lịch chạy của các thông báo tự động khác.

## Quy tắc tính ngày

- Dùng ngày hiện tại theo múi giờ Việt Nam tại thời điểm Admin bấm cảnh báo.
- So sánh phần ngày của thời điểm cảnh báo với phần ngày của `HanNop`; không dùng giờ và phút để làm tròn.
- `X` luôn là số nguyên không âm trong nội dung hiển thị.
- Trước hạn: `X = HanNop.Date - ngayCanhBao.Date`.
- Quá hạn: `X = ngayCanhBao.Date - HanNop.Date`.

## Nội dung thông báo

Tiêu đề chung:

`Cảnh báo chỉ số {Mã chỉ số} - {Tên chỉ số}`

Nội dung theo trạng thái:

| Trạng thái | Điều kiện | Nội dung |
| --- | --- | --- |
| Sắp đến hạn | Ngày cảnh báo trước ngày hạn nộp | `Chỉ số {Mã} - {Tên}: Còn X ngày đến hạn nộp. Vui lòng chuẩn bị và nộp đúng hạn.` |
| Hạn nộp hôm nay | Ngày cảnh báo bằng ngày hạn nộp | `Chỉ số {Mã} - {Tên}: Hôm nay là hạn nộp. Vui lòng nộp trước khi hết ngày.` |
| Quá hạn | Ngày cảnh báo sau ngày hạn nộp | `Chỉ số {Mã} - {Tên}: Đã quá hạn nộp X ngày. Vui lòng cập nhật ngay để tránh ảnh hưởng đến tiến độ.` |

## Phân loại và giao diện

- Sắp đến hạn: màu vàng.
- Hạn nộp hôm nay: màu cam.
- Quá hạn: màu đỏ.
- Trạng thái được xác định và lưu khi Admin gửi cảnh báo để nội dung và màu của thông báo cũ luôn nhất quán.
- Danh sách thông báo User và trang chi tiết thông báo cùng hiển thị màu tương ứng. Màu áp dụng cho dấu hiệu trạng thái và vùng nhấn của thông báo, không làm giảm độ tương phản của nội dung.
- Cảnh báo quá hạn dùng loại `QuaHan` hiện có. Cảnh báo trước hạn dùng loại `NhacHan` hiện có. Bổ sung loại riêng cho hạn nộp hôm nay để phân biệt màu cam mà không cần tính lại trạng thái khi hiển thị.

## Luồng dữ liệu

1. Admin gửi `kyBaoCaoId`, `khoaPhongId` và `chiSoChatLuongId` từ Dashboard.
2. Dịch vụ kiểm tra kỳ đang mở, chỉ số còn được phân công và chưa có báo cáo hợp lệ.
3. Dịch vụ tính chênh lệch ngày từ `HanNop` và thời gian Việt Nam được controller truyền vào.
4. Dịch vụ tạo tiêu đề, nội dung và loại thông báo theo một trong ba trạng thái.
5. Thông báo được liên kết với kỳ và chỉ số, sau đó gửi đến các tài khoản User thuộc khoa/phòng.
6. User thấy nội dung và màu tương ứng trong danh sách; khi mở chi tiết vẫn có thể đi đến màn hình nhập đúng chỉ số.

## Xử lý lỗi và tính tương thích

- Nếu chỉ số đã nộp, không còn được phân công hoặc kỳ không còn mở, giữ nguyên kết quả từ chối cảnh báo hiện tại.
- Nếu cảnh báo của cùng kỳ, khoa/phòng và chỉ số đã được gửi trong ngày, không tạo thông báo trùng.
- Các bản ghi thông báo cũ và các loại thông báo khác vẫn hiển thị theo hành vi hiện tại.
- Giá trị enum mới được thêm ở cuối để không thay đổi các giá trị đã lưu trong database.

## Kiểm thử

- Trước hạn nhiều ngày: nội dung có đúng `X`, mã và tên chỉ số; loại và màu là nhắc hạn/vàng.
- Đúng ngày hạn nộp: nội dung không chứa `0 ngày`; loại và màu là hạn hôm nay/cam.
- Sau hạn nhiều ngày: nội dung có đúng số ngày quá hạn; loại và màu là quá hạn/đỏ.
- Chênh lệch giờ trong cùng một ngày không làm thay đổi trạng thái hoặc `X`.
- Cảnh báo vẫn đến đúng User của khoa/phòng và mở đúng chỉ số.
- Quy tắc chống gửi trùng trong ngày và các trường hợp từ chối hiện tại vẫn hoạt động.
