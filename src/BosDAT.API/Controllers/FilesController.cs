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
        if (string.IsNullOrWhiteSpace(storedFileName) || storedFileName.Contains(".."))
        {
            return BadRequest();
        }

        var filePath = Path.Combine(_basePath, "attachments", storedFileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var contentType = GetContentType(storedFileName);
        return PhysicalFile(Path.GetFullPath(filePath), contentType);
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
