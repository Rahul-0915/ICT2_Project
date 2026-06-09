using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;


namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamsController : ControllerBase
    {
        private readonly SvmContext _context;

        public ExamsController(SvmContext context)
        {
            _context = context;
        }

        // ==================== ADMIN/TEACHER ENDPOINTS ====================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Exam>>> GetExams(int? sessionId, string? medium, int? classId, int? sectionId, bool? publishedOnly = false)
        {
            var query = _context.Exams.AsQueryable();
            if (sessionId.HasValue) query = query.Where(e => e.SessionId == sessionId);
            if (!string.IsNullOrEmpty(medium)) query = query.Where(e => e.Medium == medium);
            if (classId.HasValue) query = query.Where(e => e.ClassId == classId);
            if (sectionId.HasValue) query = query.Where(e => e.SectionId == sectionId);
            if (publishedOnly == true) query = query.Where(e => e.IsPublished == true);
            return await query.OrderByDescending(e => e.ExamId).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Exam>> GetExam(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            return exam;
        }

        [HttpPost]
        public async Task<ActionResult<Exam>> PostExam(Exam exam)
        {
            exam.CreatedAt = DateTime.Now;
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetExam", new { id = exam.ExamId }, exam);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutExam(int id, Exam exam)
        {
            if (id != exam.ExamId) return BadRequest();
            _context.Entry(exam).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExam(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}/publish")]
        public async Task<IActionResult> PublishExam(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            exam.IsPublished = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Exam published successfully" });
        }

        // Subjects
        [HttpGet("subjects/{examId}")]
        public async Task<ActionResult<IEnumerable<ExamSubject>>> GetExamSubjects(int examId)
        {
            return await _context.ExamSubjects.Include(es => es.Subject).Where(es => es.ExamId == examId).ToListAsync();
        }

        [HttpPost("subjects/bulk")]
        public async Task<IActionResult> BulkSaveSubjects(List<ExamSubject> subjects)
        {
            if (subjects == null || !subjects.Any()) return BadRequest();
            var examId = subjects.First().ExamId;
            var existing = await _context.ExamSubjects.Where(es => es.ExamId == examId).ToListAsync();
            _context.ExamSubjects.RemoveRange(existing);
            _context.ExamSubjects.AddRange(subjects);
            await _context.SaveChangesAsync();
            return Ok(new { count = subjects.Count });
        }

        // Marks entry data (Admin/Teacher)
        [HttpGet("marks-data")]
        public async Task<IActionResult> GetMarksData(int examId, int classId, int sectionId, int? staffId = null)
        {
            try
            {
                var subjects = new List<object>();
                var students = new List<object>();
                var marksDict = new Dictionary<(int examSubjectId, int studentId), decimal?>();

                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();

                    // 1. Get subjects
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT es.exam_subject_id, es.subject_id, s.subject_name, es.total_marks, es.passing_marks
                    FROM exam_subjects es
                    INNER JOIN subjects s ON es.subject_id = s.subject_id
                    WHERE es.exam_id = @examId";
                        var p = cmd.CreateParameter();
                        p.ParameterName = "@examId";
                        p.Value = examId;
                        cmd.Parameters.Add(p);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                subjects.Add(new
                                {
                                    ExamSubjectId = reader.GetInt32(0),
                                    SubjectId = reader.GetInt32(1),
                                    SubjectName = reader.GetString(2),
                                    TotalMarks = reader.GetInt32(3),
                                    PassingMarks = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
                                });
                            }
                        }
                    }

                    if (!subjects.Any())
                    {
                        return Ok(new { Subjects = subjects, Students = new List<object>() });
                    }

                    // 2. Get students (convert grno to string)
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT student_id, roll_no, first_name + ' ' + last_name AS student_name, 
                           CAST(grno AS NVARCHAR(50)) AS grno
                    FROM students
                    WHERE class_id = @classId AND section_id = @sectionId
                    ORDER BY roll_no";
                        var p1 = cmd.CreateParameter();
                        p1.ParameterName = "@classId";
                        p1.Value = classId;
                        cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter();
                        p2.ParameterName = "@sectionId";
                        p2.Value = sectionId;
                        cmd.Parameters.Add(p2);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                students.Add(new
                                {
                                    StudentId = reader.GetInt32(0),
                                    RollNo = reader.GetInt32(1),
                                    StudentName = reader.GetString(2),
                                    Grno = reader.IsDBNull(3) ? "" : reader.GetString(3)
                                });
                            }
                        }
                    }

                    // 3. Get existing marks
                    var examSubjectIds = subjects.Select(s => ((dynamic)s).ExamSubjectId).Cast<int>().ToList();
                    if (examSubjectIds.Any())
                    {
                        var idsList = string.Join(",", examSubjectIds);
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = $@"
                        SELECT exam_subject_id, student_id, obtained_marks
                        FROM exam_marks
                        WHERE exam_subject_id IN ({idsList})";
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    int esId = reader.GetInt32(0);
                                    int studId = reader.GetInt32(1);
                                    decimal? marks = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2);
                                    marksDict[(esId, studId)] = marks;
                                }
                            }
                        }
                    }
                }

                // 4. Build final result (students and marksDict are now in scope)
                var finalStudents = students.Select(s =>
                {
                    dynamic student = s;
                    return new
                    {
                        student.StudentId,
                        student.RollNo,
                        student.StudentName,
                        student.Grno,
                        Marks = subjects.Select(sub =>
                        {
                            dynamic subject = sub;
                            decimal? obtained = marksDict.ContainsKey((subject.ExamSubjectId, student.StudentId))
                                ? marksDict[(subject.ExamSubjectId, student.StudentId)]
                                : null;
                            return new { ExamSubjectId = subject.ExamSubjectId, ObtainedMarks = obtained };
                        }).ToList()
                    };
                }).ToList();

                return Ok(new { Subjects = subjects, Students = finalStudents });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpPost("marks/save")]
        public async Task<IActionResult> SaveMarks(List<ExamMark> marks)
        {
            if (marks == null || !marks.Any()) return BadRequest();
            foreach (var mark in marks)
            {
                var existing = await _context.ExamMarks
                    .FirstOrDefaultAsync(m => m.ExamSubjectId == mark.ExamSubjectId && m.StudentId == mark.StudentId);
                if (existing != null)
                {
                    existing.ObtainedMarks = mark.ObtainedMarks;
                    existing.EnteredBy = mark.EnteredBy;
                    existing.EnteredAt = DateTime.Now;
                }
                else
                {
                    mark.EnteredAt = DateTime.Now;
                    _context.ExamMarks.Add(mark);
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Marks saved successfully" });
        }

        // Report
        [HttpGet("report")]
        public async Task<IActionResult> GetReport(int examId, int classId, int sectionId)
        {
            try
            {
                var exam = await _context.Exams.FindAsync(examId);
                if (exam == null) return NotFound(new { error = "Exam not found" });

                var examSubjects = await _context.ExamSubjects
                    .Include(es => es.Subject)
                    .Where(es => es.ExamId == examId)
                    .ToListAsync();

                if (!examSubjects.Any())
                    return Ok(new { ExamName = exam.ExamName, Students = new List<object>() });

                var students = await _context.Students
                    .Where(s => s.ClassId == classId && s.SectionId == sectionId)
                    .OrderBy(s => s.RollNo)
                    .Select(s => new { s.StudentId, s.RollNo, s.FirstName, s.LastName, s.Grno })
                    .ToListAsync();

                if (!students.Any())
                    return Ok(new { ExamName = exam.ExamName, Students = new List<object>() });

                var examSubjectIds = examSubjects.Select(es => es.ExamSubjectId).ToList();
                var allMarks = new List<ExamMark>();

                if (examSubjectIds.Any())
                {
                    var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>();
                    var inClauseParts = new List<string>();
                    for (int i = 0; i < examSubjectIds.Count; i++)
                    {
                        var paramName = $"@p{i}";
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter(paramName, examSubjectIds[i]));
                        inClauseParts.Add(paramName);
                    }
                    var inClause = string.Join(",", inClauseParts);
                    var sql = $"SELECT * FROM exam_marks WHERE exam_subject_id IN ({inClause})";
                    allMarks = await _context.ExamMarks.FromSqlRaw(sql, parameters.ToArray()).ToListAsync();
                }

                // Build list with percentage
                var studentResults = new List<dynamic>();
                foreach (var student in students)
                {
                    var subjectMarks = new List<dynamic>();
                    int totalObtained = 0;
                    int totalMax = 0;
                    bool failed = false;

                    foreach (var es in examSubjects)
                    {
                        var mark = allMarks.FirstOrDefault(m => m.ExamSubjectId == es.ExamSubjectId && m.StudentId == student.StudentId);
                        decimal obtained = mark?.ObtainedMarks ?? 0;
                        int passMarks = es.PassingMarks ?? (int)(es.TotalMarks * 0.35);
                        subjectMarks.Add(new
                        {
                            SubjectName = es.Subject?.SubjectName ?? "",
                            TotalMarks = es.TotalMarks,
                            ObtainedMarks = obtained,
                            PassingMarks = passMarks,
                            Status = obtained >= passMarks ? "Pass" : "Fail"
                        });
                        totalObtained += (int)obtained;
                        totalMax += es.TotalMarks;
                        if (obtained < passMarks) failed = true;
                    }

                    decimal percentage = totalMax > 0 ? (decimal)totalObtained / totalMax * 100 : 0;
                    studentResults.Add(new
                    {
                        student.StudentId,
                        student.RollNo,
                        StudentName = student.FirstName + " " + student.LastName,
                        Grno = student.Grno?.ToString() ?? "",
                        SubjectMarks = subjectMarks,
                        TotalObtained = totalObtained,
                        TotalMarks = totalMax,
                        Percentage = Math.Round(percentage, 2),
                        Result = failed ? "Fail" : "Pass"
                    });
                }

                // ✅ Rank assignment with tie handling (dense ranking)
                var sortedByPercentage = studentResults
                    .OrderByDescending(r => ((dynamic)r).Percentage)
                    .ToList();

                var rankAssignments = new Dictionary<int, int?>(); // StudentId -> Rank
                int currentRank = 1;
                for (int i = 0; i < sortedByPercentage.Count; i++)
                {
                    dynamic current = sortedByPercentage[i];
                    if (i > 0)
                    {
                        dynamic previous = sortedByPercentage[i - 1];
                        if (((dynamic)current).Percentage < ((dynamic)previous).Percentage)
                        {
                            currentRank = i + 1;
                        }
                    }
                    // Store rank only for top 5 ranks (i.e., rank <= 5)
                    if (currentRank <= 5)
                        rankAssignments[((dynamic)current).StudentId] = currentRank;
                    else
                        rankAssignments[((dynamic)current).StudentId] = null;
                }

                // Final list sorted by RollNo
                var finalStudents = studentResults.Select(r =>
                {
                    dynamic stud = r;
                    int? rank = rankAssignments.ContainsKey(stud.StudentId) ? rankAssignments[stud.StudentId] : null;
                    return new
                    {
                        stud.StudentId,
                        stud.RollNo,
                        stud.StudentName,
                        stud.Grno,
                        stud.SubjectMarks,
                        stud.TotalObtained,
                        stud.TotalMarks,
                        stud.Percentage,
                        stud.Result,
                        Rank = rank
                    };
                }).OrderBy(s => s.RollNo).ToList();

                return Ok(new { ExamName = exam.ExamName, Students = finalStudents });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        // Teacher exams
        [HttpGet("teacher-exams")]
        public async Task<IActionResult> GetTeacherExams(int staffId, bool? publishedOnly = true)
        {
            var teacherSubjects = await _context.TeacherSubjects
                .Where(ts => ts.StaffId == staffId)
                .Select(ts => new { ts.SubjectId, ts.ClassId, ts.SessionId })
                .ToListAsync();
            var subjectIds = teacherSubjects.Select(ts => ts.SubjectId).ToList();
            var classIds = teacherSubjects.Select(ts => ts.ClassId).ToList();

            if (!subjectIds.Any() || !classIds.Any())
                return Ok(new List<object>());

            var query = _context.ExamSubjects
                .Include(es => es.Exam)
                    .ThenInclude(e => e.Class)
                .Include(es => es.Exam)
                    .ThenInclude(e => e.Section)
                .Where(es => subjectIds.Contains(es.SubjectId))
                .Where(es => classIds.Contains(es.Exam.ClassId))
                .Select(es => es.Exam)
                .Distinct();

            if (publishedOnly == true)
                query = query.Where(e => e.IsPublished == true);

            var exams = await query.ToListAsync();
            return Ok(exams);
        }

        // ==================== STUDENT ENDPOINTS ====================
        [HttpGet("student-exams")]
        public async Task<IActionResult> GetStudentExams(int sessionId, string medium, int classId, int sectionId)
        {
            var exams = await _context.Exams
                .Where(e => e.SessionId == sessionId && e.Medium == medium && e.ClassId == classId && e.SectionId == sectionId && e.IsPublished == true)
                .OrderByDescending(e => e.ExamId)
                .Select(e => new { e.ExamId, e.ExamName, e.ExamType, e.StartDate, e.EndDate })
                .ToListAsync();
            return Ok(exams);
        }

        [HttpGet("student-marks")]
        public async Task<IActionResult> GetStudentMarks(int examId, int studentId)
        {
            var exam = await _context.Exams.FindAsync(examId);
            if (exam == null || !exam.IsPublished) return NotFound(new { message = "Exam not published or not found" });

            var examSubjects = await _context.ExamSubjects
                .Include(es => es.Subject)
                .Where(es => es.ExamId == examId)
                .ToListAsync();

            var marks = await _context.ExamMarks
                .Where(m => m.StudentId == studentId && examSubjects.Select(es => es.ExamSubjectId).Contains(m.ExamSubjectId))
                .ToListAsync();

            var result = new
            {
                ExamName = exam.ExamName,
                Subjects = examSubjects.Select(es => new
                {
                    es.SubjectId,
                    SubjectName = es.Subject?.SubjectName ?? "",
                    es.TotalMarks,
                    PassingMarks = es.PassingMarks ?? 0,
                    ObtainedMarks = marks.FirstOrDefault(m => m.ExamSubjectId == es.ExamSubjectId)?.ObtainedMarks ?? 0
                })
            };
            return Ok(result);
        }
    }
}