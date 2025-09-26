using System;

namespace LostAndFound.Domain.Entities;

public class RolePermission : BaseEntity
{
    public string RoleName { get; set; } = default!; // Identity role name

    // Operation flags for Items UI
    public bool HandoverOwner { get; set; }
    public bool HandoverOffice { get; set; }
    public bool TransferStorage { get; set; }
    public bool ReceiveStorage { get; set; }
    public bool Dispose { get; set; }
    public bool Destroy { get; set; }
    public bool Sell { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
