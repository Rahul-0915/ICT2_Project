using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using SVM.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace SVM.Controllers
{
    [LoginCheckFilter]
    public class FeeReportsController : Controller
    {
        private readonly HttpClient _client;

        public FeeReportsController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        public async Task<IActionResult> Index(int? sessionId, string medium, int? classId, int? sectionId,
            string searchTransactionId, string paymentMode, DateTime? fromDate, DateTime? toDate)
        {
            // Load Sessions
            var sessions = await GetSessions();

            // Auto select active session
            if (sessionId == null)
            {
                var activeSession = sessions.FirstOrDefault(x => x.IsActive == 1);
                if (activeSession != null)
                    sessionId = activeSession.SessionId;
            }

            // Load filters for dropdowns
            ViewBag.AllSessions = sessions;
            ViewBag.SelectedSessionId = sessionId;
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;
            ViewBag.SelectedSectionId = sectionId;

            // Payment modes for filter
            var paymentModes = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Modes" },
                new SelectListItem { Value = "Cash", Text = "Cash" },
                new SelectListItem { Value = "Razorpay", Text = "Online (Razorpay)" }
            };
            ViewBag.PaymentModes = paymentModes;
            ViewBag.SelectedPaymentMode = paymentMode;

            ViewBag.SearchTransactionId = searchTransactionId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            List<FeePaymentReportVM> payments = new List<FeePaymentReportVM>();

            // Fetch payments if session is selected
            if (sessionId.HasValue)
            {
                payments = await GetAllPayments(sessionId.Value, medium, classId, sectionId,
                    searchTransactionId, paymentMode, fromDate, toDate);
            }

            return View(payments);
        }

        // ✅ SIMPLE GET METHOD - Direct download (like ID Cards page)
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int sessionId, string medium, int? classId, int? sectionId,
            string searchTransactionId, string paymentMode, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var payments = await GetAllPayments(sessionId, medium ?? "", classId, sectionId,
                    searchTransactionId, paymentMode, fromDate, toDate);

                if (payments == null || !payments.Any())
                {
                    TempData["Error"] = "No data available to export.";
                    return RedirectToAction("Index", new { sessionId, medium, classId, sectionId });
                }

                // Build Excel data rows
                var rows = new List<object[]>();

                // Header with filters
                string sessionName = await GetSessionName(sessionId);
                string className = classId.HasValue && classId.Value > 0 ? await GetClassName(classId.Value) : "All Classes";
                string sectionName = sectionId.HasValue && sectionId.Value > 0 ? await GetSectionName(sectionId.Value) : "All Sections";
                string mediumText = string.IsNullOrEmpty(medium) ? "All Medium" : medium;

                rows.Add(new object[] { $"FEE COLLECTION REPORT" });
                rows.Add(new object[] { $"Generated on: {DateTime.Now:dd-MM-yyyy HH:mm:ss}" });
                rows.Add(new object[] { });
                rows.Add(new object[] { $"Session: {sessionName}" });
                rows.Add(new object[] { $"Medium: {mediumText}" });
                rows.Add(new object[] { $"Class: {className}" });
                rows.Add(new object[] { $"Section: {sectionName}" });

                if (!string.IsNullOrEmpty(paymentMode))
                    rows.Add(new object[] { $"Payment Mode: {paymentMode}" });

                rows.Add(new object[] { });
                rows.Add(new object[] { });

                // Table Headers
                rows.Add(new object[] { "Sr. No.", "Student Name", "Class", "Section", "Amount (₹)", "Payment Mode", "Transaction ID", "Payment Date" });

                // Table Data
                int srNo = 1;
                decimal totalAmount = 0;

                foreach (var payment in payments)
                {
                    rows.Add(new object[]
                    {
                        srNo++,
                        payment.StudentName,
                        payment.ClassName,
                        payment.SectionName,
                        payment.AmountPaid,
                        payment.PaymentMode,
                        payment.TransactionId,
                        payment.PaymentDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? "-"
                    });
                    totalAmount += payment.AmountPaid;
                }

                rows.Add(new object[] { });
                rows.Add(new object[] { "GRAND TOTAL:", "", "", "", totalAmount, "", "", "" });

                // Cash/Online Summary
                var cashTotal = payments.Where(p => p.PaymentMode == "Cash").Sum(p => p.AmountPaid);
                var onlineTotal = payments.Where(p => p.PaymentMode == "Razorpay").Sum(p => p.AmountPaid);

                rows.Add(new object[] { });
                rows.Add(new object[] { "SUMMARY:" });
                rows.Add(new object[] { $"Cash Collection: {cashTotal:N2}" });
                rows.Add(new object[] { $"Online Collection: {onlineTotal:N2}" });
                rows.Add(new object[] { $"Total Records: {payments.Count}" });

                rows.Add(new object[] { });
                rows.Add(new object[] { "This is a system generated report. No signature required." });

                // Convert to worksheet
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Fee Collection Report");

                    int row = 1;
                    foreach (var rowData in rows)
                    {
                        for (int col = 0; col < rowData.Length; col++)
                        {
                            worksheet.Cells[row, col + 1].Value = rowData[col];

                            // Apply styles
                            if (rowData[0]?.ToString() == "FEE COLLECTION REPORT")
                            {
                                worksheet.Cells[row, col + 1].Style.Font.Size = 18;
                                worksheet.Cells[row, col + 1].Style.Font.Bold = true;
                                worksheet.Cells[row, col + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            }
                            else if (rowData[0]?.ToString() == "GRAND TOTAL:")
                            {
                                worksheet.Cells[row, col + 1].Style.Font.Bold = true;
                                worksheet.Cells[row, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[row, col + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                            }
                            else if (row == 10) // Headers row
                            {
                                worksheet.Cells[row, col + 1].Style.Font.Bold = true;
                                worksheet.Cells[row, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[row, col + 1].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                            }

                            // Format currency columns
                            if (col == 4 && rowData[col] is decimal)
                            {
                                worksheet.Cells[row, col + 1].Style.Numberformat.Format = "#,##0.00";
                            }
                        }
                        row++;
                    }

                    // Merge title cells
                    worksheet.Cells["A1:H1"].Merge = true;
                    worksheet.Cells["A2:H2"].Merge = true;

                    // Auto fit columns
                    worksheet.Cells.AutoFitColumns();

                    // Generate file
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    string fileName = $"Fee_Collection_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excel Export Error: {ex.Message}");
                TempData["Error"] = "Failed to generate Excel report: " + ex.Message;
                return RedirectToAction("Index", new { sessionId, medium, classId, sectionId });
            }
        }

        // ========== HELPER METHODS ==========
        private async Task<List<Session>> GetSessions()
        {
            var res = await _client.GetAsync("Sessions");
            if (!res.IsSuccessStatusCode) return new List<Session>();
            var data = await res.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Session>>(data, options) ?? new List<Session>();
        }

        private async Task<string> GetSessionName(int sessionId)
        {
            var sessions = await GetSessions();
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            return session?.SessionName ?? sessionId.ToString();
        }

        private async Task<string> GetClassName(int classId)
        {
            var res = await _client.GetAsync($"Classes/{classId}");
            if (!res.IsSuccessStatusCode) return classId.ToString();
            var data = await res.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var classObj = JsonSerializer.Deserialize<Class>(data, options);
            return classObj?.ClassName ?? classId.ToString();
        }

        private async Task<string> GetSectionName(int sectionId)
        {
            var res = await _client.GetAsync($"Sections/{sectionId}");
            if (!res.IsSuccessStatusCode) return sectionId.ToString();
            var data = await res.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var sectionObj = JsonSerializer.Deserialize<Section>(data, options);
            return sectionObj?.SectionName ?? sectionId.ToString();
        }

        private async Task<List<FeePaymentReportVM>> GetAllPayments(int sessionId, string medium, int? classId, int? sectionId,
    string searchTransactionId, string paymentMode, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                // Build the URL with all parameters - SINGLE API CALL!
                string url = $"FeePayments/FilteredPayments?sessionId={sessionId}";

                if (!string.IsNullOrEmpty(medium))
                    url += $"&medium={Uri.EscapeDataString(medium)}";
                if (classId.HasValue && classId.Value > 0)
                    url += $"&classId={classId.Value}";
                if (sectionId.HasValue && sectionId.Value > 0)
                    url += $"&sectionId={sectionId.Value}";
                if (!string.IsNullOrEmpty(searchTransactionId))
                    url += $"&searchTransactionId={Uri.EscapeDataString(searchTransactionId)}";
                if (!string.IsNullOrEmpty(paymentMode))
                    url += $"&paymentMode={Uri.EscapeDataString(paymentMode)}";
                if (fromDate.HasValue)
                    url += $"&fromDate={fromDate.Value:yyyy-MM-dd}";
                if (toDate.HasValue)
                    url += $"&toDate={toDate.Value:yyyy-MM-dd}";

                var response = await _client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<FeePaymentReportVM>();

                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var payments = JsonSerializer.Deserialize<List<FeePaymentReportVM>>(data, options);

                return payments ?? new List<FeePaymentReportVM>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllPayments: {ex.Message}");
                return new List<FeePaymentReportVM>();
            }
        }
        [HttpGet]
        public async Task<IActionResult> SearchAjax(int sessionId, string medium, int? classId, int? sectionId,
         string searchTransactionId, string paymentMode, DateTime? fromDate, DateTime? toDate)
        {
            var payments = await GetAllPayments(sessionId, medium ?? "", classId, sectionId,
                searchTransactionId, paymentMode, fromDate, toDate);

            // Build the HTML directly
            if (payments == null || !payments.Any())
            {
                return Content(@"
            <div class='no-data'>
                <i class='fa-regular fa-receipt' style='font-size:48px; color:#cbd5e1; margin-bottom:15px; display:block;'></i>
                <p>No payment records found for the selected criteria</p>
            </div>
        ");
            }

            var cashTotal = payments.Where(p => p.PaymentMode == "Cash").Sum(p => p.AmountPaid);
            var onlineTotal = payments.Where(p => p.PaymentMode == "Razorpay").Sum(p => p.AmountPaid);
            var grandTotal = cashTotal + onlineTotal;

            var sb = new System.Text.StringBuilder();

            // Summary Cards
            sb.Append(@"
    <div class='summary-cards'>
        <div class='summary-card cash'>
            <h3><i class='fa-solid fa-money-bill-wave'></i> Cash Collection</h3>
            <div class='amount'>₹" + cashTotal.ToString("N2") + @"</div>
        </div>
        <div class='summary-card online'>
            <h3><i class='fa-solid fa-credit-card'></i> Online Collection</h3>
            <div class='amount'>₹" + onlineTotal.ToString("N2") + @"</div>
        </div>
        <div class='summary-card total'>
            <h3><i class='fa-solid fa-chart-simple'></i> Grand Total</h3>
            <div class='amount'>₹" + grandTotal.ToString("N2") + @"</div>
        </div>
        <div class='summary-card'>
            <h3><i class='fa-regular fa-receipt'></i> Total Transactions</h3>
            <div class='amount'>" + payments.Count + @"</div>
        </div>
    </div>
    
    <div class='table-wrapper'>
        <table id='paymentsTable' class='data-table'>
            <thead>
                <tr>
                    <th>Sr. No.</th>
                    <th>Student Name</th>
                    <th>Class</th>
                    <th>Section</th>
                    <th>Amount (₹)</th>
                    <th>Payment Mode</th>
                    <th>Transaction ID</th>
                    <th>Payment Date</th>
                </tr>
            </thead>
            <tbody>");

            int sr = 1;
            foreach (var payment in payments)
            {
                string paymentDateFormatted = payment.PaymentDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? "-";
                string badgeClass = payment.PaymentMode == "Cash" ? "badge-cash" : "badge-online";

                sb.Append($@"
                <tr>
                    <td>{sr}</td>
                    <td>{System.Net.WebUtility.HtmlEncode(payment.StudentName)}</td>
                    <td>{System.Net.WebUtility.HtmlEncode(payment.ClassName)}</td>
                    <td>{System.Net.WebUtility.HtmlEncode(payment.SectionName)}</td>
                    <td>₹{payment.AmountPaid:N2}</td>
                    <td><span class='{badgeClass}'>{payment.PaymentMode}</span></td>
                    <td>{System.Net.WebUtility.HtmlEncode(payment.TransactionId)}</td>
                    <td>{paymentDateFormatted}</td>
                </tr>");
                sr++;
            }

            sb.Append(@"
            </tbody>
        </table>
    </div>
    
    <div class='total-box'>
        <div class='total-item'>Total Records: <strong>" + payments.Count + @"</strong></div>
        <div class='total-item'>Total Collection: <strong>₹" + grandTotal.ToString("N2") + @"</strong></div>
    </div>");

            return Content(sb.ToString());
        }
        [HttpGet]
        public async Task<IActionResult> GetClassesByMedium(string medium, int sessionId)
        {
            try
            {
                var res = await _client.GetAsync($"Classes/ByMediumAndSession?medium={medium}&sessionId={sessionId}");
                if (!res.IsSuccessStatusCode)
                    return Json(new List<SelectListItem>());

                var data = await res.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classes = JsonSerializer.Deserialize<List<Class>>(data, options);

                var result = classes?.Select(c => new SelectListItem
                {
                    Value = c.ClassId.ToString(),
                    Text = c.ClassName
                }).ToList() ?? new List<SelectListItem>();

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetClassesByMedium: {ex.Message}");
                return Json(new List<SelectListItem>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSectionsByClass(int classId)
        {
            try
            {
                var res = await _client.GetAsync($"Sections/ByClass/{classId}");
                if (!res.IsSuccessStatusCode)
                    return Json(new List<SelectListItem>());

                var data = await res.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var sections = JsonSerializer.Deserialize<List<Section>>(data, options);

                var result = sections?.Select(s => new SelectListItem
                {
                    Value = s.SectionId.ToString(),
                    Text = s.SectionName
                }).ToList() ?? new List<SelectListItem>();

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSectionsByClass: {ex.Message}");
                return Json(new List<SelectListItem>());
            }
        }
    }

    // DTO Classes
    public class FeePaymentReportVM
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SectionName { get; set; } = "";
        public decimal AmountPaid { get; set; }
        public string PaymentMode { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public DateTime? PaymentDate { get; set; }
    }

    public class FeePaymentDTO
    {
        public int PaymentId { get; set; }
        public int StudentId { get; set; }
        public int FeeId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMode { get; set; } = "";
        public string TransactionId { get; set; } = "";
    }

    public class StudentWithDetails
    {
        public Student Student { get; set; } = new Student();
        public Class Class { get; set; } = new Class();
        public Section Section { get; set; } = new Section();
        public Session Session { get; set; } = new Session();
        public User User { get; set; } = new User();
    }
}