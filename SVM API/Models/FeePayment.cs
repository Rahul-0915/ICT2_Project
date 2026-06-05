using System;
using System.Collections.Generic;

namespace SVM_API.Models;

public partial class FeePayment
{
    public int PaymentId { get; set; }

    public int? StudentId { get; set; }

    public int? FeeId { get; set; }

    public decimal? AmountPaid { get; set; }

    public DateTime? PaymentDate { get; set; }
    public string? PaymentMode { get; set; }
    public string? TransactionId { get; set; }
    public virtual FeeStructure? Fee { get; set; }

    public virtual Student? Student { get; set; }
}
