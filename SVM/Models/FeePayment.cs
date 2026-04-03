using System;
using System.Collections.Generic;

namespace SVM.Models;

public partial class FeePayment
{
    public int PaymentId { get; set; }

    public int? StudentId { get; set; }

    public int? FeeId { get; set; }

    public decimal? AmountPaid { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public string? PaymentMode { get; set; }

    public virtual FeeStructure? Fee { get; set; }

    public virtual Student? Student { get; set; }
}
