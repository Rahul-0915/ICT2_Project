using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;

namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeePaymentsController : ControllerBase
    {
        private readonly SvmContext _context;

        public FeePaymentsController(SvmContext context)
        {
            _context = context;
        }

        // GET: api/FeePayments/TotalPaid/{studentId}
        [HttpGet("TotalPaid/{studentId}")]
        public async Task<ActionResult<decimal>> GetTotalPaid(int studentId)
        {
            var total = await _context.FeePayments
                .Where(p => p.StudentId == studentId)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

            return Ok(total);
        }

        // GET: api/FeePayments/ByStudent/{studentId}
        [HttpGet("ByStudent/{studentId}")]
        public async Task<ActionResult<IEnumerable<FeePayment>>> GetPaymentsByStudent(int studentId)
        {
            var payments = await _context.FeePayments
                .Where(p => p.StudentId == studentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return Ok(payments);
        }

        // POST: api/FeePayments/CashPayment
        [HttpPost("CashPayment")]
        public async Task<ActionResult<FeePayment>> ProcessCashPayment([FromBody] CashPaymentRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { message = "Invalid payment request" });

                var student = await _context.Students.FindAsync(request.StudentId);
                if (student == null)
                    return BadRequest(new { message = "Student not found" });

                var feeStructure = await _context.FeeStructures.FindAsync(request.FeeId);
                if (feeStructure == null)
                    return BadRequest(new { message = "Fee structure not found" });

                var payment = new FeePayment
                {
                    StudentId = request.StudentId,
                    FeeId = request.FeeId,
                    AmountPaid = request.AmountPaid,
                    PaymentDate = DateTime.Now,
                    PaymentMode = "Cash",
                    TransactionId = request.TransactionId ?? $"CASH-{DateTime.Now.Ticks}"
                    // ❌ NO Remark property - remove this line
                };

                _context.FeePayments.Add(payment);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, paymentId = payment.PaymentId, message = "Payment recorded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/FeePayments/StudentFeeDetails/{studentId}
        [HttpGet("StudentFeeDetails/{studentId}")]
        public async Task<IActionResult> GetStudentFeeDetails(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Section)
                .Include(s => s.Session)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            var feeStructure = await _context.FeeStructures
                .FirstOrDefaultAsync(f => f.SessionId == student.SessionId && f.ClassId == student.ClassId);

            if (feeStructure == null)
                return BadRequest(new { message = "Fee Structure Not Found For This Student" });

            decimal totalFees = (feeStructure.AdmissionFees ?? 0) +
                               ((feeStructure.MonthlyFees ?? 0) * 12) +
                               (feeStructure.OtherActivityFees ?? 0) +
                               (feeStructure.ComputerFees ?? 0);

            decimal alreadyPaid = await _context.FeePayments
                .Where(p => p.StudentId == studentId)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

            decimal payableAmount = totalFees - alreadyPaid;
            if (payableAmount < 0) payableAmount = 0;

            return Ok(new
            {
                studentId = student.StudentId,
                studentName = $"{student.FirstName} {student.LastName}",
                medium = student.Class?.Medium ?? "N/A",
                className = student.Class?.ClassName ?? "N/A",
                sectionName = student.Section?.SectionName ?? "N/A",
                sessionName = student.Session?.SessionName ?? "N/A",
                totalFees = totalFees,
                alreadyPaid = alreadyPaid,
                payableAmount = payableAmount,
                feeId = feeStructure.FeeId
            });
        }
        // GET: api/FeePayments/FilteredPayments
        [HttpGet("FilteredPayments")]
        public async Task<ActionResult<IEnumerable<object>>> GetFilteredPayments(
            int sessionId,
            string? medium = null,
            int? classId = null,
            int? sectionId = null,
            string? searchTransactionId = null,
            string? paymentMode = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                // Start with students query
                var studentsQuery = _context.Students
                    .Include(s => s.Class)
                    .Include(s => s.Section)
                    .Where(s => s.SessionId == sessionId);

                // Apply filters
                if (!string.IsNullOrEmpty(medium))
                    studentsQuery = studentsQuery.Where(s => s.Class != null && s.Class.Medium == medium);

                if (classId.HasValue && classId.Value > 0)
                    studentsQuery = studentsQuery.Where(s => s.ClassId == classId.Value);

                if (sectionId.HasValue && sectionId.Value > 0)
                    studentsQuery = studentsQuery.Where(s => s.SectionId == sectionId.Value);

                // Get all payments for these students in ONE query with JOIN
                var paymentsQuery = from student in studentsQuery
                                    join payment in _context.FeePayments
                                    on student.StudentId equals payment.StudentId
                                    where payment.PaymentDate != null
                                    select new
                                    {
                                        StudentId = student.StudentId,
                                        StudentName = student.FirstName + " " + student.LastName,
                                        ClassName = student.Class != null ? student.Class.ClassName : "N/A",
                                        SectionName = student.Section != null ? student.Section.SectionName : "N/A",
                                        AmountPaid = payment.AmountPaid,
                                        PaymentMode = payment.PaymentMode,
                                        TransactionId = payment.TransactionId,
                                        PaymentDate = payment.PaymentDate
                                    };

                // Apply additional filters
                if (!string.IsNullOrEmpty(searchTransactionId))
                    paymentsQuery = paymentsQuery.Where(p => p.TransactionId.ToLower().Contains(searchTransactionId.ToLower()));

                if (!string.IsNullOrEmpty(paymentMode))
                    paymentsQuery = paymentsQuery.Where(p => p.PaymentMode == paymentMode);

                if (fromDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.PaymentDate >= fromDate.Value);

                if (toDate.HasValue)
                    paymentsQuery = paymentsQuery.Where(p => p.PaymentDate <= toDate.Value);

                var result = await paymentsQuery
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        // GET: api/FeePayments/GenerateReceipt/{paymentId}
        [HttpGet("GenerateReceipt/{paymentId}")]
        public async Task<IActionResult> GenerateReceipt(int paymentId)
        {
            var payment = await _context.FeePayments
                .Include(p => p.Student)
                    .ThenInclude(s => s != null ? s.Class : null)
                .Include(p => p.Student)
                    .ThenInclude(s => s != null ? s.Section : null)
                .Include(p => p.Fee)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
                return NotFound(new { message = "Payment not found" });

            if (payment.Student == null)
                return NotFound(new { message = "Student not found" });

            return Ok(new
            {
                receiptNo = payment.PaymentId,
                studentName = $"{payment.Student.FirstName} {payment.Student.LastName}",
                className = payment.Student.Class?.ClassName ?? "N/A",
                sectionName = payment.Student.Section?.SectionName ?? "N/A",
                amountPaid = payment.AmountPaid,
                paymentDate = payment.PaymentDate?.ToString("dd MMM yyyy, hh:mm tt") ?? DateTime.Now.ToString("dd MMM yyyy, hh:mm tt"),
                paymentMode = payment.PaymentMode,
                transactionId = payment.TransactionId
                // ❌ NO Remark
            });
        }
    }

    public class CashPaymentRequest
    {
        public int StudentId { get; set; }
        public int FeeId { get; set; }
        public decimal AmountPaid { get; set; }
        public string? TransactionId { get; set; }
        // ❌ NO Remark property
    }
}