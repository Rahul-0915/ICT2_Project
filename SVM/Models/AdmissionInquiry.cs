using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SVM.Models;

public partial class AdmissionInquiry
{
    public int InquiryId { get; set; }

    [Required(ErrorMessage = "Student name is required")]
    public string? StudentName { get; set; }

    [Required(ErrorMessage = "Parent name is required")]
    public string? ParentName { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[0-9]{10}$",
        ErrorMessage = "Enter valid 10 digit mobile number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Class is required")]
    public string? ClassName { get; set; }

    [Required(ErrorMessage = "Message is required")]
    public string? Message { get; set; }

    public DateTime? InquiryDate { get; set; }

    public bool? IsSeen { get; set; }

    public bool? IsAttended { get; set; }

    public string? ReplyMessage { get; set; }

    public DateTime? AttendedDate { get; set; }

    public int? SessionId { get; set; }
}