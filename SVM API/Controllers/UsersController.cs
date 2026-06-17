using BCrypt.Net;  // This gives access to BCrypt.Net.BCrypt
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SVM_API.Models;
using SVM_API.Services;

namespace SVM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SvmContext _context;
        private readonly IEmailService _emailService;

        public UsersController(SvmContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            foreach (var u in users) u.Password = null;
            return users;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Password = null;
            return user;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId) return BadRequest();

            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            // Update fields
            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.FullName = user.FullName;
            existing.GroupId = user.GroupId;
            // existing.IsActive = user.IsActive;   // REMOVE – no IsActive in User model
            existing.ProfilePhoto = user.ProfilePhoto;

            // Hash password only if provided
            if (!string.IsNullOrEmpty(user.Password))
                existing.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Entry(existing).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!UserExists(id)) return NotFound(); else throw; }
            return NoContent();
        }
        [HttpPost]
        public async Task<ActionResult<User>> PostUser([FromBody] User user)
        {
            try
            {
                // Trim and case‑insensitive check
                string normalisedUsername = user.Username?.Trim();
                string normalisedEmail = user.Email?.Trim();

                if (_context.Users.Any(u => u.Username.ToLower() == normalisedUsername.ToLower()))
                    return BadRequest(new { error = "Username already exists" });

                if (_context.Users.Any(u => u.Email.ToLower() == normalisedEmail.ToLower()))
                    return BadRequest(new { error = "Email already exists" });

                user.Username = normalisedUsername;
                user.Email = normalisedEmail;

                // Hash password
                if (!string.IsNullOrEmpty(user.Password))
                    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                else
                    user.Password = string.Empty;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                user.Password = null;
                return CreatedAtAction("GetUser", new { id = user.UserId }, user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("check")]
        public async Task<ActionResult<bool>> CheckExists(string username, string email)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.Trim().ToLower() ||
                               u.Email.ToLower() == email.Trim().ToLower());
            return Ok(exists);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        //[HttpPost("login")]
        //public async Task<ActionResult> Login([FromBody] LoginRequest request)
        //{
        //    var user = await _context.Users
        //        .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);
        //    if (user == null)
        //        return Unauthorized(new { error = "Invalid username/email or password" });

        //    // Verify password
        //    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        //        return Unauthorized(new { error = "Invalid username/email or password" });

        //    user.Password = null;
        //    return Ok(user);
        //}
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username == request.Username ||
                    u.Email == request.Username);

            if (user == null)
                return Unauthorized(new { error = "Invalid username/email or password" });

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                return Unauthorized(new { error = "Invalid username/email or password" });

            user.Password = null;

            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Email,
                user.FullName,
                user.GroupId
            });

        }
        private bool UserExists(int id) => _context.Users.Any(e => e.UserId == id);

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Identifier || u.Email == request.Identifier);

            if (user == null)
                return NotFound(new { error = "User not found" });

            if (string.IsNullOrEmpty(user.Email))
                return BadRequest(new { error = "No email registered for this user" });

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Store OTP and expiry
            user.ResetOTP = otp;
            user.ResetOTPExpiry = DateTime.UtcNow.AddMinutes(15);
            await _context.SaveChangesAsync();

            // Send email
            try
            {
                await _emailService.SendOtpEmailAsync(user.Email, otp);
                return Ok(new { message = "OTP sent to your registered email" });
            }
            catch (Exception ex)
            {
                // Log error 
                return StatusCode(500, new { error = "Failed to send email. Please try again later." });
            }
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Identifier || u.Email == request.Identifier);

            if (user == null)
                return NotFound(new { error = "User not found" });

            // Check OTP and expiry
            if (user.ResetOTP != request.OTP || user.ResetOTPExpiry < DateTime.UtcNow)
                return BadRequest(new { error = "Invalid or expired OTP" });

            // Hash new password
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Clear OTP fields so they cannot be reused
            user.ResetOTP = null;
            user.ResetOTPExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully" });
        }



        public class ForgotPasswordRequest
        {
            public string Identifier { get; set; }  // username or email
        }

        public class ResetPasswordRequest
        {
            public string Identifier { get; set; }
            public string OTP { get; set; }
            public string NewPassword { get; set; }
        }
    }


}