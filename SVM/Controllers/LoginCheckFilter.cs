using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SVM.Controllers
{
    public class LoginCheckFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            var userId = session.GetString("UserId");
            var groupId = session.GetString("GroupId");

            var controller =
                context.RouteData.Values["controller"]?.ToString() ?? "";

            var action =
                context.RouteData.Values["action"]?.ToString() ?? "";

            // No Cache
            context.HttpContext.Response.Headers["Cache-Control"] =
                "no-cache, no-store, must-revalidate";

            context.HttpContext.Response.Headers["Pragma"] =
                "no-cache";

            context.HttpContext.Response.Headers["Expires"] =
                "0";

            // =========================
            // LOGIN PAGE
            // =========================

            if (controller == "Account" &&
                action == "Login")
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    if (groupId == "1")
                    {
                        context.Result =
                            new RedirectToActionResult(
                                "AdminPanel",
                                "Admin",
                                null);

                        return;
                    }

                    if (groupId == "3")
                    {
                        context.Result =
                            new RedirectToActionResult(
                                "Student",
                                "StudentPanel",
                                null);

                        return;
                    }
                }

                return;
            }

            // =========================
            // NOT LOGGED IN
            // =========================

            if (string.IsNullOrEmpty(userId))
            {
                context.Result =
                    new RedirectToActionResult(
                        "Login",
                        "Account",
                        null);

                return;
            }

            // =========================
            // STUDENT PANEL PROTECTION
            // =========================

            if (controller == "StudentPanel" &&
                action == "Student")
            {
                if (groupId != "3")
                {
                    session.Clear();

                    context.Result =
                        new RedirectToActionResult(
                            "Login",
                            "Account",
                            null);

                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}