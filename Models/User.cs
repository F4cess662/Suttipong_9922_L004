using System;
using System.Collections.Generic;

namespace Suttipong_9922_L004.Models;

public partial class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime? LastActive { get; set; }

    public string? Status { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? Role { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

    public virtual ICollection<UserTrip> UserTrips { get; set; } = new List<UserTrip>();
}
