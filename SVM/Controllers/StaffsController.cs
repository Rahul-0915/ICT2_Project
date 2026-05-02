using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SVM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SVM.Controllers
{
    public class StaffsController : Controller
    {
        private readonly SvmContext _context;

        public StaffsController(SvmContext context)
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

        // GET: Staffs
        public async Task<IActionResult> Index()
        {
            var svmContext = _context.Staff.Include(s => s.User);
            return View(await svmContext.ToListAsync());
        }

        // GET: Staffs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StaffId == id);
            if (staff == null) return NotFound();

            return View(staff);
        }

        // GET: Staffs/Create
        [HttpGet]
        public IActionResult Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["Error"] = "You must be logged in to add a staff member. Please login first.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AdminName = GetCurrentUserName();
            ViewBag.AdminId = userId.Value;
            return View();
        }

        // POST: Staffs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StaffId,FirstName,LastName,Designation,Qualification,ExperienceYears,JoiningDate,Salary,Phone,Email,Address")] Staff staff, IFormFile? photoFile)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                ModelState.AddModelError("", "User not logged in.");
                return View(staff);
            }
            staff.UserId = currentUserId.Value;

            // Remove UserId from ModelState to avoid validation issues
            ModelState.Remove("UserId");
            ModelState.Remove("StafPhoto");

            // Photo upload logic
            if (photoFile != null && photoFile.Length > 0)
            {
                if (photoFile.Length > 5 * 1024 * 1024)
                    ModelState.AddModelError("StafPhoto", "File size must be less than 5 MB");
                else
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "staff");
                    Directory.CreateDirectory(uploadPath);
                    string filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }
                    staff.StafPhoto = $"/images/staff/{fileName}";
                }
            }
            else
            {
                ModelState.AddModelError("StafPhoto", "Staff Photo is required");
            }

            if (ModelState.IsValid)
            {
                _context.Add(staff);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Staff created successfully! Name: {staff.FirstName} {staff.LastName}";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AdminName = GetCurrentUserName();
            ViewBag.AdminId = currentUserId;
            return View(staff);
        }

        // GET: Staffs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Include the User navigation property to get the creator's name
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null) return NotFound();

            // Pass the original creator's name to the view
            ViewBag.AddedByName = staff.User?.FullName ?? staff.User?.Username ?? "Unknown";

            return View(staff);
        }
        // POST: Staffs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,UserId,FirstName,LastName,Designation,Qualification,ExperienceYears,JoiningDate,Salary,Phone,Email,Address,StafPhoto")] Staff staff, IFormFile? photoFile)
        {
            if (id != staff.StaffId) return NotFound();

            // Handle image upload if new image is provided
            if (photoFile != null && photoFile.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(staff.StafPhoto))
                {
                    string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", staff.StafPhoto.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoFile.FileName);
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "staff");
                Directory.CreateDirectory(uploadPath);
                string filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoFile.CopyToAsync(stream);
                }
                staff.StafPhoto = $"/images/staff/{fileName}";
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(staff);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Staff updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StaffExists(staff.StaffId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(staff);
        }

        // GET: Staffs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StaffId == id);
            if (staff == null) return NotFound();

            return View(staff);
        }

        // POST: Staffs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff != null)
            {
                // Delete staff photo if exists
                if (!string.IsNullOrEmpty(staff.StafPhoto))
                {
                    string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", staff.StafPhoto.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _context.Staff.Remove(staff);
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Staff deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool StaffExists(int id)
        {
            return _context.Staff.Any(e => e.StaffId == id);
        }
    }
}