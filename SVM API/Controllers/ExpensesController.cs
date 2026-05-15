using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;
using System.Text;

namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly SvmContext _context;
        private readonly IWebHostEnvironment _environment;

        public ExpensesController(SvmContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =========================
        // GET ALL
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetExpenses()
        {
            var expenses = await _context.Expenses
                .OrderByDescending(x => x.ExpenseId)
                .ToListAsync();

            return Ok(expenses);
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExpense(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);

            if (expense == null)
                return NotFound();

            return Ok(expense);
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost]
        public async Task<IActionResult> CreateExpense(
            [FromForm] Expense expense,
            IFormFile? receiptFile)
        {
            try
            {
                // Voucher Number Generate
                var lastExpense = await _context.Expenses
                    .OrderByDescending(x => x.ExpenseId)
                    .FirstOrDefaultAsync();

                int nextNumber = 1;
                if (lastExpense != null && !string.IsNullOrEmpty(lastExpense.VoucherNo))
                {
                    string[] parts = lastExpense.VoucherNo.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                    {
                        nextNumber = lastNum + 1;
                    }
                }

                expense.VoucherNo = $"EXP-{DateTime.Now.Year}-{nextNumber:D4}";

                // Receipt Upload
                if (receiptFile != null && receiptFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "expenses");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + receiptFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await receiptFile.CopyToAsync(fileStream);
                    }

                    expense.ReceiptFile = $"/uploads/expenses/{uniqueFileName}";
                }

                expense.CreatedAt = DateTime.Now;
                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Expense Added Successfully", voucherNo = expense.VoucherNo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =========================
        // UPDATE
        // =========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(
            int id,
            [FromForm] Expense expense,
            IFormFile? receiptFile)
        {
            try
            {
                var existingExpense = await _context.Expenses.FindAsync(id);
                if (existingExpense == null)
                    return NotFound(new { success = false, message = "Expense not found" });

                existingExpense.Title = expense.Title;
                existingExpense.Category = expense.Category;
                existingExpense.Amount = expense.Amount;
                existingExpense.PaymentMethod = expense.PaymentMethod;
                existingExpense.PaidTo = expense.PaidTo;
                existingExpense.ExpenseDate = expense.ExpenseDate;
                existingExpense.Description = expense.Description;
                existingExpense.Status = expense.Status;
                existingExpense.CreatedBy = expense.CreatedBy;

                // Handle receipt file upload
                if (receiptFile != null && receiptFile.Length > 0)
                {
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existingExpense.ReceiptFile))
                    {
                        string oldFilePath = Path.Combine(_environment.WebRootPath, existingExpense.ReceiptFile.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Save new file
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "expenses");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + receiptFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await receiptFile.CopyToAsync(fileStream);
                    }

                    existingExpense.ReceiptFile = $"/uploads/expenses/{uniqueFileName}";
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Expense Updated Successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            try
            {
                var expense = await _context.Expenses.FindAsync(id);
                if (expense == null)
                    return NotFound(new { success = false, message = "Expense not found" });

                // Delete receipt file if exists
                if (!string.IsNullOrEmpty(expense.ReceiptFile))
                {
                    string filePath = Path.Combine(_environment.WebRootPath, expense.ReceiptFile.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Expense Deleted Successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =========================
        // EXPORT CSV (Without any external package)
        // =========================
        // =========================
        // EXPORT CSV WITH TOTAL SUMMARY
        // =========================
        // =========================
        // EXPORT CSV WITH TOTAL SUMMARY (Fixed)
        // =========================
        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportToCsv()
        {
            var expenses = await _context.Expenses
                .OrderByDescending(x => x.ExpenseDate)
                .ToListAsync();

            var sb = new StringBuilder();

            // ===== HEADERS =====
            sb.AppendLine("S.No,Voucher No,Title,Category,Amount,Payment Method,Paid To,Date,Status,Description");

            // ===== DATA ROWS =====
            int serialNo = 1;
            decimal totalAmount = 0;

            foreach (var expense in expenses)
            {
                totalAmount += expense.Amount;  // Amount add ho raha hai

                sb.AppendLine($"{serialNo++}," +
                              $"{EscapeCsv(expense.VoucherNo)}," +
                              $"{EscapeCsv(expense.Title)}," +
                              $"{EscapeCsv(expense.Category)}," +
                              $"{expense.Amount}," +
                              $"{EscapeCsv(expense.PaymentMethod)}," +
                              $"{EscapeCsv(expense.PaidTo)}," +
                              $"{expense.ExpenseDate:dd-MM-yyyy}," +
                              $"{EscapeCsv(expense.Status)}," +
                              $"{EscapeCsv(expense.Description)}");
            }

            // ===== TOTAL SUMMARY (Same line mein proper format) =====
            sb.AppendLine(); // Blank line
            sb.AppendLine($"\"TOTAL AMOUNT\",\"{totalAmount:N2}\",\"Total Records: {expenses.Count}\",,,,,,,");
            sb.AppendLine($"\"Generated On\",\"{DateTime.Now:dd-MM-yyyy HH:mm:ss}\",,,,,,,,");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"Expenses_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            return value;
        }
    }
    }