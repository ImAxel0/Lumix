namespace Lumix.Views.Preferences.AudioFolders;

public class AudioFolders
{
    public static List<string> FoldersPath { get; set; } = new()
    {
#if LOCAL_DEV
        "C:\\Users\\Alex\\Desktop\\Sample",
        "C:\\Users\\Alex\\Desktop\\Reference",
        "C:\\Users\\Alex\\Desktop\\Midi",
        "C:\\Users\\Alex\\Downloads"
#endif
    };

    public static string SelectedFolder { get; private set; } = string.Empty;

    public static void SelectFolder(string folderPath)
    {
        SelectedFolder = folderPath;
    }
}
