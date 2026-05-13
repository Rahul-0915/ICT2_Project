using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int? UserId { get; set; }

    [Required(ErrorMessage = "Enter FirstName")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Enter LastName")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Required Designation")]
    public string? Designation { get; set; }

    [Required(ErrorMessage = "Required Qualification")]
    public string? Qualification { get; set; }

    [Required(ErrorMessage = "Experience Years is required")]
    [Range(0, 50, ErrorMessage = "Experience years must be between 0 and 50")]
    public int? ExperienceYears { get; set; }

    [Required(ErrorMessage = "Joining Date is required")]
    public DateTime? JoiningDate { get; set; }

    [Required(ErrorMessage = "Salary is required")]
    [Range(0, 1000000, ErrorMessage = "Salary must be a positive amount")]
    public decimal? Salary { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must be exactly 10 digits")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address (e.g., name@example.com)")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Address is required")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Staff photo is required")]
    public string? StafPhoto { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }

    [JsonIgnore]
    public virtual ICollection<StaffAttendance> StaffAttendances { get; set; } = new List<StaffAttendance>();

    [JsonIgnore]
    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}