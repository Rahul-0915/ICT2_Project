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
            }

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to load subjects");
            }

            return View(subjectList);
        }

        // GET: Subjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var response = await _client.GetAsync($"Subjects/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var data = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var subjectData = JsonSerializer.Deserialize<Subject>(data, option);

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
            if (id == null)
            {
                return NotFound();
            }

            var response = await _client.GetAsync($"Subjects/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

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
            if (id != subject.SubjectId)
            {
                return NotFound();
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
            if (id == null)
            {
                return NotFound();
            }

            var response = await _client.GetAsync($"Subjects/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var data = await response.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var subjectData = JsonSerializer.Deserialize<Subject>(data, option);

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

        private async Task<bool> SubjectExists(int id)
        {
            var response = await _client.GetAsync($"Subjects/{id}");
            return response.IsSuccessStatusCode;
        }

        private async Task LoadClasses(int? selectedClassId = null)
        {
            var response = await _client.GetAsync("Classes");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classes = JsonSerializer.Deserialize<List<Class>>(data, option);
                ViewData["ClassId"] = new SelectList(classes, "ClassId", "ClassName", selectedClassId);
            }
            else
            {
                // Handle error - create empty select list to avoid null reference
                ViewData["ClassId"] = new SelectList(new List<Class>(), "ClassId", "ClassName", selectedClassId);
                ModelState.AddModelError("", "Unable to load classes");
            }
        }
    }
}