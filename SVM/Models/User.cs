using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SVM.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public int? GroupId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? ProfilePhoto { get; set; }

    public virtual Groupmaster? Group { get; set; }
    [JsonIgnore]
    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
