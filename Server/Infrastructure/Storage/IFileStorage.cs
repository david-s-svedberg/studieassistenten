namespace StudieAssistenten.Server.Infrastructure.Storage;

/// <summary>
/// Abstraction for file storage operations.
/// Can be implemented for local file system, Azure Blob Storage, AWS S3, etc.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves a file stream to storage
    /// </summary>
    /// <param name="stream">The file stream to save</param>
    /// <param name="fileName">The unique file name</param>
    /// <returns>The path/identifier where the file was saved</returns>
    Task<string> SaveAsync(Stream stream, string fileName);

    /// <summary>
    /// Reads a file from storage
    /// </summary>
    /// <param name="filePath">The file path/identifier</param>
    /// <returns>The file content as a stream</returns>
    Task<Stream> ReadAsync(string filePath);

    /// <summary>
    /// Deletes a file from storage
    /// </summary>
    /// <param name="filePath">The file path/identifier to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(string filePath);

    /// <summary>
    /// Checks if a file exists in storage
    /// </summary>
    /// <param name="filePath">The file path/identifier to check</param>
    /// <returns>True if file exists</returns>
    Task<bool> ExistsAsync(string filePath);

    /// <summary>
    /// Gets the full path for a file
    /// </summary>
    /// <param name="fileName">The file name</param>
    /// <returns>The full file path</returns>
    string GetFilePath(string fileName);
}
