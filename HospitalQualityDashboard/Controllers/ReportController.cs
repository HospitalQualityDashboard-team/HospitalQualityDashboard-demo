// Muc dich: dieu huong qua trinh bao cao theo vai tro sang Area tuong ung.
using HospitalQualityDashboard.Models.ViewModels;
using System.Web.Mvc;

namespace HospitalQualityDashboard.Controllers
{
    public class ReportController : PageController
    {
        public ActionResult Index(int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Index", "Report", new { area = "Admin", kyBaoCaoId, khoaPhongId, chiSoChatLuongId });
            }
            else
            {
                return RedirectToAction("Index", "Report", new { area = "User", kyBaoCaoId, chiSoChatLuongId });
            }
        }

        public ActionResult Nhap(int kyBaoCaoId)
        {
            return RedirectToAction("Nhap", "Report", new { area = "User", kyBaoCaoId });
        }

        public ActionResult Edit(int? id, int? kyBaoCaoId, int? khoaPhongId, int? chiSoChatLuongId)
        {
            if (IsAdmin)
            {
                return RedirectToAction("Edit", "Report", new { area = "Admin", id });
            }
            else
            {
                return RedirectToAction("Edit", "Report", new { area = "User", id, kyBaoCaoId, chiSoChatLuongId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ReportEntryViewModel model)
        {
            return RedirectToAction("Edit", "Report", new { area = "User" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Submit(int id)
        {
            return RedirectToAction("Submit", "Report", new { area = "User", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            return new HttpStatusCodeResult(410, "Quy trình duyệt báo cáo hiện không được sử dụng.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string yKienPhanHoi)
        {
            return new HttpStatusCodeResult(410, "Quy trình duyệt báo cáo hiện không được sử dụng.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Lock", "Report", new { area = "Admin", id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var admin = RequireAdmin();
            if (admin != null) return admin;
            return RedirectToAction("Delete", "Report", new { area = "Admin", id });
        }
    }
}
