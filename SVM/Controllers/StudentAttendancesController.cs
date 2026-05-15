using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SVM.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SVM.Controllers
{
    public class StudentAttendancesController : Controller
    {
        private readonly HttpClient _client;

        public StudentAttendancesController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // ============================= INDEX (Report with filters) =============================
        // ============================= INDEX (Report with filters) =============================
        public async Task<IActionResult> Index(int? sessionId, string? medium, int? classId, int? sectionId, string? date)
        {
            // Parse date safely
            DateTime selectedDate;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                selectedDate = parsed;
            }
            else
            {
                selectedDate = DateTime.Today;
            }

            // Load dropdown data
            await LoadSessionsDropdown();
            await LoadMediumsDropdown();

            if (sessionId.HasValue && sessionId > 0)
            {
                await LoadClassesDropdownBySession(sessionId.Value, medium);
                if (classId.HasValue && classId > 0)
                    await LoadSectionsDropdownByClass(classId.Value);
            }

            ViewBag.SelectedSessionId = sessionId;
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSectionId = sectionId;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");

            List<AttendanceReportItem> reportItems = new List<AttendanceReportItem>();
            AttendanceTotals totals = null;

            // Use the helper method to get report data
            var (items, tot, isMarked) = await GetAttendanceReportData(sessionId, medium, classId, sectionId, selectedDate);

            if (isMarked)
            {
                reportItems = items;
                totals = tot;
                ViewBag.Totals = totals;
                return View(reportItems);
            }
            else if (sessionId.HasValue && classId.HasValue && sectionId.HasValue)
            {
                // Filters were selected but no attendance marked
                ViewBag.NoAttendanceMessage = "⚠ No attendance has been marked for this date.";
            }

            ViewBag.Totals = null;
            return View(new List<AttendanceReportItem>());
        }

        // ============================= DETAILS =============================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"StudentAttendances/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var attendance = JsonSerializer.Deserialize<StudentAttendance>(json, options);
            await LoadNavigationProperties(attendance, options);

            return View(attendance);
        }

        // ============================= CREATE =============================
        public async Task<IActionResult> Create()
        {
            await LoadClassesDropdown();
            await LoadSectionsDropdown();
            await LoadSessionsDropdown();
            await LoadStudentsDropdown();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,AttendanceDate,Status,ClassId,SectionId,SessionId")] StudentAttendance studentAttendance)
        {
            ModelState.Remove("Id");
            if (ModelState.IsValid)
            {
                var response = await _client.PostAsJsonAsync("StudentAttendances", studentAttendance);
                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(Index));
                ModelState.AddModelError("", "Failed to create attendance.");
            }

            await LoadClassesDropdown(studentAttendance.ClassId);
            await LoadSectionsDropdown(studentAttendance.SectionId);
            await LoadSessionsDropdown(studentAttendance.SessionId);
            await LoadStudentsDropdown(studentAttendance.StudentId);
            return View(studentAttendance);
        }

        // ============================= EDIT =============================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"StudentAttendances/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var attendance = JsonSerializer.Deserialize<StudentAttendance>(json, options);

            await LoadClassesDropdown(attendance.ClassId);
            await LoadSectionsDropdown(attendance.SectionId);
            await LoadSessionsDropdown(attendance.SessionId);
            await LoadStudentsDropdown(attendance.StudentId);

            return View(attendance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,AttendanceDate,Status,ClassId,SectionId,SessionId")] StudentAttendance studentAttendance)
        {
            if (id != studentAttendance.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"StudentAttendances/{id}", studentAttendance);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Attendance updated!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Update failed.");
            }

            await LoadClassesDropdown(studentAttendance.ClassId);
            await LoadSectionsDropdown(studentAttendance.SectionId);
            await LoadSessionsDropdown(studentAttendance.SessionId);
            await LoadStudentsDropdown(studentAttendance.StudentId);
            return View(studentAttendance);
        }

        // ============================= DELETE =============================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"StudentAttendances/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var attendance = JsonSerializer.Deserialize<StudentAttendance>(json, options);
            await LoadNavigationProperties(attendance, options);

            return View(attendance);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"StudentAttendances/{id}");
            if (!response.IsSuccessStatusCode)
                ModelState.AddModelError("", "Delete failed.");
            else
                TempData["Success"] = "Attendance deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ============================= HELPER METHODS =============================
        private async Task LoadNavigationProperties(StudentAttendance attendance, JsonSerializerOptions options)
        {
            if (attendance.StudentId > 0)
            {
                var resp = await _client.GetAsync($"Students/{attendance.StudentId}");
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadAsStringAsync();
                    attendance.Student = JsonSerializer.Deserialize<Student>(data, options);
                }
            }
            if (attendance.ClassId > 0)
            {
                var resp = await _client.GetAsync($"Classes/{attendance.ClassId}");
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadAsStringAsync();
                    attendance.Class = JsonSerializer.Deserialize<Class>(data, options);
                }
            }
            if (attendance.SectionId > 0)
            {
                var resp = await _client.GetAsync($"Sections/{attendance.SectionId}");
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadAsStringAsync();
                    attendance.Section = JsonSerializer.Deserialize<Section>(data, options);
                }
            }
            if (attendance.SessionId > 0)
            {
                var resp = await _client.GetAsync($"Sessions/{attendance.SessionId}");
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadAsStringAsync();
                    attendance.Session = JsonSerializer.Deserialize<Session>(data, options);
                }
            }
        }

        private async Task LoadSessionsDropdown(int? selectedId = null)
        {
            var response = await _client.GetAsync("Sessions");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sessions = JsonSerializer.Deserialize<List<Session>>(data, options);
                ViewData["SessionId"] = new SelectList(sessions, "SessionId", "SessionName", selectedId);
                ViewBag.Sessions = sessions;
            }
            else
                ViewData["SessionId"] = new SelectList(new List<Session>(), "SessionId", "SessionName");
        }

        private async Task LoadMediumsDropdown()
        {
            var mediums = new List<string> { "Gujarati", "English" };
            ViewBag.Mediums = mediums;
        }

        private async Task LoadClassesDropdownBySession(int sessionId, string? medium = null)
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allClasses = JsonSerializer.Deserialize<List<Class>>(data, options);
                var filtered = allClasses.Where(c => c.SessionId == sessionId).ToList();
                if (!string.IsNullOrEmpty(medium))
                    filtered = filtered.Where(c => c.Medium == medium).ToList();
                ViewBag.Classes = filtered;
            }
            else
                ViewBag.Classes = new List<Class>();
        }

        private async Task LoadSectionsDropdownByClass(int classId)
        {
            var response = await _client.GetAsync("Sections");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allSections = JsonSerializer.Deserialize<List<Section>>(data, options);
                ViewBag.Sections = allSections.Where(s => s.ClassId == classId).ToList();
            }
            else
                ViewBag.Sections = new List<Section>();
        }

        private async Task LoadClassesDropdown(int? selectedId = null)
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classes = JsonSerializer.Deserialize<List<Class>>(data, options);
                ViewData["ClassId"] = new SelectList(classes, "ClassId", "ClassName", selectedId);
            }
            else
                ViewData["ClassId"] = new SelectList(new List<Class>(), "ClassId", "ClassName");
        }

        private async Task LoadSectionsDropdown(int? selectedId = null)
        {
            var response = await _client.GetAsync("Sections");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sections = JsonSerializer.Deserialize<List<Section>>(data, options);
                ViewData["SectionId"] = new SelectList(sections, "SectionId", "SectionName", selectedId);
            }
            else
                ViewData["SectionId"] = new SelectList(new List<Section>(), "SectionId", "SectionName");
        }

        private async Task LoadStudentsDropdown(int? selectedId = null)
        {
            var response = await _client.GetAsync("Students");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var students = JsonSerializer.Deserialize<List<Student>>(data, options);
                ViewData["StudentId"] = new SelectList(students, "StudentId", "FirstName", selectedId);
            }
            else
                ViewData["StudentId"] = new SelectList(new List<Student>(), "StudentId", "FirstName");
        }

        // GET: StudentAttendances/GetClassesBySession
        [HttpGet]
        public async Task<JsonResult> GetClassesBySession(int sessionId, string? medium)
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allClasses = JsonSerializer.Deserialize<List<Class>>(data, options);
                var filtered = allClasses.Where(c => c.SessionId == sessionId).ToList();
                if (!string.IsNullOrEmpty(medium))
                    filtered = filtered.Where(c => c.Medium == medium).ToList();
                var result = filtered.Select(c => new { value = c.ClassId, text = c.ClassName }).ToList();
                return Json(result);
            }
            return Json(new List<object>());
        }

        // GET: StudentAttendances/GetSectionsByClass
        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var response = await _client.GetAsync("Sections");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allSections = JsonSerializer.Deserialize<List<Section>>(data, options);
                var filtered = allSections.Where(s => s.ClassId == classId)
                                          .Select(s => new { value = s.SectionId, text = s.SectionName })
                                          .ToList();
                return Json(filtered);
            }
            return Json(new List<object>());
        }
        private async Task<(List<AttendanceReportItem> Items, AttendanceTotals Totals, bool IsMarked)> GetAttendanceReportData(int? sessionId, string? medium, int? classId, int? sectionId, DateTime selectedDate)
        {
            List<AttendanceReportItem> reportItems = new();
            AttendanceTotals totals = null;
            bool isMarked = false;

            if (sessionId.HasValue && sessionId > 0 && classId.HasValue && sectionId.HasValue)
            {
                string url = $"StudentAttendances/advanced-report?sessionId={sessionId}&classId={classId}&sectionId={sectionId}&date={selectedDate:yyyy-MM-dd}";
                if (!string.IsNullOrEmpty(medium))
                    url += $"&medium={Uri.EscapeDataString(medium)}";

                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResult = JsonSerializer.Deserialize<ApiReportResponse>(json, options);

                    if (apiResult != null && apiResult.IsAttendanceMarked && apiResult.Students != null)
                    {
                        isMarked = true;
                        reportItems = apiResult.Students.Select(s => new AttendanceReportItem
                        {
                            StudentId = s.StudentId,
                            FullName = s.FullName,
                            RollNo = s.RollNo ?? 0,
                            Gender = s.Gender ?? "N/A",
                            Status = s.Status,
                            AttendanceId = s.AttendanceId
                        }).ToList();

                        if (apiResult.Totals != null)
                        {
                            totals = new AttendanceTotals
                            {
                                TotalPresent = apiResult.Totals.TotalPresent,
                                TotalAbsent = apiResult.Totals.TotalAbsent,
                                GirlsPresent = apiResult.Totals.GirlsPresent,
                                GirlsAbsent = apiResult.Totals.GirlsAbsent,
                                BoysPresent = apiResult.Totals.BoysPresent,
                                BoysAbsent = apiResult.Totals.BoysAbsent
                            };
                        }
                    }
                }
            }
            return (reportItems, totals, isMarked);
        }
        // GET: StudentAttendances/ExportToExcel
        public async Task<IActionResult> ExportToExcel(int? sessionId, string? medium, int? classId, int? sectionId, string? date)
        {
            // Parse date
            DateTime selectedDate;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                selectedDate = parsed;
            else
                selectedDate = DateTime.Today;

            // Get report data
            var (reportItems, totals, isMarked) = await GetAttendanceReportData(sessionId, medium, classId, sectionId, selectedDate);

            if (!isMarked || reportItems == null || reportItems.Count == 0)
            {
                TempData["Error"] = "No attendance data available to export.";
                return RedirectToAction(nameof(Index), new { sessionId, medium, classId, sectionId, date });
            }

            // Build CSV content
            var csv = new StringBuilder();

            // Add headers
            csv.AppendLine("Student Name,Roll No,Gender,Status");

            // Add rows
            foreach (var item in reportItems)
            {
                csv.AppendLine($"\"{item.FullName}\",{item.RollNo},\"{item.Gender}\",{item.Status}");
            }

            // Add totals summary (optional)
            if (totals != null)
            {
                csv.AppendLine();
                csv.AppendLine($"Total Present,{totals.TotalPresent}");
                csv.AppendLine($"Total Absent,{totals.TotalAbsent}");
                csv.AppendLine($"Girls Present,{totals.GirlsPresent}");
                csv.AppendLine($"Girls Absent,{totals.GirlsAbsent}");
                csv.AppendLine($"Boys Present,{totals.BoysPresent}");
                csv.AppendLine($"Boys Absent,{totals.BoysAbsent}");
            }

            // Generate file name
            string fileName = $"Attendance_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            byte[] fileBytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(fileBytes, "text/csv", fileName);
        }

        // ============================= DTOs =============================
        public class ApiReportResponse
        {
            public List<ApiStudentItem> Students { get; set; }
            public ApiTotals Totals { get; set; }
            public bool IsAttendanceMarked { get; set; }
        }

        public class ApiStudentItem
        {
            public int StudentId { get; set; }
            public string FullName { get; set; }
            public int? RollNo { get; set; }
            public string Gender { get; set; }
            public string Status { get; set; }
            public int AttendanceId { get; set; }   // ✅ Added
        }

        public class ApiTotals
        {
            public int TotalPresent { get; set; }
            public int TotalAbsent { get; set; }
            public int GirlsPresent { get; set; }
            public int GirlsAbsent { get; set; }
            public int BoysPresent { get; set; }
            public int BoysAbsent { get; set; }
        }

        public class AttendanceReportItem
        {
            public int StudentId { get; set; }
            public string FullName { get; set; } = "";
            public int RollNo { get; set; }
            public string Gender { get; set; } = "";
            public string Status { get; set; } = "";
            public int AttendanceId { get; set; }   // ✅ Added
        }

        public class AttendanceTotals
        {
            public int TotalPresent { get; set; }
            public int TotalAbsent { get; set; }
            public int GirlsPresent { get; set; }
            public int GirlsAbsent { get; set; }
            public int BoysPresent { get; set; }
            public int BoysAbsent { get; set; }
        }
    }
}