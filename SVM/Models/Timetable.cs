using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // 👈 Add this
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class Timetable
{
    [Column("timetable_id")]
    public int TimetableId { get; set; }

    [Column("session_id")]
    public int SessionId { get; set; }

    [Column("class_id")]
    public int ClassId { get; set; }

    [Column("section_id")]
    public int SectionId { get; set; }

    [Required]
    [Column("day_name")]
    public string DayName { get; set; } = null!;

    [Column("lecture_no")]
    public int LectureNo { get; set; }

    [Column("subject_id")]
    public int? SubjectId { get; set; }

    [Column("staff_id")]
    public int? StaffId { get; set; }

    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly EndTime { get; set; }

    [Column("is_break")]
    public bool? IsBreak { get; set; }

    // Navigation Properties
    public virtual Session? Session { get; set; }
    public virtual Class? Class { get; set; }
    public virtual Section? Section { get; set; }
    public virtual Subject? Subject { get; set; }
    public virtual Staff? Staff { get; set; }
}