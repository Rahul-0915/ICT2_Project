using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM_API.Models
{
    [Table("exams")]
    public class Exam
    {
        [Key]
        [Column("exam_id")]
        public int ExamId { get; set; }

        [Column("exam_name")]
        public string ExamName { get; set; } = null!;

        [Column("exam_type")]
        public string ExamType { get; set; } = null!;

        [Column("session_id")]
        public int SessionId { get; set; }

        [Column("class_id")]
        public int ClassId { get; set; }

        [Column("section_id")]
        public int SectionId { get; set; }

        [Column("medium")]
        public string Medium { get; set; } = null!;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("is_active")]
        public int? IsActive { get; set; } = 1;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("is_published")]
        public bool IsPublished { get; set; }

        // Navigation properties
        [ForeignKey("SessionId")]
        public virtual Session? Session { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        public virtual ICollection<ExamSubject> ExamSubjects { get; set; } = new List<ExamSubject>();
    }
}