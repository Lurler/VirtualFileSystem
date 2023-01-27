using System.IO.Compression;

namespace VirtualFileSystem;

internal class VirtualFile
{
    /// <summary>
    /// Whether the file is a normal file in the file system or from inside the zipped archive
    /// </summary>
    internal enum FileType
    {
        Normal,
        Zipped
    }

    private readonly FileType accessType;
    private readonly string accessPath;
    private readonly ZipArchive? zipArchive;

    /// <summary>
    /// Constructor for normal files.
    /// </summary>
    internal VirtualFile(string accessPath)
    {
        accessType = FileType.Normal;
        this.accessPath = accessPath;
    }

    /// <summary>
    /// Constructor for zipped files.
    /// </summary>
    internal VirtualFile(ZipArchive zipArchiveReference, string accessPath)
    {
        accessType = FileType.Zipped;
        this.zipArchive = zipArchiveReference;
        this.accessPath = accessPath;
    }

    internal Stream GetFileStream()
    {
        // if it's a normal file - retun the simple file stream
        if (accessType == FileType.Normal)
            return new FileStream(accessPath, FileMode.Open, FileAccess.Read, FileShare.None);

        // otherwise it's a zipped file
        return zipArchive?.GetEntry(accessPath)?.Open()
            ?? throw new InvalidOperationException("File does not exist in the archive.");
    }

    internal byte[] GetData()
    {
        using var ms = new MemoryStream();
        GetFileStream().CopyTo(ms);
        return ms.ToArray();
    }

}
