using System.ComponentModel.DataAnnotations;

namespace SVM.Models
{
	public class Expense
	{
		public int ExpenseId { get; set; }

		public string VoucherNo { get; set; } = null!;

		[Required(ErrorMessage = "Title is required")]
		[StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
		public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
		public string? Category { get; set; }

		[Required(ErrorMessage = "Amount is required")]
		[Range(1, 99999999, ErrorMessage = "Amount must be greater than 0")]
		public decimal Amount { get; set; }

		[Required(ErrorMessage = "Payment Method is required")]
		[StringLength(50)]
		public string? PaymentMethod { get; set; }

		[Required(ErrorMessage = "Paid To field is required")]
		[StringLength(200)]
		public string? PaidTo { get; set; }

		[Required(ErrorMessage = "Expense Date is required")]
		public DateTime ExpenseDate { get; set; }

		[StringLength(1000, ErrorMessage = "Description is too long")]
		public string? Description { get; set; }

		public string? ReceiptFile { get; set; }

		[Required(ErrorMessage = "Status is required")]
		[StringLength(50)]
		public string? Status { get; set; } = "Paid";

		public DateTime CreatedAt { get; set; }

		public int CreatedBy { get; set; }
	}
}