using System;
using System.Collections.Generic;

namespace SVM.Models;

public partial class Groupmaster
{
    public int GId { get; set; }

    public string? GroupName { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
