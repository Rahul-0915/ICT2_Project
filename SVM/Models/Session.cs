using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class Session
{
    public int SessionId { get; set; }

    public string? SessionName { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? IsActive { get; set; }
    [JsonIgnore]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
