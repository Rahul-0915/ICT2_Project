using System;
using System.Collections.Generic;

namespace SVM_API.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int? UserId { get; set; }

    public string? AdmissionNo { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public int? RollNo { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? FatherName { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public int? Grno { get; set; }

    public string? BloodGroup { get; set; }

    public string? AadharNo { get; set; }

    public int? ClassId { get; set; }

    public int? SectionId { get; set; }

    public int? SessionId { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Pincode { get; set; }

    public string? Phone { get; set; }

    public string? MotherPhone { get; set; }

    public string? PreviousSchool { get; set; }

    public virtual Class? Class { get; set; }

    public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();

    public virtual Section? Section { get; set; }

    public virtual Session? Session { get; set; }

    public virtual ICollection<StudentAttendance> StudentAttendances { get; set; } = new List<StudentAttendance>();

    public virtual User? User { get; set; }
}
