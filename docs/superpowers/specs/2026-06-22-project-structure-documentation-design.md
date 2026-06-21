# Project Structure Documentation Design

## Muc tieu

Tao `PROJECT_STRUCTURE.md` bang tieng Viet de nguoi moi co the hieu cau truc va
vai tro cua tung thanh phan trong du an HospitalQualityDashboard-demo ma khong
phai doc toan bo ma nguon truoc.

## Pham vi

Tai lieu phai bao phu:

- kien truc tong the ASP.NET MVC 4 va luong xu ly request;
- chuc nang cua tung thu muc trong repository va web project;
- tung file code do du an tu viet trong Controller, Model, DTO, ViewModel,
  Service, Razor View, JavaScript, SQL va PowerShell;
- cac file khoi dong, cau hinh, project metadata va tai lieu nghiep vu quan
  trong;
- quan he su dung chinh giua Controller, Service, DTO/ViewModel, View va database.

Bootstrap, jQuery, Modernizr va cac file minified/source map cua thu vien ben
thu ba chi duoc mo ta theo nhom. Khong phan tich noi dung noi bo cua thu vien.
File secret, file sinh khi build va du lieu local khong duoc dua vao danh sach
file code.

## Cau truc tai lieu

`PROJECT_STRUCTURE.md` gom cac phan:

1. Muc dich va doi tuong doc.
2. Tong quan kien truc va luong du lieu.
3. Cay thu muc cap cao.
4. Mo ta chi tiet theo tung thu muc.
5. Cac luong nghiep vu xuyen module.
6. Ban do phu thuoc va huong dan tim noi can sua.
7. Quy uoc cap nhat tai lieu khi them, doi ten hoac xoa file.

## Mau mo ta

Moi thu muc co:

- chuc nang;
- loai file chua ben trong;
- vi tri cua no trong kien truc;
- cac thu muc hoac lop phu thuoc chinh.

Moi file tu viet co:

- duong dan va ten file;
- chuc nang chinh;
- class, DTO, view model, action, script hoac migration chinh;
- du lieu dau vao va ket qua dau ra khi thong tin nay co y nghia;
- noi su dung va quan he voi cac file khac;
- luu y bao mat, phan quyen, transaction hoac gioi han nghiep vu neu co.

Vi du:

```md
### Models/DTOs

Chua cac doi tuong truyen du lieu giua Controller, Service va ViewModel.

#### AssignmentDtos.cs

- Chua du lieu truyen tai cua nghiep vu phan cong chi so.
- Duoc Controller dung de nhan du lieu va Service dung de truy van/cap nhat.
- Lien quan truc tiep den AssignmentController va AssignmentService.
```

## Cac file Markdown duoc cap nhat

- Tao `PROJECT_STRUCTURE.md` tai thu muc goc repository.
- Cap nhat `README.md` de them lien ket va huong dan su dung tai lieu.
- Cap nhat `HospitalQualityDashboard-demo/implementation-notes.md` de ghi nhan
  lan bo sung tai lieu cau truc.

## Tieu chi hoan thanh

- Moi file code tu viet dang ton tai trong project deu co mot muc mo ta.
- Khong con tham chieu den ten file Service cu da bi tach hoac di chuyen.
- Ten folder, file, class va luong nghiep vu khop voi ma nguon hien tai.
- Cac lien ket Markdown hoat dong tu vi tri file chua lien ket.
- Tai lieu co muc luc, de tim kiem va khong lap lai noi dung lich su trong
  `implementation-notes.md`.
