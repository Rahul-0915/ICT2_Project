using System;
using System.Collections.Generic;

namespace SVM_API.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public string? ClassName { get; set; }

    public string? Medium { get; set; }

    public int? SessionId { get; set; }

    public virtual ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();

    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

    public virtual Session? Session { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public virtual ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
