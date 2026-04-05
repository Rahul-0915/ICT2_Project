using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    [Required(ErrorMessage = "User is required")]
    public int? UserId { get; set; }

    [Required(ErrorMessage = "First Name is required")]
    [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Last Name is required")]
    [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Designation is required")]
    [StringLength(100, ErrorMessage = "Designation cannot exceed 100 characters")]
    public string? Designation { get; set; }

    [StringLength(100, ErrorMessage = "Qualification cannot exceed 100 characters")]
    public string? Qualification { get; set; }

    [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years")]
    public int? ExperienceYears { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Joining Date")]
    [Required(ErrorMessage = "Joining Date is required")]
    public DateOnly? JoiningDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number")]
    [DataType(DataType.Currency)]
    public decimal? Salary { get; set; }

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 digits")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters")]
    public string? Address { get; set; }

    [Display(Name = "Staff Photo")]
    public string? StafPhoto { get; set; }

    [NotMapped]
    [Display(Name = "Upload Image")]
    public IFormFile ImageFile { get; set; }
    public virtual ICollection<StaffAttendance> StaffAttendances { get; set; } = new List<StaffAttendance>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();

    public virtual User? User { get; set; }
}
