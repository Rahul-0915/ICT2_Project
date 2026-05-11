using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SVM.Models;

namespace SVM.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _client;

        public AccountController(IHttpClientFactory client)
        {
            _client = client.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // GET: Login Page
        [HttpGet]
        public IActionResult Login()
        {
            // Agar already login hai to Admin panel pe bhejo
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("AdminPanel", "Admin");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // ✅ Call the new login API
            var loginData = new { Username = model.Username, Password = model.Password };
            var response = await _client.PostAsJsonAsync("Users/login", loginData);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var user = JsonSerializer.Deserialize<User>(json, options);

                if (user != null)
                {
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
                    HttpContext.Session.SetString("GroupId", user.GroupId?.ToString() ?? "");
                    return RedirectToAction("AdminPanel", "Admin");
                }
            }

            ViewBag.Error = "Invalid username or password";
            return View(model);
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login");
        }
    }
}