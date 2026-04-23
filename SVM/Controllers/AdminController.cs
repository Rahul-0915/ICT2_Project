using Microsoft.AspNetCore.Mvc;

namespace SVM.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult AdminPanel()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Cache Disable
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            return View();
        }

        public IActionResult AdminDashboard()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Cache Disable
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return View();
        }
    }
}