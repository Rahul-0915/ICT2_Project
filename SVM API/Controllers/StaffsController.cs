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
            public class StaffsController : ControllerBase
            {
                private readonly SvmContext _context;

                public StaffsController(SvmContext context)
                {
                    _context = context;
                }

                [HttpGet]
                public async Task<ActionResult<IEnumerable<Staff>>> GetStaff()
                {
                    return await _context.Staff
                        .Include(s => s.User)  
                        .ToListAsync();
                }

                [HttpGet("{id}")]
                public async Task<ActionResult<Staff>> GetStaff(int id)
                {
                    var staff = await _context.Staff
                        .Include(s => s.User)   
                        .FirstOrDefaultAsync(s => s.StaffId == id);

                    if (staff == null)
                    {
                        return NotFound();
                    }
                    return staff;
                }

                // PUT: api/Staffs/5
                // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
                [HttpPut("{id}")]
                public async Task<IActionResult> PutStaff(int id, Staff staff)
                {
                    if (id != staff.StaffId)
                    {
                        return BadRequest();
                    }

                    _context.Entry(staff).State = EntityState.Modified;

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!StaffExists(id))
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

                // POST: api/Staffs
                // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
                [HttpPost]
                public async Task<ActionResult<Staff>> PostStaff(Staff staff)
                {
                    _context.Staff.Add(staff);
                    await _context.SaveChangesAsync();

                    return CreatedAtAction("GetStaff", new { id = staff.StaffId }, staff);
                }

                // DELETE: api/Staffs/5
                [HttpDelete("{id}")]
                public async Task<IActionResult> DeleteStaff(int id)
                {
                    var staff = await _context.Staff.FindAsync(id);
                    if (staff == null)
                    {
                        return NotFound();
                    }

                    _context.Staff.Remove(staff);
                    await _context.SaveChangesAsync();

                    return NoContent();
                }

                private bool StaffExists(int id)
                {
                    return _context.Staff.Any(e => e.StaffId == id);
                }
    

                [HttpGet("ByUserId/{userId}")]
                public async Task<ActionResult<Staff>> GetStaffByUserId(int userId)
                {
                    var staff = await _context.Staff
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.UserId == userId);

                    if (staff == null)
                    {
                        return NotFound(new { message = "Staff not found for this user" });
                    }

                    return Ok(staff);
                }
            }
        }
