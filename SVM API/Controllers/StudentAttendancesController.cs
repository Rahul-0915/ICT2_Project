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
    public class StudentAttendancesController : ControllerBase
    {
        private readonly SvmContext _context;

        public StudentAttendancesController(SvmContext context)
        {
            _context = context;
        }

        // ========== EXISTING METHODS ==========
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentAttendance>>> GetStudentAttendances()
        {
            return await _context.StudentAttendances.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudentAttendance>> GetStudentAttendance(int id)
        {
            var studentAttendance = await _context.StudentAttendances.FindAsync(id);
            if (studentAttendance == null) return NotFound();
            return studentAttendance;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudentAttendance(int id, StudentAttendance studentAttendance)
        {
            if (id != studentAttendance.Id) return BadRequest();
            _context.Entry(studentAttendance).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentAttendanceExists(id)) return NotFound();
                else throw;
            }
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<StudentAttendance>> PostStudentAttendance(StudentAttendance studentAttendance)
        {
            _context.StudentAttendances.Add(studentAttendance);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetStudentAttendance", new { id = studentAttendance.Id }, studentAttendance);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudentAttendance(int id)
        {
            var studentAttendance = await _context.StudentAttendances.FindAsync(id);
            if (studentAttendance == null) return NotFound();
            _context.StudentAttendances.Remove(studentAttendance);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool StudentAttendanceExists(int id)
        {
            return _context.StudentAttendances.Any(e => e.Id == id);
        }

        // ========== NEW METHODS FOR ANDROID ==========

        [HttpGet("students")]
        public async Task<ActionResult<IEnumerable<object>>> GetFilteredStudents(
            [FromQuery] int sessionId,
            [FromQuery] string? medium,
            [FromQuery] int? classId,
            [FromQuery] int? sectionId)
        {
            var query = _context.Students
                .Include(s => s.Class)
                .Include(s => s.Section)
                .Where(s => s.SessionId == sessionId);

            if (!string.IsNullOrEmpty(medium))
                query = query.Where(s => s.Class != null && s.Class.Medium == medium);
            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId.Value);
            if (sectionId.HasValue)
                query = query.Where(s => s.SectionId == sectionId.Value);

            var students = await query.Select(s => new {
                s.StudentId,
                s.FirstName,
                s.LastName,
                s.RollNo,
                ClassName = s.Class != null ? s.Class.ClassName : "",
                SectionName = s.Section != null ? s.Section.SectionName : "",
                s.AdmissionNo
            }).ToListAsync();

            return Ok(students);
        }
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkMarkAttendance([FromBody] BulkAttendanceRequest request)
        {
            if (request.Attendances == null || request.Attendances.Count == 0)
                return BadRequest(new { message = "No attendance data" });

            // Check if already marked for this class/section/date
            bool anyExists = await _context.StudentAttendances
                .AnyAsync(a => a.ClassId == request.ClassId
                               && a.SectionId == request.SectionId
                               && a.SessionId == request.SessionId
                               && a.AttendanceDate.Date == request.AttendanceDate.Date);

            if (anyExists)
            {
                return Conflict(new { message = "Attendance already marked for this class, section, and date.", alreadyExists = true });
            }

            var records = new List<StudentAttendance>();
            foreach (var item in request.Attendances)
            {
                bool studentExists = await _context.StudentAttendances
                    .AnyAsync(a => a.StudentId == item.StudentId && a.AttendanceDate.Date == request.AttendanceDate.Date);
                if (!studentExists)
                {
                    records.Add(new StudentAttendance
                    {
                        StudentId = item.StudentId,
                        AttendanceDate = request.AttendanceDate,
                        Status = item.Status,
                        ClassId = request.ClassId,
                        SectionId = request.SectionId,
                        SessionId = request.SessionId
                    });
                }
            }

            if (records.Any())
            {
                _context.StudentAttendances.AddRange(records);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Saved {records.Count} records", alreadyExists = false });
            }
            else
            {
                return Conflict(new { message = "No new attendance added (all students already marked).", alreadyExists = true });
            }
        }
        [HttpGet("report")]
        public async Task<ActionResult<IEnumerable<object>>> GetAttendanceReport(
            [FromQuery] int sessionId,
            [FromQuery] string? medium,
            [FromQuery] int? classId,
            [FromQuery] int? sectionId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] DateTime? date)
        {
            var query = _context.StudentAttendances
                .Include(a => a.Student)
                .Include(a => a.Class)
                .Include(a => a.Section)
                .Where(a => a.SessionId == sessionId);

            if (date.HasValue)
                query = query.Where(a => a.AttendanceDate == date.Value);
            else
            {
                if (fromDate.HasValue) query = query.Where(a => a.AttendanceDate >= fromDate.Value);
                if (toDate.HasValue) query = query.Where(a => a.AttendanceDate <= toDate.Value);
            }
            if (classId.HasValue) query = query.Where(a => a.ClassId == classId.Value);
            if (sectionId.HasValue) query = query.Where(a => a.SectionId == sectionId.Value);
            if (!string.IsNullOrEmpty(medium))
                query = query.Where(a => a.Class != null && a.Class.Medium == medium);

            var result = await query.Select(a => new {
                a.Id,
                a.StudentId,
                StudentName = a.Student.FirstName + " " + a.Student.LastName,
                a.AttendanceDate,
                a.Status,
                ClassName = a.Class.ClassName,
                SectionName = a.Section.SectionName
            }).ToListAsync();

            return Ok(result);
        }
    }

    public class BulkAttendanceRequest
    {
        public int ClassId { get; set; }
        public int SectionId { get; set; }
        public int SessionId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public List<BulkAttendanceItem> Attendances { get; set; } = new();
    }

    public class BulkAttendanceItem
    {
        public int StudentId { get; set; }
        public string Status { get; set; } = "";
    }
}