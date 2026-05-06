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
    public class SessionsController : ControllerBase
    {
        private readonly SvmContext _context;

        public SessionsController(SvmContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
        {
            return await _context.Sessions.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Session>> GetSession(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();
            return session;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutSession(int id, Session session)
        {
            if (id != session.SessionId) return BadRequest();

            // Enforce single active session
            if (session.IsActive == 1)
            {
                var otherSessions = await _context.Sessions.Where(s => s.SessionId != id).ToListAsync();
                foreach (var s in otherSessions)
                {
                    s.IsActive = 0;
                }
            }

            _context.Entry(session).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Session>> PostSession(Session session)
        {
            // Enforce single active session
            if (session.IsActive == 1)
            {
                var allSessions = await _context.Sessions.ToListAsync();
                foreach (var s in allSessions)
                {
                    s.IsActive = 0;
                }
            }

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetSession", new { id = session.SessionId }, session);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null) return NotFound();

            // Optional: check for dependents (students, classes) before delete
            bool hasStudents = await _context.Students.AnyAsync(s => s.SessionId == id);
            if (hasStudents) return BadRequest("Cannot delete: Students assigned to this session.");

            bool hasClasses = await _context.Classes.AnyAsync(c => c.SessionId == id);
            if (hasClasses) return BadRequest("Cannot delete: Classes assigned to this session.");

            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool SessionExists(int id)
        {
            return _context.Sessions.Any(e => e.SessionId == id);
        }
    }
}