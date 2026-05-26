import csv

# Define the file path
file_path = r"d:\Hoc_Tap\Thuc_Tap\HospitalQualityDashboard\HospitalQualityDashboard\Tai_Lieu\Mau_Import_Nhan_Vien.csv"

# Sample data
headers = ["MaNhanVien", "HoTen", "GioiTinh", "ChucVu", "Email", "SoDienThoai", "IDKHOAPHONG"]
rows = [
    ["NV001", "Nguyễn Văn A", "Nam", "Bác sĩ", "nva@hospital.vn", "0901234567", "101"],
    ["NV002", "Trần Thị B", "Nữ", "Điều dưỡng", "ttb@hospital.vn", "0912345678", "102"],
    ["NV003", "Lê Văn C", "Nam", "Kỹ thuật viên", "lvc@hospital.vn", "0923456789", "103"],
]

# Write to CSV with UTF-8 with BOM (utf-8-sig) for Microsoft Excel compatibility
with open(file_path, mode='w', encoding='utf-8-sig', newline='') as file:
    writer = csv.writer(file)
    writer.writerow(headers)
    writer.writerows(rows)

print("Created CSV template successfully at:", file_path)
