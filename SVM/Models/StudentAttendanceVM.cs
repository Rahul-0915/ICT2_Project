namespace SVM.Models
{
    public class StudentAttendanceVM
    {
        public List<StudentMonthItem> Students { get; set; }
        public List<DateTime> Dates { get; set; }
        public int TotalDays { get; set; }
    }

    public class StudentMonthItem
    {
        public int RollNo { get; set; }
        public string StudentName { get; set; }
        public decimal Percentage { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public List<DailyStatus> DailyStatus { get; set; }
    }

    public class DailyStatus
    {
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }
}