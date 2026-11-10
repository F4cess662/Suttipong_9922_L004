using System;
using System.Collections.Generic;

namespace Suttipong_9922_L004.Models;

public partial class TransportType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public decimal? BaseFare { get; set; }

    public decimal? FarePerKm { get; set; }

    public decimal? FarePerStation { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<TripLocation> TripLocations { get; set; } = new List<TripLocation>();
}
