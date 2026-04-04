using System;
using System.Collections.Generic;

namespace SVM_API.Models;

public partial class TeacherSubject
{
    public int Id { get; set; }

    public int? StaffId { get; set; }

    public int? SubjectId { get; set; }

    public int? ClassId { get; set; }

    public int? SessionId { get; set; }

    public virtual Class? Class { get; set; }

    public virtual Session? Session { get; set; }

    public virtual Staff? Staff { get; set; }

    public virtual Subject? Subject { get; set; }
}
