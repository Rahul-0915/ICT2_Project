using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SVM.Models;

public partial class Section
{
    public int SectionId { get; set; }
    [Required(ErrorMessage = "Division  is required")]
    public string? SectionName { get; set; }
    [Required(ErrorMessage = "Class is required")]
    public int? ClassId { get; set; }

    public virtual Class? Class { get; set; }

    public virtual ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
