using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SVM.Models;

public partial class SvmContext : DbContext
{
    public SvmContext()
    {
    }

    public SvmContext(DbContextOptions<SvmContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdmissionInquiry> AdmissionInquiries { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<FeePayment> FeePayments { get; set; }

    public virtual DbSet<FeeStructure> FeeStructures { get; set; }

    public virtual DbSet<Groupmaster> Groupmasters { get; set; }

    public virtual DbSet<Section> Sections { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<StaffAttendance> StaffAttendances { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentAttendance> StudentAttendances { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<TeacherSubject> TeacherSubjects { get; set; }

    public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            => optionsBuilder.UseSqlServer("Data Source=LAPTOP-4UGH4KDC\\SQLEXPRESS;Initial Catalog=SVM;Integrated Security=True;Encrypt=False");
    //    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Data Source=LAPTOP-0UK50KGM\\SQLEXPRESS;Initial Catalog=SVM;Integrated Security=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdmissionInquiry>(entity =>
        {
            entity.HasKey(e => e.InquiryId).HasName("PK__admissio__A1FB453A6383C8BA");

            entity.ToTable("admission_inquiry");

            entity.Property(e => e.InquiryId).HasColumnName("inquiry_id");
            entity.Property(e => e.ClassName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("class_name");
            entity.Property(e => e.InquiryDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("inquiry_date");
            entity.Property(e => e.Message)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("message");
            entity.Property(e => e.ParentName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("parent_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.StudentName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("student_name");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__classes__FDF47986108B795B");

            entity.ToTable("classes");

            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.ClassName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("class_name");
            entity.Property(e => e.Medium)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("medium");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.Session).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__classes__session__1273C1CD");
        });

        modelBuilder.Entity<FeePayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__fee_paym__ED1FC9EA5DCAEF64");

            entity.ToTable("fee_payment");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.AmountPaid)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount_paid");
            entity.Property(e => e.FeeId).HasColumnName("fee_id");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("payment_mode");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Fee).WithMany(p => p.FeePayments)
                .HasForeignKey(d => d.FeeId)
                .HasConstraintName("FK__fee_payme__fee_i__60A75C0F");

            entity.HasOne(d => d.Student).WithMany(p => p.FeePayments)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__fee_payme__stude__5FB337D6");
        });

        modelBuilder.Entity<FeeStructure>(entity =>
        {
            entity.HasKey(e => e.FeeId).HasName("PK__fee_stru__A19C8AFB5812160E");

            entity.ToTable("fee_structure");

            entity.Property(e => e.FeeId).HasColumnName("fee_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.FeeType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("fee_type");
            entity.Property(e => e.SectionId).HasColumnName("section_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Class).WithMany(p => p.FeeStructures)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__fee_struc__class__59FA5E80");

            entity.HasOne(d => d.Section).WithMany(p => p.FeeStructures)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__fee_struc__secti__5AEE82B9");
        });

        modelBuilder.Entity<Groupmaster>(entity =>
        {
            entity.HasKey(e => e.GId).HasName("PK__groupmas__49FB61C47F60ED59");

            entity.ToTable("groupmaster");

            entity.Property(e => e.GId).HasColumnName("g_id");
            entity.Property(e => e.GroupName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("group_name");
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.HasKey(e => e.SectionId).HasName("PK__sections__F842676A46E78A0C");

            entity.ToTable("sections");

            entity.Property(e => e.SectionId).HasColumnName("section_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.SectionName)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("section_name");

            entity.HasOne(d => d.Class).WithMany(p => p.Sections)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__sections__class___48CFD27E");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__sessions__69B13FDC0CBAE877");

            entity.ToTable("sessions");

            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SessionName)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("session_name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__staff__1963DD9C07F6335A");

            entity.ToTable("staff");

            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("address");
            entity.Property(e => e.Designation)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("designation");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
          
            entity.Property(e => e.ExperienceYears).HasColumnName("experience_years");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.JoiningDate).HasColumnName("joining_date");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Qualification)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("qualification");
            entity.Property(e => e.Salary)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("salary");
            entity.Property(e => e.StafPhoto)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("staf_photo");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__staff__user_id__09DE7BCC");
        });

        modelBuilder.Entity<StaffAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__staff_at__3213E83F2F10007B");

            entity.ToTable("staff_attendance");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttendanceDate).HasColumnName("attendance_date");
            entity.Property(e => e.CheckinTime).HasColumnName("checkin_time");
            entity.Property(e => e.CheckoutTime).HasColumnName("checkout_time");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffAttendances)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__staff_att__staff__30F848ED");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__students__2A33069A4BAC3F29");

            entity.ToTable("students");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.AadharNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("aadhar_no");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("address");
            entity.Property(e => e.AdmissionDate).HasColumnName("admission_date");
            entity.Property(e => e.AdmissionNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("admission_no");
            entity.Property(e => e.BloodGroup)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("blood_group");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("city");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.FatherName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("father_name");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.Grno).HasColumnName("GRNO");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.MotherPhone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("mother_phone");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Pincode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("pincode");
            entity.Property(e => e.PreviousSchool)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("previous_school");
            entity.Property(e => e.RollNo).HasColumnName("roll_no");
            entity.Property(e => e.SectionId).HasColumnName("section_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("state");
            entity.Property(e => e.StudentPhoto)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("student_photo");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Class).WithMany(p => p.Students)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__students__class___4E88ABD4");

            entity.HasOne(d => d.Section).WithMany(p => p.Students)
                .HasForeignKey(d => d.SectionId)
                .HasConstraintName("FK__students__sectio__4F7CD00D");

            entity.HasOne(d => d.Session).WithMany(p => p.Students)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__students__sessio__5070F446");

            entity.HasOne(d => d.User).WithMany(p => p.Students)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__students__user_i__4D94879B");
        });

        modelBuilder.Entity<StudentAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__student___3213E83F534D60F1");

            entity.ToTable("student_attendance");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttendanceDate).HasColumnName("attendance_date");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentAttendances)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__student_a__stude__5535A963");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("PK__subjects__5004F66015502E78");

            entity.ToTable("subjects");

            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.SubjectName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("subject_name");

            entity.HasOne(d => d.Class).WithMany(p => p.Subjects)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__subjects__class___173876EA");
        });

        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__teacher___3213E83F1A14E395");

            entity.ToTable("teacher_subject");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");

            entity.HasOne(d => d.Class).WithMany(p => p.TeacherSubjects)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("FK__teacher_s__class__1DE57479");

            entity.HasOne(d => d.Session).WithMany(p => p.TeacherSubjects)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK__teacher_s__sessi__1ED998B2");

            entity.HasOne(d => d.Staff).WithMany(p => p.TeacherSubjects)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK__teacher_s__staff__1BFD2C07");

            entity.HasOne(d => d.Subject).WithMany(p => p.TeacherSubjects)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__teacher_s__subje__1CF15040");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__users__B9BE370F03317E3D");

            entity.ToTable("users");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.ProfilePhoto)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("profile_photo");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("username");

            entity.HasOne(d => d.Group).WithMany(p => p.Users)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK__users__group_id__0519C6AF");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
