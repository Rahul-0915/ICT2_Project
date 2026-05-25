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
        //public async Task<IActionResult> Index()
        //{
        //    List<Class> classList = new List<Class>();
        //    var response = await _client.GetAsync("Classes");
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var data = await response.Content.ReadAsStringAsync();
        //        var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        //        classList = JsonSerializer.Deserialize<List<Class>>(data, option);

        //    }
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        ModelState.AddModelError("", "Failed to load classes");
        //    }
        //    return View(classList);

        //}

        //RAHUL .....INDEX 
        public async Task<IActionResult> Index()
        {
            List<Class> classList = new List<Class>();

            // CLASSES
            var classResponse = await _client.GetAsync("Classes");

            // SECTIONS
            var sectionResponse = await _client.GetAsync("Sections");

            // STUDENTS
            var studentResponse = await _client.GetAsync("Students");

            var option = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // GET CLASS DATA
            if (classResponse.IsSuccessStatusCode)
            {
                var classData = await classResponse.Content.ReadAsStringAsync();
                classList = JsonSerializer.Deserialize<List<Class>>(classData, option);
            }

            // GET SECTION DATA
            List<Section> sections = new();
            if (sectionResponse.IsSuccessStatusCode)
            {
                var sectionData = await sectionResponse.Content.ReadAsStringAsync();
                sections = JsonSerializer.Deserialize<List<Section>>(sectionData, option);
            }

            // GET STUDENT DATA
            List<Student> students = new();
            if (studentResponse.IsSuccessStatusCode)
            {
                var studentData = await studentResponse.Content.ReadAsStringAsync();
                students = JsonSerializer.Deserialize<List<Student>>(studentData, option);
            }

            // MANUAL BINDING
            foreach (var cls in classList)
            {
                // CLASS SECTIONS
                cls.Sections = sections
                    .Where(s => s.ClassId == cls.ClassId)
                    .ToList();

                // CLASS STUDENTS
                cls.Students = students
                    .Where(s => s.ClassId == cls.ClassId)
                    .ToList();

                // SECTION STUDENTS
                foreach (var sec in cls.Sections)
                {
                    sec.Students = students
                        .Where(s => s.SectionId == sec.SectionId)
                        .ToList();
                }
            }

            // ✅ LOAD ALL SESSIONS FOR THE FILTER DROPDOWN
            var sessionResponse = await _client.GetAsync("Sessions");
            if (sessionResponse.IsSuccessStatusCode)
            {
                var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                var allSessions = JsonSerializer.Deserialize<List<Session>>(sessionData, option);
                ViewBag.AllSessions = allSessions;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassId,ClassName,Medium,SessionId")] Class classData)
        {
            // ✅ Duplicate check with SessionId
            if (await IsClassDuplicate(classData.ClassName, classData.Medium, classData.SessionId))
            {
                ModelState.AddModelError("ClassName", $"Class '{classData.ClassName}' with Medium '{classData.Medium}' already exists in this session!");
                ModelState.AddModelError("Medium", "This combination already exists in this session.");
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
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    ModelState.AddModelError("", error?["message"] ?? "Duplicate class detected!");
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClassId,ClassName,Medium,SessionId")] Class classData)
        {
            if (id != classData.ClassId) return NotFound();

            // ✅ Duplicate check with SessionId (excluding current class)
            if (await IsClassDuplicate(classData.ClassName, classData.Medium, classData.SessionId, id))
            {
                ModelState.AddModelError("ClassName", $"Class '{classData.ClassName}' with Medium '{classData.Medium}' already exists in this session!");
                ModelState.AddModelError("Medium", "This combination already exists in this session.");
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
        private async Task<bool> IsClassDuplicate(string className, string medium, int? sessionId, int? excludeId = null)
        {
            // If no session is selected, duplicate doesn't make sense
            if (sessionId == null) return false;

            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classes = JsonSerializer.Deserialize<List<Class>>(data, option);

                return classes.Any(c =>
                    c.ClassName == className &&
                    c.Medium == medium &&
                    c.SessionId == sessionId.Value &&   // Now safe because we checked null
                    (excludeId == null || c.ClassId != excludeId)
                );
            }
            return false;
        }

    }

}