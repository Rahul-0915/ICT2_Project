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
    [LoginCheckFilter]
    public class ClassesController : Controller
    {
        private readonly HttpClient _client;

        public ClassesController(IHttpClientFactory client)
        {
            _client = client.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string medium = "", string className = "", int? sessionId = null, int? classId = null)
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

            // APPLY FILTERS
            // Filter by session
            if (sessionId.HasValue && sessionId.Value > 0)
            {
                classList = classList.Where(c => c.SessionId == sessionId.Value).ToList();
            }

            // Filter by class name
            if (!string.IsNullOrEmpty(className))
            {
                classList = classList.Where(c => c.ClassName == className).ToList();
            }

            // Filter by medium
            if (!string.IsNullOrEmpty(medium))
            {
                classList = classList.Where(c => c.Medium == medium).ToList();
            }

            // Filter by specific class id
            if (classId.HasValue && classId.Value > 0)
            {
                classList = classList.Where(c => c.ClassId == classId.Value).ToList();
            }

            // LOAD ALL SESSIONS FOR THE FILTER DROPDOWN
            var sessionResponse = await _client.GetAsync("Sessions");
            if (sessionResponse.IsSuccessStatusCode)
            {
                var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                var allSessions = JsonSerializer.Deserialize<List<Session>>(sessionData, option);
                ViewBag.AllSessions = allSessions;

                // ACTIVE SESSION AUTO SELECT
                var activeSession = allSessions?.FirstOrDefault(s => s.IsActive == 1);
                ViewBag.ActiveSessionId = activeSession?.SessionId;

                var classNames = classList
                    .Select(c => c.ClassName)
                    .Distinct()
                    .ToList();

                ViewBag.ClassNames = classNames;

                // MEDIUM DROPDOWN
                var mediums = classList
                    .Select(c => c.Medium)
                    .Distinct()
                    .ToList();

                ViewBag.MediumList = mediums;

                // STORE ALL FILTERS FOR VIEW
                ViewBag.SelectedMedium = medium;
                ViewBag.SelectedClassName = className;
                ViewBag.SelectedSessionId = sessionId;
                ViewBag.SelectedClassId = classId;
            }

            return View(classList);
        }

        // GET: Classes/Details/5
        public async Task<IActionResult> Details(int? id, string medium = "", string className = "", int? sessionId = null)
        {
            // Store filters for Back button
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassName = className;
            ViewBag.SelectedSessionId = sessionId;

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
        public async Task<IActionResult> Create(string medium = "", string className = "", int? sessionId = null)
        {
            // Store filters for Back button
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassName = className;
            ViewBag.SelectedSessionId = sessionId;

            await LoadSessions();
            LoadMediums();

            var response = await _client.GetAsync("Classes");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var classList = JsonSerializer.Deserialize<List<Class>>(data, option);

                ViewBag.ClassNames = classList?
                    .Select(x => x.ClassName)
                    .Distinct()
                    .ToList();
            }
            else
            {
                ViewBag.ClassNames = new List<string>();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClassId,ClassName,Medium,SessionId")] Class classData)
        {
            // Duplicate check with SessionId
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
                    // Get the newly created class to get its ID
                    var createdClass = await response.Content.ReadFromJsonAsync<Class>();

                    // Redirect with filters including the new class ID
                    return RedirectToAction(nameof(Index), new
                    {
                        medium = classData.Medium,
                        className = classData.ClassName,
                        sessionId = classData.SessionId,
                        classId = createdClass?.ClassId  // Pass the new class ID
                    });
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
        public async Task<IActionResult> Edit(int? id, string medium = "", string className = "", int? sessionId = null)
        {
            // Store filters for Back button
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassName = className;
            ViewBag.SelectedSessionId = sessionId;

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
            var classData = JsonSerializer.Deserialize<Class>(data,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            await LoadSessions();
            LoadMediums();

            var classResponse = await _client.GetAsync("Classes");

            if (classResponse.IsSuccessStatusCode)
            {
                var classDataList = await classResponse.Content.ReadAsStringAsync();
                var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var classList = JsonSerializer.Deserialize<List<Class>>(classDataList, option);

                ViewBag.ClassNames = classList?
                    .Select(x => x.ClassName)
                    .Distinct()
                    .ToList();
            }
            else
            {
                ViewBag.ClassNames = new List<string>();
            }

            return View(classData);
        }

        // POST: Classes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClassId,ClassName,Medium,SessionId")] Class classData)
        {
            if (id != classData.ClassId) return NotFound();

            // Duplicate check with SessionId 
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
                    // Pass classId also in redirect
                    return RedirectToAction(nameof(Index), new
                    {
                        medium = classData.Medium,
                        className = classData.ClassName,
                        sessionId = classData.SessionId,
                        classId = id 
                    });
                }
                ModelState.AddModelError("", "Update failed!");
            }
            await LoadSessions();
            LoadMediums();
            return View(classData);
        }

        // GET: Classes/Delete/5
        public async Task<IActionResult> Delete(int? id, string medium = "", string className = "", int? sessionId = null)
        {
            // Store filters for Back button
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassName = className;
            ViewBag.SelectedSessionId = sessionId;

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
        public async Task<IActionResult> DeleteConfirmed(int id, string medium = "", string className = "", int? sessionId = null)
        {
            var response = await _client.DeleteAsync($"Classes/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Delete failed!");
            }

            // Redirect with filters
            return RedirectToAction(nameof(Index), new
            {
                medium = medium,
                className = className,
                sessionId = sessionId
            });
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
                    c.SessionId == sessionId.Value &&
                    (excludeId == null || c.ClassId != excludeId)
                );
            }
            return false;
        }
    }
}