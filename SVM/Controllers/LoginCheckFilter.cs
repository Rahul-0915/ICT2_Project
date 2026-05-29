using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SVM.Controllers
{
    public class LoginCheckFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.Session.GetString("UserId");

            // Current controller/action
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // LOGIN NAHI HAI
            if (string.IsNullOrEmpty(user))
            {
                // Login page allow karo
                if (controller != "Account" || action != "Login")
                {
                    context.Result =
                        new RedirectToActionResult("Login", "Account", null);
                }
            }
            else
            {
                // LOGIN HAI aur fir bhi login page pe ja raha hai
                if (controller == "Account" && action == "Login")
                {
                    context.Result =
                        new RedirectToActionResult("AdminDashboard", "Admin", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}