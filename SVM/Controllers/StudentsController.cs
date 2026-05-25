using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using SVM.Models;

namespace SVM.Controllers
{
    public class StudentsController : Controller
    {
        private readonly HttpClient _client;

        public StudentsController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // Helper: Get current logged-in user ID from Session
        private int? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdString, out int id))
                return id;
            return null;
        }

        // Helper: Get current logged-in user full name from Session
        private string GetCurrentUserName()
        {
            return HttpContext.Session.GetString("FullName") ?? "Admin";
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? sessionId, string medium, int? classId, int? sectionId)
        {
            List<Student> studentList = new List<Student>();
            var response = await _client.GetAsync("Students");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allStudents = JsonSerializer.Deserialize<List<Student>>(data, options);

                // Apply filters if provided (do NOT filter by medium – classId already restricts to correct medium)
                if (sessionId.HasValue)
                    allStudents = allStudents.Where(s => s.SessionId == sessionId).ToList();
                if (classId.HasValue)
                    allStudents = allStudents.Where(s => s.ClassId == classId).ToList();
                if (sectionId.HasValue)
                    allStudents = allStudents.Where(s => s.SectionId == sectionId).ToList();

                studentList = allStudents;

                // Manually load related entities for each student
                foreach (var student in studentList)
                {
                    if (student.ClassId.HasValue)
                    {
                        var classResp = await _client.GetAsync($"Classes/{student.ClassId}");
                        if (classResp.IsSuccessStatusCode)
                        {
                            var classData = await classResp.Content.ReadAsStringAsync();
                            student.Class = JsonSerializer.Deserialize<Class>(classData, options);
                        }
                    }

                    if (student.SectionId.HasValue)
                    {
                        var sectionResp = await _client.GetAsync($"Sections/{student.SectionId}");
                        if (sectionResp.IsSuccessStatusCode)
                        {
                            var sectionData = await sectionResp.Content.ReadAsStringAsync();
                            student.Section = JsonSerializer.Deserialize<Section>(sectionData, options);
                        }
                    }

                    if (student.SessionId.HasValue)
                    {
                        var sessionResp = await _client.GetAsync($"Sessions/{student.SessionId}");
                        if (sessionResp.IsSuccessStatusCode)
                        {
                            var sessionData = await sessionResp.Content.ReadAsStringAsync();
                            student.Session = JsonSerializer.Deserialize<Session>(sessionData, options);
                        }
                    }

                    if (student.UserId.HasValue)
                    {
                        var userResp = await _client.GetAsync($"Users/{student.UserId}");
                        if (userResp.IsSuccessStatusCode)
                        {
                            var userData = await userResp.Content.ReadAsStringAsync();
                            student.User = JsonSerializer.Deserialize<User>(userData, options);
                        }
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Failed to load students.");
            }

            // Load all sessions for the filter dropdown
            var sessionResponse = await _client.GetAsync("Sessions");
            if (sessionResponse.IsSuccessStatusCode)
            {
                var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allSessions = JsonSerializer.Deserialize<List<Session>>(sessionData, options);
                ViewBag.AllSessions = allSessions;
                ViewBag.SelectedSessionId = sessionId;   // for pre-selection
            }

            // Load all classes (will be filtered later via AJAX)
            var classResponse = await _client.GetAsync("Classes");
            if (classResponse.IsSuccessStatusCode)
            {
                var classData = await classResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allClasses = JsonSerializer.Deserialize<List<Class>>(classData, options);
                ViewBag.AllClasses = allClasses;
            }

            // Pass selected filter values to the view
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSectionId = sectionId;

            return View(studentList);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Students/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var student = JsonSerializer.Deserialize<Student>(data, options);

            // Load related entities
            if (student.ClassId.HasValue)
            {
                var classResp = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResp.IsSuccessStatusCode)
                {
                    var classData = await classResp.Content.ReadAsStringAsync();
                    student.Class = JsonSerializer.Deserialize<Class>(classData, options);
                }
            }

            if (student.SectionId.HasValue)
            {
                var sectionResp = await _client.GetAsync($"Sections/{student.SectionId}");
                if (sectionResp.IsSuccessStatusCode)
                {
                    var sectionData = await sectionResp.Content.ReadAsStringAsync();
                    student.Section = JsonSerializer.Deserialize<Section>(sectionData, options);
                }
            }

            if (student.SessionId.HasValue)
            {
                var sessionResp = await _client.GetAsync($"Sessions/{student.SessionId}");
                if (sessionResp.IsSuccessStatusCode)
                {
                    var sessionData = await sessionResp.Content.ReadAsStringAsync();
                    student.Session = JsonSerializer.Deserialize<Session>(sessionData, options);
                }
            }

            if (student.UserId.HasValue)
            {
                var userResp = await _client.GetAsync($"Users/{student.UserId}");
                if (userResp.IsSuccessStatusCode)
                {
                    var userData = await userResp.Content.ReadAsStringAsync();
                    student.User = JsonSerializer.Deserialize<User>(userData, options);
                }
            }

            // ✅ Store filter values for "Back to List" link
            ViewBag.BackSessionId = student.SessionId;
            ViewBag.BackClassId = student.ClassId;
            ViewBag.BackSectionId = student.SectionId;
            ViewBag.BackMedium = student.Class?.Medium;

            return View(student);
        }
        // GET: Students/Create
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["Error"] = "You must be logged in to add a student. Please login first.";
                return RedirectToAction(nameof(Index));
            }

            await LoadClassesDropdown();
            await LoadSectionsDropdown();
            await LoadSessionsDropdown();

            ViewBag.AdminName = GetCurrentUserName();
            ViewBag.AdminId = userId.Value;
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,AdmissionDate,FirstName,LastName,FatherName,Dob,Gender,Grno,BloodGroup,AadharNo,ClassId,SectionId,SessionId,Address,City,State,Pincode,Phone,MotherPhone,PreviousSchool")] Student student, IFormFile? photoFile)
        {
            // Generate Admission No and Roll No
            student.AdmissionNo = await GenerateAdmissionNumber();
            if (student.ClassId.HasValue && student.SectionId.HasValue)
            {
                student.RollNo = await GenerateRollNumber(student.ClassId.Value, student.SectionId.Value);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                ModelState.AddModelError("", "User not logged in.");
                await LoadDropdownsWithSelected(student);
                return View(student);
            }
            student.UserId = currentUserId.Value;

            // Remove model state validation for auto-generated fields
            ModelState.Remove("AdmissionNo");
            ModelState.Remove("RollNo");
            ModelState.Remove("UserId");

            // Photo upload logic
            if (photoFile != null && photoFile.Length > 0)
            {
                if (photoFile.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError("StudentPhoto", "File size must be less than 5 MB");
                else
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "students");
                    Directory.CreateDirectory(uploadPath);
                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }
                    student.StudentPhoto = $"/images/students/{fileName}";
                }
            }
            else
            {
                ModelState.AddModelError("StudentPhoto", "Student Photo is required");
            }

            if (ModelState.IsValid)
            {
                var response = await _client.PostAsJsonAsync("Students", student);
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to create student. Please try again.");
            }

            await LoadDropdownsWithSelected(student);
            ViewBag.AdminName = GetCurrentUserName();
            ViewBag.AdminId = currentUserId;
            return View(student);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Students/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var student = JsonSerializer.Deserialize<Student>(data, options);

            // Get the medium from the student's current class
            string currentMedium = null;
            if (student.ClassId.HasValue)
            {
                var classResp = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResp.IsSuccessStatusCode)
                {
                    var classData = await classResp.Content.ReadAsStringAsync();
                    var classObj = JsonSerializer.Deserialize<Class>(classData, options);
                    currentMedium = classObj?.Medium;
                }
            }

            // ✅ Store filter values for "Back to List" link
            ViewBag.BackSessionId = student.SessionId;
            ViewBag.BackClassId = student.ClassId;
            ViewBag.BackSectionId = student.SectionId;
            ViewBag.BackMedium = currentMedium;

            // Load filtered dropdowns
            await LoadSessionsDropdown(student.SessionId);
            await LoadClassesDropdown(student.ClassId, student.SessionId, currentMedium);
            await LoadSectionsDropdown(student.SectionId);
            LoadMediums();

            // Get added by user name
            if (student.UserId.HasValue)
            {
                var userResp = await _client.GetAsync($"Users/{student.UserId}");
                if (userResp.IsSuccessStatusCode)
                {
                    var userData = await userResp.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<User>(userData, options);
                    ViewBag.AddedByName = user?.FullName ?? user?.Username ?? "Unknown";
                }
            }

            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,UserId,AdmissionNo,AdmissionDate,RollNo,FirstName,LastName,FatherName,Dob,Gender,Grno,BloodGroup,AadharNo,ClassId,SectionId,SessionId,Address,City,State,Pincode,Phone,MotherPhone,PreviousSchool,StudentPhoto")] Student student, IFormFile? StudentPhoto)
        {
            if (id != student.StudentId) return NotFound();

            // Handle image upload if new image is provided
            if (StudentPhoto != null && StudentPhoto.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(student.StudentPhoto))
                {
                    string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.StudentPhoto.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Create unique filename
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(StudentPhoto.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "students");
                Directory.CreateDirectory(uploadPath);
                string filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await StudentPhoto.CopyToAsync(stream);
                }
                student.StudentPhoto = $"/images/students/{fileName}";
            }

            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"Students/{id}", student);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Student updated successfully!";

                    // ---------- PRESERVE FILTERS ----------
                    // Get the medium from the student's updated class
                    string medium = null;
                    if (student.ClassId.HasValue)
                    {
                        var classResp = await _client.GetAsync($"Classes/{student.ClassId}");
                        if (classResp.IsSuccessStatusCode)
                        {
                            var classData = await classResp.Content.ReadAsStringAsync();
                            var classObj = JsonSerializer.Deserialize<Class>(classData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            medium = classObj?.Medium;
                        }
                    }

                    // Redirect to Index with the current filters (Session, Medium, Class, Section)
                    return RedirectToAction(nameof(Index), new
                    {
                        sessionId = student.SessionId,
                        medium = medium,
                        classId = student.ClassId,
                        sectionId = student.SectionId
                    });
                }
                ModelState.AddModelError("", "Update failed!");
            }

            // If we reach here, something went wrong – reload the dropdowns with the entered data
            await LoadClassesDropdown(student.ClassId);
            await LoadSectionsDropdown(student.SectionId);
            await LoadSessionsDropdown(student.SessionId);
            return View(student);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Students/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var student = JsonSerializer.Deserialize<Student>(data, options);

            // Load related entities for display
            if (student.ClassId.HasValue)
            {
                var classResp = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResp.IsSuccessStatusCode)
                {
                    var classData = await classResp.Content.ReadAsStringAsync();
                    student.Class = JsonSerializer.Deserialize<Class>(classData, options);
                }
            }

            if (student.SectionId.HasValue)
            {
                var sectionResp = await _client.GetAsync($"Sections/{student.SectionId}");
                if (sectionResp.IsSuccessStatusCode)
                {
                    var sectionData = await sectionResp.Content.ReadAsStringAsync();
                    student.Section = JsonSerializer.Deserialize<Section>(sectionData, options);
                }
            }

            if (student.SessionId.HasValue)
            {
                var sessionResp = await _client.GetAsync($"Sessions/{student.SessionId}");
                if (sessionResp.IsSuccessStatusCode)
                {
                    var sessionData = await sessionResp.Content.ReadAsStringAsync();
                    student.Session = JsonSerializer.Deserialize<Session>(sessionData, options);
                }
            }

            if (student.UserId.HasValue)
            {
                var userResp = await _client.GetAsync($"Users/{student.UserId}");
                if (userResp.IsSuccessStatusCode)
                {
                    var userData = await userResp.Content.ReadAsStringAsync();
                    student.User = JsonSerializer.Deserialize<User>(userData, options);
                }
            }

            // ✅ Store filter values for "Back to List" link
            ViewBag.BackSessionId = student.SessionId;
            ViewBag.BackClassId = student.ClassId;
            ViewBag.BackSectionId = student.SectionId;
            ViewBag.BackMedium = student.Class?.Medium;

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? sessionId, string medium, int? classId, int? sectionId)
        {
            // First get the student to delete the photo file and also to retrieve filter values
            Student? student = null;
            var response = await _client.GetAsync($"Students/{id}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                student = JsonSerializer.Deserialize<Student>(data, options);

                if (!string.IsNullOrEmpty(student?.StudentPhoto))
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.StudentPhoto.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }

            // If filter values weren't passed in the request (e.g., from the Delete view), use the student's own values
            if (student != null)
            {
                if (sessionId == null) sessionId = student.SessionId;
                if (classId == null) classId = student.ClassId;
                if (sectionId == null) sectionId = student.SectionId;
                if (string.IsNullOrEmpty(medium) && classId.HasValue)
                {
                    // Fetch medium from class
                    var classResp = await _client.GetAsync($"Classes/{classId.Value}");
                    if (classResp.IsSuccessStatusCode)
                    {
                        var classData = await classResp.Content.ReadAsStringAsync();
                        var classObj = JsonSerializer.Deserialize<Class>(classData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        medium = classObj?.Medium;
                    }
                }
            }

            var deleteResponse = await _client.DeleteAsync($"Students/{id}");
            if (!deleteResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Delete failed!");
                // If delete fails, return to the Delete view with the same student data
                return View(student);
            }

            TempData["Success"] = "Student deleted successfully!";

            // Redirect to Index with the preserved filter values
            return RedirectToAction(nameof(Index), new
            {
                sessionId = sessionId,
                medium = medium,
                classId = classId,
                sectionId = sectionId
            });
        }
        // ---------------------- Helper Methods ----------------------

        private async Task<bool> StudentExists(int id)
        {
            var response = await _client.GetAsync($"Students/{id}");
            return response.IsSuccessStatusCode;
        }

        // Generate 6-digit sequential Admission Number using API data
        private async Task<string> GenerateAdmissionNumber()
        {
            var response = await _client.GetAsync("Students");
            if (!response.IsSuccessStatusCode) return "000001";

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var students = JsonSerializer.Deserialize<List<Student>>(data, options);

            int nextNumber = 1;
            if (students != null && students.Any())
            {
                var lastStudent = students.OrderByDescending(s => s.StudentId).FirstOrDefault();
                if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.AdmissionNo) && int.TryParse(lastStudent.AdmissionNo, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }
            return nextNumber.ToString("D6");
        }

        // Generate Roll Number based on Class and Section using API data
        private async Task<int> GenerateRollNumber(int classId, int sectionId)
        {
            var response = await _client.GetAsync("Students");
            if (!response.IsSuccessStatusCode) return 1;

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var students = JsonSerializer.Deserialize<List<Student>>(data, options);

            var studentsInClassSection = students?
                .Where(s => s.ClassId == classId && s.SectionId == sectionId && s.RollNo.HasValue)
                .OrderByDescending(s => s.RollNo)
                .ToList();

            int nextRollNo = 1;
            if (studentsInClassSection != null && studentsInClassSection.Any())
            {
                nextRollNo = studentsInClassSection.First().RollNo.Value + 1;
            }
            return nextRollNo;
        }

        // Load Classes dropdown (optionally with selected value, filtered by session and medium)
        private async Task LoadClassesDropdown(int? selectedClassId = null, int? sessionId = null, string medium = null)
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allClasses = JsonSerializer.Deserialize<List<Class>>(data, options);

                // Filter by sessionId and medium if provided
                var filteredClasses = allClasses;
                if (sessionId.HasValue)
                    filteredClasses = filteredClasses.Where(c => c.SessionId == sessionId).ToList();
                if (!string.IsNullOrEmpty(medium))
                    filteredClasses = filteredClasses.Where(c => c.Medium == medium).ToList();

                ViewData["ClassId"] = new SelectList(filteredClasses, "ClassId", "ClassName", selectedClassId);
            }
            else
            {
                ViewData["ClassId"] = new SelectList(new List<Class>(), "ClassId", "ClassName");
            }
        }

        // Load Sections dropdown
        private async Task LoadSectionsDropdown(int? selectedSectionId = null)
        {
            var response = await _client.GetAsync("Sections");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sections = JsonSerializer.Deserialize<List<Section>>(data, options);
                ViewData["SectionId"] = new SelectList(sections, "SectionId", "SectionName", selectedSectionId);
            }
            else
            {
                ViewData["SectionId"] = new SelectList(new List<Section>(), "SectionId", "SectionName");
            }
        }

        // Load Sessions dropdown
        private async Task LoadSessionsDropdown(int? selectedSessionId = null)
        {
            var response = await _client.GetAsync("Sessions");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sessions = JsonSerializer.Deserialize<List<Session>>(data, options);
                ViewData["SessionId"] = new SelectList(sessions, "SessionId", "SessionName", selectedSessionId);
            }
            else
            {
                ViewData["SessionId"] = new SelectList(new List<Session>(), "SessionId", "SessionName");
            }
        }

        // Helper to reload all dropdowns with selected values (used when ModelState invalid on Create/Edit)
        private async Task LoadDropdownsWithSelected(Student student)
        {
            await LoadClassesDropdown(student.ClassId);
            await LoadSectionsDropdown(student.SectionId);
            await LoadSessionsDropdown(student.SessionId);
        }

        // GET: GetClassesByMedium (for cascading dropdowns)
        [HttpGet]
        public async Task<JsonResult> GetClassesByMedium(string medium, int sessionId)
        {
            var response = await _client.GetAsync("Classes");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allClasses = JsonSerializer.Deserialize<List<Class>>(data, options);
                var filteredClasses = allClasses
                    .Where(c => c.Medium == medium && c.SessionId == sessionId)
                    .Select(c => new { value = c.ClassId, text = c.ClassName })
                    .ToList();
                return Json(filteredClasses);
            }
            return Json(new List<object>());
        }

        // GET: GetSectionsByClass
        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sectionResponse = await _client.GetAsync("Sections");
            if (!sectionResponse.IsSuccessStatusCode)
                return Json(new List<object>());

            var sectionData = await sectionResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var allSections = JsonSerializer.Deserialize<List<Section>>(sectionData, options);
            if (allSections == null)
                return Json(new List<object>());

            var filteredSections = allSections
                .Where(x => x.ClassId == classId)
                .OrderBy(x => x.SectionName)
                .Select(x => new { value = x.SectionId, text = x.SectionName })
                .ToList();
            return Json(filteredSections);
        }

        // Promotion
        public async Task<IActionResult> Promotion()
        {
            List<Student> studentList = new List<Student>();
            var response = await _client.GetAsync("Students");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                studentList = JsonSerializer.Deserialize<List<Student>>(data, options);
                foreach (var student in studentList)
                {
                    if (student.ClassId.HasValue)
                    {
                        var classResp = await _client.GetAsync($"Classes/{student.ClassId}");
                        if (classResp.IsSuccessStatusCode)
                        {
                            var classData = await classResp.Content.ReadAsStringAsync();
                            student.Class = JsonSerializer.Deserialize<Class>(classData, options);
                        }
                    }
                    if (student.SectionId.HasValue)
                    {
                        var sectionResp = await _client.GetAsync($"Sections/{student.SectionId}");
                        if (sectionResp.IsSuccessStatusCode)
                        {
                            var sectionData = await sectionResp.Content.ReadAsStringAsync();
                            student.Section = JsonSerializer.Deserialize<Section>(sectionData, options);
                        }
                    }
                    if (student.SessionId.HasValue)
                    {
                        var sessionResp = await _client.GetAsync($"Sessions/{student.SessionId}");
                        if (sessionResp.IsSuccessStatusCode)
                        {
                            var sessionData = await sessionResp.Content.ReadAsStringAsync();
                            student.Session = JsonSerializer.Deserialize<Session>(sessionData, options);
                        }
                    }
                }
            }
            var sessionResponse = await _client.GetAsync("Sessions");
            if (sessionResponse.IsSuccessStatusCode)
            {
                var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allSessions = JsonSerializer.Deserialize<List<Session>>(sessionData, options);
                ViewBag.AllSessions = allSessions;
            }
            return View(studentList);
        }

        // GET: GetNextClass
        [HttpGet]
        public async Task<JsonResult> GetNextClass(int currentClassId, int newSessionId)
        {
            var classResponse = await _client.GetAsync("Classes");
            if (!classResponse.IsSuccessStatusCode)
                return Json(null);

            var classData = await classResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var allClasses = JsonSerializer.Deserialize<List<Class>>(classData, options);
            var currentClass = allClasses.FirstOrDefault(x => x.ClassId == currentClassId);
            if (currentClass == null)
                return Json(null);

            int currentNumber = 0;
            int.TryParse(currentClass.ClassName, out currentNumber);
            int nextNumber = currentNumber + 1;
            var nextClass = allClasses.FirstOrDefault(x =>
                x.ClassName == nextNumber.ToString() &&
                x.Medium == currentClass.Medium &&
                x.SessionId == newSessionId);
            if (nextClass == null)
                return Json(null);
            return Json(new { value = nextClass.ClassId, text = nextClass.ClassName });
        }

        [HttpPost]
        public async Task<IActionResult> PromoteStudents([FromBody] PromotionRequest request)
        {
            var response = await _client.PostAsJsonAsync("Students/PromoteStudents", request);
            if (response.IsSuccessStatusCode)
                return Ok();
            var error = await response.Content.ReadAsStringAsync();
            return BadRequest(error);
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
    }
}