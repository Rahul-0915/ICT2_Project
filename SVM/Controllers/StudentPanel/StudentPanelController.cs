using Microsoft.AspNetCore.Mvc;

namespace SVM.Controllers.StudentPanel
{
    public class StudentPanelController : Controller
    {
        public IActionResult Student()
        {
            // Login check
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Only Student Allowed
            if (HttpContext.Session.GetString("GroupId") != "3")
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Prevent Back After Logout
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return View();
        }
    }
}