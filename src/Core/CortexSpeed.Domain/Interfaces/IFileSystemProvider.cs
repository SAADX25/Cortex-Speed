namespace CortexSpeed.Domain.Interfaces;

/// <summary>
/// Abstraction for file system operations to allow mocking and testing without disk IO.
/// </summary>
public interface IFileSystemProvider
{
    Stream OpenFileForWrite(string filePath);
    Task PreAllocateFileAsync(string filePath, long sizeInBytes);
    void DeleteFile(string filePath);
    bool FileExists(string filePath);
    void MoveFile(string sourceFilePath, string destFilePath);
}
