namespace BosDAT.Core.Interfaces.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? IpAddress { get; }
}
