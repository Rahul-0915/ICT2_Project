namespace SVM_API.Models
{
    public partial class ExamSubject
    {
        public int ExamSubjectId { get; set; }

        public int ExamId { get; set; }

        public int SubjectId { get; set; }

        public int TotalMarks { get; set; }

        // New Column
        public int? PassingMarks { get; set; }

        // Navigation Properties
        public virtual Exam? Exam { get; set; }

        public virtual Subject? Subject { get; set; }

        public virtual ICollection<ExamMark> ExamMarks { get; set; } = new List<ExamMark>();
    }
}