using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;

namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdatesController : ControllerBase
    {
        private readonly SvmContext _context;

        public UpdatesController(SvmContext context)
        {
            _context = context;
        }

        // GET: api/Updates
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Updates>>> GetUpdates()
        {
            return await _context.Updates.ToListAsync();
        }

        // GET: api/Updates/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Updates>> GetUpdates(int id)
        {
            var updates = await _context.Updates.FindAsync(id);
            if (updates == null) return NotFound();
            return updates;
        }

        // POST: api/Updates
        [HttpPost]
        public async Task<ActionResult<Updates>> PostUpdates(Updates updates)
        {
            updates.CreatedAt = DateTime.Now;
            _context.Updates.Add(updates);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetUpdates", new { id = updates.Id }, updates);
        }

        // PUT: api/Updates/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUpdates(int id, Updates updates)
        {
            if (id != updates.Id) return BadRequest();

            _context.Entry(updates).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UpdatesExists(id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        // DELETE: api/Updates/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUpdates(int id)
        {
            var updates = await _context.Updates.FindAsync(id);
            if (updates == null) return NotFound();

            _context.Updates.Remove(updates);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool UpdatesExists(int id)
        {
            return _context.Updates.Any(e => e.Id == id);
        }
        // GET: api/Updates/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Updates>>> GetActiveUpdates()
        {
            var activeUpdates = await _context.Updates
                .Where(u => u.Status == 1)   // Status 1 = Active
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
            return activeUpdates;
        }
    }
}