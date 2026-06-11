namespace SVM.Models
{
    public class StudentDashboardVM
    {
        public Student Student { get; set; }

        public StudentFeeVM? Fee { get; set; }

        public StudentExamResult? LastResult { get; set; }

        public List<Updates> Notices { get; set; } = new();

        public List<Timetable> TodayTimetable { get; set; } = new();
    }
}