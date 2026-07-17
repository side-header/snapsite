namespace SiteSnap.Application;

public static class WorkspacePath
{
    public static string ToAbsolutePath(string rootDir, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(rootDir, normalized);
    }

    public static bool IsInFolder(string relativePath, string folderPath)
    {
        var normalizedPath = NormalizeRelativePath(relativePath);
        var normalizedFolder = NormalizeRelativePath(folderPath).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalizedFolder))
        {
            return !string.IsNullOrWhiteSpace(normalizedPath);
        }

        return normalizedPath.StartsWith(normalizedFolder + "/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSameOrDescendantFolder(string candidateFolderPath, string ancestorFolderPath)
    {
        var candidate = NormalizeRelativePath(candidateFolderPath).TrimEnd('/');
        var ancestor = NormalizeRelativePath(ancestorFolderPath).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(ancestor))
        {
            return true;
        }

        return string.Equals(candidate, ancestor, StringComparison.OrdinalIgnoreCase) ||
            candidate.StartsWith(ancestor + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeRelativePath(string path)
    {
        return (path ?? string.Empty)
            .Replace('\\', '/')
            .Trim('/');
    }
}
