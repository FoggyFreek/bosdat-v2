namespace BosDAT.Core.Interfaces;

public interface IFileStorageService
{
    Task<(string StoredFileName, string FilePath)> SaveAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    void Delete(string storedFileName);
    string GetUrl(string storedFileName);
}
