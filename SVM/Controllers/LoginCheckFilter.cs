using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SVM.Controllers
{
    public class LoginCheckFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            var groupId = context.HttpContext.Session.GetString("GroupId");

            var controller =
                context.RouteData.Values["controller"]?.ToString();

            var action =
                context.RouteData.Values["action"]?.ToString();

            // No Cache
            context.HttpContext.Response.Headers["Cache-Control"] =
                "no-cache, no-store, must-revalidate";

            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Expires"] = "0";

            // Login page
            if (controller == "Account" && action == "Login")
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (groupId == "1")
                    {
                        context.Result =
                            new RedirectToActionResult(
                                "AdminDashboard",
                                "Admin",
                                null);
                    }
                    else if (groupId == "3")
                    {
                        context.Result =
                            new RedirectToActionResult(
                                "Index",
                                "Student",
                                null);
                    }
                }

                return;
            }

            // Not Logged In
            if (string.IsNullOrEmpty(userId))
            {
                context.Result =
                    new RedirectToActionResult(
                        "Login",
                        "Account",
                        null);

                return;
            }

            // STUDENT AREA PROTECTION
            if (controller == "Student" && groupId != "3")
            {
                context.HttpContext.Session.Clear();

                context.Result =
                    new RedirectToActionResult(
                        "Login",
                        "Account",
                        null);

                return;
            }

            // ADMIN AREA PROTECTION
            if (controller == "Admin" && groupId != "1")
            {
                context.HttpContext.Session.Clear();

                context.Result =
                    new RedirectToActionResult(
                        "Login",
                        "Account",
                        null);

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}