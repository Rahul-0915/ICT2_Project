using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SVM.Models
{
    [Table("fee_structure")]
    public partial class FeeStructure
    {
        [Key]
        [Column("fee_id")]
        public int FeeId { get; set; }

        [Column("class_id")]
        public int? ClassId { get; set; }

        [Column("admission_fees")]
        public decimal? AdmissionFees { get; set; }

        [Column("monthly_fees")]
        public decimal? MonthlyFees { get; set; }

        [Column("other_activity_fees")]   
        public decimal? OtherActivityFees { get; set; }

        [Column("computer_fees")]          
        public decimal? ComputerFees { get; set; }

        [Column("total_amount")]
        public decimal? TotalAmount { get; set; }

        [Column("session_id")]
        public int? SessionId { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        // Optional: Keep if needed, but we won't use in UI
        // public virtual ICollection<MonthlyFeeDetail> MonthlyFeeDetails { get; set; } = new List<MonthlyFeeDetail>();
    }
}