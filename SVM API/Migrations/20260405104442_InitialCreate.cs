using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SVM_API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admission_inquiry",
                columns: table => new
                {
                    inquiry_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    parent_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    class_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    message = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    inquiry_date = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__admissio__A1FB453A6383C8BA", x => x.inquiry_id);
                });

            migrationBuilder.CreateTable(
                name: "groupmaster",
                columns: table => new
                {
                    g_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    group_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__groupmas__49FB61C47F60ED59", x => x.g_id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    session_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    session_name = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sessions__69B13FDC0CBAE877", x => x.session_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    password = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    group_id = table.Column<int>(type: "int", nullable: true),
                    full_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    profile_photo = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__users__B9BE370F03317E3D", x => x.user_id);
                    table.ForeignKey(
                        name: "FK__users__group_id__0519C6AF",
                        column: x => x.group_id,
                        principalTable: "groupmaster",
                        principalColumn: "g_id");
                });

            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    class_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    medium = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    session_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__classes__FDF47986108B795B", x => x.class_id);
                    table.ForeignKey(
                        name: "FK__classes__session__1273C1CD",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id");
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    staff_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    employee_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    first_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    designation = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    qualification = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    experience_years = table.Column<int>(type: "int", nullable: true),
                    joining_date = table.Column<DateOnly>(type: "date", nullable: true),
                    salary = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    staf_photo = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__staff__1963DD9C07F6335A", x => x.staff_id);
                    table.ForeignKey(
                        name: "FK__staff__user_id__09DE7BCC",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "sections",
                columns: table => new
                {
                    section_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    section_name = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    class_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sections__F842676A46E78A0C", x => x.section_id);
                    table.ForeignKey(
                        name: "FK__sections__class___48CFD27E",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id");
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    subject_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    class_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__subjects__5004F66015502E78", x => x.subject_id);
                    table.ForeignKey(
                        name: "FK__subjects__class___173876EA",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id");
                });

            migrationBuilder.CreateTable(
                name: "staff_attendance",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    staff_id = table.Column<int>(type: "int", nullable: true),
                    attendance_date = table.Column<DateOnly>(type: "date", nullable: true),
                    checkin_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    checkout_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__staff_at__3213E83F2F10007B", x => x.id);
                    table.ForeignKey(
                        name: "FK__staff_att__staff__30F848ED",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "staff_id");
                });

            migrationBuilder.CreateTable(
                name: "fee_structure",
                columns: table => new
                {
                    fee_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    class_id = table.Column<int>(type: "int", nullable: true),
                    section_id = table.Column<int>(type: "int", nullable: true),
                    fee_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__fee_stru__A19C8AFB5812160E", x => x.fee_id);
                    table.ForeignKey(
                        name: "FK__fee_struc__class__59FA5E80",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK__fee_struc__secti__5AEE82B9",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "section_id");
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    admission_no = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    admission_date = table.Column<DateOnly>(type: "date", nullable: true),
                    roll_no = table.Column<int>(type: "int", nullable: true),
                    first_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    last_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    father_name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    GRNO = table.Column<int>(type: "int", nullable: true),
                    blood_group = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    aadhar_no = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    class_id = table.Column<int>(type: "int", nullable: true),
                    section_id = table.Column<int>(type: "int", nullable: true),
                    session_id = table.Column<int>(type: "int", nullable: true),
                    address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    city = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    state = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    pincode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    mother_phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    previous_school = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    student_photo = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__students__2A33069A4BAC3F29", x => x.student_id);
                    table.ForeignKey(
                        name: "FK__students__class___4E88ABD4",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK__students__sectio__4F7CD00D",
                        column: x => x.section_id,
                        principalTable: "sections",
                        principalColumn: "section_id");
                    table.ForeignKey(
                        name: "FK__students__sessio__5070F446",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id");
                    table.ForeignKey(
                        name: "FK__students__user_i__4D94879B",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "teacher_subject",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    staff_id = table.Column<int>(type: "int", nullable: true),
                    subject_id = table.Column<int>(type: "int", nullable: true),
                    class_id = table.Column<int>(type: "int", nullable: true),
                    session_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__teacher___3213E83F1A14E395", x => x.id);
                    table.ForeignKey(
                        name: "FK__teacher_s__class__1DE57479",
                        column: x => x.class_id,
                        principalTable: "classes",
                        principalColumn: "class_id");
                    table.ForeignKey(
                        name: "FK__teacher_s__sessi__1ED998B2",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "session_id");
                    table.ForeignKey(
                        name: "FK__teacher_s__staff__1BFD2C07",
                        column: x => x.staff_id,
                        principalTable: "staff",
                        principalColumn: "staff_id");
                    table.ForeignKey(
                        name: "FK__teacher_s__subje__1CF15040",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "subject_id");
                });

            migrationBuilder.CreateTable(
                name: "fee_payment",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    fee_id = table.Column<int>(type: "int", nullable: true),
                    amount_paid = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_mode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__fee_paym__ED1FC9EA5DCAEF64", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK__fee_payme__fee_i__60A75C0F",
                        column: x => x.fee_id,
                        principalTable: "fee_structure",
                        principalColumn: "fee_id");
                    table.ForeignKey(
                        name: "FK__fee_payme__stude__5FB337D6",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateTable(
                name: "student_attendance",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    attendance_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__student___3213E83F534D60F1", x => x.id);
                    table.ForeignKey(
                        name: "FK__student_a__stude__5535A963",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_classes_session_id",
                table: "classes",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_payment_fee_id",
                table: "fee_payment",
                column: "fee_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_payment_student_id",
                table: "fee_payment",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_class_id",
                table: "fee_structure",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_structure_section_id",
                table: "fee_structure",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_sections_class_id",
                table: "sections",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_user_id",
                table: "staff",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_attendance_staff_id",
                table: "staff_attendance",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_attendance_student_id",
                table: "student_attendance",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_class_id",
                table: "students",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_section_id",
                table: "students",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_session_id",
                table: "students",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_user_id",
                table: "students",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_subjects_class_id",
                table: "subjects",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_subject_class_id",
                table: "teacher_subject",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_subject_session_id",
                table: "teacher_subject",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_subject_staff_id",
                table: "teacher_subject",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_subject_subject_id",
                table: "teacher_subject",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_group_id",
                table: "users",
                column: "group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admission_inquiry");

            migrationBuilder.DropTable(
                name: "fee_payment");

            migrationBuilder.DropTable(
                name: "staff_attendance");

            migrationBuilder.DropTable(
                name: "student_attendance");

            migrationBuilder.DropTable(
                name: "teacher_subject");

            migrationBuilder.DropTable(
                name: "fee_structure");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "sections");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "classes");

            migrationBuilder.DropTable(
                name: "groupmaster");

            migrationBuilder.DropTable(
                name: "sessions");
        }
    }
}
