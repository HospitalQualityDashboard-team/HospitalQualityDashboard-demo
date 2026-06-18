# Thiet ke bo sung chu thich cho ma nguon backend

## Muc tieu

Bo sung chu thich tieng Viet ngan gon cho ma nguon backend tu viet cua du an de nguoi bao tri nhanh chong hieu trach nhiem cua tung thanh phan va cac quy tac nghiep vu khong hien nhien. Viec nay khong thay doi hanh vi chuong trinh.

## Pham vi

- Bao gom cac tep `.cs` do du an tu viet trong `Controllers/`, `Areas/Admin/Controllers/`, `Areas/User/Controllers/`, `Models/` va `Services/`.
- Khong sua `App_Start/`, `Properties/`, `Global.asax.cs`, Razor view, CSS, JavaScript, SQL, PowerShell, thu vien ben thu ba, tep minify, source map, cau hinh hoac tai lieu nghiep vu.
- Ton trong cac thay doi chua commit dang co va khong hoan tac noi dung cua nguoi dung.

## Quy tac chu thich

- Them mot dong mo ta muc dich o dau tep khi tep chua co mo ta phu hop.
- Them chu thich cho class hoac nhom class khi ten goi chua truyen dat day du vai tro.
- Them chu thich `// ...` bang tieng Viet truoc moi method va constructor co than lenh, bao gom cac muc truy cap `public`, `private`, `protected` va `internal`.
- Neu method co attribute, dat chu thich truoc ca cum attribute de phan mo ta gan voi toan bo action.
- Noi dung chu thich phai mo ta muc dich cua method; khong chi lap lai nguyen ten method.
- Chu thich tai cac doan co quy tac nghiep vu, phan quyen, chuyen trang thai, cache, import/export, xu ly thoi gian, truy van SQL phuc tap hoac rang buoc du lieu kho suy ra.
- Khong chu thich cac lenh gan bien, constructor don gian, CRUD truc tiep hay doan ma da tu mo ta ro rang.
- Khong thay doi chu ky API, cau truc truy van, ten bien, dinh dang du lieu hoac luong dieu khien.
- Dung tieng Viet co dau trong cac tep hien dang dung UTF-8; giu nguyen encoding hien tai cua tep.

## Trinh tu thuc hien

1. Lap danh sach tep backend tu viet va danh gia muc chu thich hien co.
2. Xu ly cac tep C# theo nhom: model/DTO, controller va service.
3. Quet lai khai bao method/constructor de xac nhan moi thanh pham co chu thich lien ket ngay phia tren.
4. Ra soat diff de loai bo chu thich du thua va xac nhan khong co thay doi hanh vi.
5. Build project va chay cac kiem tra tinh phu hop khong can ung dung/database neu co.

## Tieu chi hoan thanh

- Tat ca tep backend tu viet trong pham vi da duoc xem xet.
- Tat ca method va constructor co than lenh trong pham vi deu co chu thich tieng Viet mo ta muc dich.
- Moi tep can giai thich co chu thich phu hop; tep tu mo ta ro rang khong bi chen chu thich vo ich.
- Diff chi chua chu thich va tai lieu quy trinh lien quan.
- Project build thanh cong, hoac moi tro ngai build do moi truong duoc ghi ro.
