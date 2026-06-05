
namespace SVM.Models
{
    public class ReceiptViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string SectionName { get; set; }     
        public string Medium { get; set; }
        public decimal AdmissionFees { get; set; }
        public decimal MonthlyFees { get; set; }
        public decimal OtherActivityFees { get; set; }
        public decimal ComputerFees { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentDate { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentMode { get; set; } = string.Empty;
    }
}