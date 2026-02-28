using Microsoft.Extensions.Configuration;
using BosDAT.Core.Interfaces.Services;

namespace BosDAT.Infrastructure.Services;

public class LocalFileStorageService(IConfiguration configuration) : IFileStorageService
{
    private readonly string _basePath = configuration["FileStorage:BasePath"] ?? "uploads";

    public async Task<(string StoredFileName, string FilePath)> SaveAsync(
        Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var directory = Path.Combine(_basePath, "attachments");

        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, storedFileName);
        await using var fs = File.Create(filePath);
        await fileStream.CopyToAsync(fs, ct);

        return (storedFileName, filePath);
    }

    public void Delete(string storedFileName)
    {
        var filePath = Path.Combine(_basePath, "attachments", storedFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public string GetUrl(string storedFileName) => $"/api/files/{storedFileName}";
}
