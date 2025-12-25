namespace StudieAssistenten.Server.Services;

/// <summary>
/// Service for validating file content using magic bytes (file signatures)
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validates that a file's actual content matches its claimed type
    /// </summary>
    Task<(bool isValid, string? detectedType)> ValidateFileContentAsync(Stream fileStream, string claimedContentType);
}

public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;

    // File signatures (magic bytes) for supported file types
    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
    {
        {
            "application/pdf", new List<byte[]>
            {
                new byte[] { 0x25, 0x50, 0x44, 0x46 } // %PDF
            }
        },
        {
            "image/jpeg", new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, // JPEG (JFIF)
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, // JPEG (Exif)
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 }, // JPEG (Canon)
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 }  // JPEG (Samsung)
            }
        },
        {
            "image/jpg", new List<byte[]> // Alias for JPEG
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 }
            }
        },
        {
            "image/png", new List<byte[]>
            {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } // PNG
            }
        },
        {
            "text/plain", new List<byte[]>
            {
                // Text files don't have magic bytes, validate by checking if content is valid UTF-8
                // This will be handled separately
            }
        },
        {
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new List<byte[]>
            {
                new byte[] { 0x50, 0x4B, 0x03, 0x04 } // DOCX (ZIP-based format)
            }
        }
    };

    public FileValidationService(ILogger<FileValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool isValid, string? detectedType)> ValidateFileContentAsync(Stream fileStream, string claimedContentType)
    {
        // Normalize content type
        var contentType = claimedContentType.ToLowerInvariant();

        // Reset stream position
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        // Read the first 8 bytes (most magic bytes are within the first 8 bytes)
        var buffer = new byte[8];
        var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

        // Reset stream position for further processing
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        if (bytesRead == 0)
        {
            _logger.LogWarning("Empty file detected");
            return (false, null);
        }

        // Special handling for text files
        if (contentType == "text/plain")
        {
            // For text files, try to validate UTF-8 encoding
            return await ValidateTextFileAsync(fileStream);
        }

        // Check if we support this content type
        if (!FileSignatures.ContainsKey(contentType))
        {
            _logger.LogWarning("Unsupported content type for validation: {ContentType}", contentType);
            return (false, null);
        }

        // Check magic bytes
        var signatures = FileSignatures[contentType];
        foreach (var signature in signatures)
        {
            if (bytesRead >= signature.Length && buffer.Take(signature.Length).SequenceEqual(signature))
            {
                _logger.LogInformation("File content validated successfully for type: {ContentType}", contentType);
                return (true, contentType);
            }
        }

        // Try to detect actual file type
        var detectedType = DetectFileType(buffer, bytesRead);
        _logger.LogWarning(
            "File content validation failed. Claimed: {ClaimedType}, Detected: {DetectedType}",
            contentType,
            detectedType ?? "unknown");

        return (false, detectedType);
    }

    private async Task<(bool isValid, string? detectedType)> ValidateTextFileAsync(Stream fileStream)
    {
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        // Read up to 1KB to check if it's valid UTF-8 text
        var buffer = new byte[1024];
        var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        try
        {
            // Try to decode as UTF-8
            var text = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Check if decoded text contains mostly printable characters
            var printableCount = text.Count(c => !char.IsControl(c) || char.IsWhiteSpace(c));
            var printableRatio = (double)printableCount / text.Length;

            // If more than 90% of characters are printable, consider it valid text
            var isValid = printableRatio > 0.9;
            return (isValid, isValid ? "text/plain" : null);
        }
        catch
        {
            return (false, null);
        }
    }

    private string? DetectFileType(byte[] buffer, int bytesRead)
    {
        foreach (var kvp in FileSignatures)
        {
            foreach (var signature in kvp.Value)
            {
                if (bytesRead >= signature.Length && buffer.Take(signature.Length).SequenceEqual(signature))
                {
                    return kvp.Key;
                }
            }
        }

        return null;
    }
}
