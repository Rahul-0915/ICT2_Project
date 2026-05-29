using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SVM.Models;

namespace SVM.Controllers
{
    [LoginCheckFilter]
    public class TeacherSubjectsController : Controller
    {
        private readonly HttpClient _client;

        public TeacherSubjectsController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // GET: TeacherSubjects
        public async Task<IActionResult> Index(
     int? sessionId,
     string? medium,
     int? classId)
        {
            List<TeacherSubject> teacherSubjects = new();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // ================= ACTIVE SESSION =================

            var sessRes = await _client.GetAsync("Sessions");

            List<Session> sessions = new();

            if (sessRes.IsSuccessStatusCode)
            {
                var sessData = await sessRes.Content.ReadAsStringAsync();

                sessions = JsonSerializer.Deserialize<List<Session>>(sessData, options);

                if (!sessionId.HasValue)
                {
                    var activeSession =
                        sessions.FirstOrDefault(x => x.IsActive == 1);

                    if (activeSession != null)
                    {
                        sessionId = activeSession.SessionId;
                    }
                }
            }

            ViewBag.SessionId =
                new SelectList(sessions, "SessionId", "SessionName", sessionId);

            // ================= MEDIUM =================

            ViewBag.Mediums = new List<string>
    {
        "Gujarati",
        "English"
    };

            // ================= CLASS =================

            List<Class> classList = new();

            var classRes = await _client.GetAsync("Classes");

            if (classRes.IsSuccessStatusCode)
            {
                var classData = await classRes.Content.ReadAsStringAsync();

                var allClasses =
                    JsonSerializer.Deserialize<List<Class>>(classData, options);

                classList = allClasses;

                if (sessionId.HasValue)
                {
                    classList = classList
                        .Where(x => x.SessionId == sessionId)
                        .ToList();
                }

                if (!string.IsNullOrEmpty(medium))
                {
                    classList = classList
                        .Where(x => x.Medium == medium)
                        .ToList();
                }
            }

            ViewBag.ClassId =
                new SelectList(classList, "ClassId", "ClassName", classId);

            // ================= DATA =================

            var response = await _client.GetAsync("TeacherSubjects");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();

                teacherSubjects =
                    JsonSerializer.Deserialize<List<TeacherSubject>>(data, options);

                foreach (var ts in teacherSubjects)
                {
                    if (ts.StaffId.HasValue)
                    {
                        var staffRes =
                            await _client.GetAsync($"Staffs/{ts.StaffId}");

                        if (staffRes.IsSuccessStatusCode)
                        {
                            var staffData =
                                await staffRes.Content.ReadAsStringAsync();

                            ts.Staff =
                                JsonSerializer.Deserialize<Staff>(staffData, options);
                        }
                    }

                    if (ts.SubjectId.HasValue)
                    {
                        var subRes =
                            await _client.GetAsync($"Subjects/{ts.SubjectId}");

                        if (subRes.IsSuccessStatusCode)
                        {
                            var subData =
                                await subRes.Content.ReadAsStringAsync();

                            ts.Subject =
                                JsonSerializer.Deserialize<Subject>(subData, options);
                        }
                    }

                    if (ts.ClassId.HasValue)
                    {
                        var cRes =
                            await _client.GetAsync($"Classes/{ts.ClassId}");

                        if (cRes.IsSuccessStatusCode)
                        {
                            var cData =
                                await cRes.Content.ReadAsStringAsync();

                            ts.Class =
                                JsonSerializer.Deserialize<Class>(cData, options);
                        }
                    }

                    if (ts.SessionId.HasValue)
                    {
                        var sRes =
                            await _client.GetAsync($"Sessions/{ts.SessionId}");

                        if (sRes.IsSuccessStatusCode)
                        {
                            var sData =
                                await sRes.Content.ReadAsStringAsync();

                            ts.Session =
                                JsonSerializer.Deserialize<Session>(sData, options);
                        }
                    }
                }

                // ================= FILTER =================

                if (sessionId.HasValue)
                {
                    teacherSubjects = teacherSubjects
                        .Where(x => x.SessionId == sessionId)
                        .ToList();
                }

                if (!string.IsNullOrEmpty(medium))
                {
                    teacherSubjects = teacherSubjects
                        .Where(x => x.Class?.Medium == medium)
                        .ToList();
                }

                if (classId.HasValue)
                {
                    teacherSubjects = teacherSubjects
                        .Where(x => x.ClassId == classId)
                        .ToList();
                }
            }

            ViewBag.SelectedSessionId = sessionId;
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;

            return View(teacherSubjects);
        }

        // GET: TeacherSubjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"TeacherSubjects/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var teacherSubject = JsonSerializer.Deserialize<TeacherSubject>(data, options);

            if (teacherSubject.StaffId.HasValue)
            {
                var staffRes = await _client.GetAsync($"Staffs/{teacherSubject.StaffId}");
                if (staffRes.IsSuccessStatusCode)
                {
                    var staffData = await staffRes.Content.ReadAsStringAsync();
                    teacherSubject.Staff = JsonSerializer.Deserialize<Staff>(staffData, options);
                }
            }
            if (teacherSubject.SubjectId.HasValue)
            {
                var subRes = await _client.GetAsync($"Subjects/{teacherSubject.SubjectId}");
                if (subRes.IsSuccessStatusCode)
                {
                    var subData = await subRes.Content.ReadAsStringAsync();
                    teacherSubject.Subject = JsonSerializer.Deserialize<Subject>(subData, options);
                }
            }
            if (teacherSubject.ClassId.HasValue)
            {
                var classRes = await _client.GetAsync($"Classes/{teacherSubject.ClassId}");
                if (classRes.IsSuccessStatusCode)
                {
                    var classData = await classRes.Content.ReadAsStringAsync();
                    teacherSubject.Class = JsonSerializer.Deserialize<Class>(classData, options);
                }
            }
            if (teacherSubject.SessionId.HasValue)
            {
                var sessRes = await _client.GetAsync($"Sessions/{teacherSubject.SessionId}");
                if (sessRes.IsSuccessStatusCode)
                {
                    var sessData = await sessRes.Content.ReadAsStringAsync();
                    teacherSubject.Session = JsonSerializer.Deserialize<Session>(sessData, options);
                }
            }

            return View(teacherSubject);
        }

        // GET: TeacherSubjects/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            var res = await _client.GetAsync("Sessions");
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var sessions = JsonSerializer.Deserialize<List<Session>>(
                await res.Content.ReadAsStringAsync(), option);

            var activeSession = sessions.FirstOrDefault(x => x.IsActive == 1);

            ViewBag.SessionId = new SelectList(sessions, "SessionId", "SessionName", activeSession?.SessionId);

            return View();
        }

        // POST: TeacherSubjects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StaffId,SubjectId,ClassId,SessionId")] TeacherSubject teacherSubject)
        {
            if (await IsDuplicate(teacherSubject.StaffId, teacherSubject.SubjectId, teacherSubject.ClassId, teacherSubject.SessionId))
            {
                ModelState.AddModelError("", "⚠️ This teacher is already assigned the same subject, class & session!");
                await LoadDropdowns(teacherSubject);
                return View(teacherSubject);
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(teacherSubject);
                return View(teacherSubject);
            }

            var response = await _client.PostAsJsonAsync("TeacherSubjects", teacherSubject);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Create failed!");
            await LoadDropdowns(teacherSubject);
            return View(teacherSubject);
        }

        // GET: TeacherSubjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var response =
                await _client.GetAsync($"TeacherSubjects/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data =
                await response.Content.ReadAsStringAsync();

            var options =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

            var teacherSubject =
                JsonSerializer.Deserialize<TeacherSubject>(data, options);

            // ================= LOAD CLASS =================

            if (teacherSubject.ClassId.HasValue)
            {
                var classRes =
                    await _client.GetAsync($"Classes/{teacherSubject.ClassId}");

                if (classRes.IsSuccessStatusCode)
                {
                    var classData =
                        await classRes.Content.ReadAsStringAsync();

                    teacherSubject.Class =
                        JsonSerializer.Deserialize<Class>(classData, options);
                }
            }

            // ================= STAFF =================

            var staffRes = await _client.GetAsync("Staffs");

            if (staffRes.IsSuccessStatusCode)
            {
                var staffData =
                    await staffRes.Content.ReadAsStringAsync();

                var allStaff =
                    JsonSerializer.Deserialize<List<Staff>>(staffData, options);

                var teachers =
                    allStaff.Where(x =>
                        x.Designation != null &&
                        x.Designation.Equals("Teacher",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                ViewBag.StaffId =
                    new SelectList(
                        teachers,
                        "StaffId",
                        "FirstName",
                        teacherSubject.StaffId);
            }

            // ================= SESSION =================

            var sessRes =
                await _client.GetAsync("Sessions");

            if (sessRes.IsSuccessStatusCode)
            {
                var sessData =
                    await sessRes.Content.ReadAsStringAsync();

                var sessions =
                    JsonSerializer.Deserialize<List<Session>>(sessData, options);

                ViewBag.SessionId =
                    new SelectList(
                        sessions,
                        "SessionId",
                        "SessionName",
                        teacherSubject.SessionId);
            }

            // ================= CLASS =================

            var classResponse =
                await _client.GetAsync("Classes");

            if (classResponse.IsSuccessStatusCode)
            {
                var classData =
                    await classResponse.Content.ReadAsStringAsync();

                var allClasses =
                    JsonSerializer.Deserialize<List<Class>>(classData, options);

                var filteredClasses =
                    allClasses
                    .Where(x =>
                        x.SessionId == teacherSubject.SessionId &&
                        x.Medium == teacherSubject.Class?.Medium)
                    .ToList();

                ViewBag.ClassId =
                    new SelectList(
                        filteredClasses,
                        "ClassId",
                        "ClassName",
                        teacherSubject.ClassId);
            }

            // ================= SUBJECT =================

            var subRes =
                await _client.GetAsync(
                    $"TeacherSubjects/subjects-by-class/{teacherSubject.ClassId}");

            if (subRes.IsSuccessStatusCode)
            {
                var subData =
                    await subRes.Content.ReadAsStringAsync();

                var subjects =
                    JsonSerializer.Deserialize<List<Subject>>(subData, options);

                ViewBag.SubjectId =
                    new SelectList(
                        subjects,
                        "SubjectId",
                        "SubjectName",
                        teacherSubject.SubjectId);
            }

            // ================= MEDIUM =================

            ViewBag.SelectedMedium =
                teacherSubject.Class?.Medium;

            return View(teacherSubject);
        }

        // POST: TeacherSubjects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StaffId,SubjectId,ClassId,SessionId")] TeacherSubject teacherSubject)
        {
            if (id != teacherSubject.Id) return NotFound();

            if (await IsDuplicate(teacherSubject.StaffId, teacherSubject.SubjectId, teacherSubject.ClassId, teacherSubject.SessionId, id))
            {
                ModelState.AddModelError("", "⚠️ Duplicate assignment exists!");
                await LoadDropdowns(teacherSubject);
                return View(teacherSubject);
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdowns(teacherSubject);
                return View(teacherSubject);
            }

            var response = await _client.PutAsJsonAsync($"TeacherSubjects/{id}", teacherSubject);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Update failed!");
            await LoadDropdowns(teacherSubject);
            return View(teacherSubject);
        }

        // GET: TeacherSubjects/Delete/5
        // GET: TeacherSubjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"TeacherSubjects/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var teacherSubject = JsonSerializer.Deserialize<TeacherSubject>(data, options);

            // Load Staff
            if (teacherSubject.StaffId.HasValue)
            {
                var staffRes = await _client.GetAsync($"Staffs/{teacherSubject.StaffId}");
                if (staffRes.IsSuccessStatusCode)
                {
                    var staffData = await staffRes.Content.ReadAsStringAsync();
                    teacherSubject.Staff = JsonSerializer.Deserialize<Staff>(staffData, options);
                }
            }

            // Load Subject
            if (teacherSubject.SubjectId.HasValue)
            {
                var subRes = await _client.GetAsync($"Subjects/{teacherSubject.SubjectId}");
                if (subRes.IsSuccessStatusCode)
                {
                    var subData = await subRes.Content.ReadAsStringAsync();
                    teacherSubject.Subject = JsonSerializer.Deserialize<Subject>(subData, options);
                }
            }

            // ✅ Add Class loading
            if (teacherSubject.ClassId.HasValue)
            {
                var classRes = await _client.GetAsync($"Classes/{teacherSubject.ClassId}");
                if (classRes.IsSuccessStatusCode)
                {
                    var classData = await classRes.Content.ReadAsStringAsync();
                    teacherSubject.Class = JsonSerializer.Deserialize<Class>(classData, options);
                }
            }

            // ✅ Add Session loading
            if (teacherSubject.SessionId.HasValue)
            {
                var sessRes = await _client.GetAsync($"Sessions/{teacherSubject.SessionId}");
                if (sessRes.IsSuccessStatusCode)
                {
                    var sessData = await sessRes.Content.ReadAsStringAsync();
                    teacherSubject.Session = JsonSerializer.Deserialize<Session>(sessData, options);
                }
            }

            return View(teacherSubject);
        }
        // POST: TeacherSubjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"TeacherSubjects/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Delete failed!");
            }
            return RedirectToAction(nameof(Index));
        }

        // ========== HELPER METHODS ==========

        private async Task LoadDropdowns(TeacherSubject teacherSubject = null)
        {
            // ✅ Staff dropdown - Sirf "Teacher" designation wale
            var staffRes = await _client.GetAsync("Staffs");
            if (staffRes.IsSuccessStatusCode)
            {
                var staffData = await staffRes.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allStaff = JsonSerializer.Deserialize<List<Staff>>(staffData, options);

                // Filter by Designation == "Teacher" (case-insensitive)
                var teachers = allStaff.Where(s => s.Designation != null && s.Designation.Equals("Teacher", StringComparison.OrdinalIgnoreCase)).ToList();

                ViewData["StaffId"] = new SelectList(teachers, "StaffId", "FirstName", teacherSubject?.StaffId);
                // Note: Agar FirstName+LastName chahiye toh "StaffName" property banao ya select karte waqt concatenate karo
            }

            // Subjects dropdown
            var subRes = await _client.GetAsync("Subjects");
            if (subRes.IsSuccessStatusCode)
            {
                var subData = await subRes.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var subjectList = JsonSerializer.Deserialize<List<Subject>>(subData, options);
                ViewData["SubjectId"] = new SelectList(subjectList, "SubjectId", "SubjectName", teacherSubject?.SubjectId);
            }

            // Classes dropdown
            var classRes = await _client.GetAsync("Classes");
            if (classRes.IsSuccessStatusCode)
            {
                var classData = await classRes.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classList = JsonSerializer.Deserialize<List<Class>>(classData, options);
                ViewData["ClassId"] = new SelectList(classList, "ClassId", "ClassName", teacherSubject?.ClassId);
            }

            // Sessions dropdown
            var sessRes = await _client.GetAsync("Sessions");
            if (sessRes.IsSuccessStatusCode)
            {
                var sessData = await sessRes.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sessionList = JsonSerializer.Deserialize<List<Session>>(sessData, options);
                ViewData["SessionId"] = new SelectList(sessionList, "SessionId", "SessionName", teacherSubject?.SessionId);
            }
        }

        private async Task<bool> IsDuplicate(int? staffId, int? subjectId, int? classId, int? sessionId, int? excludeId = null)
        {
            var response = await _client.GetAsync("TeacherSubjects");
            if (!response.IsSuccessStatusCode) return false;

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var all = JsonSerializer.Deserialize<List<TeacherSubject>>(data, options);

            return all.Any(ts =>
                ts.StaffId == staffId &&
                ts.SubjectId == subjectId &&
                ts.ClassId == classId &&
                ts.SessionId == sessionId &&
                (excludeId == null || ts.Id != excludeId));
        }

        [HttpGet]
        public async Task<JsonResult> GetClassesBySessionMedium(int sessionId, string medium)
        {
            var res = await _client.GetAsync($"TeacherSubjects/classes-by-session-medium?sessionId={sessionId}&medium={medium}");

            var data = await res.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var list = JsonSerializer.Deserialize<List<Class>>(data, option);

            return Json(list.Select(x => new {
                value = x.ClassId,
                text = x.ClassName
            }));
        }

        [HttpGet]
        public async Task<JsonResult> GetSubjectsByClass(int classId)
        {
            var res = await _client.GetAsync($"TeacherSubjects/subjects-by-class/{classId}");

            var data = await res.Content.ReadAsStringAsync();
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var list = JsonSerializer.Deserialize<List<Subject>>(data, option);

            return Json(list.Select(x => new {
                value = x.SubjectId,
                text = x.SubjectName
            }));
        }

    }
}