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
        // Using FileStream synchronously inside Task.Run or just normally 
        // to pre-allocate. SetLength is very fast on modern file systems (NTFS/ReFS).
        using var fs = new FileStream(
            filePath, 
            FileMode.Create, 
            FileAccess.Write, 
            FileShare.None, 
            4096);

        fs.SetLength(sizeInBytes);
        
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
