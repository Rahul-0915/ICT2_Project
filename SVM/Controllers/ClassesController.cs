using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SVM.Models;

namespace SVM.Controllers
{
    public class ClassesController : Controller
    {

        private readonly HttpClient _client;
        public ClassesController(IHttpClientFactory client)
        {
            _client = client.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");


        }

        // GET: Classes
        public async Task<IActionResult> Index()
        {
            List<Class> classList = new List<Class>();
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                classList = JsonSerializer.Deserialize<List<Class>>(data, option);

            }
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to load classes");
            }
            return View(classList);

        }

        // GET: Classes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var response = await _client.GetAsync($"Classes/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }
            var data = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var classData = JsonSerializer.Deserialize<Class>(data, option);
            return View(classData);
        }

        // GET: Classes/Create
        public async Task<IActionResult> Create()
        {
            await LoadSessions();
            LoadMediums();
            return View();
        }

        // POST: Classes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassId,ClassName,Medium,SessionId")] Class classData)
        {
            // ✅ Duplicate check before saving
            if (await IsClassDuplicate(classData.ClassName, classData.Medium))
            {
                ModelState.AddModelError("ClassName", $"Class '{classData.ClassName}' with Medium '{classData.Medium}' already exists!");
                ModelState.AddModelError("Medium", "This combination already exists.");
                await LoadSessions();
                LoadMediums();
                return View(classData);
            }

            if (ModelState.IsValid)
            {
                var response = await _client.PostAsJsonAsync("Classes", classData);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                // Check if API returned duplicate error
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorData = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", "Duplicate class detected!");
                }
                else
                {
                    ModelState.AddModelError("", "Create failed!");
                }
            }
            await LoadSessions();
            LoadMediums();
            return View(classData);
        }

        // GET: Classes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var response = await _client.GetAsync($"Classes/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }
            var data = await response.Content.ReadAsStringAsync();
            var classData = JsonSerializer.Deserialize<Class>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            await LoadSessions();
            LoadMediums();
            return View(classData);
        }

        // POST: Classes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClassId,ClassName,Medium,SessionId")] Class classData)
        {
            if (id != classData.ClassId)
            {
                return NotFound();
            }

            // ✅ Duplicate check while editing (excluding current class)
            if (await IsClassDuplicate(classData.ClassName, classData.Medium, id))
            {
                ModelState.AddModelError("ClassName", $"Class '{classData.ClassName}' with Medium '{classData.Medium}' already exists!");
                ModelState.AddModelError("Medium", "This combination already exists.");
                await LoadSessions();
                LoadMediums();
                return View(classData);
            }

            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"Classes/{id}", classData);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Update failed!");
            }
            await LoadSessions();
            LoadMediums();
            return View(classData);
        }

        // GET: Classes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var response = await _client.GetAsync($"Classes/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }
            var data = await response.Content.ReadAsStringAsync();
            var classData = JsonSerializer.Deserialize<Class>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(classData);
        }

        // POST: Classes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"Classes/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Delete failed!");
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ClassExists(int id)
        {
            var response = await _client.GetAsync($"Classes/{id}");
            return response.IsSuccessStatusCode;
        }


        private async Task LoadSessions()
        {
            var response = await _client.GetAsync("Sessions");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sessions = JsonSerializer.Deserialize<List<Session>>(data, option);
                ViewData["SessionId"] = new SelectList(sessions, "SessionId", "SessionName");
            }
        }
        private void LoadMediums()
        {
            var mediums = new List<SelectListItem>
            {
                new SelectListItem { Value = "Gujarati", Text = "Gujarati" },
                new SelectListItem { Value = "English", Text = "English" }
            };
            ViewBag.MediumList = mediums;
        }
        private async Task<bool> IsClassDuplicate(string className, string medium, int? excludeId = null)
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classes = JsonSerializer.Deserialize<List<Class>>(data, option);

                return classes.Any(c =>
                    c.ClassName == className &&
                    c.Medium == medium &&
                    (excludeId == null || c.ClassId != excludeId)
                );
            }
            return false;
        }

    }

}