using VirtualFileSystem;

namespace VFSTest;

class Program
{

    static void Main()
    {
        // Create VFS and include several mod folders
        var vfs = new VFSManager();
        vfs.AddRootContainer("Data/Mod1.pak"); // folder with the name "Mod1.pak"
        vfs.AddRootContainer("Data/Mod2.pak"); // folder with the name "Mod2.pak"
        vfs.AddRootContainer("Data/Mod3.pak"); // zip archive with the name "Mod3.pak"

        // Files we want to check
        string[] virtualPaths =
        {
            "file1.txt",
            "file2.txt",
            "folder/file3.txt",
            "file4.txt",
            "folder2/file5.txt",
        };

        // iterate over all of the files
        foreach (string path in virtualPaths)
        {
            // get file stream
            var stream = vfs.GetFileStream(path);

            // convert it to text reader
            TextReader textReader = new StreamReader(stream);

            // read all text
            var text = textReader.ReadToEnd();

            // display the text
            Console.WriteLine("Virtual path [" + path + "] -> \"" + text + "\"");
        }

        // also we can check to see if a particular file exists in the VFS
        var nonExistentFilePath = "non-existent-file.txt";
        if (vfs.FileExists(nonExistentFilePath))
        {
            Console.WriteLine("File [" + nonExistentFilePath + "] exists.");
        }
        else
        {
            Console.WriteLine("File [" + nonExistentFilePath + "] does not exist.");
        }

        Console.WriteLine("Files count in folder [] -> " + vfs.GetFilesInFolder("").Count);
        Console.WriteLine("Files count in folder [/] -> " + vfs.GetFilesInFolder("/").Count);
        Console.WriteLine("Files count in folder [folder] -> " + vfs.GetFilesInFolder("folder").Count);
        Console.WriteLine("Files count in folder [folder/] -> " + vfs.GetFilesInFolder("folder/").Count);
        Console.WriteLine("Files count in folder [folder2] -> " + vfs.GetFilesInFolder("folder2").Count);
        Console.WriteLine("Files count in folder [folder2/] -> " + vfs.GetFilesInFolder("folder2/").Count);

        // Output will be:
        //     Virtual path [file1.txt] -> "file1 in mod1"
        //     Virtual path [file2.txt] -> "file2 in mod2"
        //     Virtual path [folder/file3.txt] -> "file3 in mod2/folder"
        //     Virtual path [file4.txt] -> "file4 in the zipped mod3"
        //     Virtual path [folder2/file5.txt] -> "file5 in the zipped mod3/folder2"
        //     File [non-existent-file.txt] does not exist.
        //     Files count in folder[] -> 3
        //     Files count in folder[/] -> 3
        //     Files count in folder[folder] -> 1
        //     Files count in folder[folder/] -> 1
        //     Files count in folder[folder2] -> 1
        //     Files count in folder[folder2/] -> 1
    }

}
