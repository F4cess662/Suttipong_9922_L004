using System;
using System.Collections.Generic;

namespace Suttipong_9922_L004.Models;

public partial class TripLocation
{
    public int Id { get; set; }

    public int TripId { get; set; }

    public int LocationId { get; set; }

    public int StopOrder { get; set; }

    public int? TransportId { get; set; }

    public int? TravelDurationMins { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Location Location { get; set; } = null!;

    public virtual TransportType? Transport { get; set; }

    public virtual UserTrip Trip { get; set; } = null!;
}
