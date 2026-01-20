using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IDuplicateDetectionService
{
    Task<DuplicateCheckResultDto> CheckForDuplicatesAsync(
        CheckDuplicatesDto dto,
        CancellationToken cancellationToken = default);
}
