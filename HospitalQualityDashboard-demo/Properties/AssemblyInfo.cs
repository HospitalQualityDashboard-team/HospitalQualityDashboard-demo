// Mục đích: khai báo metadata assembly cho quá trình build ứng dụng .NET Framework.
using System.Reflection;
using System.Runtime.InteropServices;

// Metadata chung của assembly được khai báo bằng các attribute bên dưới.
// Khi đổi tên sản phẩm hoặc thông tin phát hành, chỉ cập nhật các attribute liên quan.
// Tránh đưa thông tin nhạy cảm vào metadata vì có thể xuất hiện trong file build.
[assembly: AssemblyTitle("HospitalQualityDashboardDemo")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("HospitalQualityDashboardDemo")]
[assembly: AssemblyCopyright("Copyright �  2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Không expose type sang COM; chỉ bật lại khi có tích hợp COM rõ ràng.
// Nếu cần truy cập một type từ COM, đặt ComVisible(true) trực tiếp trên type đó.
// Giữ mặc định false để giảm bề mặt tích hợp ngoài ý muốn.
[assembly: ComVisible(false)]

// GUID dùng cho typelib nếu project từng được expose qua COM.
[assembly: Guid("e45e113e-ee91-4265-a1bc-50117c4ca952")]

// Version assembly dùng cho nhận diện bản build và phụ thuộc runtime.
//
//      Phiên bản chính
//      Phiên bản phụ
//      Số build
//      Revision
//
// Có thể chỉ định đủ giá trị hoặc dùng * cho Build/Revision khi cần tự sinh.
// Dùng giá trị cố định để tránh thay đổi version ngoài ý muốn trong bản demo.
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
