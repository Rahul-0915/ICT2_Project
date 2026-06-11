

namespace SVM.Models
{
    public class StudentResultVM
    {
        public string StudentName { get; set; } = "";
        public int RollNo { get; set; }
        public string GrNo { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string SectionName { get; set; } = "";
        public List<StudentExamResult> Exams { get; set; } = new();
    }

    public class StudentExamResult
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = "";
        public string ExamType { get; set; } = "";
        public DateTime ExamDate { get; set; }
        public List<StudentSubjectMark> Subjects { get; set; } = new();
        public decimal TotalObtainedMarks { get; set; }
        public decimal TotalMaxMarks { get; set; }
        public decimal Percentage { get; set; }
        public string Result { get; set; } = "";
    }

    public class StudentMarksResponse
    {
        public string ExamName { get; set; } = "";
        public List<StudentSubjectMark> Subjects { get; set; } = new();
    }

    public class StudentSubjectMark
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = "";
        public int TotalMarks { get; set; }
        public int PassingMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
    }
}