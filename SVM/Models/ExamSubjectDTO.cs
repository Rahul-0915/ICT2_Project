namespace SVM.Models
{
    public class ExamSubjectDTO
    {
        public int ExamSubjectId { get; set; }
        public int ExamId { get; set; }
        public int SubjectId { get; set; }
        public int TotalMarks { get; set; }
        public int? PassingMarks { get; set; }
    }
}