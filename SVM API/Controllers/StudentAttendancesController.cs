using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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

            var students = await query.Select(s => new
            {
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

            var result = await query.Select(a => new
            {
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

        // GET: api/StudentAttendances/advanced-report
        // GET: api/StudentAttendances/advanced-report
        [HttpGet("advanced-report")]
        public async Task<ActionResult<object>> GetAdvancedAttendanceReport(
            [FromQuery] int sessionId,
            [FromQuery] string? medium,
            [FromQuery] int? classId,
            [FromQuery] int? sectionId,
            [FromQuery] string? date)
        {
            // Parse date safely
            DateTime reportDate;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                reportDate = parsed;
            }
            else
            {
                reportDate = DateTime.Today;
            }

            // 1. Students filter query
            var studentsQuery = _context.Students
                .Where(s => s.SessionId == sessionId);

            if (!string.IsNullOrEmpty(medium))
                studentsQuery = studentsQuery.Where(s => s.Class != null && s.Class.Medium == medium);
            if (classId.HasValue)
                studentsQuery = studentsQuery.Where(s => s.ClassId == classId.Value);
            if (sectionId.HasValue)
                studentsQuery = studentsQuery.Where(s => s.SectionId == sectionId.Value);

            var students = await studentsQuery
                .Select(s => new
                {
                    s.StudentId,
                    FullName = s.FirstName + " " + s.LastName,
                    s.RollNo,
                    s.Gender
                })
                .ToListAsync();

            if (!students.Any())
                return Ok(new { Students = new List<object>(), Totals = new { }, IsAttendanceMarked = false });

            // 2. Attendance records for the exact date
            var attendanceQuery = _context.StudentAttendances
                .Where(a => a.SessionId == sessionId
                            && a.AttendanceDate.Date == reportDate.Date);

            if (classId.HasValue)
                attendanceQuery = attendanceQuery.Where(a => a.ClassId == classId.Value);
            if (sectionId.HasValue)
                attendanceQuery = attendanceQuery.Where(a => a.SectionId == sectionId.Value);
            if (!string.IsNullOrEmpty(medium))
                attendanceQuery = attendanceQuery.Where(a => a.Class != null && a.Class.Medium == medium);

            // ✅ Check if any attendance exists for this date & filters
            bool anyAttendanceExists = await attendanceQuery.AnyAsync();

            if (!anyAttendanceExists)
            {
                return Ok(new
                {
                    Students = new List<object>(),
                    Totals = new { },
                    IsAttendanceMarked = false
                });
            }

            // ✅ Get attendance data including Id
            var attendanceData = await attendanceQuery
                .Select(a => new { a.StudentId, a.Id, a.Status })
                .ToListAsync();
            var attendanceDict = attendanceData.ToDictionary(a => a.StudentId, a => new { a.Id, a.Status });

            // 3. Build report items with AttendanceId
            var reportItems = students.Select(s => new
            {
                s.StudentId,
                s.FullName,
                s.RollNo,
                s.Gender,
                Status = attendanceDict.ContainsKey(s.StudentId) ? attendanceDict[s.StudentId].Status : "Absent",
                AttendanceId = attendanceDict.ContainsKey(s.StudentId) ? attendanceDict[s.StudentId].Id : 0
            }).ToList();

            // 4. Totals
            var totals = new
            {
                TotalPresent = reportItems.Count(r => r.Status == "Present"),
                TotalAbsent = reportItems.Count(r => r.Status == "Absent"),
                GirlsPresent = reportItems.Count(r => r.Gender == "Female" && r.Status == "Present"),
                GirlsAbsent = reportItems.Count(r => r.Gender == "Female" && r.Status == "Absent"),
                BoysPresent = reportItems.Count(r => r.Gender == "Male" && r.Status == "Present"),
                BoysAbsent = reportItems.Count(r => r.Gender == "Male" && r.Status == "Absent")
            };

            return Ok(new { Students = reportItems, Totals = totals, IsAttendanceMarked = true });
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

        [HttpGet("monthly-report")]
        public async Task<ActionResult<object>> GetMonthlyReport(
    [FromQuery] int sessionId,
    [FromQuery] string? medium,
    [FromQuery] int? classId,
    [FromQuery] int? sectionId,
    [FromQuery] int year,
    [FromQuery] int month)
        {
            // 1. Students
            var studentsQuery = _context.Students.Where(s => s.SessionId == sessionId);
            if (!string.IsNullOrEmpty(medium))
                studentsQuery = studentsQuery.Where(s => s.Class != null && s.Class.Medium == medium);
            if (classId.HasValue) studentsQuery = studentsQuery.Where(s => s.ClassId == classId.Value);
            if (sectionId.HasValue) studentsQuery = studentsQuery.Where(s => s.SectionId == sectionId.Value);

            var students = await studentsQuery
                .Select(s => new { s.StudentId, s.RollNo, s.FirstName, s.LastName, s.Gender })
                .OrderBy(s => s.RollNo)
                .ToListAsync();

            if (!students.Any())
                return Ok(new { Students = new List<object>(), Dates = new List<DateTime>(), TotalDays = 0 });

            // 2. Attendance for the month
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var attendances = await _context.StudentAttendances
                .Where(a => a.SessionId == sessionId
                    && a.AttendanceDate >= startDate
                    && a.AttendanceDate <= endDate)
                .ToListAsync();

            // 3. Filter attendance by class/section if needed
            if (classId.HasValue)
                attendances = attendances.Where(a => a.ClassId == classId.Value).ToList();
            if (sectionId.HasValue)
                attendances = attendances.Where(a => a.SectionId == sectionId.Value).ToList();

            // 4. Get distinct dates
            var dates = attendances.Select(a => a.AttendanceDate.Date).Distinct().OrderBy(d => d).ToList();

            // If no attendance, return empty
            if (!dates.Any())
                return Ok(new { Students = new List<object>(), Dates = new List<DateTime>(), TotalDays = 0 });

            // 5. Create dictionary
            var dict = attendances
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.AttendanceDate.Date, a => a.Status));

            // 6. Build report with correct counts
            var report = new List<object>();

            foreach (var s in students)
            {
                var dailyStatus = new List<object>();
                int presentCount = 0;

                foreach (var d in dates)
                {
                    string status;
                    if (dict.ContainsKey(s.StudentId) && dict[s.StudentId].ContainsKey(d))
                    {
                        status = dict[s.StudentId][d] == "Present" ? "P" : "A";
                        if (status == "P") presentCount++;
                    }
                    else
                    {
                        status = "A";
                    }
                    dailyStatus.Add(new { Date = d, Status = status });
                }

                int totalDays = dates.Count;
                int absentCount = totalDays - presentCount;
                decimal percentage = totalDays > 0 ? Math.Round((decimal)presentCount / totalDays * 100, 2) : 0;

                report.Add(new
                {
                    RollNo = s.RollNo ?? 0,
                    StudentName = s.FirstName + " " + s.LastName,
                    Gender = s.Gender ?? "N/A",
                    DailyStatus = dailyStatus,
                    Present = presentCount,
                    Absent = absentCount,
                    TotalDays = totalDays,
                    Percentage = percentage
                });
            }

            return Ok(new { Students = report, Dates = dates, TotalDays = dates.Count });
        }
    }
}