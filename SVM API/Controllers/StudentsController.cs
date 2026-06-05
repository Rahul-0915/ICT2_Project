using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly SvmContext _context;

        public StudentsController(SvmContext context)
        {
            _context = context;
        }

        // GET: api/Students
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
        {
            return await _context.Students.ToListAsync();
        }

        // GET: api/Students/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Student>> GetStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return student;
        }

        // PUT: api/Students/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudent(int id, Student student)
        {
            if (id != student.StudentId)
            {
                return BadRequest();
            }

            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
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

        // POST: api/Students
        [HttpPost]
        public async Task<ActionResult<Student>> PostStudent(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetStudent", new { id = student.StudentId }, student);
        }

        // DELETE: api/Students/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }

        [HttpPost("PromoteStudents")]
        public async Task<IActionResult> PromoteStudents([FromBody] PromotionRequest request)
        {
            try
            {
                if (request == null || request.StudentIds == null || !request.StudentIds.Any())
                {
                    return BadRequest("No students selected");
                }

                var oldStudents = new List<Student>();
                var alreadyPromoted = new List<string>();

                foreach (var id in request.StudentIds)
                {
                    var student = await _context.Students.FindAsync(id);

                    if (student != null)
                    {
                        var alreadyExists = await _context.Students.AnyAsync(x =>
                            x.UserId == student.UserId &&
                            x.ClassId == request.NewClassId &&
                            x.SectionId == request.NewSectionId &&
                            x.SessionId == request.NewSessionId);

                        if (alreadyExists)
                        {
                            alreadyPromoted.Add($"{student.FirstName} {student.LastName}");
                        }
                        else
                        {
                            oldStudents.Add(student);
                        }
                    }
                }

                if (alreadyPromoted.Any())
                {
                    return BadRequest($"Already promoted: {string.Join(", ", alreadyPromoted)}");
                }

                if (!oldStudents.Any())
                {
                    return BadRequest("No valid students found for promotion");
                }

                var existingStudents = await _context.Students
                    .Where(x => x.ClassId == request.NewClassId &&
                                x.SectionId == request.NewSectionId &&
                                x.SessionId == request.NewSessionId)
                    .ToListAsync();

                int rollNo = (existingStudents.Any() ? (existingStudents.Max(x => x.RollNo) ?? 0) : 0) + 1;

                var newStudents = new List<Student>();

                foreach (var old in oldStudents)
                {
                    string newAdmissionNo = old.AdmissionNo;

                    if (!string.IsNullOrEmpty(old.AdmissionNo) &&
                        !old.AdmissionNo.StartsWith("promoted-"))
                    {
                        newAdmissionNo = "promoted-" + old.AdmissionNo;
                    }

                    var newStudent = new Student
                    {
                        AdmissionNo = newAdmissionNo,
                        AdmissionDate = old.AdmissionDate,
                        FirstName = old.FirstName,
                        LastName = old.LastName,
                        FatherName = old.FatherName,
                        Dob = old.Dob,
                        Gender = old.Gender,
                        Grno = old.Grno,
                        BloodGroup = old.BloodGroup,
                        AadharNo = old.AadharNo,
                        Email = old.Email,
                        Address = old.Address,
                        City = old.City,
                        State = old.State,
                        Pincode = old.Pincode,
                        Phone = old.Phone,
                        MotherPhone = old.MotherPhone,
                        PreviousSchool = old.PreviousSchool,
                        StudentPhoto = old.StudentPhoto,
                        UserId = old.UserId,
                        ClassId = request.NewClassId,
                        SectionId = request.NewSectionId,
                        SessionId = request.NewSessionId,
                        RollNo = rollNo
                    };

                    newStudents.Add(newStudent);
                    rollNo++;
                }

                await _context.Students.AddRangeAsync(newStudents);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"{newStudents.Count} students promoted successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }
        [HttpGet("WithDetails")]
        public async Task<ActionResult<IEnumerable<StudentWithDetails>>> GetStudentsWithDetails(
            int? sessionId,
            string? medium,
            int? classId,
            int? sectionId)
        {
            var query = _context.Students
                .Include(s => s.Class)
                .Include(s => s.Section)
                .Include(s => s.Session)
                .Include(s => s.User)
                .AsQueryable();

            if (sessionId.HasValue)
                query = query.Where(s => s.SessionId == sessionId);

            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId);

            if (sectionId.HasValue)
                query = query.Where(s => s.SectionId == sectionId);

            var students = await query.ToListAsync();

            var result = students.Select(s => new StudentWithDetails
            {
                Student = new Student
                {
                    StudentId = s.StudentId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    RollNo = s.RollNo,
                    Grno = s.Grno,
                    Phone = s.Phone,
                    Dob = s.Dob,
                    Address = s.Address,
                    StudentPhoto = s.StudentPhoto,
                    ClassId = s.ClassId,
                    SectionId = s.SectionId,
                    SessionId = s.SessionId,
                    UserId = s.UserId
                },
                Class = s.Class == null ? null : new Class
                {
                    ClassId = s.Class.ClassId,
                    ClassName = s.Class.ClassName,
                    Medium = s.Class.Medium,
                    SessionId = s.Class.SessionId
                },
                Section = s.Section == null ? null : new Section
                {
                    SectionId = s.Section.SectionId,
                    SectionName = s.Section.SectionName,
                    ClassId = s.Section.ClassId
                },
                Session = s.Session == null ? null : new Session
                {
                    SessionId = s.Session.SessionId,
                    SessionName = s.Session.SessionName
                },
                User = s.User == null ? null : new User
                {
                    UserId = s.User.UserId,
                    FullName = s.User.FullName,
                    Username = s.User.Username
                }
            }).ToList();

            // ✅ Set JsonSerializerOptions to handle cycles
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                PropertyNameCaseInsensitive = true
            };

            return Ok(JsonSerializer.Serialize(result, options));
        }
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<Student>> GetStudentByUser(int userId)
        {
            return await GetStudentByUserWithOptionalSession(userId, checkSession: true);
        }
        [HttpGet("ByUserNoSession/{userId}")]
        public async Task<ActionResult<Student>> GetStudentByUserNoSession(int userId)
        {
            return await GetStudentByUserWithOptionalSession(userId, checkSession: false);
        }
        private async Task<ActionResult<Student>> GetStudentByUserWithOptionalSession(int userId, bool checkSession)
        {
            IQueryable<Student> query = _context.Students;

            // Sirf tabhi session filter lagao jab checkSession = true ho
            if (checkSession)
            {
                var activeSession = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.IsActive == 1);

                if (activeSession != null)
                {
                    query = query.Where(x => x.SessionId == activeSession.SessionId);
                }
                else
                {
                    // Agar active session nahi hai toh koi student nahi milega
                    return NotFound(new { error = "No active session found" });
                }
            }

            var student = await query.FirstOrDefaultAsync(x => x.UserId == userId);

            if (student == null)
            {
                // Try by email as fallback
                var user = await _context.Users.FindAsync(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    if (checkSession && student == null)
                    {
                        query = _context.Students;
                        var activeSession = await _context.Sessions
                            .FirstOrDefaultAsync(s => s.IsActive == 1);
                        if (activeSession != null)
                        {
                            query = query.Where(x => x.SessionId == activeSession.SessionId);
                        }
                        student = await query.FirstOrDefaultAsync(x => x.Email == user.Email);
                    }
                    else
                    {
                        student = await _context.Students
                            .FirstOrDefaultAsync(x => x.Email == user.Email);
                    }
                }
            }

            if (student == null)
                return NotFound(new { message = $"No student found for UserId: {userId}" });

            return Ok(new
            {
                student.StudentId,
                student.UserId,
                student.FirstName,
                student.LastName,
                student.ClassId,
                student.SectionId,
                student.SessionId,
                student.RollNo,
                student.StudentPhoto,
                student.AdmissionNo
            });
        }
        // DTO for response
        public class StudentWithDetails
        {
            public Student Student { get; set; }
            public Class Class { get; set; }
            public Section Section { get; set; }
            public Session Session { get; set; }
            public User User { get; set; }
        }
    }
}