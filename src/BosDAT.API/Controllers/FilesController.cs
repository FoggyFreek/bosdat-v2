using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BosDAT.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController(IConfiguration configuration) : ControllerBase
{
    private readonly string _basePath = configuration["FileStorage:BasePath"] ?? "uploads";

    [HttpGet("{storedFileName}")]
    public IActionResult GetFile(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return BadRequest();
        }

        // Strip any directory components supplied by the caller to prevent path traversal.
        var safeFileName = Path.GetFileName(storedFileName);

        // Reject if the sanitized name is empty, contains a path separator, or is an absolute path.
        if (string.IsNullOrEmpty(safeFileName) || safeFileName != storedFileName || Path.IsPathRooted(storedFileName))
        {
            return BadRequest();
        }

        var attachmentsDir = Path.GetFullPath(Path.Combine(_basePath, "attachments"));
        var filePath = Path.GetFullPath(Path.Combine(attachmentsDir, safeFileName));

        // Confirm the resolved path is strictly inside the expected directory.
        if (!filePath.StartsWith(attachmentsDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !filePath.Equals(attachmentsDir, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var contentType = GetContentType(safeFileName);
        return PhysicalFile(filePath, contentType);
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
