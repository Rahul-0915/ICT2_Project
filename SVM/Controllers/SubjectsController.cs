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
    public class SubjectsController : Controller
    {
        private readonly HttpClient _client;

        public SubjectsController(IHttpClientFactory client)
        {
            _client = client.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // GET: Subjects
        public async Task<IActionResult> Index()
        {
            List<Subject> subjectList = new List<Subject>();
            var response = await _client.GetAsync("Subjects");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                subjectList = JsonSerializer.Deserialize<List<Subject>>(data, option);

                // Manually load Class for each subject
                foreach (var subject in subjectList)
                {
                    if (subject.ClassId.HasValue)
                    {
                        var classResponse = await _client.GetAsync($"Classes/{subject.ClassId}");
                        if (classResponse.IsSuccessStatusCode)
                        {
                            var classData = await classResponse.Content.ReadAsStringAsync();
                            subject.Class = JsonSerializer.Deserialize<Class>(classData, option);
                        }
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Failed to load subjects");
            }

            return View(subjectList);
        }

        // GET: Subjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Subjects/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var subjectData = JsonSerializer.Deserialize<Subject>(data, option);

            // Load Class details with Medium
            if (subjectData.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{subjectData.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classData = await classResponse.Content.ReadAsStringAsync();
                    subjectData.Class = JsonSerializer.Deserialize<Class>(classData, option);
                }
            }

            return View(subjectData);
        }

        // GET: Subjects/Create
        public async Task<IActionResult> Create()
        {
            await LoadClasses();
            return View();
        }

        // POST: Subjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SubjectId,SubjectName,ClassId")] Subject subject)
        {
            // Duplicate check
            if (await IsSubjectDuplicate(subject.SubjectName, subject.ClassId))
            {
                ModelState.AddModelError("SubjectName", $"⚠️ Subject '{subject.SubjectName}' already exists in this Class!");
                await LoadClasses(subject.ClassId);
                return View(subject);
            }

            if (ModelState.IsValid)
            {
                var response = await _client.PostAsJsonAsync("Subjects", subject);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Create failed!");
            }

            await LoadClasses(subject.ClassId);
            return View(subject);
        }

        // GET: Subjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Subjects/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var subjectData = JsonSerializer.Deserialize<Subject>(data, option);

            await LoadClasses(subjectData.ClassId);
            return View(subjectData);
        }

        // POST: Subjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SubjectId,SubjectName,ClassId")] Subject subject)
        {
            if (id != subject.SubjectId) return NotFound();

            // Duplicate check while editing
            if (await IsSubjectDuplicate(subject.SubjectName, subject.ClassId, id))
            {
                ModelState.AddModelError("SubjectName", $"⚠️ Subject '{subject.SubjectName}' already exists in this Class!");
                await LoadClasses(subject.ClassId);
                return View(subject);
            }

            if (!ModelState.IsValid)
            {
                await LoadClasses(subject.ClassId);
                return View(subject);
            }

            var response = await _client.PutAsJsonAsync($"Subjects/{id}", subject);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Update failed!");
            await LoadClasses(subject.ClassId);
            return View(subject);
        }

        // GET: Subjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Subjects/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var subjectData = JsonSerializer.Deserialize<Subject>(data, option);

            // Load Class details with Medium
            if (subjectData.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{subjectData.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classData = await classResponse.Content.ReadAsStringAsync();
                    subjectData.Class = JsonSerializer.Deserialize<Class>(classData, option);
                }
            }

            return View(subjectData);
        }
        // POST: Subjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"Subjects/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Delete failed!");
            }
            return RedirectToAction(nameof(Index));
        }

        // Duplicate Check Method
        private async Task<bool> IsSubjectDuplicate(string subjectName, int? classId, int? excludeId = null)
        {
            var response = await _client.GetAsync("Subjects");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var subjects = JsonSerializer.Deserialize<List<Subject>>(data, option);

                return subjects.Any(s =>
                    s.SubjectName == subjectName &&
                    s.ClassId == classId &&
                    (excludeId == null || s.SubjectId != excludeId)
                );
            }
            return false;
        }

        private async Task<bool> SubjectExists(int id)
        {
            var response = await _client.GetAsync($"Subjects/{id}");
            return response.IsSuccessStatusCode;
        }

        // Updated LoadClasses - Show ClassName with Medium
        private async Task LoadClasses(int? selectedClassId = null)
        {
            var response = await _client.GetAsync("Classes");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classes = JsonSerializer.Deserialize<List<Class>>(data, option);

                // Format: "6 - Gujarati" or "6 - English"
                var classList = classes.Select(c => new SelectListItem
                {
                    Value = c.ClassId.ToString(),
                    Text = $"{c.ClassName} - {c.Medium}"
                }).ToList();

                ViewData["ClassId"] = new SelectList(classList, "Value", "Text", selectedClassId?.ToString());
            }
            else
            {
                ViewData["ClassId"] = new SelectList(new List<Class>(), "ClassId", "ClassName", selectedClassId);
                ModelState.AddModelError("", "Unable to load classes");
            }
        }
    }
}