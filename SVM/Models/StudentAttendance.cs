using System;
using System.Collections.Generic;

namespace SVM.Models;

public partial class StudentAttendance
{
    public int Id { get; set; }

    public int? StudentId { get; set; }

    public DateOnly? AttendanceDate { get; set; }

    public string? Status { get; set; }

    public virtual Student? Student { get; set; }
}
