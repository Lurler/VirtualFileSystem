using System.IO.Compression;
using System.Text.RegularExpressions;

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
    private readonly Dictionary<string, VirtualFile> virtualFiles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Stores all folders that exist in the VFS with at least one file.
    /// </summary>
    private readonly List<string> virtualFolders = new();

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
    private string FormatPath(string path)
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

                // create virtual file
                var virtualPath = FormatPath(entry.FullName);
                var virtualFile = new VirtualFile(zip, entry.FullName);

                // add it to the list of virtual files
                virtualFiles[virtualPath] = virtualFile;

                // register virtual folder if it isn't registered yet
                var virtualFolder = Path.GetDirectoryName(virtualPath) + '/';
                if (!string.IsNullOrEmpty(virtualFolder) && !virtualFolders.Contains(virtualFolder))
                    virtualFolders.Add(virtualFolder);
            }
        }
        catch
        {
            throw new ArgumentException("Incorrect container. The vfs container must be a folder or a zip archive.");
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
            var relativePath = FormatPath(file);

            // create virtual file and add into dictionary
            var vf = new VirtualFile(path + "/" + file);

            // add new if doesn't exists or replace
            if (virtualFiles.ContainsKey(relativePath))
                virtualFiles[relativePath] = vf;
            else
                virtualFiles.Add(relativePath, vf);

            // register virtual folder if it isn't registered yet
            var virtualFolder = Path.GetDirectoryName(relativePath) + '/';
            if (!string.IsNullOrEmpty(virtualFolder) && !virtualFolders.Contains(virtualFolder))
                virtualFolders.Add(virtualFolder);
        }

    }

    /// <summary>
    /// Checks if a file with a given virtual path exists in the VFS.
    /// </summary>
    public bool FileExists(string virtualPath)
    {
        return virtualFiles.ContainsKey(FormatPath(virtualPath));
    }

    /// <summary>
    /// Checks if a folder with a given virtual path exists in the VFS.
    /// </summary>
    public bool FolderExists(string virtualPath)
    {
        // add final slash if needed
        if (virtualPath.Last() != '/')
            virtualPath += '/';

        return virtualFolders.Contains(FormatPath(virtualPath));
    }

    /// <summary>
    /// Returns a stream to a file with a given virtual path.
    /// </summary>
    public Stream GetFileStream(string virtualPath)
    {
        return virtualFiles[FormatPath(virtualPath)].GetFileStream();
    }

    /// <summary>
    /// Read all data from the file and return it as an array of bytes.
    /// </summary>
    public byte[] GetFileContents(string virtualPath)
    {
        return virtualFiles[FormatPath(virtualPath)].GetData();
    }

    /// <summary>
    /// Get list of files in a given folder (list of paths).
    /// </summary>
    public List<string> GetFilesInFolder(string virtualPath)
    {
        // add final slash if needed
        if (virtualPath.Length == 0 || virtualPath.Last() != '/')
            virtualPath += '/';

        // check if we want files in root folder
        if (virtualPath == "/")
            return virtualFiles.Keys.Where(s => !s.Contains('/')).ToList();

        // return empty list if no such file path exists
        if (!virtualFolders.Contains(virtualPath))
            return new List<string>();

        // prepare regex
        var regexString = "^" + virtualPath.Replace("/", @"\/") + @"[^\/]*";
        var regex = new Regex(regexString, RegexOptions.IgnoreCase);

        // get all files that are inside the provided path
        return virtualFiles.Keys.Where(s => regex.IsMatch(s)).ToList();
    }

    /// <summary>
    /// Get list of all file entries in the VFS.
    /// </summary>
    public List<string> Entries => virtualFiles.Keys.ToList();

}
