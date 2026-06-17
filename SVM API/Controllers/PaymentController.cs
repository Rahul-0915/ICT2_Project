using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Razorpay.Api;
using SVM_API.Models;
using Microsoft.EntityFrameworkCore;

namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly SvmContext _context;

        public PaymentController(SvmContext context)
        {
            _context = context;
        }
        [HttpPost("create-order")]
        public IActionResult CreateOrder([FromBody] decimal amount)
        {
            //  Validate amount
            if (amount <= 0)
            {
                return BadRequest(new { error = "Invalid amount. Fee structure may not be available for your class/session." });
            }

            try
            {
                var client = new RazorpayClient(
                    "rzp_test_SxA17sOstoKC4x",
                    "V5TPP3ZQMr6xvOamcCLrE6zi"
                );

                var options = new Dictionary<string, object>
        {
            { "amount", amount * 100 },
            { "currency", "INR" },
            { "receipt", Guid.NewGuid().ToString() }
        };

                var order = client.Order.Create(options);

                return Ok(new
                {
                    orderId = order["id"].ToString(),
                    key = "rzp_test_SxA17sOstoKC4x"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("success")]
        public async Task<IActionResult> PaymentSuccess([FromBody] PaymentDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("No payment data received");

                if (dto.StudentId <= 0)
                    return BadRequest("Invalid StudentId");

                if (dto.FeeId <= 0)
                    return BadRequest("Invalid FeeId");

                //  Check if student exists
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student == null)
                    return BadRequest($"Student with ID {dto.StudentId} does not exist");

                //  Check if fee structure exists
                var fee = await _context.FeeStructures.FindAsync(dto.FeeId);
                if (fee == null)
                    return BadRequest($"Fee structure with ID {dto.FeeId} does not exist");

                var payment = new FeePayment
                {
                    StudentId = dto.StudentId,
                    FeeId = dto.FeeId,
                    AmountPaid = dto.Amount,
                    PaymentDate = DateTime.Now,
                    PaymentMode = "Razorpay",
                    TransactionId = dto.PaymentId
                };

                _context.FeePayments.Add(payment);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                // Get the inner most exception message
                var innerEx = ex.InnerException;
                var msg = ex.Message;
                while (innerEx != null)
                {
                    msg = innerEx.Message;
                    innerEx = innerEx.InnerException;
                }
                return StatusCode(500, $"Error saving payment: {msg}");
            }
        }
        [HttpGet("receipt/{studentId}")]
        public async Task<IActionResult> GetReceipt(int studentId)
        {
            var payment = await _context.FeePayments
      .Include(p => p.Student)
          .ThenInclude(s => s.Class)   //  Load Class details
      .Include(p => p.Fee)
      .Where(p => p.StudentId == studentId)
      .OrderByDescending(p => p.PaymentDate)
      .FirstOrDefaultAsync();

            if (payment == null)
                return NotFound("No payment found for this student.");

            var receipt = new
            {
                StudentName = payment.Student != null ? $"{payment.Student.FirstName} {payment.Student.LastName}" : "N/A",
                ClassName = payment.Student?.Class?.ClassName ?? "N/A",
                AdmissionFees = payment.Fee?.AdmissionFees ?? 0,
                MonthlyFees = payment.Fee?.MonthlyFees ?? 0,
                OtherActivityFees = payment.Fee?.OtherActivityFees ?? 0,
                ComputerFees = payment.Fee?.ComputerFees ?? 0,
                TotalAmount = payment.AmountPaid,
                PaymentDate = payment.PaymentDate?.ToString("dd MMM yyyy, hh:mm tt"),
                TransactionId = payment.TransactionId,
                PaymentMode = payment.PaymentMode
            };

            return Ok(receipt);
        }
        public class PaymentDTO
        {
            public int StudentId { get; set; }
            public int FeeId { get; set; }
            public decimal Amount { get; set; }
            public string PaymentId { get; set; }
        }
    }
}
