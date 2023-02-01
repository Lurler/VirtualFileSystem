namespace VirtualFileSystem;

/// <summary>
/// Concrete implementation for a virtual file.
/// This implementation is for accessing files on the hard drive.
/// </summary>
internal class VirtualOSFile : BaseVirtualFile
{
    private readonly string accessPath;

    internal VirtualOSFile(string accessPath)
    {
        this.accessPath = accessPath;
    }

    internal override Stream GetFileStream()
    {
        return new FileStream(accessPath, FileMode.Open, FileAccess.Read, FileShare.None);
    }
}
