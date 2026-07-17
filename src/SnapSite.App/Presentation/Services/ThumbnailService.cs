using Avalonia.Media.Imaging;
using SiteSnap.Application;
using System.Security.Cryptography;
using System.Text;

namespace SiteSnap.Presentation.Services;

public sealed class ThumbnailService
{
    private const int ThumbnailWidth = 180;
    private static readonly string ThumbnailCacheRoot = Path.Combine(AppCacheRoot(), "thumbnails");
    private readonly Dictionary<string, Bitmap?> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> loading = new(StringComparer.OrdinalIgnoreCase);

    public string CacheRoot => ThumbnailCacheRoot;

    public Bitmap? TryGetCached(string absolutePath)
    {
        return cache.TryGetValue(absolutePath, out var bitmap) ? bitmap : null;
    }

    public async Task<Bitmap?> LoadAsync(string rootDir, string relativePath)
    {
        var absolutePath = WorkspacePath.ToAbsolutePath(rootDir, relativePath);
        if (cache.TryGetValue(absolutePath, out var existing))
        {
            return existing;
        }

        if (!File.Exists(absolutePath))
        {
            cache[absolutePath] = null;
            return null;
        }

        var cachePath = CachePath(rootDir, relativePath, absolutePath);
        if (File.Exists(cachePath))
        {
            await using var cachedStream = File.OpenRead(cachePath);
            var cachedBitmap = new Bitmap(cachedStream);
            cache[absolutePath] = cachedBitmap;
            return cachedBitmap;
        }

        if (!loading.Add(absolutePath))
        {
            return null;
        }

        try
        {
            var bitmap = await Task.Run(() =>
            {
                using var stream = File.OpenRead(absolutePath);
                return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
            });

            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            await using (var output = File.Create(cachePath))
            {
                bitmap.Save(output);
            }

            cache[absolutePath] = bitmap;
            return bitmap;
        }
        catch
        {
            cache[absolutePath] = null;
            return null;
        }
        finally
        {
            loading.Remove(absolutePath);
        }
    }

    public void ClearCache()
    {
        cache.Clear();
        loading.Clear();

        if (Directory.Exists(ThumbnailCacheRoot))
        {
            Directory.Delete(ThumbnailCacheRoot, recursive: true);
        }
    }

    private static string CachePath(string rootDir, string relativePath, string absolutePath)
    {
        var stamp = File.GetLastWriteTimeUtc(absolutePath).Ticks;
        var size = new FileInfo(absolutePath).Length;
        var key = Hash($"{Path.GetFullPath(absolutePath)}|{stamp}|{size}");
        var safeName = string.Concat(Path.GetFileNameWithoutExtension(relativePath).Take(32).Select(ch =>
            char.IsLetterOrDigit(ch) ? ch : '_'));
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "photo";
        }
        return Path.Combine(ThumbnailCacheRoot, $"{safeName}-{key}.png");
    }

    private static string AppCacheRoot()
    {
        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Caches", "SiteSnap");
        }

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(local, "SiteSnap", "Cache");
        }

        var xdg = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrWhiteSpace(xdg))
        {
            return Path.Combine(xdg, "SiteSnap");
        }

        var fallback = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(fallback)
            ? Path.Combine(Path.GetTempPath(), "SiteSnap", "Cache")
            : Path.Combine(fallback, "SiteSnap", "Cache");
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}
