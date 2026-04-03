using Microsoft.AspNetCore.Mvc;

namespace SVM.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult AdminPanel()
        {
            return View();
        }
        public IActionResult AdminDashboard()
        {
            return View();
        }
    }
}
