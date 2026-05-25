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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
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

                // Get max roll number
                var existingStudents = await _context.Students
                    .Where(x => x.ClassId == request.NewClassId &&
                                x.SectionId == request.NewSectionId &&
                                x.SessionId == request.NewSessionId)
                    .ToListAsync();

                int rollNo = (existingStudents.Any() ? (existingStudents.Max(x => x.RollNo) ?? 0) : 0) + 1;

                var newStudents = new List<Student>();

                foreach (var old in oldStudents)
                {
                    // ✅ SAFE AdmissionNo logic (NO duplicate prefix bug)
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
    }
}