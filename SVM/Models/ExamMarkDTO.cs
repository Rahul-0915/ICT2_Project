namespace SVM.Models
{
    public class ExamMarkDTO
    {
        public int ExamSubjectId { get; set; }
        public int StudentId { get; set; }
        public decimal? ObtainedMarks { get; set; }
        public int? EnteredBy { get; set; }
    }
}