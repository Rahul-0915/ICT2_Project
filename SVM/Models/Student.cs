using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM.Models;

public partial class Student
{
    [Key]
    public int StudentId { get; set; }

    [Display(Name = "Added By")]
    public int? UserId { get; set; }

    // Admission Number - Auto generated (no need for Required attribute)
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Admission Number must be exactly 6 digits")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Admission Number must contain only 6 digits")]
    [Display(Name = "Admission Number")]
    public string? AdmissionNo { get; set; }  // Removed Required - will be auto-generated

    [Required(ErrorMessage = "Admission Date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Admission Date")]
    public DateOnly? AdmissionDate { get; set; }

    // Roll Number - Will be auto-incremented per class/section
    [Range(1, 9999, ErrorMessage = "Roll Number must be between 1 and 9999")]
    [Display(Name = "Roll Number")]
    public int? RollNo { get; set; }  // Removed Required - will be auto-generated

    [Required(ErrorMessage = "First Name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First Name can only contain letters and spaces")]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Last Name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last Name can only contain letters and spaces")]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Father's Name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Father's Name must be between 3 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z\s.]+$", ErrorMessage = "Father's Name can only contain letters, spaces and dots")]
    [Display(Name = "Father's Name")]
    public string? FatherName { get; set; }

    [Required(ErrorMessage = "Date of Birth is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    [CustomValidation(typeof(Student), nameof(ValidateAge))]
    public DateOnly? Dob { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "GR Number is required")]
    [Range(1, 999999, ErrorMessage = "GR Number must be between 1 and 999999")]
    [Display(Name = "GR Number")]
    public int? Grno { get; set; }

    [Display(Name = "Blood Group")]
    [RegularExpression("^(A\\+|A-|B\\+|B-|O\\+|O-|AB\\+|AB-)$", ErrorMessage = "Please select a valid Blood Group")]
    public string? BloodGroup { get; set; }

    [StringLength(12, MinimumLength = 12, ErrorMessage = "Aadhar Number must be exactly 12 digits")]
    [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "Aadhar Number must contain only 12 digits")]
    [Display(Name = "Aadhar Number")]
    public string? AadharNo { get; set; }

    [Required(ErrorMessage = "Class is required")]
    [Display(Name = "Class")]
    public int? ClassId { get; set; }

    [Required(ErrorMessage = "Section is required")]
    [Display(Name = "Section")]
    public int? SectionId { get; set; }

    [Required(ErrorMessage = "Session is required")]
    [Display(Name = "Academic Session")]
    public int? SessionId { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "City can only contain letters and spaces")]
    [Display(Name = "City")]
    public string? City { get; set; }

    [Required(ErrorMessage = "State is required")]
    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "State can only contain letters and spaces")]
    [Display(Name = "State")]
    public string? State { get; set; }

    [Required(ErrorMessage = "Pincode is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Pincode must be exactly 6 digits")]
    [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Pincode must contain only 6 digits")]
    [Display(Name = "Pincode")]
    public string? Pincode { get; set; }

    [Required(ErrorMessage = "Phone Number is required")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone Number must be exactly 10 digits")]
    [RegularExpression(@"^[6-9][0-9]{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number")]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Mother's Phone is required")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Mother's Phone must be exactly 10 digits")]
    [RegularExpression(@"^[6-9][0-9]{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number")]
    [Display(Name = "Mother's Phone")]
    public string? MotherPhone { get; set; }

    [StringLength(200, ErrorMessage = "Previous School name cannot exceed 200 characters")]
    [Display(Name = "Previous School (if any)")]
    public string? PreviousSchool { get; set; }

    [StringLength(500, ErrorMessage = "Photo path cannot exceed 500 characters")]
    [Display(Name = "Student Photo")]
    public string? StudentPhoto { get; set; }

    // Navigation properties
    public virtual Class? Class { get; set; }
    public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
    public virtual Section? Section { get; set; }
    public virtual Session? Session { get; set; }
    public virtual ICollection<StudentAttendance> StudentAttendances { get; set; } = new List<StudentAttendance>();
    public virtual User? User { get; set; }

    // Custom Validation for Age (5 to 25 years)
    public static ValidationResult? ValidateAge(DateOnly? dob, ValidationContext context)
    {
        if (dob.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dob.Value.Year;

            if (dob.Value > today.AddYears(-age)) age--;

            if (age < 5)
            {
                return new ValidationResult("Student must be at least 5 years old");
            }
            if (age > 25)
            {
                return new ValidationResult("Student age cannot exceed 25 years");
            }
        }
        return ValidationResult.Success;
    }
}