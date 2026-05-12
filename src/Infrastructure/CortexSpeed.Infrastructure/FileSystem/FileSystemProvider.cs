using CortexSpeed.Domain.Interfaces;

namespace CortexSpeed.Infrastructure.FileSystem;

public class FileSystemProvider : IFileSystemProvider
{
    // A 64KB buffer strikes the perfect balance between memory efficiency 
    // and disk I/O throughput across 16 concurrent streams. 16 * 64KB = ~1MB total memory.
    private const int BufferSize = 65536;

    public Stream OpenFileForWrite(string filePath)
    {
        // By using FileShare.ReadWrite, we allow 16 different threads to safely open 
        // their own file handles pointing to the same physical file on disk.
        // FileOptions.Asynchronous forces the OS into true overlapped non-blocking I/O.
        return new FileStream(
            filePath, 
            FileMode.OpenOrCreate, 
            FileAccess.Write, 
            FileShare.ReadWrite, 
            BufferSize, 
            FileOptions.Asynchronous | FileOptions.RandomAccess);
    }

    public Task PreAllocateFileAsync(string filePath, long sizeInBytes)
    {
        // Use OpenOrCreate so we don't truncate an already existing partially downloaded file on resume.
        using var fs = new FileStream(
            filePath, 
            FileMode.OpenOrCreate, 
            FileAccess.Write, 
            FileShare.None, 
            4096);

        if (fs.Length != sizeInBytes)
        {
            fs.SetLength(sizeInBytes);
        }
        
        return Task.CompletedTask;
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    public void MoveFile(string sourceFilePath, string destFilePath)
    {
        if (File.Exists(destFilePath))
        {
            File.Delete(destFilePath);
        }
        File.Move(sourceFilePath, destFilePath);
    }
}
