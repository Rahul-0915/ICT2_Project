using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SVM.Models;
using System.Text.Json;

namespace SVM.Controllers.StudentPanel
{
    public class StudentPanelController : Controller
    {
        private readonly HttpClient _client;

        public StudentPanelController(IHttpClientFactory clientFactory)
        {
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
            string qrText = $"http://192.168.1.70:5269/StudentPanel/ViewCard?id={student.StudentId}";
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
    }
}