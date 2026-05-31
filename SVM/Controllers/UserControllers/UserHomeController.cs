using Microsoft.AspNetCore.Mvc;
using SVM.Models;
using System.Text.Json;


namespace SVM.Controllers.UserControllers
{
    public class UserHomeController : Controller
    {
        private readonly HttpClient _client;

        public UserHomeController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        public async Task<IActionResult> UserHome()
        {
            ViewBag.ShowPreloader = true;

            //get sesssion staff students admin 
            await LoadStatisticsAsync();

            await LoadTopperStudentsAsync();

            var response = await _client.GetAsync("Updates/active");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var updates = JsonSerializer.Deserialize<List<Updates>>(data, options);

                // sirf events + latest 10
                var latestEvents = updates
                    .Where(x => x.Category == "event")
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(10)
                    .ToList();

                return View(latestEvents);
            }

            return View(new List<Updates>());
        }

        public IActionResult About()
        {
            ViewBag.ShowPreloader = false;
            return View();
        }

        // =========================
        // NOTICE BOARD
        // =========================

        public async Task<IActionResult> Notice()
        {
            ViewBag.ShowPreloader = false;

            var response = await _client.GetAsync("Updates/active");

            if (!response.IsSuccessStatusCode)
            {
                return View(new List<Updates>());
            }

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var updates = JsonSerializer.Deserialize<List<Updates>>(data, options);

            var notices = updates
                .Where(x => x.Category != null &&
                            x.Category.ToLower() == "notice")
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(notices);
        }

        // =========================
        // GALLERY
        // =========================

        public async Task<IActionResult> Gallery()
        {
            ViewBag.ShowPreloader = false;

            var response = await _client.GetAsync("Updates/active");

            if (!response.IsSuccessStatusCode)
            {
                return View(new List<Updates>());
            }

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var updates = JsonSerializer.Deserialize<List<Updates>>(data, options);

            var gallery = updates
                .Where(x => x.Category != null &&
                            x.Category.ToLower() == "event")
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(gallery);
        }
        private async Task<Session?> GetActiveSessionAsync()
        {
            var response = await _client.GetAsync("Sessions");

            if (!response.IsSuccessStatusCode)
                return null;

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var sessions = JsonSerializer.Deserialize<List<Session>>(data, options);

            return sessions?
                .FirstOrDefault(x => x.IsActive == 1);
        }
        private async Task LoadStatisticsAsync()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // =========================
            // ACTIVE SESSION
            // =========================

            var activeSession = await GetActiveSessionAsync();

            if (activeSession == null)
                return;

            ViewBag.ActiveSession =
                activeSession.SessionName;

            // =========================
            // TOTAL STUDENTS
            // =========================

            int totalStudents = 0;

            var studentResponse =
                await _client.GetAsync("Students");

            if (studentResponse.IsSuccessStatusCode)
            {
                var studentData =
                    await studentResponse.Content.ReadAsStringAsync();

                var students =
                    JsonSerializer.Deserialize<List<Student>>(studentData, options);

                totalStudents = students?
                    .Count(x => x.SessionId == activeSession.SessionId) ?? 0;
            }

            ViewBag.TotalStudents = totalStudents;

            // =========================
            // TOTAL STAFF
            // =========================

            int totalStaff = 0;

            int totalAdminStaff = 0;

            var staffResponse =
                await _client.GetAsync("Staffs");

            if (staffResponse.IsSuccessStatusCode)
            {
                var staffData =
                    await staffResponse.Content.ReadAsStringAsync();

                var staffs =
                    JsonSerializer.Deserialize<List<Staff>>(staffData, options);

                if (staffs != null)
                {
                    // TOTAL STAFF
                    totalStaff = staffs.Count();

                    // ONLY CLERK
                    totalAdminStaff = staffs
                        .Count(x =>
                            x.Designation != null &&
                            x.Designation.ToLower() == "clerk");
                }
            }

            ViewBag.TotalStaff = totalStaff;

            ViewBag.TotalAdminStaff = totalAdminStaff;
        }
        // =========================
        // TOPPER STUDENTS IMAGES
        // =========================

        private async Task LoadTopperStudentsAsync()
        {
            var response = await _client.GetAsync("Updates/active");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.TopperStudents = new List<Updates>();
                return;
            }

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var updates =
                JsonSerializer.Deserialize<List<Updates>>(data, options);

            var topperStudents = updates?
                .Where(x =>
                    //x.Title != null &&
                    //x.Title.ToLower() == "toperstudents")
                x.Category != null &&
                x.Category.ToLower() == "toperstudents")
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            ViewBag.TopperStudents = topperStudents;
        }

        public async Task<IActionResult> Staff()
        {
            List<Staff> staffList = new List<Staff>();

            var response = await _client.GetAsync("Staffs");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                staffList = JsonSerializer.Deserialize<List<Staff>>(data, options);
            }

            // Organize staff into sections
            var organizedStaff = new StaffViewModel
            {
                // Teaching Staff Section: Principal first, then Teachers
                TeachingStaff = staffList
                    .Where(x => x.Designation != null &&
                               (x.Designation.ToLower() == "principal" ||
                                x.Designation.ToLower() == "teacher" ||
                                x.Designation.ToLower().Contains("teaching")))
                    .OrderByDescending(x => x.Designation.ToLower() == "principal")
                    .ThenBy(x => x.FirstName)
                    .ToList(),

                // Admin Staff Section: All admin/clerk staff
                AdminStaff = staffList
                    .Where(x => x.Designation != null &&
                               (x.Designation.ToLower() == "clerk" ||
                                x.Designation.ToLower() == "admin" ||
                                x.Designation.ToLower().Contains("admin")))
                    .OrderBy(x => x.FirstName)
                    .ToList()
            };

            return View(organizedStaff);
        }
    }
}