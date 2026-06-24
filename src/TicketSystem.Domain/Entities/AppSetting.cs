namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;

public class AppSetting : IAuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
