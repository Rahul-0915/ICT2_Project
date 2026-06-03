using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SVM.Models;
using SVM.Services;
using System.Text.Json;
using System.Linq;
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
                        return View(student);
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

            return View(student);
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
            string qrText = $"http://192.168.1.75:5269/StudentPanel/ViewCard?id={student.StudentId}";
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

            int y = year ?? DateTime.Now.Year;
            int m = month ?? DateTime.Now.Month;

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

            // ================= VIEWBAG =================
            ViewBag.Year = y;
            ViewBag.Month = m;

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
            string? studentId =
                HttpContext.Session.GetString("StudentId");

            if (string.IsNullOrEmpty(studentId))
                return RedirectToAction("Login", "Account");

            var response =
                await _client.GetAsync(
                    $"FeeStructures/StudentFee/{studentId}");

            if (!response.IsSuccessStatusCode)
                return View();

            var json =
                await response.Content.ReadAsStringAsync();

            var fee =
                JsonSerializer.Deserialize<StudentFeeVM>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

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

    }
}