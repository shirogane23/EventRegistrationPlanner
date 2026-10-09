using System;
using System.Collections.Generic;

namespace EventFlow.Api.Models;

public partial class Event
{
    public Guid EventId { get; set; }

    public Guid OwnerUserId { get; set; }

    public Guid VenueId { get; set; }

    public string Title { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public DateTime RegistrationDeadlineUtc { get; set; }

    public int Capacity { get; set; }

    public string Visibility { get; set; } = null!;

    public bool ApprovalRequired { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Registration> Registration { get; set; } = new List<Registration>();

    public virtual Venue Venue { get; set; } = null!;
}
