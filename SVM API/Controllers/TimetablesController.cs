using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;

namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimetablesController : ControllerBase
    {
        private readonly SvmContext _context;

        public TimetablesController(SvmContext context)
        {
            _context = context;
        }

        // GET: api/Timetables?sessionId=1&classId=2&sectionId=3
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Timetable>>> GetTimetables(
            int? sessionId, int? classId, int? sectionId)
        {
            var query = _context.Timetables
                .Include(t => t.Subject)
                .Include(t => t.Staff)
                .Include(t => t.Session)
                .Include(t => t.Class)
                .Include(t => t.Section)
                .AsQueryable();

            if (sessionId.HasValue)
                query = query.Where(t => t.SessionId == sessionId);
            if (classId.HasValue)
                query = query.Where(t => t.ClassId == classId);
            if (sectionId.HasValue)
                query = query.Where(t => t.SectionId == sectionId);

            return await query.ToListAsync();
        }

        // GET: api/Timetables/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Timetable>> GetTimetable(int id)
        {
            var timetable = await _context.Timetables
                .Include(t => t.Subject)
                .Include(t => t.Staff)
                .FirstOrDefaultAsync(t => t.TimetableId == id);

            if (timetable == null)
                return NotFound();

            return timetable;
        }

        // PUT: api/Timetables/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTimetable(int id, Timetable timetable)
        {
            if (id != timetable.TimetableId) return BadRequest();

            _context.Entry(timetable).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Timetables
        [HttpPost]
        public async Task<ActionResult<Timetable>> PostTimetable(Timetable timetable)
        {
            _context.Timetables.Add(timetable);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetTimetable", new { id = timetable.TimetableId }, timetable);
        }

        // DELETE: api/Timetables/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimetable(int id)
        {
            var timetable = await _context.Timetables.FindAsync(id);
            if (timetable == null) return NotFound();

            _context.Timetables.Remove(timetable);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("SectionsByClass/{classId}")]
        public async Task<ActionResult<IEnumerable<Section>>> GetSectionsByClass(int classId)
        {
            var sections = await _context.Sections.Where(s => s.ClassId == classId).ToListAsync();
            return sections;
        }
        [HttpGet("GetClassesByMedium")]
        public async Task<ActionResult<IEnumerable<Class>>> GetClassesByMedium(string medium)
        {
            var classes = await _context.Classes
                .Where(c => c.Medium == medium)
                .ToListAsync();
            return classes;
        }
        [HttpGet("GetTeacherMapping")]
        public async Task<ActionResult<Dictionary<int, Staff>>> GetTeacherMapping(int sessionId, int classId)
        {
            var teacherSubjects = await _context.TeacherSubjects
                .Where(ts => ts.SessionId == sessionId && ts.ClassId == classId)
                .Include(ts => ts.Staff)
                .ToListAsync();

            var mapping = teacherSubjects
                .Where(ts => ts.SubjectId.HasValue && ts.Staff != null)
                .ToDictionary(ts => ts.SubjectId.Value, ts => ts.Staff);

            return mapping;
        }
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<TeacherSubject>>> GetFiltered(
    int? subjectId, int? classId, int? sessionId, int? staffId)
        {
            var query = _context.TeacherSubjects.AsQueryable();
            if (subjectId.HasValue) query = query.Where(ts => ts.SubjectId == subjectId);
            if (classId.HasValue) query = query.Where(ts => ts.ClassId == classId);
            if (sessionId.HasValue) query = query.Where(ts => ts.SessionId == sessionId);
            if (staffId.HasValue) query = query.Where(ts => ts.StaffId == staffId);
            return await query.ToListAsync();
        }
    }
}