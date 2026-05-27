using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SVM.Models;

namespace SVM.Controllers
{
    public class AdminController : Controller
    {
        private readonly HttpClient _client;

        public AdminController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        public async Task<IActionResult> AdminDashboard()
        {
			int totalClasses = 0;
			if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            // ----- Existing updates logic (bilkul same) -----
            List<Updates> updatesList = new List<Updates>();
            try
            {
                var response = await _client.GetAsync("Updates");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    updatesList = JsonSerializer.Deserialize<List<Updates>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

			// ----- Naye counts (students, staff, subjects, active session) -----
			int totalStudents = 0;
			int totalStaff = 0;
			int totalSubjects = 0;
			string activeSessionName = "N/A";

			Session activeSession = null;   // ✅ ADD THIS
			decimal totalExpense = 0;

			

            try
            {
				var sessionsResp = await _client.GetAsync("Sessions");
				if (sessionsResp.IsSuccessStatusCode)
				{
					var sessionsJson = await sessionsResp.Content.ReadAsStringAsync();
					var sessions = JsonSerializer.Deserialize<List<Session>>(sessionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					activeSession = sessions?.FirstOrDefault(s => s.IsActive == 1);
					var classesResp = await _client.GetAsync("Classes");

					if (classesResp.IsSuccessStatusCode)
					{
						var classesJson = await classesResp.Content.ReadAsStringAsync();

						var classes = JsonSerializer.Deserialize<List<Class>>(classesJson,
							new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

						if (activeSession != null)
						{
							totalClasses = classes?
								.Count(c => c.SessionId == activeSession.SessionId) ?? 0;
						}
					}

					if (activeSession == null && sessions != null)
					{
						var today = DateOnly.FromDateTime(DateTime.Today);
						activeSession = sessions.FirstOrDefault(s => today >= s.StartDate && today <= s.EndDate);
					}

					if (activeSession != null)
						activeSessionName = activeSession.SessionName;
				}
				// Students count (ACTIVE SESSION ONLY)
				var studentsResp = await _client.GetAsync("Students");
				if (studentsResp.IsSuccessStatusCode)
				{
					var studentsJson = await studentsResp.Content.ReadAsStringAsync();

					var students = JsonSerializer.Deserialize<List<Student>>(
						studentsJson,
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					if (activeSession != null)
					{
						totalStudents = students?
							.Count(s => s.SessionId == activeSession.SessionId) ?? 0;
					}
				}

				// Staff count (agar API exist karti hai)
				var staffResp = await _client.GetAsync("Staffs");
                if (staffResp.IsSuccessStatusCode)
                {
                    var staffJson = await staffResp.Content.ReadAsStringAsync();
                    var staffList = JsonSerializer.Deserialize<List<Staff>>(staffJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    totalStaff = staffList?.Count ?? 0;
                }

                // Subjects count
                var subjectsResp = await _client.GetAsync("Subjects");
                if (subjectsResp.IsSuccessStatusCode)
                {
                    var subjectsJson = await subjectsResp.Content.ReadAsStringAsync();
                    var subjects = JsonSerializer.Deserialize<List<Subject>>(subjectsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    totalSubjects = subjects?.Count ?? 0;
                }
                // Expenses total
                var expensesResp = await _client.GetAsync("Expenses");

                if (expensesResp.IsSuccessStatusCode)
                {
                    var expensesJson = await expensesResp.Content.ReadAsStringAsync();

                    var expenses = JsonSerializer.Deserialize<List<Expense>>
                    (
                        expensesJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );

                    totalExpense = expenses?.Sum(e => e.Amount) ?? 0;
                }
                // Active session name
                // Active session name
         
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalStaff = totalStaff;
            ViewBag.TotalSubjects = totalSubjects;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.ActiveSessionName = activeSessionName;
			ViewBag.TotalClasses = totalClasses;
			return View(updatesList ?? new List<Updates>());
        }

        // Keep AdminPanel as it was, or remove if not used
        public async Task<IActionResult> AdminPanel()
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            List<Updates> updatesList = new List<Updates>();

            try
            {
                var response = await _client.GetAsync("Updates");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    updatesList = JsonSerializer.Deserialize<List<Updates>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return View(updatesList ?? new List<Updates>());
        }
    }
}