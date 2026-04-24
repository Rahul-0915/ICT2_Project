using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class User
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

  
    public int? GroupId { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
    public string? FullName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 15 digits")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "Profile photo path too long")]
    public string? ProfilePhoto { get; set; }

    public virtual Groupmaster? Group { get; set; }

    [JsonIgnore]
    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    [JsonIgnore]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    // For file upload
    [JsonIgnore]
    public IFormFile? ImageFile { get; set; }
}