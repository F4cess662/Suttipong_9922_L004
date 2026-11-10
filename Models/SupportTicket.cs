using System;
using System.Collections.Generic;

namespace Suttipong_9922_L004.Models;

public partial class SupportTicket
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int? EmployeeId { get; set; }

    public string Subject { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual Employee? Employee { get; set; }
}
