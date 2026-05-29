using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM_API.Models;

[Table("admission_inquiry")]
public partial class AdmissionInquiry
{
    [Column("inquiry_id")]
    public int InquiryId { get; set; }

    [Column("student_name")]
    public string? StudentName { get; set; }

    [Column("parent_name")]
    public string? ParentName { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("class_name")]
    public string? ClassName { get; set; }

    [Column("message")]
    public string? Message { get; set; }

    [Column("inquiry_date")]
    public DateTime? InquiryDate { get; set; }

    [Column("is_seen")]
    public bool? IsSeen { get; set; }

    [Column("is_attended")]
    public bool? IsAttended { get; set; }

    [Column("reply_message")]
    public string? ReplyMessage { get; set; }

    [Column("attended_date")]
    public DateTime? AttendedDate { get; set; }

    [Column("session_id")]
    public int? SessionId { get; set; }
}