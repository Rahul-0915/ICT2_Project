using System;
using System.Collections.Generic;

namespace SVM_API.Models;

public partial class StaffAttendance
{
    public int Id { get; set; }

    public int? StaffId { get; set; }

    public DateOnly? AttendanceDate { get; set; }

    public TimeOnly? CheckinTime { get; set; }

    public TimeOnly? CheckoutTime { get; set; }

    public string? Status { get; set; }

    public virtual Staff? Staff { get; set; }
}
