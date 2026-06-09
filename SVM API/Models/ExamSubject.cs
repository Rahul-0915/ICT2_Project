using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM_API.Models
{
    [Table("exam_subjects")]
    public class ExamSubject
    {
        [Key]
        [Column("exam_subject_id")]
        public int ExamSubjectId { get; set; }

        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("subject_id")]
        public int SubjectId { get; set; }

        [Column("total_marks")]
        public int TotalMarks { get; set; }

        [Column("passing_marks")]
        public int? PassingMarks { get; set; }

        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        public virtual ICollection<ExamMark> ExamMarks { get; set; } = new List<ExamMark>();
    }
}