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

        // first display all files
        Console.WriteLine("Entries in VFS: ");
        foreach (var item in vfs.Entries)
        {
            Console.WriteLine("> " + item);
        }
        Console.WriteLine();

        // and also folders
        Console.WriteLine("Folders in VFS: ");
        foreach (var item in vfs.Folders)
        {
            Console.WriteLine("> " + item);
        }
        Console.WriteLine();

        // iterate over all of the files and get their contents
        Console.WriteLine("Reading file contents: ");
        foreach (string path in vfs.Entries)
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
        Console.WriteLine();

        // count files
        Console.WriteLine("Files count in folder [] -> " + vfs.GetFilesInFolder("").Count);
        Console.WriteLine("Files count in folder [/] -> " + vfs.GetFilesInFolder("/").Count);
        Console.WriteLine("Files count in folder [folder] -> " + vfs.GetFilesInFolder("folder").Count);
        Console.WriteLine("Files count in folder [folder/] -> " + vfs.GetFilesInFolder("folder/").Count);
        Console.WriteLine("Files count in folder [folder2] -> " + vfs.GetFilesInFolder("folder2").Count);
        Console.WriteLine("Files count in folder [folder2/] -> " + vfs.GetFilesInFolder("folder2/").Count);
        Console.WriteLine("Files count in folder [] with extension [txt] -> " + vfs.GetFilesInFolder("", "txt").Count);
        Console.WriteLine();

        // check recursion
        Console.WriteLine("Get file list:");
        var list1 = vfs.GetFilesInFolder("",false);
        Console.WriteLine("Files in root no recursion: " + string.Join(", ", list1));
        var list2 = vfs.GetFilesInFolder("", true);
        Console.WriteLine("Files in root with recursion: " + string.Join(", ", list2));
        var list3 = vfs.GetFilesInFolder("folder/",false);
        Console.WriteLine("Files in \"folder\" folder no recursion: " + string.Join(", ", list3));
        var list4 = vfs.GetFilesInFolder("folder/", true);
        Console.WriteLine("Files in \"folder\" folder with recursion: " + string.Join(", ", list4));
        Console.WriteLine();

        // check recursion 2
        Console.WriteLine("Get folder list:");
        var list5 = vfs.GetFoldersInFolder("", false);
        Console.WriteLine("Folders in root no recursion: " + string.Join(", ", list5));
        var list6 = vfs.GetFoldersInFolder("", true);
        Console.WriteLine("Folders in root with recursion: " + string.Join(", ", list6));
        var list7 = vfs.GetFoldersInFolder("folder/", false);
        Console.WriteLine("Folders in \"folder\" no recursion: " + string.Join(", ", list7));
        var list8 = vfs.GetFoldersInFolder("folder/", true);
        Console.WriteLine("Folders in \"folder\" with recursion: " + string.Join(", ", list8));

        Console.ReadLine();
    }

}
