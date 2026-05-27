using Microsoft.AspNetCore.Mvc;

namespace SVM.Controllers.UserControllers
{
    public class UserHomeController : Controller
    {
        public IActionResult UserHome()
        {
            ViewBag.ShowPreloader = true;
            return View();
        }
        public IActionResult About()
        {
            ViewBag.ShowPreloader = false;
            return View();
        }


    }
}
