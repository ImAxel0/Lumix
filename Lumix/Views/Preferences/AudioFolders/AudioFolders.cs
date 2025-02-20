namespace Lumix.Views.Preferences.AudioFolders;

public class AudioFolders
{
    public static List<string> FoldersPath { get; set; } = new();
    public static string SelectedFolder { get; private set; } = string.Empty;

    public static void SelectFolder(string folderPath)
    {
        SelectedFolder = folderPath;
    }
}
