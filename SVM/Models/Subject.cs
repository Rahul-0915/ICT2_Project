using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class Subject
{
    [Required(ErrorMessage = "Subject Name Is Required")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Subject Name Required")]
    public string? SubjectName { get; set; }

    [Required(ErrorMessage = "Class Is Required")]
    public int? ClassId { get; set; }
    
    public virtual Class? Class { get; set; }

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
