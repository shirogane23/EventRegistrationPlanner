using System;
using System.Collections.Generic;

namespace EventFlow.Api.Models;

public partial class Venue
{
    public Guid VenueId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public int Capacity { get; set; }

    public DateTime CreatedUtc { get; set; }

    public virtual ICollection<Event> Event { get; set; } = new List<Event>();
}
