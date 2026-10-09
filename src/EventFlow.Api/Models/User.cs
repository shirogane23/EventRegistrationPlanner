using System;
using System.Collections.Generic;

namespace EventFlow.Api.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string DemoIdentity { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }

    public virtual ICollection<Event> Event { get; set; } = new List<Event>();

    public virtual ICollection<Registration> Registration { get; set; } = new List<Registration>();
}
