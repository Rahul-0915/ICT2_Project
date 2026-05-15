using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SVM.Models;

namespace SVM.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly HttpClient _client;
        private readonly IWebHostEnvironment _environment;

        public ExpensesController(IHttpClientFactory clientFactory, IWebHostEnvironment environment)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
            _environment = environment;
        }

        // =========================
        // Helper : Current User ID
        // =========================
        private int? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdString, out int id))
                return id;
            return null;
        }

        // =========================
        // Helper : Current User Name
        // =========================
        private string GetCurrentUserName()
        {
            return HttpContext.Session.GetString("FullName") ?? "Admin";
        }

        // =========================
        // GET : Expenses Index
        // =========================
        public async Task<IActionResult> Index()
        {
            List<Expense> expenses = new();

            var response = await _client.GetAsync("Expenses");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                expenses = JsonSerializer.Deserialize<List<Expense>>(data, options);
            }
            else
            {
                TempData["Error"] = "Failed to load expenses.";
            }

            return View(expenses);
        }

        // =========================
        // GET : Create
        // =========================
        public IActionResult Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["Error"] = "You must be logged in to add an expense.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AdminName = GetCurrentUserName();
            return View();
        }

        // =========================
        // POST : Create
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense, IFormFile? receiptFile)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["Error"] = "Please login first.";
                return RedirectToAction(nameof(Index));
            }

            expense.CreatedBy = currentUserId.Value;
            expense.Status = "Paid";

            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(expense.Title ?? ""), "Title");
            formData.Add(new StringContent(expense.Category ?? ""), "Category");
            formData.Add(new StringContent(expense.Amount.ToString()), "Amount");
            formData.Add(new StringContent(expense.PaymentMethod ?? ""), "PaymentMethod");
            formData.Add(new StringContent(expense.PaidTo ?? ""), "PaidTo");
            formData.Add(new StringContent(expense.ExpenseDate.ToString("yyyy-MM-dd")), "ExpenseDate");
            formData.Add(new StringContent(expense.Description ?? ""), "Description");
            formData.Add(new StringContent(expense.Status), "Status");
            formData.Add(new StringContent(expense.CreatedBy.ToString()), "CreatedBy");

            if (receiptFile != null && receiptFile.Length > 0)
            {
                var streamContent = new StreamContent(receiptFile.OpenReadStream());
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(receiptFile.ContentType);
                formData.Add(streamContent, "receiptFile", receiptFile.FileName);
            }

            var response = await _client.PostAsync("Expenses", formData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Expense Added Successfully!";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Failed to add expense");
            ViewBag.AdminName = GetCurrentUserName();
            return View(expense);
        }

        // =========================
        // GET : Edit
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var response = await _client.GetAsync($"Expenses/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var expense = JsonSerializer.Deserialize<Expense>(data, options);

            ViewBag.AdminName = GetCurrentUserName();
            return View(expense);
        }

        // =========================
        // POST : Edit
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense, IFormFile? receiptFile)
        {
            expense.CreatedBy = GetCurrentUserId() ?? expense.CreatedBy;

            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(expense.Title ?? ""), "Title");
            formData.Add(new StringContent(expense.Category ?? ""), "Category");
            formData.Add(new StringContent(expense.Amount.ToString()), "Amount");
            formData.Add(new StringContent(expense.PaymentMethod ?? ""), "PaymentMethod");
            formData.Add(new StringContent(expense.PaidTo ?? ""), "PaidTo");
            formData.Add(new StringContent(expense.ExpenseDate.ToString("yyyy-MM-dd")), "ExpenseDate");
            formData.Add(new StringContent(expense.Description ?? ""), "Description");
            formData.Add(new StringContent(expense.Status ?? "Paid"), "Status");
            formData.Add(new StringContent(expense.CreatedBy.ToString()), "CreatedBy");

            if (receiptFile != null && receiptFile.Length > 0)
            {
                var streamContent = new StreamContent(receiptFile.OpenReadStream());
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(receiptFile.ContentType);
                formData.Add(streamContent, "receiptFile", receiptFile.FileName);
            }

            var response = await _client.PutAsync($"Expenses/{id}", formData);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Expense Updated Successfully!";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", "Failed to update expense");
            return View(expense);
        }

        // =========================
        // GET : Delete
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _client.GetAsync($"Expenses/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var expense = JsonSerializer.Deserialize<Expense>(data, options);

            return View(expense);
        }

        // =========================
        // POST : Delete
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var response = await _client.DeleteAsync($"Expenses/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Expense Deleted Successfully!";
            }
            else
            {
                TempData["Error"] = "Delete Failed!";
            }

            return RedirectToAction(nameof(Index));
        }
        // =========================
        // GET : Print Voucher
        // =========================
        public async Task<IActionResult> Print(int id)
        {
            var response = await _client.GetAsync($"Expenses/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var expense = JsonSerializer.Deserialize<Expense>(data, options);

            return View(expense);
        }
        // =========================
        // EXPORT EXCEL - Direct Download
        // =========================
        // =========================
        // EXPORT EXCEL/CSV - Direct Download
        // =========================
        public async Task<IActionResult> ExportExcel()
        {
            var response = await _client.GetAsync("Expenses/ExportExcel");

            if (response.IsSuccessStatusCode)
            {
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"Expenses_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(fileBytes, "text/csv", fileName);
            }

            TempData["Error"] = "Failed to export expenses!";
            return RedirectToAction(nameof(Index));
        }
    }
    }