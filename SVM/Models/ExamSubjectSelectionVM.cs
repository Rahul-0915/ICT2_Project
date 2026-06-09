using System.ComponentModel.DataAnnotations;

namespace SVM.Models
{
    public class ExamSubjectSelectionVM
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = "";

        public bool IsSelected { get; set; }

        [Range(1, 500, ErrorMessage = "Total marks must be between 1 and 500")]
        public int TotalMarks { get; set; }

        [Range(0, 500, ErrorMessage = "Passing marks must be between 0 and total marks")]
        public int PassingMarks { get; set; }
    }
}