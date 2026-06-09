namespace SVM.Models
{
    public class ExamListVM
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = "";
        public string ExamType { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
        public int ClassId { get; set; }
        public int SectionId { get; set; }
    }
}