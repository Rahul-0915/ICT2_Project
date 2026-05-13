using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SVM_API.Models;

[Table("student_attendance")]
public partial class StudentAttendance
{
    [Key]
    [Column("attendance_id")]
    public int Id { get; set; }

    [Column("student_id")]
    public int StudentId { get; set; }

    [Column("attendance_date")]
    public DateTime AttendanceDate { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("section_id")]
    public int SectionId { get; set; }

    [Column("session_id")]
    public int SessionId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ClassId))]
    public virtual Class? Class { get; set; }

    [ForeignKey(nameof(SectionId))]
    public virtual Section? Section { get; set; }

    [ForeignKey(nameof(SessionId))]
    public virtual Session? Session { get; set; }

    [ForeignKey(nameof(StudentId))]
    public virtual Student? Student { get; set; }
}