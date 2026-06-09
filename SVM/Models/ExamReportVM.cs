namespace SVM.Models
{
    public class ExamReportVM
    {
        public string ExamName { get; set; } = "";
        public List<StudentReportVM> Students { get; set; } = new();
    }

    public class StudentReportVM
    {
        public int RollNo { get; set; }
        public string StudentName { get; set; } = "";
        public string GrNo { get; set; } = "";
        public List<SubjectMarkVM> SubjectMarks { get; set; } = new();
        public int TotalObtained { get; set; }
        public int TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public string Result { get; set; } = "";
        public int? Rank { get; set; }
    }

    public class SubjectMarkVM
    {
        public string SubjectName { get; set; } = "";
        public int TotalMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public int PassingMarks { get; set; }
        public string Status { get; set; } = "";
    }
}