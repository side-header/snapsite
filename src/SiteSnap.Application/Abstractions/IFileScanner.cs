namespace SiteSnap.Application.Abstractions;

public interface IFileScanner
{
    ScanResult Scan(string rootDir);
}
