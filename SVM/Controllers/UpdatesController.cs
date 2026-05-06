using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using SVM.Models;

namespace SVM.Controllers
{
    public class UpdatesController : Controller
    {
        private readonly HttpClient _client;

        public UpdatesController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // GET: Updates
        public async Task<IActionResult> Index()
        {
            var response = await _client.GetAsync("Updates");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updates = JsonSerializer.Deserialize<List<Updates>>(data, options);
                return View(updates);
            }
            return View(new List<Updates>());
        }

        // GET: Updates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Updates/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var updates = JsonSerializer.Deserialize<Updates>(data, options);
            return View(updates);
        }

        // GET: Updates/Create
        public IActionResult Create()
        {
            LoadCategoriesAndStatus(); // optional if you want dynamic
            return View();
        }

        // POST: Updates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Updates updates, IFormFile? UploadFile)
        {
            // File handling (exactly like StudentsController)
            if (UploadFile != null && UploadFile.Length > 0)
            {
                if (UploadFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("UploadFile", "File size must be less than 5 MB");
                }
                else
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadFile.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "updates");
                    Directory.CreateDirectory(uploadPath);
                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadFile.CopyToAsync(stream);
                    }
                    updates.FilePath = $"/images/updates/{fileName}";
                }
            }
            else
            {
                ModelState.AddModelError("UploadFile", "Please upload a file (image or PDF)");
            }

            // Remove validation for auto-set fields (if any)
            ModelState.Remove("CreatedAt");
            ModelState.Remove("FilePath");

            if (ModelState.IsValid)
            {
                updates.CreatedAt = DateTime.Now;
                var response = await _client.PostAsJsonAsync("Updates", updates);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Update created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Failed to create: {error}");
                }
            }

            LoadCategoriesAndStatus();
            return View(updates);
        }

        // GET: Updates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Updates/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var updates = JsonSerializer.Deserialize<Updates>(data, options);

            LoadCategoriesAndStatus(updates.Category, updates.Status);
            return View(updates);
        }

        // POST: Updates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Updates updates, IFormFile? UploadFile)
        {
            if (id != updates.Id) return NotFound();

            // Handle new file upload if provided
            if (UploadFile != null && UploadFile.Length > 0)
            {
                if (UploadFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("UploadFile", "File size must be less than 5 MB");
                }
                else
                {
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(updates.FilePath))
                    {
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", updates.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    // Save new file
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadFile.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "updates");
                    Directory.CreateDirectory(uploadPath);
                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadFile.CopyToAsync(stream);
                    }
                    updates.FilePath = $"/images/updates/{fileName}";
                }
            }

            // Remove validation for fields that should not be re-validated
            ModelState.Remove("CreatedAt");
            ModelState.Remove("FilePath");

            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"Updates/{id}", updates);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Update updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Update failed!");
            }

            LoadCategoriesAndStatus(updates.Category, updates.Status);
            return View(updates);
        }

        // GET: Updates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Updates/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var updates = JsonSerializer.Deserialize<Updates>(data, options);
            return View(updates);
        }

        // POST: Updates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // First get the update to delete the physical file
            var response = await _client.GetAsync($"Updates/{id}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var updates = JsonSerializer.Deserialize<Updates>(data, options);

                if (!string.IsNullOrEmpty(updates?.FilePath))
                {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", updates.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }

            var deleteResponse = await _client.DeleteAsync($"Updates/{id}");
            if (deleteResponse.IsSuccessStatusCode)
                TempData["Success"] = "Update deleted successfully!";
            else
                TempData["Error"] = "Delete failed!";

            return RedirectToAction(nameof(Index));
        }

        // Helper: Load static dropdowns (can be extended from DB later)
        private void LoadCategoriesAndStatus(string selectedCategory = null, int? selectedStatus = null)
        {
            var categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Select Category --" },
                new SelectListItem { Value = "notice", Text = "Notice" },
                new SelectListItem { Value = "event", Text = "Event" }
            };
            ViewBag.CategoryList = new SelectList(categories, "Value", "Text", selectedCategory);

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Active" },
                new SelectListItem { Value = "0", Text = "Inactive" }
            };
            ViewBag.StatusList = new SelectList(statuses, "Value", "Text", selectedStatus.HasValue ? selectedStatus.Value.ToString() : null);
        }
    }
}