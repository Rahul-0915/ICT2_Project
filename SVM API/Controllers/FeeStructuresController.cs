using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;

namespace SVM_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class FeeStructuresController : ControllerBase
	{
		private readonly SvmContext _context;
		public FeeStructuresController(SvmContext context) => _context = context;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<FeeStructure>>> GetFeeStructures([FromQuery] int? sessionId, [FromQuery] int? classId)
		{
			var query = _context.FeeStructures.AsQueryable();
			if (sessionId.HasValue) query = query.Where(f => f.SessionId == sessionId);
			if (classId.HasValue) query = query.Where(f => f.ClassId == classId);
			return await query.ToListAsync();
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<FeeStructure>> GetFeeStructure(int id)
		{
			var fee = await _context.FeeStructures.FirstOrDefaultAsync(f => f.FeeId == id);
			if (fee == null) return NotFound();
			return fee;
		}

        // ✅ NEW API: Get classes by both Medium AND Session
        [HttpGet("GetClassesByMediumAndSession")]
        public async Task<ActionResult<IEnumerable<object>>> GetClassesByMediumAndSession(
    [FromQuery] string medium,
    [FromQuery] int sessionId)
        {
            if (string.IsNullOrEmpty(medium) || sessionId == 0)
                return Ok(new List<object>());

            // Sirf us session ki classes dikhao
            var result = await _context.Classes
                .Where(c => c.Medium == medium && c.SessionId == sessionId)
                .Select(c => new {
                    value = c.ClassId,
                    text = c.ClassName
                })
                
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost]
		public async Task<ActionResult<FeeStructure>> PostFeeStructure(FeeStructure feeStructure)
		{
			var existing = await _context.FeeStructures
				.FirstOrDefaultAsync(f => f.SessionId == feeStructure.SessionId && f.ClassId == feeStructure.ClassId);

			if (existing != null)
			{
				return Conflict(new { message = "Fee structure already exists for this session and class." });
			}

			decimal total = (feeStructure.AdmissionFees ?? 0)
							+ ((feeStructure.MonthlyFees ?? 0) * 12)
							+ (feeStructure.OtherActivityFees ?? 0)
							+ (feeStructure.ComputerFees ?? 0);
			feeStructure.TotalAmount = total;

			_context.FeeStructures.Add(feeStructure);
			await _context.SaveChangesAsync();
			return CreatedAtAction("GetFeeStructure", new { id = feeStructure.FeeId }, feeStructure);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> PutFeeStructure(int id, FeeStructure feeStructure)
		{
			if (id != feeStructure.FeeId) return BadRequest();

			var existing = await _context.FeeStructures
				.FirstOrDefaultAsync(f => f.SessionId == feeStructure.SessionId
									   && f.ClassId == feeStructure.ClassId
									   && f.FeeId != id);
			if (existing != null)
			{
				return Conflict(new { message = "Another fee structure already exists for this session and class." });
			}

			decimal total = (feeStructure.AdmissionFees ?? 0)
							+ ((feeStructure.MonthlyFees ?? 0) * 12)
							+ (feeStructure.OtherActivityFees ?? 0)
							+ (feeStructure.ComputerFees ?? 0);
			feeStructure.TotalAmount = total;

			_context.Entry(feeStructure).State = EntityState.Modified;
			await _context.SaveChangesAsync();
			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteFeeStructure(int id)
		{
			var fee = await _context.FeeStructures.FindAsync(id);
			if (fee == null) return NotFound();
			_context.FeeStructures.Remove(fee);
			await _context.SaveChangesAsync();
			return NoContent();
		}
	}
}