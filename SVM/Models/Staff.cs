using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int? UserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Designation { get; set; }

    public string? Qualification { get; set; }

    public int? ExperienceYears { get; set; }

    public DateTime? JoiningDate { get; set; }

    public decimal? Salary { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? StafPhoto { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }

    // Add this missing navigation property
    [JsonIgnore]
    public virtual ICollection<StaffAttendance> StaffAttendances { get; set; } = new List<StaffAttendance>();

    // Add this if TeacherSubjects relationship exists
    [JsonIgnore]
    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}