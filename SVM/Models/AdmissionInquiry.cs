using System;
using System.Collections.Generic;

namespace SVM.Models;

public partial class AdmissionInquiry
{
    public int InquiryId { get; set; }

    public string? StudentName { get; set; }

    public string? ParentName { get; set; }

    public string? Phone { get; set; }

    public string? ClassName { get; set; }

    public string? Message { get; set; }

    public DateTime? InquiryDate { get; set; }
}
