using BosDAT.Core.DTOs;

namespace BosDAT.Core.Interfaces;

public interface IInvoiceRunService
{
    Task<InvoiceRunResultDto> RunBulkInvoiceGenerationAsync(
        StartInvoiceRunDto dto, string initiatedBy, Guid userId, CancellationToken ct = default);

    Task<InvoiceRunsPageDto> GetRunsAsync(int page, int pageSize, CancellationToken ct = default);

    Task<InvoiceRunDto?> GetRunByIdAsync(Guid id, CancellationToken ct = default);
}
