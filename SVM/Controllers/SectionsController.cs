using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;


namespace SVM.Controllers
{
    public class SectionsController : Controller
    {
        private readonly HttpClient _client;

        public SectionsController(IHttpClientFactory client)
        {
            _client = client.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // GET: Sections – ONLY ONE Index method
        public async Task<IActionResult> Index(string medium = "")
        {
            List<Section> sectionList = new List<Section>();

            var response = await _client.GetAsync("Sections");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                sectionList = JsonSerializer.Deserialize<List<Section>>(data, option);

                // Load Class details for each section (to get Medium and ClassName)
                foreach (var section in sectionList)
                {
                    if (section.ClassId.HasValue)
                    {
                        var classResponse = await _client.GetAsync($"Classes/{section.ClassId}");
                        if (classResponse.IsSuccessStatusCode)
                        {
                            var classData = await classResponse.Content.ReadAsStringAsync();
                            section.Class = JsonSerializer.Deserialize<Class>(classData, option);
                        }
                    }
                }

                // Filter by medium if selected
                if (!string.IsNullOrEmpty(medium))
                {
                    sectionList = sectionList.Where(s => s.Class?.Medium == medium).ToList();
                }
            }
            else
            {
                ModelState.AddModelError("", "Failed to load sections");
            }

            ViewBag.SelectedMedium = medium;
            return View(sectionList);
        }


        // GET: Sections/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var response = await _client.GetAsync($"Sections/{id}");

            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();

            var section = JsonSerializer.Deserialize<Section>(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(section);

        }

        // GET: Sections/Create
        public async Task<IActionResult> Create()
        {
            // Class dropdown initially empty – will be populated by JS after medium selection
            ViewData["ClassId"] = new SelectList(new List<Class>(), "ClassId", "ClassName");   // ← only ClassName
            return View();
        }

        // POST: Sections/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Sections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SectionId,SectionName,ClassId")] Section section)
        {
            // ✅ Duplicate check - Same Section under same Class
            if (await IsSectionDuplicate(section.SectionName, section.ClassId))
            {
                ModelState.AddModelError("SectionName", $"⚠️ Section '{section.SectionName}' already exists in this Class!");
                await LoadClasses();
                return View(section);
            }

            if (!ModelState.IsValid)
            {
                await LoadClasses();
                return View(section);
            }

            var response = await _client.PostAsJsonAsync("Sections", section);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Create failed!");
            await LoadClasses();
            return View(section);
        }


        // GET: Sections/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Sections/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var section = JsonSerializer.Deserialize<Section>(data, option);

            // Load current class details to get its medium
            if (section.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{section.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classData = await classResponse.Content.ReadAsStringAsync();
                    var currentClass = JsonSerializer.Deserialize<Class>(classData, option);
                    ViewBag.CurrentMedium = currentClass?.Medium;
                    ViewBag.CurrentClassId = section.ClassId;
                }
            }

            // Do NOT populate ViewData["ClassId"] here – will be filled by JS
            return View(section);
        }
        // POST: Sections/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Sections/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SectionId,SectionName,ClassId")] Section section)
        {
            if (id != section.SectionId)
                return NotFound();

            // ✅ Duplicate check while editing (excluding current section)
            if (await IsSectionDuplicate(section.SectionName, section.ClassId, id))
            {
                ModelState.AddModelError("SectionName", $"⚠️ Section '{section.SectionName}' already exists in this Class!");
                await LoadClasses();
                return View(section);
            }

            if (!ModelState.IsValid)
            {
                await LoadClasses();
                return View(section);
            }

            var response = await _client.PutAsJsonAsync($"Sections/{id}", section);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Update failed!");
            await LoadClasses();
            return View(section);
        }


        // GET: Sections/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Sections/{id}");

            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();

            var section = JsonSerializer.Deserialize<Section>(data, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(section);
        }

        // POST: Sections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"Sections/{id}");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Delete failed!");
            }

            return RedirectToAction(nameof(Index));
        }
        private async Task LoadClasses()
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classes = JsonSerializer.Deserialize<List<Class>>(data, option);

                ViewData["ClassId"] = new SelectList(classes, "ClassId", "ClassName");   // ← only ClassName
            }
        }
        // Add this method in SectionsController
        private async Task<bool> IsSectionDuplicate(string sectionName, int? classId, int? excludeId = null)
        {
            var response = await _client.GetAsync("Sections");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sections = JsonSerializer.Deserialize<List<Section>>(data, option);

                return sections.Any(s =>
                    s.SectionName == sectionName &&
                    s.ClassId == classId &&
                    (excludeId == null || s.SectionId != excludeId)
                );
            }
            return false;
        }
        [HttpGet]
        public async Task<JsonResult> GetClassesByMedium(string medium)
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allClasses = JsonSerializer.Deserialize<List<Class>>(data, option);
                var filteredClasses = allClasses.Where(c => c.Medium == medium)
                                                .Select(c => new { value = c.ClassId, text = c.ClassName })   // ← only ClassName
                                                .ToList();
                return Json(filteredClasses);
            }
            return Json(new List<object>());
        }

    }
}
