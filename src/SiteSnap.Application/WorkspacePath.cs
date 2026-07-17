namespace SiteSnap.Application;

public static class WorkspacePath
{
    public static string ToAbsolutePath(string rootDir, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(rootDir, normalized);
    }
}
