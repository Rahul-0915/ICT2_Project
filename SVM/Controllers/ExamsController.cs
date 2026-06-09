using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SVM.Models;

namespace SVM.Controllers
{
    [LoginCheckFilter]
    public class ExamsController : Controller
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExamsController(IHttpClientFactory factory, IHttpContextAccessor httpContextAccessor)
        {
            _client = factory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
            _httpContextAccessor = httpContextAccessor;
        }

        private int GetCurrentUserId() => int.TryParse(_httpContextAccessor.HttpContext?.Session.GetString("UserId"), out int id) ? id : 0;
        private int GetCurrentStaffId()
        {
            var groupId = _httpContextAccessor.HttpContext?.Session.GetString("GroupId");
            if (groupId == "2")
                return int.TryParse(_httpContextAccessor.HttpContext?.Session.GetString("StaffId"), out int sid) ? sid : 0;
            return 0;
        }

        // ==================== INDEX ====================
        public async Task<IActionResult> Index(int? sessionId, string medium, int? classId, int? sectionId)
        {
            var sessions = await GetSessions();
            if (sessionId == null) sessionId = sessions.FirstOrDefault(x => x.IsActive == 1)?.SessionId;
            ViewBag.AllSessions = sessions;
            ViewBag.SelectedSessionId = sessionId;
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSectionId = sectionId;

            if (sessionId.HasValue && !string.IsNullOrEmpty(medium))
                ViewBag.Classes = await GetClassesBySessionAndMedium(sessionId.Value, medium);
            if (classId.HasValue)
                ViewBag.Sections = await GetSectionsByClass(classId.Value);

            List<ExamListVM> exams = new();
            if (sessionId.HasValue && !string.IsNullOrEmpty(medium) && classId.HasValue && sectionId.HasValue)
            {
                var resp = await _client.GetAsync($"Exams?sessionId={sessionId}&medium={medium}&classId={classId}&sectionId={sectionId}");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(json);
                    foreach (var exam in doc.RootElement.EnumerateArray())
                    {
                        exams.Add(new ExamListVM
                        {
                            ExamId = exam.GetProperty("examId").GetInt32(),
                            ExamName = exam.GetProperty("examName").GetString(),
                            ExamType = exam.GetProperty("examType").GetString(),
                            StartDate = exam.GetProperty("startDate").GetDateTime(),
                            EndDate = exam.GetProperty("endDate").GetDateTime(),
                            IsActive = exam.GetProperty("isActive").GetInt32() == 1,
                            IsPublished = exam.GetProperty("isPublished").GetBoolean(),
                            ClassId = exam.GetProperty("classId").GetInt32(),
                            SectionId = exam.GetProperty("sectionId").GetInt32()
                        });
                    }
                }
            }
            return View(exams);
        }

        // ==================== CREATE ====================
        [HttpGet] public async Task<IActionResult> Create() { await LoadDropdowns(); return View(); }
        [HttpPost]
        public async Task<IActionResult> Create(ExamDTO exam)
        {
            if (ModelState.IsValid)
            {
                var examForApi = new
                {
                    exam.ExamName,
                    exam.ExamType,
                    exam.SessionId,
                    exam.ClassId,
                    exam.SectionId,
                    exam.Medium,
                    StartDate = exam.StartDate,
                    EndDate = exam.EndDate,
                    IsActive = 1,
                    CreatedBy = GetCurrentUserId(),
                    IsPublished = false
                };
                var response = await _client.PostAsJsonAsync("Exams", examForApi);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Exam created!";
                    return RedirectToAction(nameof(Index), new { sessionId = exam.SessionId, medium = exam.Medium, classId = exam.ClassId, sectionId = exam.SectionId });
                }
                ModelState.AddModelError("", "Failed to create exam");
            }
            await LoadDropdowns();
            return View(exam);
        }

        // ==================== EDIT ====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var resp = await _client.GetAsync($"Exams/{id}");
            if (!resp.IsSuccessStatusCode) return NotFound();
            var exam = JsonSerializer.Deserialize<ExamDTO>(await resp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            await LoadDropdowns();
            return View(exam);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, ExamDTO exam)
        {
            if (id != exam.ExamId) return BadRequest();
            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"Exams/{id}", exam);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Exam updated!";
                    return RedirectToAction(nameof(Index), new { sessionId = exam.SessionId, medium = exam.Medium, classId = exam.ClassId, sectionId = exam.SectionId });
                }
                ModelState.AddModelError("", "Update failed");
            }
            await LoadDropdowns();
            return View(exam);
        }

        // ==================== DELETE ====================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var resp = await _client.GetAsync($"Exams/{id}");
            if (!resp.IsSuccessStatusCode) return NotFound();
            var exam = JsonSerializer.Deserialize<ExamDTO>(await resp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(exam);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id, int? sessionId, string medium, int? classId, int? sectionId)
        {
            await _client.DeleteAsync($"Exams/{id}");
            TempData["Success"] = "Exam deleted!";
            return RedirectToAction(nameof(Index), new { sessionId, medium, classId, sectionId });
        }

        // ==================== PUBLISH ====================
        [HttpPost]
        public async Task<IActionResult> Publish(int id, int? sessionId, string medium, int? classId, int? sectionId)
        {
            await _client.PatchAsync($"Exams/{id}/publish", null);
            TempData["Success"] = "Exam published successfully!";
            return RedirectToAction(nameof(Index), new { sessionId, medium, classId, sectionId });
        }

        // ==================== SUBJECTS ====================
        public async Task<IActionResult> Subjects(int examId)
        {
            var examResp = await _client.GetAsync($"Exams/{examId}");
            if (!examResp.IsSuccessStatusCode) return NotFound();
            var exam = JsonSerializer.Deserialize<ExamDTO>(await examResp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var classId = exam.ClassId;

            var subjectsResp = await _client.GetAsync($"Subjects/ByClass/{classId}");
            var allSubjects = JsonSerializer.Deserialize<List<Subject>>(await subjectsResp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var examSubjectsResp = await _client.GetAsync($"Exams/subjects/{examId}");
            var examSubjects = JsonSerializer.Deserialize<List<ExamSubjectDTO>>(await examSubjectsResp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var subjectsVM = allSubjects.Select(sub => new ExamSubjectSelectionVM
            {
                SubjectId = sub.SubjectId,
                SubjectName = sub.SubjectName,
                IsSelected = examSubjects.Any(es => es.SubjectId == sub.SubjectId),
                TotalMarks = examSubjects.FirstOrDefault(es => es.SubjectId == sub.SubjectId)?.TotalMarks ?? 0,
                PassingMarks = examSubjects.FirstOrDefault(es => es.SubjectId == sub.SubjectId)?.PassingMarks ?? (int)((examSubjects.FirstOrDefault(es => es.SubjectId == sub.SubjectId)?.TotalMarks ?? 100) * 0.35m)
            }).ToList();

            ViewBag.Exam = exam;
            return View(subjectsVM);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSubjects(int examId, List<ExamSubjectSelectionVM> subjects)
        {
            try
            {
                var toAdd = subjects.Where(s => s.IsSelected && s.TotalMarks > 0)
                    .Select(s => new ExamSubjectDTO
                    {
                        ExamId = examId,
                        SubjectId = s.SubjectId,
                        TotalMarks = s.TotalMarks,
                        PassingMarks = s.PassingMarks
                    }).ToList();
                var response = await _client.PostAsJsonAsync("Exams/subjects/bulk", toAdd);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SubjectSuccess"] = $"{toAdd.Count} subjects saved successfully!";
                }
                else
                {
                    TempData["SubjectError"] = "Failed to save subjects.";
                }
            }
            catch (Exception ex)
            {
                TempData["SubjectError"] = ex.Message;
            }
            return RedirectToAction("Subjects", new { examId });
        }

        // ==================== MARKS ENTRY ====================
        public async Task<IActionResult> MarksEntry(int examId, int? classId = null, int? sectionId = null)
        {
            var examResp = await _client.GetAsync($"Exams/{examId}");
            if (!examResp.IsSuccessStatusCode) return NotFound();
            var exam = JsonSerializer.Deserialize<ExamDTO>(await examResp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            int finalClassId = classId ?? exam.ClassId;
            int finalSectionId = sectionId ?? exam.SectionId;

            int staffId = GetCurrentStaffId();
            var url = $"Exams/marks-data?examId={examId}&classId={finalClassId}&sectionId={finalSectionId}";
            if (staffId > 0) url += $"&staffId={staffId}";
            var marksResp = await _client.GetAsync(url);
            if (!marksResp.IsSuccessStatusCode)
            {
                var errorContent = await marksResp.Content.ReadAsStringAsync();
                ViewBag.Error = $"API Error: {marksResp.StatusCode} - {errorContent}";
                ViewBag.Exam = exam;
                ViewBag.Subjects = new List<dynamic>();
                ViewBag.Students = new List<dynamic>();
                return View();
            }
            var marksJson = await marksResp.Content.ReadAsStringAsync();

            // For debugging – you can remove this line later
            ViewBag.DebugJson = marksJson;

            using JsonDocument doc = JsonDocument.Parse(marksJson);
            var root = doc.RootElement;

            var subjects = root.GetProperty("subjects").EnumerateArray().Select(s => new
            {
                ExamSubjectId = s.GetProperty("examSubjectId").GetInt32(),
                SubjectName = s.GetProperty("subjectName").GetString(),
                TotalMarks = s.GetProperty("totalMarks").GetInt32(),
                PassingMarks = s.GetProperty("passingMarks").GetInt32()
            }).ToList();

            var students = root.GetProperty("students").EnumerateArray().Select(s => new
            {
                StudentId = s.GetProperty("studentId").GetInt32(),
                RollNo = s.GetProperty("rollNo").GetInt32(),
                StudentName = s.GetProperty("studentName").GetString(),
                Grno = s.GetProperty("grno").GetString(),
                Marks = s.GetProperty("marks").EnumerateArray().Select(m => new
                {
                    ExamSubjectId = m.GetProperty("examSubjectId").GetInt32(),
                    ObtainedMarks = m.TryGetProperty("obtainedMarks", out var om) && om.ValueKind != JsonValueKind.Null ? om.GetDecimal() : (decimal?)null
                }).ToList()
            }).ToList();

            // Ensure students is not null (it will be a list, possibly empty)
            if (students == null || !students.Any())
            {
                ViewBag.Error = "No students found for the selected class and section. Please check student enrollment.";
            }

            ViewBag.Subjects = subjects.Select(s => (dynamic)s).ToList();
            ViewBag.Students = students.Select(s => (dynamic)s).ToList();

            ViewBag.Exam = exam;
            
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SaveMarks(List<ExamMarkDTO> marks, int examId)
        {
            if (marks == null || !marks.Any())
            {
                TempData["Error"] = "No marks data";
                return RedirectToAction("Index");
            }
            int userId = GetCurrentUserId();
            foreach (var m in marks) m.EnteredBy = userId;
            var response = await _client.PostAsJsonAsync("Exams/marks/save", marks);
            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Marks saved!";
            else
                TempData["Error"] = await response.Content.ReadAsStringAsync();
            return RedirectToAction("Index", new
            {
                sessionId = ViewBag.SelectedSessionId,
                medium = ViewBag.SelectedMedium,
                classId = ViewBag.SelectedClassId,
                sectionId = ViewBag.SelectedSectionId
            });
        }

        // ==================== REPORT ====================
        public async Task<IActionResult> Report(int? sessionId, string medium, int? classId, int? sectionId, int? examId)
        {
            var sessions = await GetSessions();
            ViewBag.AllSessions = sessions;
            ViewBag.SelectedSessionId = sessionId;
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSectionId = sectionId;

            if (sessionId.HasValue && !string.IsNullOrEmpty(medium))
                ViewBag.Classes = await GetClassesBySessionAndMedium(sessionId.Value, medium);
            if (classId.HasValue)
                ViewBag.Sections = await GetSectionsByClass(classId.Value);
            if (sessionId.HasValue && !string.IsNullOrEmpty(medium) && classId.HasValue && sectionId.HasValue)
            {
                var examsResp = await _client.GetAsync($"Exams?sessionId={sessionId}&medium={medium}&classId={classId}&sectionId={sectionId}");
                if (examsResp.IsSuccessStatusCode)
                {
                    var exams = JsonSerializer.Deserialize<List<ExamDTO>>(await examsResp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.Exams = exams;
                }
            }

            if (examId.HasValue)
            {
                var reportResp = await _client.GetAsync($"Exams/report?examId={examId}&classId={classId}&sectionId={sectionId}");
                if (reportResp.IsSuccessStatusCode)
                {
                    var report = JsonSerializer.Deserialize<ExamReportVM>(await reportResp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(report);
                }
                else
                    ViewBag.Error = "No data found";
            }
            return View(new ExamReportVM());
        }

        // ==================== AJAX HELPERS ====================
        [HttpGet]
        public async Task<JsonResult> GetClassesByMedium(string medium, int sessionId) => Json(await GetClassesBySessionAndMedium(sessionId, medium));
        [HttpGet]
        public async Task<JsonResult> GetSectionsJson(int classId) => Json(await GetSectionsByClass(classId));
        [HttpGet]
        public async Task<JsonResult> GetExamsByFilters(int sessionId, string medium, int classId, int sectionId)
        {
            var resp = await _client.GetAsync($"Exams?sessionId={sessionId}&medium={medium}&classId={classId}&sectionId={sectionId}");
            if (!resp.IsSuccessStatusCode) return Json(new List<object>());
            using JsonDocument doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var exams = doc.RootElement.EnumerateArray().Select(e => new { ExamId = e.GetProperty("examId").GetInt32(), ExamName = e.GetProperty("examName").GetString() }).ToList();
            return Json(exams);
        }
        [HttpGet]
        public async Task<JsonResult> GetSessionsJson()
        {
            var sessions = await GetSessions();
            return Json(sessions.Select(s => new { s.SessionId, s.SessionName, IsActive = s.IsActive == 1 }));
        }

        // ==================== PRIVATE HELPERS ====================
        private async Task LoadDropdowns() => ViewBag.Sessions = await GetSessions();
        private async Task<List<Session>> GetSessions()
        {
            var res = await _client.GetAsync("Sessions");
            if (!res.IsSuccessStatusCode) return new();
            var data = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Session>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        private async Task<List<Class>> GetClassesBySessionAndMedium(int sessionId, string medium)
        {
            var res = await _client.GetAsync($"Classes/WithFilters?sessionId={sessionId}&medium={medium}");
            if (!res.IsSuccessStatusCode) return new();
            var data = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Class>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        private async Task<List<Section>> GetSectionsByClass(int classId)
        {
            var res = await _client.GetAsync($"Sections/ByClass/{classId}");
            if (!res.IsSuccessStatusCode) return new();
            var data = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Section>>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
    }
}