using System;
using System.Collections.Generic;

namespace SVM_API.Models;

public partial class Staff
{
    public int StaffId { get; set; }

    public int? UserId { get; set; }

    public string? EmployeeId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Designation { get; set; }

    public string? Qualification { get; set; }

    public int? ExperienceYears { get; set; }

    public DateOnly? JoiningDate { get; set; }

    public decimal? Salary { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<StaffAttendance> StaffAttendances { get; set; } = new List<StaffAttendance>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();

    public virtual User? User { get; set; }
}
