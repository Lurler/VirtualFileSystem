namespace VirtualFileSystem;

internal abstract class BaseVirtualFile
{
    internal abstract Stream GetFileStream();

    internal byte[] GetData()
    {
        using var ms = new MemoryStream();
        GetFileStream().CopyTo(ms);
        return ms.ToArray();
    }
}
