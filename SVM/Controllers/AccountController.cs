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
            if (HttpContext.Session.GetString("UserId") != null)
            {
                // Check role and redirect accordingly
                var groupId = HttpContext.Session.GetString("GroupId");
                if (groupId == "1")
                    return RedirectToAction("AdminPanel", "Admin");
                else if (groupId == "3")
                    return RedirectToAction("Student", "StudentPanel");
                else
                    return RedirectToAction("Login");
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
                    // Store basic user info in session
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("FullName", user.FullName ?? user.Username);
                    HttpContext.Session.SetString("GroupId", user.GroupId?.ToString() ?? "");
                    HttpContext.Session.SetString("Email", user.Email ?? "");

                    // ✅ IMPORTANT: Agar student hai (GroupId = 3) toh student details bhi fetch karo
                    if (user.GroupId == 3)
                    {
                        await LoadStudentDataInSession(user.UserId);
                    }

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
                        ViewBag.Error = "Teacher login is supported on the S.V.M Mobile App only.";
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

            // Handle error response
            var errorJson = await response.Content.ReadAsStringAsync();

            try
            {
                var errorObj = JsonSerializer.Deserialize<Dictionary<string, string>>(errorJson);

                if (errorObj != null && errorObj.ContainsKey("error"))
                {
                    ViewBag.Error = errorObj["error"];
                }
                else
                {
                    ViewBag.Error = "Login failed.";
                }
            }
            catch
            {
                ViewBag.Error = "Login failed. Please try again.";
            }

            return View(model);
        }

        // ✅ New method: Load student data into session
        private async Task LoadStudentDataInSession(int userId)
        {
            try
            {
                // Pehle try karo session filter wale endpoint se (current session)
                var response = await _client.GetAsync($"Students/ByUser/{userId}");

                // Agar nahi mila toh bina session filter wale endpoint se try karo
                if (!response.IsSuccessStatusCode)
                {
                    response = await _client.GetAsync($"Students/ByUserNoSession/{userId}");
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var student = JsonSerializer.Deserialize<Student>(json, options);

                    if (student != null)
                    {
                        // Store student info in session
                        HttpContext.Session.SetString("StudentId", student.StudentId.ToString());
                        HttpContext.Session.SetString("StudentName", $"{student.FirstName} {student.LastName}");
                        HttpContext.Session.SetString("StudentPhoto", student.StudentPhoto ?? "");
                        HttpContext.Session.SetString("ClassId", student.ClassId?.ToString() ?? "");
                        HttpContext.Session.SetString("RollNo", student.RollNo?.ToString() ?? "");
                    }
                }
                else
                {
                    // Log but don't throw - student might be new
                    Console.WriteLine($"Student data not found for UserId: {userId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading student data: {ex.Message}");
                // Don't throw - login should still work even if student data not found
            }
        }

        public IActionResult Logout()
        {
            var groupId = HttpContext.Session.GetString("GroupId");

            HttpContext.Session.Clear();
            Response.Cookies.Delete(".AspNetCore.Session");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            if (groupId == "1") // Admin
            {
                return RedirectToAction("Login", "Account");
            }

            // Student aur baki users
            return RedirectToAction("UserHome", "UserHome");
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

            var errorJson = await response.Content.ReadAsStringAsync();
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
                TempData["Success"] = "Password reset successful. Please login with your new password.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = result;
            ViewBag.Identifier = identifier;

            return View();
        }
    }
}