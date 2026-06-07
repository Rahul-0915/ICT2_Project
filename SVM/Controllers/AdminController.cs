using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SVM.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SVM.Controllers
{
    [LoginCheckFilter]
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

			// =========================
			// ADMISSION INQUIRIES LOAD
			// =========================
			var inquiryResp = await _client.GetAsync("AdmissionInquiries");

			var inquiryJson = await inquiryResp.Content.ReadAsStringAsync();

			var inquiries = JsonSerializer.Deserialize<List<AdmissionInquiry>>(
				inquiryJson,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
			);

			// TOTAL
			ViewBag.TotalInquiries = inquiries?.Count ?? 0;

			// UNSEEN
			ViewBag.UnseenCount = inquiries?.Count(x => x.IsSeen == false) ?? 0;

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

        // GET: Admin/FeesPayment (Search Page)
        public async Task<IActionResult> FeesPayment(int? sessionId, string medium, int? classId, int? sectionId)
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.FullName = HttpContext.Session.GetString("FullName");

            List<Student> studentList = new List<Student>();

            var url = $"Students/WithDetails?sessionId={sessionId}&medium={medium}&classId={classId}&sectionId={sectionId}";
            var response = await _client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var studentsWithDetails = JsonSerializer.Deserialize<List<StudentWithDetails>>(data, options);

                if (studentsWithDetails != null && studentsWithDetails.Any())
                {
                    studentList = studentsWithDetails.Select(sd =>
                    {
                        var student = sd.Student;
                        student.Class = sd.Class;
                        student.Section = sd.Section;
                        student.Session = sd.Session;
                        student.User = sd.User;
                        return student;
                    }).ToList();
                }
            }

            // Load filters
            await LoadFeesFiltersAsync(sessionId, medium);

            // Auto-select active session
            if (!sessionId.HasValue)
            {
                var sessionResponse = await _client.GetAsync("Sessions");
                if (sessionResponse.IsSuccessStatusCode)
                {
                    var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var allSessions = JsonSerializer.Deserialize<List<Session>>(sessionData, options);
                    var activeSession = allSessions?.FirstOrDefault(x => x.IsActive == 1);
                    if (activeSession != null)
                    {
                        sessionId = activeSession.SessionId;
                    }
                }
            }

            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSectionId = sectionId;
            ViewBag.SelectedSessionId = sessionId;

            return View(studentList);
        }

        private async Task LoadFeesFiltersAsync(int? sessionId, string medium)
        {
            // Load sessions
            var sessionResponse = await _client.GetAsync("Sessions");
            if (sessionResponse.IsSuccessStatusCode)
            {
                var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allSessions = JsonSerializer.Deserialize<List<Session>>(sessionData, options);
                ViewBag.AllSessions = allSessions;
            }

            // Load classes with filters
            var classUrl = $"Classes/WithFilters?sessionId={sessionId}&medium={medium}";
            var classResponse = await _client.GetAsync(classUrl);
            if (classResponse.IsSuccessStatusCode)
            {
                var classData = await classResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allClasses = JsonSerializer.Deserialize<List<Class>>(classData, options);
                ViewBag.AllClasses = allClasses;
            }
        }

       

		// REPLACE the existing GetStudentFeeForCash method with this:
		[HttpGet]
		public async Task<JsonResult> GetStudentFeeForCash(int studentId)
		{
			try
			{
				var response = await _client.GetAsync($"FeePayments/StudentFeeDetails/{studentId}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();

                    try
                    {
                        using var doc = JsonDocument.Parse(errorJson);

                        string message = doc.RootElement
                                            .GetProperty("message")
                                            .GetString();

                        return Json(new
                        {
                            success = false,
                            message = message
                        });
                    }
                    catch
                    {
                        return Json(new
                        {
                            success = false,
                            message = errorJson
                        });
                    }
                }
                var json = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var feeDetails = JsonSerializer.Deserialize<dynamic>(json, options);

				return Json(new { success = true, data = feeDetails });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// REPLACE CashPayment method with this:
		[HttpPost]
		public async Task<IActionResult> CashPayment([FromBody] CashPaymentRequest request)
		{
			try
			{
				if (request == null)
					return Json(new { success = false, message = "Invalid payment data" });

				if (request.StudentId <= 0)
					return Json(new { success = false, message = "Invalid Student ID" });

				if (request.AmountPaid <= 0)
					return Json(new { success = false, message = "Amount must be greater than 0" });

				var paymentRequest = new
				{
					StudentId = request.StudentId,
					FeeId = request.FeeId,
					AmountPaid = request.AmountPaid,
					TransactionId = $"CASH-{DateTime.Now.Ticks}",
					
				};

				var response = await _client.PostAsJsonAsync("FeePayments/CashPayment", paymentRequest);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					var result = JsonSerializer.Deserialize<dynamic>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					int paymentId = result?.GetProperty("paymentId").GetInt32() ?? 0;

					return Json(new { success = true, message = "Payment recorded successfully!", paymentId = paymentId });
				}
				else
				{
					return Json(new { success = false, message = $"Payment failed: {responseContent}" });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}
        // Add this method to AdminController
        [HttpGet]
        public async Task<IActionResult> PrintReceipt(int id)
        {
            var response = await _client.GetAsync($"FeePayments/GenerateReceipt/{id}");

            if (!response.IsSuccessStatusCode)
                return Content("Receipt not found");

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var receipt = new
            {
                receiptNo = root.GetProperty("receiptNo").GetInt32(),
                studentName = root.GetProperty("studentName").GetString(),
                className = root.GetProperty("className").GetString(),
                sectionName = root.GetProperty("sectionName").GetString(),
                amountPaid = root.GetProperty("amountPaid").GetDecimal(),
                paymentDate = root.GetProperty("paymentDate").GetString(),
                paymentMode = root.GetProperty("paymentMode").GetString(),
                transactionId = root.GetProperty("transactionId").GetString()
            };

            // Get student details to fetch medium
            // First get payment to get studentId
            var paymentResponse = await _client.GetAsync($"FeePayments/{id}");
            if (paymentResponse.IsSuccessStatusCode)
            {
                var paymentJson = await paymentResponse.Content.ReadAsStringAsync();
                var payment = JsonSerializer.Deserialize<dynamic>(paymentJson, options);
                int studentId = payment?.GetProperty("studentId").GetInt32() ?? 0;

                // Get student to fetch medium from class
                var studentResponse = await _client.GetAsync($"Students/{studentId}");
                if (studentResponse.IsSuccessStatusCode)
                {
                    var studentJson = await studentResponse.Content.ReadAsStringAsync();
                    var student = JsonSerializer.Deserialize<dynamic>(studentJson, options);
                    int classId = student?.GetProperty("classId").GetInt32() ?? 0;

                    // Get class to fetch medium
                    var classResponse = await _client.GetAsync($"Classes/{classId}");
                    if (classResponse.IsSuccessStatusCode)
                    {
                        var classJson = await classResponse.Content.ReadAsStringAsync();
                        var classObj = JsonSerializer.Deserialize<dynamic>(classJson, options);
                        string medium = classObj?.GetProperty("medium").GetString() ?? "Gujarati";
                        ViewBag.Medium = medium;
                    }
                }
            }

            // Get fee structure for the student
            // You can fetch based on student's class and session
            ViewBag.Receipt = receipt;
            ViewBag.FeeStructure = null; // Set actual fee structure if needed

            return View();
        }
        // Add this at the end of the file, after the AdminController class closing brace
        public class CashPaymentRequest
        {
            public int StudentId { get; set; }
            public int FeeId { get; set; }
            public decimal AmountPaid { get; set; }
            
        }
        // Add this inside the AdminController class
        public class StudentWithDetails
        {
            public Student Student { get; set; }
            public Class Class { get; set; }
            public Section Section { get; set; }
            public Session Session { get; set; }
            public User User { get; set; }
        }

        [HttpGet]
        public async Task<JsonResult> CheckStudentPaymentStatus(int studentId)
        {
            try
            {
                var response = await _client.GetAsync($"FeePayments/StudentFeeDetails/{studentId}");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { totalFees = 0, alreadyPaid = 0 });
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var feeDetails = JsonSerializer.Deserialize<dynamic>(json, options);

                decimal totalFees = feeDetails?.GetProperty("totalFees").GetDecimal() ?? 0;
                decimal alreadyPaid = feeDetails?.GetProperty("alreadyPaid").GetDecimal() ?? 0;

                return Json(new { totalFees = totalFees, alreadyPaid = alreadyPaid });
            }
            catch (Exception ex)
            {
                return Json(new { totalFees = 0, alreadyPaid = 0 });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetStudentReceipts(int studentId)
        {
            try
            {
                var response = await _client.GetAsync($"FeePayments/ByStudent/{studentId}");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, receipts = new List<object>() });
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var payments = JsonSerializer.Deserialize<List<dynamic>>(json, options);

                var receipts = new List<object>();
                foreach (var payment in payments)
                {
                    receipts.Add(new
                    {
                        paymentId = payment.GetProperty("paymentId").GetInt32(),
                        receiptNo = payment.GetProperty("paymentId").GetInt32(),
                        amountPaid = payment.GetProperty("amountPaid").GetDecimal(),
                        paymentDate = payment.GetProperty("paymentDate").GetString(),
                        paymentMode = payment.GetProperty("paymentMode").GetString(),
                        transactionId = payment.GetProperty("transactionId").GetString()
                    });
                }

                return Json(new { success = true, receipts = receipts });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, receipts = new List<object>(), message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAllSessions()
        {
            var response = await _client.GetAsync("Sessions");
            if (!response.IsSuccessStatusCode)
                return Json(new List<object>());

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sessions = JsonSerializer.Deserialize<List<dynamic>>(json, options);

            var result = new List<object>();
            foreach (var session in sessions)
            {
                result.Add(new
                {
                    sessionId = session.GetProperty("sessionId").GetInt32(),
                    sessionName = session.GetProperty("sessionName").GetString(),
                    isActive = session.GetProperty("isActive").GetInt32()
                });
            }
            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetAllClassesList()
        {
            var response = await _client.GetAsync("Classes");
            if (!response.IsSuccessStatusCode)
                return Json(new List<object>());

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var classes = JsonSerializer.Deserialize<List<dynamic>>(json, options);

            var result = new List<object>();
            foreach (var cls in classes)
            {
                result.Add(new
                {
                    classId = cls.GetProperty("classId").GetInt32(),
                    className = cls.GetProperty("className").GetString(),
                    medium = cls.GetProperty("medium").GetString(),
                    sessionId = cls.GetProperty("sessionId").GetInt32()
                });
            }
            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetAllSectionsList()
        {
            var response = await _client.GetAsync("Sections");
            if (!response.IsSuccessStatusCode)
                return Json(new List<object>());

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sections = JsonSerializer.Deserialize<List<dynamic>>(json, options);

            var result = new List<object>();
            foreach (var section in sections)
            {
                result.Add(new
                {
                    sectionId = section.GetProperty("sectionId").GetInt32(),
                    sectionName = section.GetProperty("sectionName").GetString(),
                    classId = section.GetProperty("classId").GetInt32()
                });
            }
            return Json(result);
        }



    }


}