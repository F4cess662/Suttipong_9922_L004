using System;
using System.Collections.Generic;

namespace Suttipong_9922_L004.Models;

public partial class UserTrip
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TripName { get; set; } = null!;

    public decimal? TotalDistanceKm { get; set; }

    public decimal? EstimatedCost { get; set; }

    public bool? IsFavorite { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<TripLocation> TripLocations { get; set; } = new List<TripLocation>();

    public virtual User User { get; set; } = null!;
}
