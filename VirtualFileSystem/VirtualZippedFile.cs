using System.IO.Compression;

namespace VirtualFileSystem;

/// <summary>
/// Concrete implementation for a virtual file.
/// This implementation is for accessing files inside an archive.
/// </summary>
internal class VirtualZippedFile : BaseVirtualFile
{
    private readonly string accessPath;
    private readonly ZipArchive zipArchive;

    internal VirtualZippedFile(ZipArchive zipArchiveReference, string accessPath)
    {
        this.zipArchive = zipArchiveReference;
        this.accessPath = accessPath;
    }

    internal override Stream GetFileStream()
    {
        return zipArchive.GetEntry(accessPath)?.Open()
            ?? throw new InvalidOperationException("File does not exist in the archive.");
    }
}
