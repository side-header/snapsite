using NewGreen.Domain;

namespace NewGreen.Infrastructure.FileSystem;

public sealed class FileScanner
{
    private const int MaxFolderDepth = 7;

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".newgreen-cache",
        "exports"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff"
    };

    public ScanResult Scan(string rootDir)
    {
        var photos = new List<PhotoItem>();
        var folders = new List<FolderItem>();
        var otherFiles = new List<OtherFileItem>();

        foreach (var dir in EnumerateDirectories(rootDir, 1))
        {
            var rel = ToRelativePath(rootDir, dir);
            folders.Add(new FolderItem(rel, Path.GetFileName(dir)));
        }

        foreach (var file in EnumerateFiles(rootDir, 0))
        {
            var rel = ToRelativePath(rootDir, file);
            if (!IsImage(file))
            {
                otherFiles.Add(new OtherFileItem(rel, Path.GetFileName(file)));
                continue;
            }

            photos.Add(new PhotoItem(rel, Path.GetFileName(file)));
        }

        photos.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        folders.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        otherFiles.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));

        return new ScanResult { Photos = photos, Folders = folders, OtherFiles = otherFiles };
    }

    private static IEnumerable<string> EnumerateDirectories(string rootDir, int depth)
    {
        if (depth > MaxFolderDepth)
        {
            yield break;
        }

        foreach (var dir in Directory.EnumerateDirectories(rootDir))
        {
            if (ShouldIgnoreDirectory(dir))
            {
                continue;
            }

            yield return dir;

            foreach (var child in EnumerateDirectories(dir, depth + 1))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<string> EnumerateFiles(string rootDir, int depth)
    {
        foreach (var file in Directory.EnumerateFiles(rootDir))
        {
            yield return file;
        }

        if (depth >= MaxFolderDepth)
        {
            yield break;
        }

        foreach (var dir in Directory.EnumerateDirectories(rootDir))
        {
            if (ShouldIgnoreDirectory(dir))
            {
                continue;
            }

            foreach (var file in EnumerateFiles(dir, depth + 1))
            {
                yield return file;
            }
        }
    }

    public static bool IsIgnoredRelativePath(string relativePath)
    {
        var firstPart = AppState.NormalizePath(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstPart is not null && IgnoredDirectories.Contains(firstPart);
    }

    public static bool IsImage(string path)
    {
        return ImageExtensions.Contains(Path.GetExtension(path));
    }

    private static bool ShouldIgnoreDirectory(string path)
    {
        return IgnoredDirectories.Contains(Path.GetFileName(path));
    }

    public static string ToRelativePath(string rootDir, string path)
    {
        return AppState.NormalizePath(Path.GetRelativePath(rootDir, path));
    }

    public static string ToAbsolutePath(string rootDir, string relativePath)
    {
        return Path.Combine(rootDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
