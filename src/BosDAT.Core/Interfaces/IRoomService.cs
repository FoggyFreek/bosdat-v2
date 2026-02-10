using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IRoomService
{
    Task<List<RoomDto>> GetAllAsync(bool? activeOnly, CancellationToken ct = default);
    Task<RoomDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<RoomDto> CreateAsync(CreateRoomDto dto, CancellationToken ct = default);
    Task<RoomDto?> UpdateAsync(int id, UpdateRoomDto dto, CancellationToken ct = default);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, CancellationToken ct = default);
    Task<(bool Success, string? ErrorMessage, RoomDto? Room)> ArchiveAsync(int id, CancellationToken ct = default);
    Task<RoomDto?> ReactivateAsync(int id, CancellationToken ct = default);
}
