using System.ComponentModel.DataAnnotations;

namespace SVM_API.Models
{
	public class Expense
	{
		public int ExpenseId { get; set; }

        public string? VoucherNo { get; set; }

        [Required]
		public string Title { get; set; } = null!;

		[Required]
		public string Category { get; set; } = null!;

		[Required]
		public decimal Amount { get; set; }

		[Required]
		public string PaymentMethod { get; set; } = null!;

		[Required]
		public string PaidTo { get; set; } = null!;

		public DateTime ExpenseDate { get; set; }

		public string? Description { get; set; }

		public string? ReceiptFile { get; set; }

		public string Status { get; set; } = "Paid";

		public DateTime CreatedAt { get; set; }

		public int CreatedBy { get; set; }
	}
}