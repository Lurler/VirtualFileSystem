using System.IO.Compression;
using System.Text;

namespace VirtualFileSystem;

/// <summary>
/// Virtual File System (VFS) manager
/// </summary>
public class VFSManager
{
    /// <summary>
    /// Stores paths to all files in the VFS with newer files overriding the existing files as they are loaded
    /// if the virtual paths collide.
    /// </summary>
    private readonly Dictionary<string, BaseVirtualFile> virtualFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Stores all folders that exist in the VFS with at least one file.
    /// </summary>
    private readonly HashSet<string> virtualFolders = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get a list of all virtual file entries in the VFS.
    /// </summary>
    public List<string> Entries => virtualFiles.Keys.ToList();

    /// <summary>
    /// Get a list of all virtual folders in the VFS.
    /// </summary>
    public List<string> Folders => virtualFolders.ToList();

    /// <summary>
    /// Adds a new root container which can be either a folder on the hard drive or a zip file.
    /// </summary>
    public void AddRootContainer(string path)
    {
        // check if it's a zipped container
        if (File.Exists(path))
        {
            IncludeArchive(path);
            return;
        }
        
        // check if it's just a folder
        if (Directory.Exists(path))
        {
            IncludeFolder(path);
            return;
        }

        // otherwise incorrect path provided
        throw new ArgumentException("Incorrect path provided.");
    }

    /// <summary>
    /// Formats virtual path to be uniform, so there are no identical entries but with different paths.
    /// </summary>
    private string NormalizePath(string path)
    {
        return path
            .Replace(@"\\", @"\")
            .Replace(@"\", @"/");
    }

    private void IncludeArchive(string path)
    {
        try
        {
            // try opening the archive and iterate over all entries there
            var zip = ZipFile.OpenRead(path);
            foreach (var entry in zip.Entries)
            {
                // skip directories
                if (entry.FullName.Last() == '/')
                    continue;

                // standardize slashes
                var virtualPath = NormalizePath(entry.FullName);

                // create a virtual file, then add or replace it in the dictionary
                virtualFiles[virtualPath] = new VirtualZippedFile(zip, entry.FullName);

                // register virtual folder if it isn't registered yet
                var virtualFolder = NormalizePath(Path.GetDirectoryName(virtualPath) ?? "");
                if (!string.IsNullOrEmpty(virtualFolder) && !virtualFolders.Contains(virtualFolder + "/", StringComparer.OrdinalIgnoreCase))
                    virtualFolders.Add(virtualFolder + "/");
            }
        }
        catch (InvalidDataException ex)
        {
            throw new ArgumentException("The zip archive is invalid.", ex);
        }
        catch (IOException ex)
        {
            throw new ArgumentException("Failed to access the zip archive.", ex);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Incorrect container. The vfs container must be a folder or a zip archive.", ex);
        }
    }

    private void IncludeFolder(string path)
    {
        // load complete file list into the list
        int rootLength = path.Length + (path.EndsWith(@"\") ? 0 : 1);
        var files =
            Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Select(p => p.Remove(0, rootLength))
                .ToList();

        // go through all of the files that were found
        foreach (string file in files)
        {
            // standardize slashes
            var relativePath = NormalizePath(file);

            // create a virtual file, then add or replace it in the dictionary
            virtualFiles[relativePath] = new VirtualOSFile(path + "/" + file);

            // register virtual folder if it isn't registered yet
            var virtualFolder = NormalizePath(Path.GetDirectoryName(relativePath) ?? "");
            if (!string.IsNullOrEmpty(virtualFolder) && !virtualFolders.Contains(virtualFolder + "/", StringComparer.OrdinalIgnoreCase))
                virtualFolders.Add(virtualFolder + "/");
        }

    }

    /// <summary>
    /// Checks if a file with a given virtual path exists in the VFS.
    /// </summary>
    public bool FileExists(string virtualPath)
    {
        return virtualFiles.ContainsKey(NormalizePath(virtualPath));
    }

    /// <summary>
    /// Checks if a folder with a given virtual path exists in the VFS.
    /// </summary>
    public bool FolderExists(string virtualPath)
    {
        // add final slash if needed
        if (!virtualPath.EndsWith("/"))
            virtualPath += '/';

        return virtualFolders.Contains(NormalizePath(virtualPath));
    }

    /// <summary>
    /// Returns a stream to a file with a given virtual path.
    /// </summary>
    public Stream GetFileStream(string virtualPath)
    {
        if (!virtualFiles.TryGetValue(NormalizePath(virtualPath), out BaseVirtualFile? value))
        {
            throw new FileNotFoundException($"The virtual file '{virtualPath}' does not exist.");
        }
        return value.GetFileStream();
    }

    /// <summary>
    /// Read all data from the file and return it as an array of bytes.
    /// </summary>
    public byte[] GetFileContents(string virtualPath)
    {
        if (!virtualFiles.TryGetValue(NormalizePath(virtualPath), out BaseVirtualFile? value))
        {
            throw new FileNotFoundException($"The virtual file '{virtualPath}' does not exist.");
        }
        return value.GetData();
    }

    /// <summary>
    /// Read all data from the file and return it as text.
    /// </summary>
    public string GetFileContentsAsText(string virtualPath, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;  // defaults to UTF-8 if no encoding is provided
        using var stream = GetFileStream(virtualPath);
        using var reader = new StreamReader(stream, encoding);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Get list of files in a given folder (list of their paths).
    /// </summary>
    public List<string> GetFilesInFolder(string virtualPath, bool recursive = false)
    {
        // add final slash if needed
        if (virtualPath.Length == 0 || virtualPath.Last() != '/')
            virtualPath += '/';

        // check if we want files in root folder
        if (virtualPath == "/")
            return recursive 
                ? virtualFiles.Keys.ToList()
                : virtualFiles.Keys.Where(s => !s.Contains('/')).ToList();

        // return empty list if no such path exists
        if (!virtualFolders.Contains(virtualPath))
            return new();

        // finally get the file list (if any)
        return virtualFiles.Keys
            .Where(s => recursive
                ? s.StartsWith(virtualPath, StringComparison.OrdinalIgnoreCase)
                : s.StartsWith(virtualPath, StringComparison.OrdinalIgnoreCase) && s.IndexOf('/', virtualPath.Length) == -1)
            .ToList();
    }

    /// <summary>
    /// Get list of folders in a given folder (list of paths).
    /// </summary>
    public List<string> GetFoldersInFolder(string virtualPath, bool recursive = false)
    {
        // add final slash if needed
        if (virtualPath.Length > 0 && virtualPath.Last() != '/')
            virtualPath += '/';

        return virtualFolders
            .Where(s => recursive
                ? s.StartsWith(virtualPath) && !s.Equals(virtualPath, StringComparison.OrdinalIgnoreCase)
                : IsDirectChild(virtualPath, s)) // non-recursive: only match folders directly in the folder
            .ToList();
    }

    /// <summary>
    /// Determines if a folder is a direct child of the given parent folder.
    /// </summary>
    private bool IsDirectChild(string parentPath, string childPath)
    {
        if (!childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase))
            return false;

        // Get the remaining part of the child path after the parent path
        return (childPath.Substring(parentPath.Length)).Count(c => c == '/') == 1;
    }

    /// <summary>
    /// Get list of files in a given folder (list of paths) with additional extension filtering.
    /// Extension string must be provided without a dot.
    /// </summary>
    public List<string> GetFilesInFolder(string virtualPath, string extension, bool recursive = false)
    {
        return GetFilesInFolder(virtualPath, recursive)
            .Where(s => s.EndsWith("." + extension, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

}
