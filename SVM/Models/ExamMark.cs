namespace SVM.Models
{
    public partial class ExamMark
    {
        public int MarkId { get; set; }

        public int ExamSubjectId { get; set; }

        public int StudentId { get; set; }

        public decimal? ObtainedMarks { get; set; }

        public int? EnteredBy { get; set; }

        public DateTime? EnteredAt { get; set; }

        // Navigation Properties
        public virtual ExamSubject? ExamSubject { get; set; }

        public virtual Student? Student { get; set; }
    }
}