using System;
using System.Collections.Generic;

namespace Suttipong_9922_L004.Models;

public partial class Labstudent
{
    public string StdId { get; set; } = null!;

    public string? StdPassword { get; set; }

    public string? StdName { get; set; }

    public string? StdLastname { get; set; }
}
