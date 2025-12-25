namespace StudieAssistenten.Server.Infrastructure.Storage;

/// <summary>
/// Local file system implementation of IFileStorage
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
    {
        _logger = logger;
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
            _logger.LogInformation("Created uploads directory: {Path}", _uploadPath);
        }
    }

    public async Task<string> SaveAsync(Stream stream, string fileName)
    {
        var filePath = GetFilePath(fileName);

        // Final safety check: ensure the resolved path is still within the upload directory
        var fullPath = Path.GetFullPath(filePath);
        var uploadPathFull = Path.GetFullPath(_uploadPath);

        if (!fullPath.StartsWith(uploadPathFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid file path - potential path traversal attack");
        }

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);

        _logger.LogInformation("File saved: {FileName} at {Path}", fileName, filePath);

        return filePath;
    }

    public async Task<Stream> ReadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var memoryStream = new MemoryStream();
        using var fileStream = File.OpenRead(filePath);
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public Task<bool> DeleteAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {Path}", filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", filePath);
            throw;
        }
    }

    public Task<bool> ExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }

    public string GetFilePath(string fileName)
    {
        return Path.Combine(_uploadPath, fileName);
    }
}
