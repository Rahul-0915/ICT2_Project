using System;
using System.Collections.Generic;

namespace SVM_API.Models;

public partial class FeeStructure
{
    public int FeeId { get; set; }

    public int? ClassId { get; set; }

    public int? SectionId { get; set; }

    public string? FeeType { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateOnly? DueDate { get; set; }

    public virtual Class? Class { get; set; }

    public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();

    public virtual Section? Section { get; set; }
}
