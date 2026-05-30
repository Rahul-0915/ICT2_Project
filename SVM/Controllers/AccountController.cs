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
            if (!ModelState.IsValid)
                return View(model);

            var loginData = new
            {
                Username = model.Username,
                Password = model.Password
            };

            var response = await _client.PostAsJsonAsync("Users/login", loginData);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var user = JsonSerializer.Deserialize<User>(json, options);

                if (user != null)
                {
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
                    HttpContext.Session.SetString("GroupId", user.GroupId?.ToString() ?? "");

                    // ROLE BASED LOGIN
                    if (user.GroupId == 1)
                    {
                        return RedirectToAction("AdminPanel", "Admin");
                    }
                    else if (user.GroupId == 3)
                    {
                        return RedirectToAction("Student", "StudentPanel");
                    }
                    else if (user.GroupId == 2)
                    {
                        HttpContext.Session.Clear();
                        ViewBag.Error = "Teacher login is supported on the S.V.M Mobile App only.   ";
                        return View(model);
                    }
                    else
                    {
                        HttpContext.Session.Clear();
                        ViewBag.Error = "Invalid role.";
                        return View(model);
                    }
                }
            }

            ViewBag.Error = "Invalid username/email or password";
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
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string identifier)
        {
            var response = await _client.PostAsJsonAsync("Users/forgot-password", new
            {
                Identifier = identifier
            });

            if (response.IsSuccessStatusCode)
            {
                TempData["Identifier"] = identifier;
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "User not found or email not registered";
            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword()
        {
            ViewBag.Identifier = TempData["Identifier"]?.ToString();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string identifier, string otp, string newPassword)
        {
            Console.WriteLine("IDENTIFIER = " + identifier);
            Console.WriteLine("OTP = " + otp);
            Console.WriteLine("NEW PASSWORD = " + newPassword);

            var response = await _client.PostAsJsonAsync("Users/reset-password", new
            {
                Identifier = identifier,
                OTP = otp,
                NewPassword = newPassword
            });

            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Password reset successful";
                return RedirectToAction("Login");
            }

            ViewBag.Error = result;
            ViewBag.Identifier = identifier;

            return View();
        }
    }
}