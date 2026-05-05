using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM.Models
{
    [Table("Updates")]
    public class Update
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }   

        [Required]
        [MaxLength(10)]
        public string Category { get; set; }     

        [MaxLength(500)]
        public string FilePath { get; set; }     

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}