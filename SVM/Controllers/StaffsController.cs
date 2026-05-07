using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using SVM.Models;

namespace SVM.Controllers
{
    public class StaffsController : Controller
    {
        private readonly HttpClient _client;

        public StaffsController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7191/api/");
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
            List<Staff> staffList = new List<Staff>();
            var response = await _client.GetAsync("Staffs");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                staffList = JsonSerializer.Deserialize<List<Staff>>(data, options);

                // Manually load User (FullName) for each staff member
                foreach (var staff in staffList)
                {
                    if (staff.UserId.HasValue)
                    {
                        var userResp = await _client.GetAsync($"Users/{staff.UserId}");
                        if (userResp.IsSuccessStatusCode)
                        {
                            var userData = await userResp.Content.ReadAsStringAsync();
                            staff.User = JsonSerializer.Deserialize<User>(userData, options);
                        }
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Failed to load staff members.");
            }

            return View(staffList);
        }

        // GET: Staffs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Staffs/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var staff = JsonSerializer.Deserialize<Staff>(data, options);

            // Load User (creator) details
            if (staff.UserId.HasValue)
            {
                var userResp = await _client.GetAsync($"Users/{staff.UserId}");
                if (userResp.IsSuccessStatusCode)
                {
                    var userData = await userResp.Content.ReadAsStringAsync();
                    staff.User = JsonSerializer.Deserialize<User>(userData, options);
                }
            }

            return View(staff);
        }

        // GET: Staffs/Create
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

            // Remove auto-generated fields from validation
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
                // ==============================
                // STEP 1 : Insert into Users Table
                // ==============================

                var newUser = new MultipartFormDataContent();

                newUser.Add(new StringContent(staff.Email ?? ""), "Email");
                newUser.Add(new StringContent(staff.Phone ?? ""), "Phone");

                // username auto generate
                string username = (staff.FirstName + staff.LastName).Replace(" ", "");

                newUser.Add(new StringContent(username), "Username");

                // default password
                newUser.Add(new StringContent("123456"), "Password");

                // fullname
                newUser.Add(new StringContent($"{staff.FirstName} {staff.LastName}"), "FullName");

                // GroupId = 2 for Staff
                newUser.Add(new StringContent("2"), "GroupId");

                // profile photo blank
                newUser.Add(new StringContent(staff.StafPhoto ?? ""), "ProfilePhoto");

                var userResponse = await _client.PostAsync("Users", newUser);

                if (!userResponse.IsSuccessStatusCode)
                {
                    var userError = await userResponse.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"User Insert Failed : {userError}");
                    return View(staff);
                }

                // Get created user data
                var createdUser = await userResponse.Content.ReadFromJsonAsync<User>();

                // generated user_id
                staff.UserId = createdUser.UserId;

                // ==============================
                // STEP 2 : Insert into Staff Table
                // ==============================

                var staffForApi = new
                {
                    staff.FirstName,
                    staff.LastName,
                    staff.Designation,
                    staff.Qualification,
                    staff.ExperienceYears,
                    JoiningDate = staff.JoiningDate.Value.ToString("yyyy-MM-dd"),
                    staff.Salary,
                    staff.Phone,
                    staff.Email,
                    staff.Address,
                    staff.StafPhoto,
                    staff.UserId
                };

                var response = await _client.PostAsJsonAsync("Staffs", staffForApi);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Staff created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"API Error: {response.StatusCode} - {errorContent}");
                }
            }

            ViewBag.AdminName = GetCurrentUserName();
            ViewBag.AdminId = currentUserId;
            return View(staff);
        }

        // GET: Staffs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Staffs/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var staff = JsonSerializer.Deserialize<Staff>(data, options);

            // Get the original added-by user's full name
            if (staff.UserId.HasValue)
            {
                var userResp = await _client.GetAsync($"Users/{staff.UserId}");
                if (userResp.IsSuccessStatusCode)
                {
                    var userData = await userResp.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<User>(userData, options);
                    ViewBag.AddedByName = user?.FullName ?? user?.Username ?? "Unknown";
                }
            }

            return View(staff);
        }

        // POST: Staffs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,FirstName,LastName,Designation,Qualification,ExperienceYears,JoiningDate,Salary,Phone,Email,Address")] Staff updatedStaff, IFormFile? photoFile)
        {
            if (id != updatedStaff.StaffId) return NotFound();

            // 1. Load existing staff from API
            var getResponse = await _client.GetAsync($"Staffs/{id}");
            if (!getResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Could not find the staff record.");
                return View(updatedStaff);
            }

            var data = await getResponse.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var existingStaff = JsonSerializer.Deserialize<Staff>(data, options);
            if (existingStaff == null) return NotFound();

            // 2. Update only editable fields
            existingStaff.FirstName = updatedStaff.FirstName;
            existingStaff.LastName = updatedStaff.LastName;
            existingStaff.Designation = updatedStaff.Designation;
            existingStaff.Qualification = updatedStaff.Qualification;
            existingStaff.ExperienceYears = updatedStaff.ExperienceYears;
            existingStaff.JoiningDate = updatedStaff.JoiningDate;
            existingStaff.Salary = updatedStaff.Salary;
            existingStaff.Phone = updatedStaff.Phone;
            existingStaff.Email = updatedStaff.Email;
            existingStaff.Address = updatedStaff.Address;

            // 3. Handle photo upload
            if (photoFile != null && photoFile.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingStaff.StafPhoto))
                {
                    string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingStaff.StafPhoto.TrimStart('/'));
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
                existingStaff.StafPhoto = $"/images/staff/{fileName}";
            }
            // ==============================
            // UPDATE USER TABLE ALSO
            // ==============================

            if (existingStaff.UserId.HasValue)
            {
                // Get existing user
                var userGetResponse = await _client.GetAsync($"Users/{existingStaff.UserId}");

                if (userGetResponse.IsSuccessStatusCode)
                {
                    var userData = await userGetResponse.Content.ReadAsStringAsync();

                    var userOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var existingUser = JsonSerializer.Deserialize<User>(userData, userOptions);

                    if (existingUser != null)
                    {
                        // update values
                        existingUser.FullName = $"{existingStaff.FirstName} {existingStaff.LastName}";
                        existingUser.Email = existingStaff.Email;
                        existingUser.Phone = existingStaff.Phone;

                        // username auto update
                        existingUser.Username =
                            (existingStaff.FirstName + existingStaff.LastName)
                            .Replace(" ", "");

                        // update profile photo also
                        existingUser.ProfilePhoto = existingStaff.StafPhoto;

                        // keep GroupId = 2
                        existingUser.GroupId = 2;

                        // call PUT API
                        var userUpdateResponse =
                            await _client.PutAsJsonAsync(
                                $"Users/{existingUser.UserId}",
                                existingUser
                            );

                        if (!userUpdateResponse.IsSuccessStatusCode)
                        {
                            var userError =
                                await userUpdateResponse.Content.ReadAsStringAsync();

                            ModelState.AddModelError(
                                "",
                                $"User update failed : {userError}"
                            );

                            return View(updatedStaff);
                        }
                    }
                }
            }


            // 4. Prepare object with DateOnly-compatible JoiningDate
            var staffForApi = new
            {
                existingStaff.StaffId,
                existingStaff.UserId,
                existingStaff.FirstName,
                existingStaff.LastName,
                existingStaff.Designation,
                existingStaff.Qualification,
                existingStaff.ExperienceYears,
                JoiningDate = existingStaff.JoiningDate.Value.ToString("yyyy-MM-dd"),
                existingStaff.Salary,
                existingStaff.Phone,
                existingStaff.Email,
                existingStaff.Address,
                existingStaff.StafPhoto
            };

            // 5. Send update to API
            var putResponse = await _client.PutAsJsonAsync($"Staffs/{id}", staffForApi);
            if (putResponse.IsSuccessStatusCode)
            {
                TempData["Success"] = "Staff updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await putResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Update failed: {errorContent}");
                return View(updatedStaff);
            }
        }

        // GET: Staffs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var response = await _client.GetAsync($"Staffs/{id}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var data = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var staff = JsonSerializer.Deserialize<Staff>(data, options);

            // Load User (creator) details for display
            if (staff.UserId.HasValue)
            {
                var userResp = await _client.GetAsync($"Users/{staff.UserId}");
                if (userResp.IsSuccessStatusCode)
                {
                    var userData = await userResp.Content.ReadAsStringAsync();
                    staff.User = JsonSerializer.Deserialize<User>(userData, options);
                }
            }

            return View(staff);
        }

        // POST: Staffs/Delete/5
        // POST: Staffs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Staff? staff = null;

            // First get the staff
            var response = await _client.GetAsync($"Staffs/{id}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                staff = JsonSerializer.Deserialize<Staff>(data, options);

                // Delete image
                if (!string.IsNullOrEmpty(staff?.StafPhoto))
                {
                    string imagePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        staff.StafPhoto.TrimStart('/')
                    );

                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }

            // ==============================
            // DELETE STAFF FIRST
            // ==============================

            var deleteResponse = await _client.DeleteAsync($"Staffs/{id}");

            if (!deleteResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Staff delete failed!");
                return View();
            }

            // ==============================
            // DELETE USER ALSO
            // ==============================

            if (staff?.UserId != null)
            {
                var userDeleteResponse =
                    await _client.DeleteAsync($"Users/{staff.UserId}");

                if (!userDeleteResponse.IsSuccessStatusCode)
                {
                    var userError =
                        await userDeleteResponse.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        $"User delete failed : {userError}"
                    );
                }
            }

            TempData["Success"] = "Staff deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        private async Task<bool> StaffExists(int id)
        {
            var response = await _client.GetAsync($"Staffs/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}