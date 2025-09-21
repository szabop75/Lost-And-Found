using System;

namespace LostAndFound.Domain.Entities;

public class RoleAuditLog : BaseEntity
{
    public string TargetUserId { get; set; } = default!;         // kinek a szerepköre változott
    public string? TargetEmail { get; set; }

    public string? OldRole { get; set; }
    public string? NewRole { get; set; }

    public string PerformedByUserId { get; set; } = default!;    // ki végezte a módosítást
    public string? PerformedByEmail { get; set; }

    public string Action { get; set; } = default!;               // CreateUser / UpdateRole / DeleteUser
}
