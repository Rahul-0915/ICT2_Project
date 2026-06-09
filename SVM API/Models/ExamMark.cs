using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM_API.Models
{
    [Table("exam_marks")]
    public class ExamMark
    {
        [Key]
        [Column("mark_id")]
        public int MarkId { get; set; }

        [Column("exam_subject_id")]
        public int ExamSubjectId { get; set; }

        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("obtained_marks")]
        public decimal? ObtainedMarks { get; set; }

        [Column("entered_by")]
        public int? EnteredBy { get; set; }

        [Column("entered_at")]
        public DateTime? EnteredAt { get; set; }

        [ForeignKey("ExamSubjectId")]
        public virtual ExamSubject? ExamSubject { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
}