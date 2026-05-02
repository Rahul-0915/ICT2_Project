using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SVM.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SvmContext _context;

        public StudentsController(SvmContext context)
        {
            _context = context;
        }
        // Helper: Get current logged-in user ID from Session
        private int? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (int.TryParse(userIdString, out int id))
                return id;
            return null;
        }

        // Helper: Get current logged-in user full name from Session
        private string GetCurrentUserName()
        {
            return HttpContext.Session.GetString("FullName") ?? "Admin";
        }
        // GET: Students
        public async Task<IActionResult> Index()
        {
            var svmContext = _context.Students.Include(s => s.Class).Include(s => s.Section).Include(s => s.Session).Include(s => s.User);
            return View(await svmContext.ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // ... same as original
            if (id == null) return NotFound();
            var student = await _context.Students.Include(s => s.Class).Include(s => s.Section).Include(s => s.Session).Include(s => s.User).FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null) return NotFound();
            return View(student);
        }
        // GET: Students/Create
        public IActionResult Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["Error"] = "You must be logged in to add a student. Please login first.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName");
            ViewData["SectionId"] = new SelectList(_context.Sections, "SectionId", "SectionName");
            ViewData["SessionId"] = new SelectList(_context.Sessions, "SessionId", "SessionName");
            // No ViewData["UserId"] – dropdown hataya

            ViewBag.AdminName = GetCurrentUserName();
            ViewBag.AdminId = userId.Value;
            return View();
        }


        // POST: Students/Create (same as before, unchanged)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,AdmissionDate,FirstName,LastName,FatherName,Dob,Gender,Grno,BloodGroup,AadharNo,ClassId,SectionId,SessionId,Address,City,State,Pincode,Phone,MotherPhone,PreviousSchool")] Student student, IFormFile? photoFile)
        {
            student.AdmissionNo = await GenerateAdmissionNumber();
            if (student.ClassId.HasValue && student.SectionId.HasValue)
            {
                student.RollNo = await GenerateRollNumber(student.ClassId.Value, student.SectionId.Value);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                ModelState.AddModelError("", "User not logged in.");
                return View(student);
            }
            student.UserId = currentUserId.Value;

            ModelState.Remove("AdmissionNo");
            ModelState.Remove("RollNo");
            ModelState.Remove("UserId");

            // Photo upload logic
            if (photoFile != null && photoFile.Length > 0)
            {
                if (photoFile.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError("StudentPhoto", "File size must be less than 5 MB");
                else
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "students");
                    Directory.CreateDirectory(uploadPath);
                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }
                    student.StudentPhoto = $"/images/students/{fileName}";
                }
            }
            else
            {
                ModelState.AddModelError("StudentPhoto", "Student Photo is required");
            }

            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                //TempData["Success"] = $"Student created successfully! Admission No: {student.AdmissionNo}";
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", student.ClassId);
            ViewData["SectionId"] = new SelectList(_context.Sections, "SectionId", "SectionName", student.SectionId);
            ViewData["SessionId"] = new SelectList(_context.Sessions, "SessionId", "SessionName", student.SessionId);
            ViewBag.AdminName = GetCurrentUserName();
            ViewBag.AdminId = currentUserId;
            return View(student);
        }
        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Include User navigation property to get the original creator's name
            var student = await _context.Students
                .Include(s => s.User)  // Include User
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();

            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", student.ClassId);
            ViewData["SectionId"] = new SelectList(_context.Sections, "SectionId", "SectionName", student.SectionId);
            ViewData["SessionId"] = new SelectList(_context.Sessions, "SessionId", "SessionName", student.SessionId);
            // No ViewData["UserId"] dropdown – we will use hidden field + readonly text

            // Pass the original added-by user's full name to the view
            ViewBag.AddedByName = student.User?.FullName ?? student.User?.Username ?? "Unknown";

            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,UserId,AdmissionNo,AdmissionDate,RollNo,FirstName,LastName,FatherName,Dob,Gender,Grno,BloodGroup,AadharNo,ClassId,SectionId,SessionId,Address,City,State,Pincode,Phone,MotherPhone,PreviousSchool,StudentPhoto")] Student student, IFormFile? StudentPhoto)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            // Handle image upload if new image is provided
            if (StudentPhoto != null && StudentPhoto.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(student.StudentPhoto))
                {
                    string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.StudentPhoto.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Create unique filename
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(StudentPhoto.FileName);

                // Save to wwwroot/images/students
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "students");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await StudentPhoto.CopyToAsync(stream);
                }

                // Update relative path to database
                student.StudentPhoto = $"/images/students/{fileName}";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Student updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClassId"] = new SelectList(_context.Classes, "ClassId", "ClassName", student.ClassId);
            ViewData["SectionId"] = new SelectList(_context.Sections, "SectionId", "SectionName", student.SectionId);
            ViewData["SessionId"] = new SelectList(_context.Sessions, "SessionId", "SessionName", student.SessionId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", student.UserId);
            return View(student);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Section)
                .Include(s => s.Session)
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                // Delete student photo if exists
                if (!string.IsNullOrEmpty(student.StudentPhoto))
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", student.StudentPhoto.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Student deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }

        // Generate 6-digit sequential Admission Number
        private async Task<string> GenerateAdmissionNumber()
        {
            var lastStudent = await _context.Students
                .OrderByDescending(s => s.StudentId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.AdmissionNo))
            {
                if (int.TryParse(lastStudent.AdmissionNo, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return nextNumber.ToString("D6"); // Returns 000001, 000002, etc.
        }

        // Generate Roll Number based on Class and Section
        private async Task<int> GenerateRollNumber(int classId, int sectionId)
        {
            var lastStudentInClass = await _context.Students
                .Where(s => s.ClassId == classId && s.SectionId == sectionId)
                .OrderByDescending(s => s.RollNo)
                .FirstOrDefaultAsync();

            int nextRollNo = 1;
            if (lastStudentInClass != null && lastStudentInClass.RollNo.HasValue)
            {
                nextRollNo = lastStudentInClass.RollNo.Value + 1;
            }

            return nextRollNo;
        }

        [HttpGet]
        public async Task<JsonResult> GetClassesByMedium(string medium)
        {
            var classes = await _context.Classes
                .Where(c => c.Medium == medium)
                .Select(c => new { value = c.ClassId, text = c.ClassName })
                .ToListAsync();

            return Json(classes);
        }

        [HttpGet]
        public async Task<JsonResult> GetSectionsByClass(int classId)
        {
            var sections = await _context.Sections
                .Where(s => s.ClassId == classId)
                .Select(s => new { value = s.SectionId, text = s.SectionName })
                .ToListAsync();

            return Json(sections);
        }
    }
}