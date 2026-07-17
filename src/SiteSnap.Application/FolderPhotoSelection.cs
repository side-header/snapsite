namespace SiteSnap.Application;

public static class FolderPhotoSelection
{
    public static bool ShouldClearOnFolderToggle(bool isRule1Selection)
    {
        return !isRule1Selection;
    }

    public static List<string> Toggle(
        IReadOnlyList<string> currentSelection,
        IReadOnlyList<string> eligiblePhotos,
        string folderPath)
    {
        if (HasSelectionInFolder(currentSelection, folderPath))
        {
            return currentSelection
                .Where(path => !WorkspacePath.IsInFolder(path, folderPath))
                .ToList();
        }

        var result = currentSelection.ToList();
        var selected = currentSelection
            .Select(NormalizePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in eligiblePhotos.Where(path => WorkspacePath.IsInFolder(path, folderPath)))
        {
            if (selected.Add(NormalizePath(path)))
            {
                result.Add(path);
            }
        }

        return result;
    }

    public static bool HasSelectionInFolder(IEnumerable<string> selectedPhotos, string folderPath)
    {
        return selectedPhotos.Any(path => WorkspacePath.IsInFolder(path, folderPath));
    }

    private static string NormalizePath(string path)
    {
        return (path ?? string.Empty).Replace('\\', '/').Trim('/');
    }
}
