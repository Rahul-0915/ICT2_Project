using System.ComponentModel.DataAnnotations;

namespace SVM.Models
{
    public class ExamDTO
    {
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Exam name is required")]
        [StringLength(100, ErrorMessage = "Exam name cannot exceed 100 characters")]
        public string ExamName { get; set; } = "";

        [Required(ErrorMessage = "Exam type is required")]
        public string ExamType { get; set; } = "";

        [Required(ErrorMessage = "Session is required")]
        public int SessionId { get; set; }

        [Required(ErrorMessage = "Class is required")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Section is required")]
        public int SectionId { get; set; }

        [Required(ErrorMessage = "Medium is required")]
        public string Medium { get; set; } = "";

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        public int IsActive { get; set; } = 1;
        public int? CreatedBy { get; set; }
        public bool IsPublished { get; set; }
    }
}