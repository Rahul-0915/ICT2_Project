namespace SVM_API.Models
{
    public partial class Exam
    {
        public int ExamId { get; set; }

        public string ExamName { get; set; } = null!;

        public string ExamType { get; set; } = null!;

        public int SessionId { get; set; }

        public int ClassId { get; set; }

        public int SectionId { get; set; }

        public string Medium { get; set; } = null!;

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int? IsActive { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Navigation Properties
        public virtual Session? Session { get; set; }

        public virtual Class? Class { get; set; }

        public virtual Section? Section { get; set; }

        public virtual ICollection<ExamSubject> ExamSubjects { get; set; } = new List<ExamSubject>();
    }
}