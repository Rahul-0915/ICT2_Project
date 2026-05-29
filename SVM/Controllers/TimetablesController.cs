using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using System.Net.Http;
using SVM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SVM.Controllers
{
    [LoginCheckFilter]
    public class TimetablesController : Controller
    {
        private readonly HttpClient _client;

        public TimetablesController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // GET: Timetables?sessionId=1&classId=2&sectionId=3&medium=Gujarati
        public async Task<IActionResult> Index(int? sessionId, int? classId, int? sectionId, string medium)
        {
			var sessions = await GetSessions();

			// ✅ Active session auto select
			if (sessionId == null)
			{
				var activeSession = sessions.FirstOrDefault(s => s.IsActive == 1);

				if (activeSession != null)
				{
					sessionId = activeSession.SessionId;
				}
			}

			ViewBag.SessionList = new SelectList(sessions, "SessionId", "SessionName", sessionId);

			ViewBag.MediumList = new SelectList(new[] { "Gujarati", "English" }, medium);

            var sections = await GetSections();
            ViewBag.SectionList = new SelectList(sections, "SectionId", "SectionName", sectionId);

            if (sessionId == null || classId == null || sectionId == null || string.IsNullOrEmpty(medium))
                return View();

            var timetables = await GetTimetables(sessionId.Value, classId.Value, sectionId.Value);

            var teacherMapping = await GetTeacherMapping(sessionId.Value, classId.Value, sectionId.Value);

            var grid = await PrepareGridAsync(timetables, teacherMapping);
            ViewBag.Grid = grid;
            ViewBag.SelectedSessionId = sessionId;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSectionId = sectionId;
            ViewBag.SelectedMedium = medium;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(int sessionId, int classId, int sectionId, string dayName, int lectureNo, string medium)
        {
            var model = new Timetable
            {
                SessionId = sessionId,
                ClassId = classId,
                SectionId = sectionId,
                DayName = dayName,
                LectureNo = lectureNo,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(8, 40),
                IsBreak = false
            };

            var subjects = await GetSubjectsByClass(classId);
            ViewBag.SubjectList = new SelectList(subjects, "SubjectId", "SubjectName");

            ViewBag.StaffList = new SelectList(new List<object>(), "StaffId", "DisplayName");

            ViewBag.ReturnSessionId = sessionId;
            ViewBag.ReturnClassId = classId;
            ViewBag.ReturnSectionId = sectionId;
            ViewBag.ReturnMedium = medium;

            TempData["ReturnMedium"] = medium;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Timetable timetable)
        {
            ModelState.Remove("StaffId");
            timetable.IsBreak = false;

            if (ModelState.IsValid)
            {
                var response = await _client.PostAsJsonAsync("Timetables", timetable);
                if (response.IsSuccessStatusCode)
                {
                    string medium = TempData["ReturnMedium"] as string ?? "";
                    return RedirectToAction(nameof(Index), new
                    {
                        sessionId = timetable.SessionId,
                        classId = timetable.ClassId,
                        sectionId = timetable.SectionId,
                        medium = medium
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"API Error: {response.StatusCode} - {errorContent}");
                }
            }

            var subjects = await GetSubjectsByClass(timetable.ClassId);
            ViewBag.SubjectList = new SelectList(subjects, "SubjectId", "SubjectName", timetable.SubjectId);

            if (timetable.SubjectId.HasValue)
            {
                var teachers = await GetTeachersForSubject(timetable.SubjectId.Value, timetable.ClassId, timetable.SessionId);
                var teacherList = teachers.Select(t => new { StaffId = t.StaffId, DisplayName = $"{t.FirstName} {t.LastName}" }).ToList();
                ViewBag.StaffList = new SelectList(teacherList, "StaffId", "DisplayName", timetable.StaffId);
            }
            else
            {
                ViewBag.StaffList = new SelectList(new List<object>(), "StaffId", "DisplayName");
            }
            return View(timetable);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string medium, int? sessionId, int? classId, int? sectionId)
        {
            var response = await _client.GetAsync($"Timetables/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = await response.Content.ReadAsStringAsync();
            var timetable = JsonSerializer.Deserialize<Timetable>(data, options);

            var subjects = await GetSubjectsByClass(timetable.ClassId);
            ViewBag.SubjectList = new SelectList(subjects, "SubjectId", "SubjectName", timetable.SubjectId);

            if (timetable.SubjectId.HasValue)
            {
                var teachers = await GetTeachersForSubject(timetable.SubjectId.Value, timetable.ClassId, timetable.SessionId);
                if (timetable.StaffId.HasValue && !teachers.Any(t => t.StaffId == timetable.StaffId.Value))
                {
                    var staffRes = await _client.GetAsync($"Staffs/{timetable.StaffId.Value}");
                    if (staffRes.IsSuccessStatusCode)
                    {
                        var staffData = await staffRes.Content.ReadAsStringAsync();
                        var extraStaff = JsonSerializer.Deserialize<Staff>(staffData, options);
                        if (extraStaff != null) teachers.Add(extraStaff);
                    }
                }
                var teacherList = teachers.Select(t => new { StaffId = t.StaffId, DisplayName = $"{t.FirstName} {t.LastName}" }).ToList();
                ViewBag.StaffList = new SelectList(teacherList, "StaffId", "DisplayName", timetable.StaffId);
            }
            else
            {
                ViewBag.StaffList = new SelectList(new List<object>(), "StaffId", "DisplayName");
            }

            ViewBag.ReturnSessionId = sessionId;
            ViewBag.ReturnClassId = classId;
            ViewBag.ReturnSectionId = sectionId;
            ViewBag.ReturnMedium = medium;

            TempData["ReturnMedium"] = medium;
            return View(timetable);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Timetable timetable)
        {
            if (id != timetable.TimetableId) return BadRequest();

            ModelState.Remove("StaffId");
            timetable.IsBreak = false;

            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"Timetables/{id}", timetable);
                if (response.IsSuccessStatusCode)
                {
                    string medium = TempData["ReturnMedium"] as string ?? "";
                    return RedirectToAction(nameof(Index), new
                    {
                        sessionId = timetable.SessionId,
                        classId = timetable.ClassId,
                        sectionId = timetable.SectionId,
                        medium = medium
                    });
                }
                ModelState.AddModelError("", "Update failed!");
            }

            var subjects = await GetSubjects();
            ViewBag.SubjectList = new SelectList(subjects, "SubjectId", "SubjectName", timetable.SubjectId);
            if (timetable.SubjectId.HasValue)
            {
                var teachers = await GetTeachersForSubject(timetable.SubjectId.Value, timetable.ClassId, timetable.SessionId);
                var teacherList = teachers.Select(t => new { StaffId = t.StaffId, DisplayName = $"{t.FirstName} {t.LastName}" }).ToList();
                ViewBag.StaffList = new SelectList(teacherList, "StaffId", "DisplayName", timetable.StaffId);
            }
            else
            {
                ViewBag.StaffList = new SelectList(new List<object>(), "StaffId", "DisplayName");
            }
            return View(timetable);
        }

        // GET: Timetables/Delete/5
        public async Task<IActionResult> Delete(int id, int? sessionId, int? classId, int? sectionId, string medium)
        {
            var response = await _client.GetAsync($"Timetables/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = await response.Content.ReadAsStringAsync();
            var timetable = JsonSerializer.Deserialize<Timetable>(data, options);

            if (timetable.SubjectId.HasValue && timetable.SessionId > 0 && timetable.ClassId > 0)
            {
                var teacherMapping = await GetTeacherMapping(timetable.SessionId, timetable.ClassId, timetable.SectionId);
                if (teacherMapping.ContainsKey(timetable.SubjectId.Value))
                {
                    timetable.Staff = teacherMapping[timetable.SubjectId.Value];
                }
            }

            ViewBag.ReturnSessionId = sessionId;
            ViewBag.ReturnClassId = classId;
            ViewBag.ReturnSectionId = sectionId;
            ViewBag.ReturnMedium = medium;

            TempData["ReturnSessionId"] = sessionId;
            TempData["ReturnClassId"] = classId;
            TempData["ReturnSectionId"] = sectionId;
            TempData["ReturnMedium"] = medium;

            return View(timetable);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _client.DeleteAsync($"Timetables/{id}");

            int? sessionId = TempData["ReturnSessionId"] as int?;
            int? classId = TempData["ReturnClassId"] as int?;
            int? sectionId = TempData["ReturnSectionId"] as int?;
            string medium = TempData["ReturnMedium"] as string;

            return RedirectToAction(nameof(Index), new
            {
                sessionId = sessionId,
                classId = classId,
                sectionId = sectionId,
                medium = medium
            });
        }

        // -------------------------- EXPORT METHODS --------------------------
        public async Task<IActionResult> ExportExcel(int sessionId, int classId, int sectionId, string medium)
        {
            var timetables = await GetTimetables(sessionId, classId, sectionId);
            var teacherMapping = await GetTeacherMapping(sessionId, classId, sectionId);
            var grid = await PrepareGridAsync(timetables, teacherMapping);
            var sessionName = await GetSessionName(sessionId);
            var className = await GetClassName(classId);
            var sectionName = await GetSectionName(sectionId);
            var html = GenerateHtmlTable(grid, sessionName, className, sectionName, medium);
            return File(System.Text.Encoding.UTF8.GetBytes(html), "application/vnd.ms-excel", $"Timetable_{sessionName}_{className}_{sectionName}_{medium}.xls");
        }

        public async Task<IActionResult> ExportWord(int sessionId, int classId, int sectionId, string medium)
        {
            var timetables = await GetTimetables(sessionId, classId, sectionId);
            var teacherMapping = await GetTeacherMapping(sessionId, classId, sectionId);
            var grid = await PrepareGridAsync(timetables, teacherMapping);
            var sessionName = await GetSessionName(sessionId);
            var className = await GetClassName(classId);
            var sectionName = await GetSectionName(sectionId);
            var html = GenerateHtmlTable(grid, sessionName, className, sectionName, medium);
            return File(System.Text.Encoding.UTF8.GetBytes(html), "application/msword", $"Timetable_{sessionName}_{className}_{sectionName}_{medium}.doc");
        }

        public async Task<IActionResult> ExportPdf(int sessionId, int classId, int sectionId, string medium)
        {
            var timetables = await GetTimetables(sessionId, classId, sectionId);
            var teacherMapping = await GetTeacherMapping(sessionId, classId, sectionId);
            var grid = await PrepareGridAsync(timetables, teacherMapping);
            var sessionName = await GetSessionName(sessionId);
            var className = await GetClassName(classId);
            var sectionName = await GetSectionName(sectionId);
            var document = CreatePdfDocument(grid, sessionName, className, sectionName, medium);
            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Timetable_{sessionName}_{className}_{sectionName}_{medium}.pdf");
        }

        // -------------------------- HELPER METHODS --------------------------
        private string GenerateHtmlTable(Dictionary<string, Dictionary<int, Timetable>> grid,
            string sessionName, string className, string sectionName, string medium)
        {
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

            string schoolName = medium?.ToLower() == "gujarati"
                ? "શારદા વિદ્યામંદિર"
                : "S.V.M English Medium School";

            var html = $@"
<html>
<head>
<meta charset='utf-8'/>
<style>
body {{
    font-family: Arial;
    text-align: center;
}}
h1 {{
    margin: 5px;
}}
.table {{
    width: 100%;
    border-collapse: collapse;
}}
.table th, .table td {{
    border: 1px solid black;
    padding: 6px;
    text-align: center;
    vertical-align: middle;
}}
.header {{
    margin-bottom: 10px;
}}
.small {{
    font-size: 12px;
}}
.footer {{
    margin-top: 30px;
    text-align: right;
}}
</style>
</head>
<body>
<h1>{schoolName}</h1>
<h2>TIME TABLE</h2>
<div class='header'>
<b>Class:</b> {className} &nbsp;&nbsp;
<b>Section:</b> {sectionName} &nbsp;&nbsp;
<b>Medium:</b> {medium}
<br/>
<b>Session:</b> {sessionName}
</div>
<table class='table'>
<thead>
<tr>
<th>Period/Day</th>";

            foreach (var d in days)
                html += $"<th>{d}</th>";

            html += "</tr></thead><tbody>";

            for (int p = 1; p <= 8; p++)
            {
                html += $"<tr><th>Period {p}</th>";

                foreach (var day in days)
                {
                    if (day == "Saturday" && p > 5)
                    {
                        html += "<td>-</td>";
                        continue;
                    }

                    var cell = grid[day].ContainsKey(p) ? grid[day][p] : null;

                    if (cell != null && cell.SubjectId != null)
                    {
                        var teacher = cell.Staff != null ? $"{cell.Staff.FirstName} {cell.Staff.LastName}" : "";

                        html += $@"
<td>
<b>{cell.Subject?.SubjectName}</b><br/>
{cell.StartTime} - {cell.EndTime}<br/>
<span class='small'>{teacher}</span>
</td>";
                    }
                    else if (cell?.IsBreak == true)
                    {
                        html += $"<td><b>BREAK</b><br/>{cell.StartTime}-{cell.EndTime}</td>";
                    }
                    else
                    {
                        html += "<td>--</td>";
                    }
                }

                html += "</tr>";
            }

            html += $@"
</tbody>
</table>
<div class='footer'>
Principal Sign: ____________________
</div>
</body>
</html>";

            return html;
        }

        private IDocument CreatePdfDocument(Dictionary<string, Dictionary<int, Timetable>> grid,
            string sessionName, string className, string sectionName, string medium)
        {
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

            string schoolName = medium?.ToLower() == "gujarati"
                ? "શારદા વિદ્યામંદિર"
                : "S.V.M English Medium School";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(schoolName).FontSize(18).Bold();
                        col.Item().AlignCenter().Text("TIME TABLE").FontSize(14).Bold();
                        col.Item().AlignCenter().Text($"Class: {className}   |   Section: {sectionName}   |   Medium: {medium}");
                        col.Item().AlignCenter().Text($"Session: {sessionName}");
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);
                            foreach (var _ in days)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).Padding(5).AlignCenter().Text("Period/Day");
                            foreach (var day in days)
                                header.Cell().Border(1).Padding(5).AlignCenter().Text(day);
                        });

                        for (int p = 1; p <= 8; p++)
                        {
                            table.Cell().Border(1).Padding(5).AlignCenter().Text($"Period {p}");

                            foreach (var day in days)
                            {
                                if (day == "Saturday" && p > 5)
                                {
                                    table.Cell().Border(1).Padding(5).AlignCenter().Text("-");
                                    continue;
                                }

                                var cell = grid[day].ContainsKey(p) ? grid[day][p] : null;

                                if (cell != null && cell.SubjectId != null)
                                {
                                    var teacherName = cell.Staff != null
                                        ? $"{cell.Staff.FirstName} {cell.Staff.LastName}"
                                        : "";

                                    table.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                                    {
                                        col.Item().Text(cell.Subject?.SubjectName).Bold();
                                        col.Item().Text($"{cell.StartTime} - {cell.EndTime}");
                                        col.Item().Text(teacherName);
                                    });
                                }
                                else if (cell != null && cell.IsBreak == true)
                                {
                                    table.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                                    {
                                        col.Item().Text("BREAK").Bold();
                                        col.Item().Text($"{cell.StartTime} - {cell.EndTime}");
                                    });
                                }
                                else
                                {
                                    table.Cell().Border(1).Padding(5).AlignCenter().Text("--");
                                }
                            }
                        }
                    });

                    page.Footer().PaddingTop(3).AlignRight().Text("Principal Sign: ____________________");
                });
            });
        }

        // -------------------------- NAME FETCHING HELPERS --------------------------
        private async Task<string> GetSessionName(int sessionId)
        {
            var sessions = await GetSessions();
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            return session?.SessionName ?? sessionId.ToString();
        }

        private async Task<string> GetClassName(int classId)
        {
            var response = await _client.GetAsync($"Classes/{classId}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classObj = JsonSerializer.Deserialize<Class>(data, options);
                return classObj != null ? classObj.ClassName : classId.ToString();
            }
            return classId.ToString();
        }

        private async Task<string> GetSectionName(int sectionId)
        {
            var sections = await GetSections();
            var section = sections.FirstOrDefault(s => s.SectionId == sectionId);
            return section?.SectionName ?? sectionId.ToString();
        }

        // -------------------------- API HELPER METHODS --------------------------
        private async Task<List<Session>> GetSessions()
        {
            var response = await _client.GetAsync("Sessions");
            if (!response.IsSuccessStatusCode) return new List<Session>();
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Session>>(data, options) ?? new List<Session>();
        }

        private async Task<List<Section>> GetSections()
        {
            var response = await _client.GetAsync("Sections");
            if (!response.IsSuccessStatusCode) return new List<Section>();
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Section>>(data, options) ?? new List<Section>();
        }

        private async Task<List<Subject>> GetSubjects()
        {
            var response = await _client.GetAsync("Subjects");
            if (!response.IsSuccessStatusCode) return new List<Subject>();
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Subject>>(data, options) ?? new List<Subject>();
        }

        private async Task<List<Timetable>> GetTimetables(int sessionId, int classId, int sectionId)
        {
            var response = await _client.GetAsync($"Timetables?sessionId={sessionId}&classId={classId}&sectionId={sectionId}");
            if (!response.IsSuccessStatusCode) return new List<Timetable>();
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Timetable>>(data, options) ?? new List<Timetable>();
        }

        private async Task<Dictionary<string, Dictionary<int, Timetable>>> PrepareGridAsync(List<Timetable> timetables, Dictionary<int, Staff> teacherMapping)
        {
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            var grid = new Dictionary<string, Dictionary<int, Timetable>>();

            var staffIds = timetables.Where(t => t.StaffId.HasValue).Select(t => t.StaffId.Value).Distinct().ToList();
            var staffDict = new Dictionary<int, Staff>();
            foreach (var sid in staffIds)
            {
                var staff = await GetStaffById(sid);
                if (staff != null) staffDict[sid] = staff;
            }

            foreach (var day in days)
            {
                var periods = new Dictionary<int, Timetable>();
                int max = day == "Saturday" ? 5 : 8;
                for (int p = 1; p <= max; p++)
                {
                    var entry = timetables.FirstOrDefault(t => t.DayName == day && t.LectureNo == p);
                    if (entry != null)
                    {
                        if (entry.StaffId.HasValue && staffDict.ContainsKey(entry.StaffId.Value))
                            entry.Staff = staffDict[entry.StaffId.Value];
                        else if (entry.SubjectId.HasValue && teacherMapping.ContainsKey(entry.SubjectId.Value))
                            entry.Staff = teacherMapping[entry.SubjectId.Value];
                    }
                    periods[p] = entry;
                }
                grid[day] = periods;
            }
            return grid;
        }

        [HttpGet]
        public async Task<JsonResult> GetClassesByMedium(string medium, int sessionId)
        {
            var response = await _client.GetAsync("Classes");
            if (!response.IsSuccessStatusCode) return Json(new List<object>());
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var allClasses = JsonSerializer.Deserialize<List<Class>>(data, options) ?? new List<Class>();

            // ✅ Filter by BOTH medium AND sessionId (like Student reference)
            var filtered = allClasses.Where(c => c.Medium == medium && c.SessionId == sessionId)
                                     .Select(c => new { value = c.ClassId, text = c.ClassName })  // ✅ Only Class Name, not "1 - Gujarati"
                                     .ToList();
            return Json(filtered);
        }

        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var response = await _client.GetAsync($"Sections/ByClass/{classId}");
            if (!response.IsSuccessStatusCode) return Json(new List<object>());
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sections = JsonSerializer.Deserialize<List<Section>>(data, options) ?? new List<Section>();
            var sectionList = sections.Select(s => new { value = s.SectionId, text = s.SectionName }).ToList();
            return Json(sectionList);
        }

        private async Task<Dictionary<int, Staff>> GetTeacherMapping(int sessionId, int classId, int sectionId)
        {
            var response = await _client.GetAsync($"TeacherSubjects?sessionId={sessionId}&classId={classId}");
            if (!response.IsSuccessStatusCode) return new Dictionary<int, Staff>();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mappings = JsonSerializer.Deserialize<List<TeacherSubject>>(data, options) ?? new List<TeacherSubject>();

            var teacherMapping = new Dictionary<int, Staff>();

            foreach (var map in mappings)
            {
                if (!map.StaffId.HasValue) continue;

                var staffRes = await _client.GetAsync($"Staffs/{map.StaffId}");
                if (staffRes.IsSuccessStatusCode)
                {
                    var staffData = await staffRes.Content.ReadAsStringAsync();
                    var staff = JsonSerializer.Deserialize<Staff>(staffData, options);
                    if (staff != null && map.SubjectId.HasValue)
                        teacherMapping[map.SubjectId.Value] = staff;
                }
            }

            return teacherMapping;
        }

        private async Task<List<Staff>> GetTeachersForSubject(int subjectId, int classId, int sessionId)
        {
            var response = await _client.GetAsync($"TeacherSubjects?subjectId={subjectId}&classId={classId}&sessionId={sessionId}");
            if (!response.IsSuccessStatusCode) return new List<Staff>();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var mappings = JsonSerializer.Deserialize<List<TeacherSubject>>(data, options) ?? new List<TeacherSubject>();

            var teachers = new List<Staff>();
            var distinctStaffIds = mappings.Where(m => m.StaffId.HasValue).Select(m => m.StaffId.Value).Distinct();

            foreach (var staffId in distinctStaffIds)
            {
                var staffRes = await _client.GetAsync($"Staffs/{staffId}");
                if (staffRes.IsSuccessStatusCode)
                {
                    var staffData = await staffRes.Content.ReadAsStringAsync();
                    var staff = JsonSerializer.Deserialize<Staff>(staffData, options);
                    if (staff != null) teachers.Add(staff);
                }
            }
            return teachers;
        }

        private async Task<Staff> GetStaffById(int staffId)
        {
            var response = await _client.GetAsync($"Staffs/{staffId}");
            if (!response.IsSuccessStatusCode) return null;
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Staff>(data, options);
        }

        [HttpGet]
        public async Task<JsonResult> GetTeachersForSubjectDropdown(int subjectId, int classId, int sessionId)
        {
            var teachers = await GetTeachersForSubject(subjectId, classId, sessionId);
            var list = teachers.Select(t => new { value = t.StaffId, text = $"{t.FirstName} {t.LastName}" });
            return Json(list);
        }
        private async Task<List<Subject>> GetSubjectsByClass(int classId)
        {
            var response = await _client.GetAsync($"Subjects/ByClass/{classId}");

            if (!response.IsSuccessStatusCode)
                return new List<Subject>();

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<Subject>>(data, options)
                   ?? new List<Subject>();
        }
    }
}