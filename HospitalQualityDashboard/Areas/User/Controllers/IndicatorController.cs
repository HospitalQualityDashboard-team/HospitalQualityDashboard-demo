using System.Web.Mvc;
using HospitalQualityDashboard.Models.ViewModels;
using HospitalQualityDashboard.Services;

namespace HospitalQualityDashboard.Areas.User.Controllers
{
    public class IndicatorController : UserBaseController
    {
        private readonly IndicatorService _service = new IndicatorService();

        public ActionResult Index()
        {
            return View(new ChiSoIndexViewModel { Items = _service.GetAll(includeInactive: false, filterKhoaPhongId: CurrentKhoaPhongId) });
        }

        public ActionResult Details(int id)
        {
            var model = _service.Get(id);
            if (model == null)
            {
                return HttpNotFound();
            }

            if (!CurrentKhoaPhongId.HasValue || !_service.IsAssigned(id, CurrentKhoaPhongId.Value))
            {
                return new HttpUnauthorizedResult("Ban khong co quyen xem chi tiet chi so nay.");
            }

            return View(model);
        }
    }
}
