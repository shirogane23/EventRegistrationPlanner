using System;
using System.Collections.Generic;

namespace EventFlow.Api.Models;

public partial class Registration
{
    public Guid RegistrationId { get; set; }

    public Guid UserId { get; set; }

    public Guid EventId { get; set; }

    public string Status { get; set; } = null!;

    public string? ConfirmationReference { get; set; }

    public string? DecisionReason { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
