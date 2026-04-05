using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class Session
{
    public int SessionId { get; set; }
    [Required(ErrorMessage = "Session Name is required")]
    public string? SessionName { get; set; }

    [Required(ErrorMessage = "Start Date is required")]

    public DateOnly? StartDate { get; set; }
    [Required(ErrorMessage = "End Date is required")]
    public DateOnly? EndDate { get; set; }
    [Required(ErrorMessage = "Status is required")]

    public int? IsActive { get; set; }
    [JsonIgnore]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
