using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using SVM.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SVM.Controllers
{
    public class FeesStructuresController : Controller
    {
        private readonly HttpClient _client;
        public FeesStructuresController(IHttpClientFactory factory)
        {
            _client = factory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
        }

        public async Task<IActionResult> Index(int? sessionId, string medium, int? classId)
        {
            var sessions = await GetSessions();
            ViewBag.SessionList = new SelectList(sessions, "SessionId", "SessionName", sessionId);
            ViewBag.MediumList = new SelectList(new[] { "Gujarati", "English" }, medium);

            if (sessionId == null || string.IsNullOrEmpty(medium) || classId == null)
                return View();

            var feeList = await GetFeeStructures(sessionId.Value, classId.Value);
            Console.WriteLine($"FeeList count: {feeList?.Count ?? 0}");

            ViewBag.FeeStructures = feeList;
            ViewBag.SelectedSessionId = sessionId;
            ViewBag.SelectedMedium = medium;
            ViewBag.SelectedClassId = classId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(
        int? sessionId,
        string medium,
        int? classId)
        {
            var model = new FeeStructure();

            if (sessionId != null)
                model.SessionId = sessionId;

            if (classId != null)
                model.ClassId = classId;

            // SESSION DROPDOWN

            var sessions = await GetSessions();

            ViewBag.SessionList =
                new SelectList(
                    sessions,
                    "SessionId",
                    "SessionName",
                    sessionId
                );

            // MEDIUM DROPDOWN

            ViewBag.MediumList =
                new SelectList(
                    new[] { "Gujarati", "English" },
                    medium
                );

            ViewBag.Medium = medium;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    FeeStructure feeStructure,
    string medium)
        {
            // REMOVE NAVIGATION VALIDATION

            ModelState.Remove("Class");

            // VALIDATION FAILED

            if (!ModelState.IsValid)
            {
                var errors =
                    ModelState.Values
                    .SelectMany(v => v.Errors);

                foreach (var error in errors)
                {
                    Console.WriteLine(
                        $"Model Error: {error.ErrorMessage}"
                    );

                    TempData["Error"] =
                        error.ErrorMessage;
                }

                // DROPDOWNS RELOAD

                var sessions = await GetSessions();

                ViewBag.SessionList =
                    new SelectList(
                        sessions,
                        "SessionId",
                        "SessionName",
                        feeStructure.SessionId
                    );

                ViewBag.MediumList =
                    new SelectList(
                        new[] { "Gujarati", "English" },
                        medium
                    );

                ViewBag.Medium = medium;

                return View(feeStructure);
            }

            try
            {
                var response =
                    await _client.PostAsJsonAsync(
                        "FeeStructures",
                        feeStructure
                    );

                // SUCCESS

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(
                        nameof(Index),
                        new
                        {
                            sessionId =
                                feeStructure.SessionId,

                            medium,

                            classId =
                                feeStructure.ClassId
                        });
                }

                // DUPLICATE

                else if (
                    response.StatusCode ==
                    System.Net.HttpStatusCode.Conflict)
                {
                    ModelState.AddModelError(
                        "",
                        "Fee structure already exists for this session and class."
                    );
                }

                // OTHER API ERROR

                else
                {
                    var errorContent =
                        await response.Content
                        .ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        $"API Error: {response.StatusCode} - {errorContent}"
                    );
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Exception: {ex.Message}"
                );
            }

            // DROPDOWNS RELOAD AGAIN

            var sessionList =
                await GetSessions();

            ViewBag.SessionList =
                new SelectList(
                    sessionList,
                    "SessionId",
                    "SessionName",
                    feeStructure.SessionId
                );

            ViewBag.MediumList =
                new SelectList(
                    new[] { "Gujarati", "English" },
                    medium
                );

            ViewBag.Medium = medium;

            return View(feeStructure);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string medium, int? classId, int? sessionId)
        {
            var response = await _client.GetAsync($"FeeStructures/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var feeStructure = JsonSerializer.Deserialize<FeeStructure>(data, options);

            ViewBag.Medium = medium;
            ViewBag.ClassId = classId;
            ViewBag.SessionId = sessionId;   // <-- ADD THIS

            return View(feeStructure);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FeeStructure feeStructure, string medium)
        {
            if (id != feeStructure.FeeId) return BadRequest();
            if (ModelState.IsValid)
            {
                var response = await _client.PutAsJsonAsync($"FeeStructures/{id}", feeStructure);
                if (response.IsSuccessStatusCode)
                {
                    // ✅ Redirect with all filters
                    return RedirectToAction(nameof(Index), new
                    {
                        sessionId = feeStructure.SessionId,
                        medium = string.IsNullOrEmpty(medium) ? ViewBag.Medium : medium,
                        classId = feeStructure.ClassId
                    });
                }
                ModelState.AddModelError("", "Update failed!");
            }
            ViewBag.Medium = medium;
            return View(feeStructure);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int id, string medium, int? classId, int? sessionId)
        {
            var response = await _client.GetAsync($"FeeStructures/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();
            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var feeStructure = JsonSerializer.Deserialize<FeeStructure>(data, options);

            // --- Fetch Class Name ---
            int classIdToFetch = feeStructure.ClassId ?? classId ?? 0;
            if (classIdToFetch != 0)
            {
                try
                {
                    var classResponse = await _client.GetAsync($"Classes/{classIdToFetch}");
                    if (classResponse.IsSuccessStatusCode)
                    {
                        var classData = await classResponse.Content.ReadAsStringAsync();
                        var classObj = JsonSerializer.Deserialize<Class>(classData, options);
                        ViewBag.ClassName = classObj?.ClassName ?? "Not Found";
                    }
                    else
                    {
                        ViewBag.ClassName = $"Unable to load class (ID: {classIdToFetch})";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ClassName = $"Error: {ex.Message}";
                }
            }
            else
            {
                ViewBag.ClassName = "No class assigned";
            }

            // --- Fetch Session Name ---
            int sessionIdToFetch = feeStructure.SessionId ?? sessionId ?? 0;
            if (sessionIdToFetch != 0)
            {
                try
                {
                    var sessionResponse = await _client.GetAsync($"Sessions/{sessionIdToFetch}");
                    if (sessionResponse.IsSuccessStatusCode)
                    {
                        var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                        var sessionObj = JsonSerializer.Deserialize<Session>(sessionData, options);
                        ViewBag.SessionName = sessionObj?.SessionName ?? "Not Found";
                    }
                    else
                    {
                        ViewBag.SessionName = $"Unable to load session (ID: {sessionIdToFetch})";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.SessionName = $"Error: {ex.Message}";
                }
            }
            else
            {
                ViewBag.SessionName = "No session assigned";
            }

            // Store filters for redirect after delete
            TempData["ReturnSessionId"] = sessionId;
            TempData["ReturnMedium"] = medium;
            TempData["ReturnClassId"] = classId;

            ViewBag.Medium = medium;
            ViewBag.ClassId = classId;
            ViewBag.SessionId = sessionId;

            return View(feeStructure);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, int? sessionId, string medium, int? classId)
        {
            await _client.DeleteAsync($"FeeStructures/{id}");

            // Use passed parameters if available; fallback to TempData
            sessionId ??= TempData["ReturnSessionId"] as int?;
            medium ??= TempData["ReturnMedium"] as string;
            classId ??= TempData["ReturnClassId"] as int?;

            return RedirectToAction(nameof(Index), new { sessionId, medium, classId });
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id, string medium, int? classId, int? sessionId)
        {
            var response = await _client.GetAsync($"FeeStructures/{id}");

            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var feeStructure = JsonSerializer.Deserialize<FeeStructure>(data, options);

            // Fetch Class Name using ClassId from feeStructure
            int classIdToFetch = feeStructure.ClassId ?? classId ?? 0;
            if (classIdToFetch != 0)
            {
                try
                {
                    var classResponse = await _client.GetAsync($"Classes/{classIdToFetch}");
                    if (classResponse.IsSuccessStatusCode)
                    {
                        var classData = await classResponse.Content.ReadAsStringAsync();
                        var classObj = JsonSerializer.Deserialize<Class>(classData, options);
                        ViewBag.ClassName = classObj?.ClassName ?? "Not Found";
                        // Also get medium from class if not provided
                        if (string.IsNullOrEmpty(medium) && classObj != null)
                        {
                            medium = classObj.Medium;
                        }
                    }
                    else
                    {
                        ViewBag.ClassName = "Class not found";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.ClassName = $"Error: {ex.Message}";
                }
            }
            else
            {
                ViewBag.ClassName = "No class assigned";
            }

            // Fetch Session Name
            int sessionIdToFetch = feeStructure.SessionId ?? sessionId ?? 0;
            if (sessionIdToFetch != 0)
            {
                try
                {
                    var sessionResponse = await _client.GetAsync($"Sessions/{sessionIdToFetch}");
                    if (sessionResponse.IsSuccessStatusCode)
                    {
                        var sessionData = await sessionResponse.Content.ReadAsStringAsync();
                        var sessionObj = JsonSerializer.Deserialize<Session>(sessionData, options);
                        ViewBag.SessionName = sessionObj?.SessionName ?? "Not Found";
                    }
                    else
                    {
                        ViewBag.SessionName = "Session not found";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.SessionName = $"Error: {ex.Message}";
                }
            }
            else
            {
                ViewBag.SessionName = "No session assigned";
            }

            // Ensure medium is set
            if (string.IsNullOrEmpty(medium))
            {
                medium = ViewBag.MediumFromClass ?? "Gujarati";
            }

            ViewBag.Medium = medium;
            ViewBag.ClassId = classId;
            ViewBag.SessionId = sessionId;

            return View(feeStructure);
        }
        // ------------------ HELPERS ------------------
        private async Task<List<Session>> GetSessions()
        {
            var res = await _client.GetAsync("Sessions");
            if (!res.IsSuccessStatusCode) return new();
            var data = await res.Content.ReadAsStringAsync();
            var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Session>>(data, opt) ?? new();
        }

        private async Task<List<FeeStructure>> GetFeeStructures(int sessionId, int classId)
        {
            var res = await _client.GetAsync($"FeeStructures?sessionId={sessionId}&classId={classId}");
            if (!res.IsSuccessStatusCode) return new();
            var data = await res.Content.ReadAsStringAsync();
            var opt = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<FeeStructure>>(data, opt) ?? new();
        }

        [HttpGet]
        public async Task<JsonResult> GetClassesByMedium(string medium, int? sessionId)
        {
            if (string.IsNullOrEmpty(medium) || !sessionId.HasValue)
                return Json(new List<object>());

            var res = await _client.GetAsync($"FeeStructures/GetClassesByMediumAndSession?medium={medium}&sessionId={sessionId}");

            if (!res.IsSuccessStatusCode)
                return Json(new List<object>());

            var data = await res.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var classes = JsonSerializer.Deserialize<List<dynamic>>(data, options) ?? new List<dynamic>();

            return Json(classes);
        }
        // In FeesStructuresController.cs
        public async Task<IActionResult> DownloadPdf(int? sessionId, string medium, int? classId)
        {
            // Validate filters
            if (sessionId == null || string.IsNullOrEmpty(medium) || classId == null)
            {
                TempData["Error"] = "Please select Session, Medium, and Class first.";
                return RedirectToAction(nameof(Index));
            }

            var feeList = await GetFeeStructures(sessionId.Value, classId.Value);
            if (feeList == null || feeList.Count == 0)
            {
                TempData["Error"] = "No fee structure found for the selected criteria.";
                return RedirectToAction(nameof(Index));
            }

            // Get names for the report header
            var sessionName = await GetSessionName(sessionId.Value);
            var className = await GetClassName(classId.Value);

            var document = CreatePdfDocument(feeList, sessionName, className, medium);
            var pdfBytes = document.GeneratePdf();

            string fileName = $"FeeStructure_{sessionName}_{className}_{medium}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        // Fetch session name by ID (reuse from existing sessions list)
        private async Task<string> GetSessionName(int sessionId)
        {
            var sessions = await GetSessions();
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            return session?.SessionName ?? sessionId.ToString();
        }

        // Fetch class name by ID (calls your API)
        private async Task<string> GetClassName(int classId)
        {
            var response = await _client.GetAsync($"Classes/{classId}");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var classObj = JsonSerializer.Deserialize<Class>(data, options);
                return classObj != null ? $"{classObj.ClassName} - {classObj.Medium}" : classId.ToString();
            }
            return classId.ToString();
        }

        // Create PDF document using QuestPDF
        private IDocument CreatePdfDocument(List<FeeStructure> feeList, string sessionName, string className, string medium)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text($"Fee Structure Report - {sessionName}, {className} ({medium})")
                        .SemiBold().FontSize(14).AlignCenter();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Admission Fees");
                            header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Monthly Fees");
                            header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Other Activity Fees");
                            header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Computer Fees");
                            header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Total Amount");
                        });

                        // Data rows
                        foreach (var fs in feeList)
                        {
                            table.Cell().Border(1).Padding(5).AlignRight().Text(fs.AdmissionFees?.ToString("C") ?? "0");
                            table.Cell().Border(1).Padding(5).AlignRight().Text(fs.MonthlyFees?.ToString("C") ?? "0");
                            table.Cell().Border(1).Padding(5).AlignRight().Text(fs.OtherActivityFees?.ToString("C") ?? "0");
                            table.Cell().Border(1).Padding(5).AlignRight().Text(fs.ComputerFees?.ToString("C") ?? "0");
                            table.Cell().Border(1).Padding(5).AlignRight().Text(fs.TotalAmount?.ToString("C") ?? "0");
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated on {DateTime.Now:dd-MM-yyyy HH:mm:ss}")
                        .FontSize(8);
                });
            });
        }

    }
}