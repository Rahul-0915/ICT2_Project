using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;

namespace SVM_API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ClassesController : ControllerBase
	{
		private readonly SvmContext _context;

		public ClassesController(SvmContext context)
		{
			_context = context;
		}

		// GET: api/Classes
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Class>>> GetClasses()
		{
			return await _context.Classes
				.Include(c => c.Session)
				.ToListAsync();
		}

		// GET: api/Classes/5
		[HttpGet("{id}")]
		public async Task<ActionResult<Class>> GetClass(int id)
		{
			var @class = await _context.Classes
				.Include(c => c.Session)
				.FirstOrDefaultAsync(c => c.ClassId == id);

			if (@class == null)
				return NotFound();

			return @class;
		}

		// PUT: api/Classes/5
		[HttpPut("{id}")]
		public async Task<IActionResult> PutClass(int id, Class @class)
		{
			if (id != @class.ClassId)
				return BadRequest();

			//  Check for duplicate in the SAME session (excluding current record)
			bool duplicateExists = await _context.Classes.AnyAsync(c =>
				c.ClassName == @class.ClassName &&
				c.Medium == @class.Medium &&
				c.SessionId == @class.SessionId &&
				c.ClassId != id);

			if (duplicateExists)
			{
				return Conflict(new { message = $"Class '{@class.ClassName}' with Medium '{@class.Medium}' already exists in this session." });
			}

			_context.Entry(@class).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!ClassExists(id))
					return NotFound();
				else
					throw;
			}

			return NoContent();
		}

		// POST: api/Classes
		[HttpPost]
		public async Task<ActionResult<Class>> PostClass(Class @class)
		{
			//  Check for duplicate in the SAME session (for new record)
			bool duplicateExists = await _context.Classes.AnyAsync(c =>
				c.ClassName == @class.ClassName &&
				c.Medium == @class.Medium &&
				c.SessionId == @class.SessionId);

			if (duplicateExists)
			{
				return Conflict(new { message = $"Class '{@class.ClassName}' with Medium '{@class.Medium}' already exists in this session." });
			}

			_context.Classes.Add(@class);
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetClass", new { id = @class.ClassId }, @class);
		}

		// DELETE: api/Classes/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteClass(int id)
		{
			var @class = await _context.Classes.FindAsync(id);
			if (@class == null)
				return NotFound();

			_context.Classes.Remove(@class);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private bool ClassExists(int id)
		{
			return _context.Classes.Any(e => e.ClassId == id);
		}
		[HttpGet("WithFilters")]
		public async Task<ActionResult<IEnumerable<Class>>> GetClassesWithFilters(int? sessionId, string medium)
		{
			var query = _context.Classes.AsQueryable();

			if (sessionId.HasValue)
				query = query.Where(c => c.SessionId == sessionId);
			if (!string.IsNullOrEmpty(medium))
				query = query.Where(c => c.Medium == medium);

			var classes = await query.ToListAsync();
			return Ok(classes);
		}
	}
}