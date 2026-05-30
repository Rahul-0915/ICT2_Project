using Microsoft.AspNetCore.Mvc;

namespace SVM.Controllers.StudentPanel
{
    [LoginCheckFilter] // login check for all page in controller 
    public class StudentPanelController : Controller
    {
        public IActionResult Student()
        {

            return View();
        }
    }
}