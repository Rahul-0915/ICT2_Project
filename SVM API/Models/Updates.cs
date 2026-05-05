using System.ComponentModel.DataAnnotations;

namespace SVM_API.Models
{
    public class Updates
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Title { get; set; }

        [Required]
        [StringLength(50)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } // "notice" or "event"

        [StringLength(255)]
        public string FilePath { get; set; }

        public int Status { get; set; } // 1 = Active, 0 = Inactive

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
