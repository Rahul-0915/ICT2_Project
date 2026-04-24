using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SVM.Models;

namespace SVM.Controllers
{
    public class UsersController : Controller
    {
        private readonly HttpClient _client;

        public UsersController(IHttpClientFactory client)
        {
            _client = client.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/"); // Your API base URL
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            List<User> users = new List<User>();

            var response = await _client.GetAsync("Users");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                users = JsonSerializer.Deserialize<List<User>>(data, options);
            }
            else
            {
                ModelState.AddModelError("", "Failed to load users");
            }

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var response = await _client.GetAsync($"Users/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var user = JsonSerializer.Deserialize<User>(data, options);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            await LoadGroups();
            return View();
        }

        public async Task<IActionResult> CreateWithGroup(int groupId)
        {
            await LoadGroups();
            ViewBag.FixedGroupId = groupId;
            ViewBag.GroupName = groupId == 1 ? "Admin" : "User";
            return View("Create");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, int? fixedGroupId, IFormFile? ImageFile)
        {
            // Remove navigation properties from validation
            ModelState.Remove("Group");
            ModelState.Remove("Staff");
            ModelState.Remove("Students");
            ModelState.Remove("GroupId");

            // Agar fixedGroupId hai to use karo
            if (fixedGroupId.HasValue)
            {
                user.GroupId = fixedGroupId.Value;
            }

            if (fixedGroupId.HasValue && user.GroupId == fixedGroupId.Value)
            {
                ModelState.Remove("GroupId");
            }

            // Handle image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Create unique filename
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);

                // Save to wwwroot/images/users
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "users");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Save relative path to database
                user.ProfilePhoto = $"/images/users/{fileName}";
            }

            if (ModelState.IsValid)
            {
                var response = await _client.PostAsJsonAsync("Users", user);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "User created successfully!";
                    return RedirectToAction("AdminPanel", "Admin");
                }
                else
                {
                    var errorData = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Create failed! {errorData}");
                }
            }

            await LoadGroups();
            return View(user);
        }
        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var response = await _client.GetAsync($"Users/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var user = JsonSerializer.Deserialize<User>(data, options);

            if (user == null)
                return NotFound();

            await LoadGroups();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.UserId)
                return NotFound();

            // Remove navigation properties from validation
            ModelState.Remove("Group");
            ModelState.Remove("Staff");
            ModelState.Remove("Students");

            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"Users/{id}", user);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "User updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorData = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Update failed! {errorData}");
                }
            }

            await LoadGroups();
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var response = await _client.GetAsync($"Users/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var user = JsonSerializer.Deserialize<User>(data, options);

            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"Users/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "User deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorData = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Delete failed! {errorData}");
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to load groups for dropdown
        private async Task LoadGroups()
        {
            var response = await _client.GetAsync("Groupmasters");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var groups = JsonSerializer.Deserialize<List<Groupmaster>>(data, options);

                ViewData["GroupId"] = new SelectList(groups, "GId", "GName");
            }
            else
            {
                ViewData["GroupId"] = new SelectList(new List<Groupmaster>(), "GId", "GName");
            }
        }
    }
}