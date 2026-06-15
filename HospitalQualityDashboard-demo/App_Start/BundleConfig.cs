// Mục đích: đăng ký các bundle CSS/JavaScript dùng chung cho ứng dụng MVC.
using System.Web.Optimization;

namespace HospitalQualityDashboardDemo
{
    public class BundleConfig
    {
        // Xem thêm thông tin về bundling tại https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Dùng bản development của Modernizr trong quá trình phát triển; khi chuẩn bị
            // release production, dùng công cụ build tại https://modernizr.com để chọn dùng test cần thiết.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            bundles.Add(new StyleBundle("~/bundles/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/Site.css"));
        }
    }
}
