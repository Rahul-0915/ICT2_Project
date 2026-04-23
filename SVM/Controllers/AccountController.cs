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

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get all users from API
            var response = await _client.GetAsync("Users");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var users = JsonSerializer.Deserialize<List<User>>(data, option);

                // Check username and password
                var user = users.FirstOrDefault(u =>
                    u.Username == model.Username &&
                    u.Password == model.Password);

                if (user != null)
                {
                    // Store in Session
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
                    HttpContext.Session.SetString("GroupId", user.GroupId?.ToString() ?? "");

                    // Redirect to Admin Panel
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