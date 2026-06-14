using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SVM.Models;
using SVM.Services;
using System.Text.Json;
using System.Linq;
using System.Text.Json.Serialization;
namespace SVM.Controllers.StudentPanel
{
    public class StudentPanelController : Controller
    {
        private readonly HttpClient _client;
        private readonly IHttpClientFactory _httpClientFactory;
        public StudentPanelController(IHttpClientFactory clientFactory)
        {
            _httpClientFactory = clientFactory;
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        // Helper method to check if current user is student (GroupId = 3)
        private bool IsStudentUser()
        {
            var groupId = HttpContext.Session.GetString("GroupId");
            return groupId == "3";
        }

        public async Task<IActionResult> MyProfile()
        {
            string? userId = HttpContext.Session.GetString("UserId");
            string? groupId = HttpContext.Session.GetString("GroupId");
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            Student? student = null;

            if (!string.IsNullOrEmpty(studentId))
            {
                var response = await _client.GetAsync($"Students/{studentId}");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    student = JsonSerializer.Deserialize<Student>(
                        responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (student != null)
                        return View(student);
                }
            }

            string endpoint;
            if (groupId == "3")
            {
                endpoint = $"Students/ByUser/{userId}";
                Console.WriteLine($"Calling Student endpoint with session filter: {endpoint}");
            }
            else
            {
                endpoint = $"Students/ByUserNoSession/{userId}";
                Console.WriteLine($"Calling Admin/Teacher endpoint without session filter: {endpoint}");
            }

            var response2 = await _client.GetAsync(endpoint);

            if (!response2.IsSuccessStatusCode)
            {
                var errorContent = await response2.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error: {response2.StatusCode} - {errorContent}");

                if (groupId == "3" && response2.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ViewBag.Error = "Your profile is not active for the current academic session. Please contact administrator.";
                    return View("Error");
                }

                return View("Student", new Student());
            }

            var responseJson2 = await response2.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseJson2))
            {
                Console.WriteLine("API returned empty response");
                return View("Student", new Student());
            }

            student = JsonSerializer.Deserialize<Student>(
                responseJson2,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
            {
                Console.WriteLine("Failed to deserialize student data");
                return View("Student", new Student());
            }

            HttpContext.Session.SetString("StudentId", student.StudentId.ToString());

            return View(student);
        }

        public async Task<IActionResult> Student()
        {
            string? userId = HttpContext.Session.GetString("UserId");
            string? groupId = HttpContext.Session.GetString("GroupId");
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            Student? student = null;

            if (!string.IsNullOrEmpty(studentId))
            {
                var response = await _client.GetAsync($"Students/{studentId}");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    student = JsonSerializer.Deserialize<Student>(
                        responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (student != null)
                    {
                        // Set Class Name and Section Name in ViewBag
                        await SetStudentDetailsInViewBag(student);
                        return View(student);
                    }
                }
            }

            string endpoint = (groupId == "3")
                ? $"Students/ByUser/{userId}"
                : $"Students/ByUserNoSession/{userId}";

            var response2 = await _client.GetAsync(endpoint);

            if (!response2.IsSuccessStatusCode)
                return View(new Student());

            var responseJson2 = await response2.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseJson2))
                return View(new Student());

            student = JsonSerializer.Deserialize<Student>(
                responseJson2,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
                return View(new Student());

            HttpContext.Session.SetString("StudentId", student.StudentId.ToString());

            // Set Class Name and Section Name in ViewBag
            await SetStudentDetailsInViewBag(student);

            return View(student);
        }

        // Add this helper method to set Class and Section details
        private async Task SetStudentDetailsInViewBag(Student student)
        {
            // Set Class Name
            if (student.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classJson = await classResponse.Content.ReadAsStringAsync();
                    var classObj = JsonSerializer.Deserialize<Class>(
                        classJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.ClassName = classObj?.ClassName ?? "N/A";
                    ViewBag.Medium = classObj?.Medium ?? "";

                    // Also store in session for other pages
                    HttpContext.Session.SetString("ClassName", classObj?.ClassName ?? "N/A");
                }
                else
                {
                    ViewBag.ClassName = "N/A";
                }
            }
            else
            {
                ViewBag.ClassName = "N/A";
            }

            // Set Section Name
            if (student.SectionId.HasValue)
            {
                var sectionResponse = await _client.GetAsync($"Sections/{student.SectionId}");
                if (sectionResponse.IsSuccessStatusCode)
                {
                    var sectionJson = await sectionResponse.Content.ReadAsStringAsync();
                    var sectionObj = JsonSerializer.Deserialize<Section>(
                        sectionJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.SectionName = sectionObj?.SectionName ?? "N/A";

                    // Also store in session for other pages
                    HttpContext.Session.SetString("SectionName", sectionObj?.SectionName ?? "N/A");
                }
                else
                {
                    ViewBag.SectionName = "N/A";
                }
            }
            else
            {
                ViewBag.SectionName = "N/A";
            }
        }
        public async Task<IActionResult> DigitalICard()
        {
            string? userId = HttpContext.Session.GetString("UserId");
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            Student? student = null;

            // Try session StudentId first
            if (!string.IsNullOrEmpty(studentId))
            {
                var response = await _client.GetAsync($"Students/{studentId}");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    student = JsonSerializer.Deserialize<Student>(
                        responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }

            // Fallback
            if (student == null)
            {
                var response = await _client.GetAsync($"Students/ByUser/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    student = JsonSerializer.Deserialize<Student>(
                        responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (student != null)
                    {
                        HttpContext.Session.SetString("StudentId", student.StudentId.ToString());
                    }
                }
            }

            if (student == null)
            {
                ViewBag.Error = "Student data not found";
                return View("Error");
            }

            // ================= CLASS DETAILS =================
            if (student.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classJson = await classResponse.Content.ReadAsStringAsync();
                    var classObj = JsonSerializer.Deserialize<Class>(
                        classJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.ClassName = classObj?.ClassName;
                    ViewBag.Medium = classObj?.Medium;   // ✅ Used for logo & school name
                }
            }

            // ================= SECTION DETAILS =================
            if (student.SectionId.HasValue)
            {
                var sectionResponse = await _client.GetAsync($"Sections/{student.SectionId}");
                if (sectionResponse.IsSuccessStatusCode)
                {
                    var sectionJson = await sectionResponse.Content.ReadAsStringAsync();
                    var sectionObj = JsonSerializer.Deserialize<Section>(
                        sectionJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.SectionName = sectionObj?.SectionName;
                }
            }

            // ================= QR CODE GENERATION =================
            string qrText = $"http://192.168.1.78:5269/StudentPanel/ViewCard?id={student.StudentId}";
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                ViewBag.QRCode = "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
            }

            return View(student);
        }
        [HttpGet]
        public async Task<IActionResult> ViewCard(int id)
        {
            var response = await _client.GetAsync($"Students/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound("Student not found");

            var responseJson = await response.Content.ReadAsStringAsync();

            var student = JsonSerializer.Deserialize<Student>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
                return NotFound("Student data invalid");

            // ================= CLASS DETAILS =================
            if (student.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");

                if (classResponse.IsSuccessStatusCode)
                {
                    var classJson = await classResponse.Content.ReadAsStringAsync();

                    var classObj = JsonSerializer.Deserialize<Class>(
                        classJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    ViewBag.ClassName = classObj?.ClassName;
                    ViewBag.Medium = classObj?.Medium;
                }
                else
                {
                    ViewBag.ClassName = "N/A";
                    ViewBag.Medium = "";
                }
            }

            // ================= SECTION DETAILS =================
            if (student.SectionId.HasValue)
            {
                var sectionResponse = await _client.GetAsync($"Sections/{student.SectionId}");

                if (sectionResponse.IsSuccessStatusCode)
                {
                    var sectionJson = await sectionResponse.Content.ReadAsStringAsync();

                    var sectionObj = JsonSerializer.Deserialize<Section>(
                        sectionJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    ViewBag.SectionName = sectionObj?.SectionName;
                }
                else
                {
                    ViewBag.SectionName = "N/A";
                }
            }
            else
            {
                ViewBag.SectionName = "N/A";
            }

            return View(student);
        }
        public IActionResult NoticeBoard()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveNotices()
        {
            var response = await _client.GetAsync("Updates/active");

            if (!response.IsSuccessStatusCode)
                return Json(new List<Updates>());

            var responseData = await response.Content.ReadAsStringAsync();

            var notices = JsonSerializer.Deserialize<List<Updates>>(
                responseData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var studentNotices = notices?
    .Where(x => x.Category == "StudentNotice")
    .OrderByDescending(x => x.CreatedAt)
    .ToList();

            return Json(studentNotices ?? new List<Updates>());
        }
        public async Task<IActionResult> MyTimetable()
        {
            string? classId = HttpContext.Session.GetString("ClassId");
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(classId))
                return RedirectToAction("Login", "Account");

            // Student detail nikalo
            var studentResponse =
                await _client.GetAsync($"Students/{studentId}");

            if (!studentResponse.IsSuccessStatusCode)
                return View(new List<Timetable>());

            var studentJson =
                await studentResponse.Content.ReadAsStringAsync();

            var student = JsonSerializer.Deserialize<Student>(
                studentJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (student == null)
                return View(new List<Timetable>());

            // Timetable fetch
            var response = await _client.GetAsync(
                $"Timetables?sessionId={student.SessionId}&classId={student.ClassId}&sectionId={student.SectionId}");

            if (!response.IsSuccessStatusCode)
                return View(new List<Timetable>());

            var json = await response.Content.ReadAsStringAsync();

            var timetable = JsonSerializer.Deserialize<List<Timetable>>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return View(timetable);
        }
        public async Task<IActionResult> Attendance(int? year, int? month)
        {
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(studentId))
                return RedirectToAction("Login", "Account");

            // ================= GET STUDENT =================
            var studentRes = await _client.GetAsync($"Students/{studentId}");

            if (!studentRes.IsSuccessStatusCode)
            {
                ViewBag.Error = "Student data not found.";
                return View(new StudentAttendanceVM
                {
                    Students = new List<StudentMonthItem>(),
                    Dates = new List<DateTime>(),
                    TotalDays = 0
                });
            }

            var studentJson = await studentRes.Content.ReadAsStringAsync();

            var student = JsonSerializer.Deserialize<Student>(
                studentJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
            {
                ViewBag.Error = "Invalid student data.";
                return View(new StudentAttendanceVM
                {
                    Students = new List<StudentMonthItem>(),
                    Dates = new List<DateTime>(),
                    TotalDays = 0
                });
            }

            // ================= VALIDATE CLASS/SESSION =================
            if (student.ClassId == null || student.SessionId == null || student.SectionId == null)
            {
                ViewBag.Error = "Attendance configuration missing.";
                return View(new StudentAttendanceVM
                {
                    Students = new List<StudentMonthItem>(),
                    Dates = new List<DateTime>(),
                    TotalDays = 0
                });
            }

            // ✅ FIX: Use current date if no year/month provided
            int y = year ?? DateTime.Now.Year;
            int m = month ?? DateTime.Now.Month;

            // ✅ Validate that month is between 1-12
            if (m < 1) m = 1;
            if (m > 12) m = 12;

            // ✅ Validate year is reasonable (not future beyond current year)
            if (y > DateTime.Now.Year) y = DateTime.Now.Year;
            if (y < 2000) y = DateTime.Now.Year;

            // ================= API CALL =================
            var response = await _client.GetAsync(
                $"StudentAttendances/monthly-report" +
                $"?sessionId={student.SessionId}" +
                $"&classId={student.ClassId}" +
                $"&sectionId={student.SectionId}" +
                $"&year={y}" +
                $"&month={m}"
            );

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Attendance data not available.";
                return View(new StudentAttendanceVM
                {
                    Students = new List<StudentMonthItem>(),
                    Dates = new List<DateTime>(),
                    TotalDays = 0
                });
            }

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<StudentAttendanceVM>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // ================= SAFE FALLBACK =================
            data ??= new StudentAttendanceVM
            {
                Students = new List<StudentMonthItem>(),
                Dates = new List<DateTime>(),
                TotalDays = 0
            };

            var currentStudent = data.Students?
    .FirstOrDefault(x => x.RollNo == student.RollNo);

            data.Students = currentStudent != null
                ? new List<StudentMonthItem> { currentStudent }
                : new List<StudentMonthItem>();

            // ================= SET VIEWBAG FOR DISPLAY =================
            ViewBag.Year = y;
            ViewBag.Month = m;
            ViewBag.MonthName = new DateTime(y, m, 1).ToString("MMMM");
            ViewBag.CurrentYear = DateTime.Now.Year;
            ViewBag.CurrentMonth = DateTime.Now.Month;

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> AiChat([FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { reply = "Please enter a question.", options = Array.Empty<object>() });
            }

            // Chat History
            var historyJson = HttpContext.Session.GetString("ChatHistory");
            var history = string.IsNullOrEmpty(historyJson)
                ? new List<ChatMessage>()
                : JsonSerializer.Deserialize<List<ChatMessage>>(historyJson) ?? new List<ChatMessage>();

            // Student Details
            string studentName = HttpContext.Session.GetString("FullName") ?? "Student";
            string className = HttpContext.Session.GetString("ClassName") ?? "N/A";
            string studentId = HttpContext.Session.GetString("StudentId") ?? "";

            // AI Prompt
            string fullPrompt = $@"
You are SVM School AI Assistant.

Student Details:
Name: {studentName}
Class: {className}
StudentId: {studentId}

Instructions:
- Answer politely.
- Help students with school-related queries.
- If user asks about attendance, timetable, notices, profile or ID card, guide them.
- Keep answers short and useful.

Student Question:
{message}
";

            // *** YAHAN CHANGE KIYA HAI ***
            string reply;
            try
            {
                var geminiService = HttpContext.RequestServices.GetRequiredService<IGeminiService>();
                reply = await geminiService.GetChatResponseAsync(fullPrompt, history);
            }
            catch (Exception ex)
            {
                reply = $"Error: {ex.Message}";
            }
            // ***************************

            // Smart Buttons (ye waisa hi rahega)
            List<object> options = new();
            string lowerMsg = message.ToLower();
            if (lowerMsg.Contains("attendance") || lowerMsg.Contains("present") || lowerMsg.Contains("absent"))
                options.Add(new { text = "📊 Open Attendance", action = "/StudentPanel/Attendance" });
            if (lowerMsg.Contains("timetable") || lowerMsg.Contains("schedule") || lowerMsg.Contains("class timing"))
                options.Add(new { text = "📅 Open Timetable", action = "/StudentPanel/MyTimetable" });
            if (lowerMsg.Contains("id card") || lowerMsg.Contains("icard") || lowerMsg.Contains("identity card"))
                options.Add(new { text = "🪪 Open Digital ICard", action = "/StudentPanel/DigitalICard" });
            if (lowerMsg.Contains("notice") || lowerMsg.Contains("announcement"))
                options.Add(new { text = "📢 Open Notice Board", action = "/StudentPanel/NoticeBoard" });
            if (lowerMsg.Contains("profile") || lowerMsg.Contains("my details"))
                options.Add(new { text = "👤 Open My Profile", action = "/StudentPanel/MyProfile" });

            // Save Chat History
            history.Add(new ChatMessage { Role = "user", Content = message });
            history.Add(new ChatMessage { Role = "model", Content = reply });
            HttpContext.Session.SetString("ChatHistory", JsonSerializer.Serialize(history.TakeLast(20)));

            return Json(new { reply, options });
        }
        public async Task<IActionResult> FeesPayment()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(studentId))
                return RedirectToAction("Login", "Account");

            var response = await _client.GetAsync($"FeeStructures/StudentFee/{studentId}");

            if (!response.IsSuccessStatusCode)
            {
                TempData["FeeError"] = "Fee Structure not available.";
                return View(new StudentFeeVM());
            }

            var json = await response.Content.ReadAsStringAsync();

            var fee = JsonSerializer.Deserialize<StudentFeeVM>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (fee == null)
            {
                TempData["FeeError"] = "Fee Structure not available.";
                fee = new StudentFeeVM();
            }

            return View(fee);
        }
        public async Task<IActionResult> Receipt()
        {
            var studentIdStr = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentIdStr) || !int.TryParse(studentIdStr, out int studentId))
                return RedirectToAction("Login", "Account");

            var paymentClient = _httpClientFactory.CreateClient();
            paymentClient.BaseAddress = new Uri("http://localhost:5175/api/");
            var response = await paymentClient.GetAsync($"Payment/receipt/{studentId}");

            if (!response.IsSuccessStatusCode)
                return NotFound("Receipt not found.");

            var json = await response.Content.ReadAsStringAsync();
            var receipt = JsonSerializer.Deserialize<ReceiptViewModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Get Student, Class, Section, Medium info
            var studentResponse = await _client.GetAsync($"Students/{studentId}");
            if (studentResponse.IsSuccessStatusCode)
            {
                var studentJson = await studentResponse.Content.ReadAsStringAsync();
                var student = JsonSerializer.Deserialize<Student>(studentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (student?.ClassId != null)
                {
                    var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                    if (classResponse.IsSuccessStatusCode)
                    {
                        var classJson = await classResponse.Content.ReadAsStringAsync();
                        var classObj = JsonSerializer.Deserialize<Class>(classJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        // ✅ Set Medium
                        receipt.Medium = classObj?.Medium ?? "Gujarati";
                        ViewBag.Medium = receipt.Medium;
                    }
                }

                // ✅ Set Section Name
                if (student?.SectionId != null)
                {
                    var sectionResponse = await _client.GetAsync($"Sections/{student.SectionId}");
                    if (sectionResponse.IsSuccessStatusCode)
                    {
                        var sectionJson = await sectionResponse.Content.ReadAsStringAsync();
                        var sectionObj = JsonSerializer.Deserialize<Section>(sectionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        receipt.SectionName = sectionObj?.SectionName ?? "N/A";
                    }
                }
            }

            return View(receipt);
        }
        public class AiResponse
        {
            public string reply { get; set; } = "";
            public List<ActionItem> actions { get; set; } = new();
        }
        public class ActionItem
        {
            public string text { get; set; } = "";
            public string action { get; set; } = "";
        }
        public async Task<IActionResult> FeeStructure()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(studentId))
                return RedirectToAction("Login", "Account");

            // Get Fee Data
            var response = await _client.GetAsync($"FeeStructures/StudentFee/{studentId}");

            // If API call fails or fee data is missing/invalid
            if (!response.IsSuccessStatusCode)
            {
                TempData["FeeError"] = "Fee structure is not available for your class/session.";
                return RedirectToAction("FeesPayment");
            }

            var json = await response.Content.ReadAsStringAsync();
            var fee = JsonSerializer.Deserialize<StudentFeeVM>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Additional check: if fee object is null or has no FeeId (i.e., no structure)
            if (fee == null || fee.FeeId == 0)
            {
                TempData["FeeError"] = "Fee structure is not available for your class/session.";
                return RedirectToAction("FeesPayment");
            }

            // Get Class & Medium info for student
            var studentResponse = await _client.GetAsync($"Students/{studentId}");
            if (studentResponse.IsSuccessStatusCode)
            {
                var studentJson = await studentResponse.Content.ReadAsStringAsync();
                var student = JsonSerializer.Deserialize<Student>(
                    studentJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (student?.ClassId != null)
                {
                    var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                    if (classResponse.IsSuccessStatusCode)
                    {
                        var classJson = await classResponse.Content.ReadAsStringAsync();
                        var classObj = JsonSerializer.Deserialize<Class>(
                            classJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        ViewBag.ClassName = classObj?.ClassName;
                        ViewBag.Medium = classObj?.Medium;
                    }
                }

                // Fetch Session name
                if (student.SessionId > 0)
                {
                    var sessionResponse = await _client.GetAsync($"Sessions/{student.SessionId}");
                    if (sessionResponse.IsSuccessStatusCode)
                    {
                        var sessionJson = await sessionResponse.Content.ReadAsStringAsync();
                        var session = JsonSerializer.Deserialize<Session>(
                            sessionJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ViewBag.SessionName = session?.SessionName ?? "2024-25";
                    }
                    else
                    {
                        ViewBag.SessionName = "2024-25";
                    }
                }
                else
                {
                    ViewBag.SessionName = "2024-25";
                }
            }

            return View(fee);
        }
        // Add this method to StudentPanelController.cs
        public async Task<IActionResult> Result()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(studentId))
                return RedirectToAction("Login", "Account");

            // Get student details
            var studentResponse = await _client.GetAsync($"Students/{studentId}");
            if (!studentResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = "Student data not found.";
                return View(new StudentResultVM());
            }

            var studentJson = await studentResponse.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<Student>(
                studentJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
            {
                ViewBag.Error = "Invalid student data.";
                return View(new StudentResultVM());
            }

            // Get Medium from Class
            string medium = "";
            if (student.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classJson = await classResponse.Content.ReadAsStringAsync();
                    var classObj = JsonSerializer.Deserialize<Class>(
                        classJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    medium = classObj?.Medium ?? "";
                }
            }

            // Get all published exams
            string examsUrl = $"Exams/student-exams?sessionId={student.SessionId}&medium={medium}&classId={student.ClassId}&sectionId={student.SectionId}";
            var examsResponse = await _client.GetAsync(examsUrl);

            if (!examsResponse.IsSuccessStatusCode)
            {
                ViewBag.Message = "No exams found.";
                return View(new StudentResultVM());
            }

            var examsJson = await examsResponse.Content.ReadAsStringAsync();

            // ✅ Deserialize with proper DateOnly handling
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new DateOnlyJsonConverter() }
            };

            var exams = JsonSerializer.Deserialize<List<Exam>>(examsJson, options) ?? new List<Exam>();

            var result = new StudentResultVM
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                RollNo = student.RollNo ?? 0,
                GrNo = student.Grno?.ToString() ?? "",
                ClassName = "",
                SectionName = "",
                Exams = new List<StudentExamResult>()
            };

            // Get class name
            if (student.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classJson = await classResponse.Content.ReadAsStringAsync();
                    var classObj = JsonSerializer.Deserialize<Class>(classJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    result.ClassName = classObj?.ClassName ?? "N/A";
                }
            }

            // Get section name
            if (student.SectionId.HasValue)
            {
                var sectionResponse = await _client.GetAsync($"Sections/{student.SectionId}");
                if (sectionResponse.IsSuccessStatusCode)
                {
                    var sectionJson = await sectionResponse.Content.ReadAsStringAsync();
                    var sectionObj = JsonSerializer.Deserialize<Section>(sectionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    result.SectionName = sectionObj?.SectionName ?? "N/A";
                }
            }

            // Get marks for each exam
            foreach (var exam in exams)
            {
                var marksResponse = await _client.GetAsync($"Exams/student-marks?examId={exam.ExamId}&studentId={student.StudentId}");

                if (marksResponse.IsSuccessStatusCode)
                {
                    var marksJson = await marksResponse.Content.ReadAsStringAsync();
                    var marksData = JsonSerializer.Deserialize<StudentMarksResponse>(
                        marksJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (marksData != null)
                    {
                        // ✅ Convert DateOnly to DateTime
                        DateTime examDate = new DateTime(exam.StartDate.Year, exam.StartDate.Month, exam.StartDate.Day);

                        var examResult = new StudentExamResult
                        {
                            ExamId = exam.ExamId,
                            ExamName = exam.ExamName ?? "",
                            ExamType = exam.ExamType ?? "",
                            ExamDate = examDate,
                            Subjects = marksData.Subjects ?? new List<StudentSubjectMark>(),
                            TotalObtainedMarks = marksData.Subjects?.Sum(s => s.ObtainedMarks) ?? 0,
                            TotalMaxMarks = marksData.Subjects?.Sum(s => s.TotalMarks) ?? 0,
                            Percentage = 0,
                            Result = "Pending"
                        };

                        if (examResult.TotalMaxMarks > 0)
                        {
                            examResult.Percentage = (examResult.TotalObtainedMarks / examResult.TotalMaxMarks) * 100;
                        }

                        bool isFailed = marksData.Subjects?.Any(s => s.ObtainedMarks < s.PassingMarks) ?? false;
                        examResult.Result = isFailed ? "Fail" : "Pass";

                        result.Exams.Add(examResult);
                    }
                }
            }

            return View(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetExamResult(int examId)
        {
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(studentId))
                return Json(new { error = "Student not found" });

            var marksResponse = await _client.GetAsync($"Exams/student-marks?examId={examId}&studentId={studentId}");

            if (!marksResponse.IsSuccessStatusCode)
                return Json(new { error = "Marks not found" });

            var marksJson = await marksResponse.Content.ReadAsStringAsync();
            var marksData = JsonSerializer.Deserialize<StudentMarksResponse>(
                marksJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return Json(marksData);
        }
        [HttpGet]
        public async Task<IActionResult> GetTodayTimetable()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentId))
                return Json(new List<object>());

            // Get student details
            var studentResponse = await _client.GetAsync($"Students/{studentId}");
            if (!studentResponse.IsSuccessStatusCode)
                return Json(new List<object>());

            var studentJson = await studentResponse.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<Student>(studentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
                return Json(new List<object>());

            // Get today's day name (Monday, Tuesday, etc.)
            string today = DateTime.Now.DayOfWeek.ToString();

            // Fix: DayName in your model might be stored differently
            // Try both possible formats
            var response = await _client.GetAsync($"Timetables?sessionId={student.SessionId}&classId={student.ClassId}&sectionId={student.SectionId}");

            if (!response.IsSuccessStatusCode)
                return Json(new List<object>());

            var json = await response.Content.ReadAsStringAsync();
            var allTimetable = JsonSerializer.Deserialize<List<Timetable>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Filter for today's day (case insensitive)
            var todayTimetable = allTimetable?
                .Where(t => t.DayName.Equals(today, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.LectureNo)
                .ToList();

            if (todayTimetable == null || todayTimetable.Count == 0)
            {
                return Json(new List<object>());
            }

            var result = todayTimetable.Select(t => new
            {
                subjectName = t.Subject?.SubjectName ?? "N/A",
                startTime = $"{t.StartTime.Hour:D2}:{t.StartTime.Minute:D2}",
                endTime = $"{t.EndTime.Hour:D2}:{t.EndTime.Minute:D2}",
                lectureNo = t.LectureNo
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPublishedResultsCount()  // Renamed for clarity
        {
            string? studentId = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentId))
                return Json(new { count = 0 });

            // Get student details first
            var studentResponse = await _client.GetAsync($"Students/{studentId}");
            if (!studentResponse.IsSuccessStatusCode)
                return Json(new { count = 0 });

            var studentJson = await studentResponse.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<Student>(studentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
                return Json(new { count = 0 });

            // Get Medium from Class
            string medium = "";
            if (student.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classJson = await classResponse.Content.ReadAsStringAsync();
                    var classObj = JsonSerializer.Deserialize<Class>(classJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    medium = classObj?.Medium ?? "";
                }
            }

            // Get exams
            var examsResponse = await _client.GetAsync($"Exams/student-exams?sessionId={student.SessionId}&medium={medium}&classId={student.ClassId}&sectionId={student.SectionId}");

            if (!examsResponse.IsSuccessStatusCode)
                return Json(new { count = 0 });

            var examsJson = await examsResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new DateOnlyJsonConverter() }
            };

            var exams = JsonSerializer.Deserialize<List<Exam>>(examsJson, options) ?? new List<Exam>();

            // Count exams that have published results
            int resultCount = 0;
            foreach (var exam in exams)
            {
                var marksResponse = await _client.GetAsync($"Exams/student-marks?examId={exam.ExamId}&studentId={student.StudentId}");
                if (marksResponse.IsSuccessStatusCode)
                {
                    resultCount++;
                }
            }

            return Json(new { count = resultCount });
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestExamResult()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentId))
                return Json(new { hasResult = false });

            // Get student details
            var studentResponse = await _client.GetAsync($"Students/{studentId}");
            if (!studentResponse.IsSuccessStatusCode)
                return Json(new { hasResult = false });

            var studentJson = await studentResponse.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<Student>(studentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null)
                return Json(new { hasResult = false });

            // Get Medium from Class
            string medium = "";
            if (student.ClassId.HasValue)
            {
                var classResponse = await _client.GetAsync($"Classes/{student.ClassId}");
                if (classResponse.IsSuccessStatusCode)
                {
                    var classJson = await classResponse.Content.ReadAsStringAsync();
                    var classObj = JsonSerializer.Deserialize<Class>(classJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    medium = classObj?.Medium ?? "";
                }
            }

            // Get exams
            var examsResponse = await _client.GetAsync($"Exams/student-exams?sessionId={student.SessionId}&medium={medium}&classId={student.ClassId}&sectionId={student.SectionId}");

            if (!examsResponse.IsSuccessStatusCode)
                return Json(new { hasResult = false });

            var examsJson = await examsResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new DateOnlyJsonConverter() }
            };

            var exams = JsonSerializer.Deserialize<List<Exam>>(examsJson, options) ?? new List<Exam>();

            // Get latest exam with results
            Exam latestExamWithResult = null;
            StudentMarksResponse latestMarks = null;

            foreach (var exam in exams.OrderByDescending(e => e.EndDate))
            {
                var marksResponse = await _client.GetAsync($"Exams/student-marks?examId={exam.ExamId}&studentId={student.StudentId}");
                if (marksResponse.IsSuccessStatusCode)
                {
                    var marksJson = await marksResponse.Content.ReadAsStringAsync();
                    var marksData = JsonSerializer.Deserialize<StudentMarksResponse>(marksJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (marksData != null && marksData.Subjects != null && marksData.Subjects.Any())
                    {
                        latestExamWithResult = exam;
                        latestMarks = marksData;
                        break;
                    }
                }
            }

            if (latestExamWithResult == null || latestMarks == null)
            {
                return Json(new { hasResult = false });
            }

            var totalObtained = latestMarks.Subjects?.Sum(s => s.ObtainedMarks) ?? 0;
            var totalMax = latestMarks.Subjects?.Sum(s => s.TotalMarks) ?? 0;
            var percentage = totalMax > 0 ? (totalObtained / totalMax) * 100 : 0;
            var isPassed = !(latestMarks.Subjects?.Any(s => s.ObtainedMarks < s.PassingMarks) ?? false);

            return Json(new
            {
                hasResult = true,
                examName = latestExamWithResult.ExamName,
                percentage = Math.Round(percentage, 2),
                result = isPassed ? "Pass" : "Fail",
                totalObtained = totalObtained,
                totalMax = totalMax
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetSubjectsCount()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentId))
                return Json(new { count = 0 });

            // First get student details to know ClassId
            var studentResponse = await _client.GetAsync($"Students/{studentId}");
            if (!studentResponse.IsSuccessStatusCode)
                return Json(new { count = 0 });

            var studentJson = await studentResponse.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<Student>(studentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (student == null || student.ClassId == null)
                return Json(new { count = 0 });

            // Use the correct endpoint from your Subjects API
            var response = await _client.GetAsync($"Subjects/ByClass/{student.ClassId}");

            if (!response.IsSuccessStatusCode)
                return Json(new { count = 0 });

            var json = await response.Content.ReadAsStringAsync();
            var subjects = JsonSerializer.Deserialize<List<Subject>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Count the subjects
            var subjectCount = subjects?.Count ?? 0;

            return Json(new { count = subjectCount });
        }
        [HttpGet]
        public async Task<IActionResult> GetPendingFees()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");
            if (string.IsNullOrEmpty(studentId))
                return Json(new { amount = 0 });

            var response = await _client.GetAsync($"FeeStructures/StudentFee/{studentId}");
            if (!response.IsSuccessStatusCode)
                return Json(new { amount = 0 });

            var json = await response.Content.ReadAsStringAsync();
            var fee = JsonSerializer.Deserialize<StudentFeeVM>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Calculate pending amount
            decimal pending = 0;
            if (fee != null && !fee.AlreadyPaid)
            {
                pending = fee.TotalAmount;
            }

            return Json(new { amount = pending });
        }
        [HttpGet]
        public async Task<IActionResult> GetAttendanceSummary()
        {
            string? studentId = HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(studentId))
                return Json(new
                {
                    percentage = 0,
                    present = 0,
                    absent = 0
                });

            var studentRes = await _client.GetAsync($"Students/{studentId}");

            if (!studentRes.IsSuccessStatusCode)
                return Json(new
                {
                    percentage = 0,
                    present = 0,
                    absent = 0
                });

            var studentJson = await studentRes.Content.ReadAsStringAsync();

            var student = JsonSerializer.Deserialize<Student>(
                studentJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (student == null)
                return Json(new
                {
                    percentage = 0,
                    present = 0,
                    absent = 0
                });

            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            var response = await _client.GetAsync(
                $"StudentAttendances/monthly-report" +
                $"?sessionId={student.SessionId}" +
                $"&classId={student.ClassId}" +
                $"&sectionId={student.SectionId}" +
                $"&year={year}" +
                $"&month={month}");

            if (!response.IsSuccessStatusCode)
                return Json(new
                {
                    percentage = 0,
                    present = 0,
                    absent = 0
                });

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<StudentAttendanceVM>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            var item = data?.Students?
    .FirstOrDefault(x => x.RollNo == student.RollNo);


            return Json(new
            {
                percentage = item?.Percentage ?? 0,
                present = item?.Present ?? 0,
                absent = item?.Absent ?? 0
            });
        }

    }
}