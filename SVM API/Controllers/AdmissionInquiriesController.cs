using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;

namespace SVM_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AdmissionInquiriesController : ControllerBase
	{
		private readonly SvmContext _context;

		public AdmissionInquiriesController(SvmContext context)
		{
			_context = context;
		}

		// GET
		[HttpGet]
		public async Task<ActionResult<IEnumerable<AdmissionInquiry>>>
			GetAdmissionInquiries()
		{
			return await _context.AdmissionInquiries.ToListAsync();
		}

		// GET BY ID
		[HttpGet("{id}")]
		public async Task<ActionResult<AdmissionInquiry>>
			GetAdmissionInquiry(int id)
		{
			var inquiry =
				await _context.AdmissionInquiries
				.FindAsync(id);

			if (inquiry == null)
			{
				return NotFound();
			}

			return inquiry;
		}

		// POST
		[HttpPost]
		public async Task<ActionResult<AdmissionInquiry>>
			PostAdmissionInquiry(AdmissionInquiry inquiry)
		{
			_context.AdmissionInquiries.Add(inquiry);

			await _context.SaveChangesAsync();

			return CreatedAtAction(
				"GetAdmissionInquiry",
				new { id = inquiry.InquiryId },
				inquiry);
		}

		// PUT
		[HttpPut("{id}")]
		public async Task<IActionResult>
			PutAdmissionInquiry(
				int id,
				AdmissionInquiry inquiry)
		{
			if (id != inquiry.InquiryId)
			{
				return BadRequest();
			}

			_context.Entry(inquiry).State =
				EntityState.Modified;

			await _context.SaveChangesAsync();

			return NoContent();
		}

		// DELETE
		[HttpDelete("{id}")]
		public async Task<IActionResult>
			DeleteAdmissionInquiry(int id)
		{
			var inquiry =
				await _context.AdmissionInquiries
				.FindAsync(id);

			if (inquiry == null)
			{
				return NotFound();
			}

			_context.AdmissionInquiries.Remove(inquiry);

			await _context.SaveChangesAsync();

			return NoContent();
		}
	}
}