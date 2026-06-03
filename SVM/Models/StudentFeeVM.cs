namespace SVM.Models
{
    public class StudentFeeVM
    {
        public int FeeId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AdmissionFees { get; set; }
        public decimal MonthlyFees { get; set; }
        public decimal OtherActivityFees { get; set; }
        public decimal ComputerFees { get; set; }
        public bool AlreadyPaid { get; set; }
    }
}
