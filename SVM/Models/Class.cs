using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class Class
{
    public int ClassId { get; set; }
    [Required(ErrorMessage = "Class Name is required")]
    public string? ClassName { get; set; }
    [Required(ErrorMessage = "Medium is required")]

    public string? Medium { get; set; }
    [Required(ErrorMessage = "Session is required")]

    public int? SessionId { get; set; }

    public virtual ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
    [JsonIgnore]
    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

    public virtual Session? Session { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    [JsonIgnore]
    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    [NotMapped]  // This won't be saved to database
    public string ClassNameWithMedium
    {
        get
        {
            return $"{ClassName} - {Medium}";
        }
    }
}
