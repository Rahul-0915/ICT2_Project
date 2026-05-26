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
    public class TeacherSubjectsController : ControllerBase
    {
        private readonly SvmContext _context;

        public TeacherSubjectsController(SvmContext context)
        {
            _context = context;
        }

        // GET: api/TeacherSubjects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeacherSubject>>> GetTeacherSubjects()
        {
            return await _context.TeacherSubjects.ToListAsync();
        }

        // GET: api/TeacherSubjects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherSubject>> GetTeacherSubject(int id)
        {
            var teacherSubject = await _context.TeacherSubjects.FindAsync(id);

            if (teacherSubject == null)
            {
                return NotFound();
            }

            return teacherSubject;
        }

        // PUT: api/TeacherSubjects/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeacherSubject(int id, TeacherSubject teacherSubject)
        {
            if (id != teacherSubject.Id)
            {
                return BadRequest();
            }

            _context.Entry(teacherSubject).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherSubjectExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TeacherSubjects
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TeacherSubject>> PostTeacherSubject(TeacherSubject teacherSubject)
        {
            _context.TeacherSubjects.Add(teacherSubject);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTeacherSubject", new { id = teacherSubject.Id }, teacherSubject);
        }

        // DELETE: api/TeacherSubjects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacherSubject(int id)
        {
            var teacherSubject = await _context.TeacherSubjects.FindAsync(id);
            if (teacherSubject == null)
            {
                return NotFound();
            }

            _context.TeacherSubjects.Remove(teacherSubject);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeacherSubjectExists(int id)
        {
            return _context.TeacherSubjects.Any(e => e.Id == id);
        }


        [HttpGet("classes-by-session-medium")]
        public async Task<ActionResult<IEnumerable<Class>>> GetClassesBySessionMedium(int sessionId, string medium)
        {
            return await _context.Classes
                .Where(c => c.SessionId == sessionId && c.Medium == medium)
                .ToListAsync();
        }
        [HttpGet("subjects-by-class/{classId}")]
        public async Task<ActionResult<IEnumerable<Subject>>> GetSubjectsByClass(int classId)
        {
            return await _context.Subjects
                .Where(s => s.ClassId == classId)
                .ToListAsync();
        }
    }
}
