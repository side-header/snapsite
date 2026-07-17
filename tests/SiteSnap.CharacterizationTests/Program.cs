using System.IO.Compression;
using SiteSnap.Domain;
using SiteSnap.Infrastructure.Export;
using SiteSnap.Infrastructure.Persistence;

namespace SiteSnap.CharacterizationTests;

internal static class Program
{
    public static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("new group defaults", NewGroupDefaults),
            ("whole cell movement", WholeCellMovement),
            ("metadata round trip", MetadataRoundTrip),
            ("document export smoke", DocumentExportSmoke)
        };

        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
                return 1;
            }
        }

        return 0;
    }

    private static void NewGroupDefaults()
    {
        var group = new AppState().AddGroup();
        Equal(3, group.Target.Count, "target cell count");
        Equal("전", group.Target[0].Label, "before label");
        Equal("중", group.Target[1].Label, "during label");
        Equal("후", group.Target[2].Label, "after label");
        Equal(0, group.Omit.Count, "omit cell count");
    }

    private static void WholeCellMovement()
    {
        var state = new AppState();
        var group = state.AddGroup();
        group.Target[0].Image = "before.jpg";
        group.Target[1].Image = "during.jpg";
        group.Target[2].Image = "after.jpg";

        True(state.MoveAssignedPhotoCellToInsertionIndex(
            group.Id, false, 2, group.Id, false, 0), "move result");
        Equal("during.jpg", group.Target[0].Image, "first image");
        Equal("before.jpg", group.Target[1].Image, "second image");
        Equal("after.jpg", group.Target[2].Image, "third image");
    }

    private static void MetadataRoundTrip()
    {
        WithTempDirectory(root =>
        {
            File.WriteAllBytes(Path.Combine(root, "before.jpg"), [0]);
            var state = new AppState { RootDir = root };
            var group = state.AddGroup();
            group.Title = "경계석 1구역";
            group.Target[0].Image = "before.jpg";
            var store = new MetadataStore();

            store.Save(root, state);
            var loaded = store.Load(root);

            Equal("경계석 1구역", loaded.Groups.Single().Title, "group title");
            Equal("before.jpg", loaded.Groups.Single().Target[0].Image, "photo path");
            True(File.Exists(MetadataStore.MetadataPath(root)), "manifest exists");
        });
    }

    private static void DocumentExportSmoke()
    {
        WithTempDirectory(root =>
        {
            var state = new AppState { RootDir = root };
            state.AddBlankPage();
            var exporter = new DocumentExporter();
            var hwpx = Path.Combine(root, "smoke.hwpx");
            var docx = Path.Combine(root, "smoke.docx");

            exporter.ExportHwpxTo(root, hwpx, state);
            exporter.ExportDocxTo(root, docx, state);

            True(File.Exists(hwpx), "HWPX exists");
            True(File.Exists(docx), "DOCX exists");
            using var hwpxArchive = ZipFile.OpenRead(hwpx);
            True(hwpxArchive.GetEntry("Contents/section0.xml") is not null, "HWPX section exists");
            using var docxArchive = ZipFile.OpenRead(docx);
            True(docxArchive.GetEntry("word/document.xml") is not null, "DOCX document exists");
        });
    }

    private static void WithTempDirectory(Action<string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "sitesnap-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            action(root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void True(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException(name);
        }
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{name}: expected {expected}, actual {actual}");
        }
    }
}
