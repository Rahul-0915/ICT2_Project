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

            // GET: Sections
            public async Task<IActionResult> Index()
            {
                List<Section> sectionList = new List<Section>();

                var response = await _client.GetAsync("Sections");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    sectionList = JsonSerializer.Deserialize<List<Section>>(data, option);
                }
                else
                {
                    ModelState.AddModelError("", "Failed to load sections");
                }

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
                await LoadClasses();
                return View();
            }

            // POST: Sections/Create
            // To protect from overposting attacks, enable the specific properties you want to bind to.
            // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create([Bind("SectionId,SectionName,ClassId")] Section section)
            {
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
                if (id == null)
                {
                    return NotFound();
                }

                var response = await _client.GetAsync($"Sections/{id}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var data = await response.Content.ReadAsStringAsync();

                var section = JsonSerializer.Deserialize<Section>(data, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                await LoadClasses();
                return View(section);
            }

            // POST: Sections/Edit/5
            // To protect from overposting attacks, enable the specific properties you want to bind to.
            // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(int id, [Bind("SectionId,SectionName,ClassId")] Section section)
            {
                if (id != section.SectionId)
                    return NotFound();

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

                    ViewData["ClassId"] = new SelectList(classes, "ClassId", "ClassName");
                }
            }

        }
    }
