using System.IO.Compression;
using SiteSnap.Application;
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
            ("group insertion order", GroupInsertionOrder),
            ("whole cell movement", WholeCellMovement),
            ("photo boundary insertion", PhotoBoundaryInsertion),
            ("photo empty cell fill", PhotoEmptyCellFill),
            ("folder photo scope", FolderPhotoScope),
            ("folder rule1 selection toggle", FolderRule1SelectionToggle),
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

    private static void GroupInsertionOrder()
    {
        var state = new AppState();
        state.AddGroup().Title = "first";
        state.AddGroup().Title = "third";

        var insertionIndex = 1;
        state.InsertGroup(insertionIndex++).Title = "inserted-1";
        state.InsertGroup(insertionIndex).Title = "inserted-2";
        state.AddGroup().Title = "last";

        Equal("first", state.Groups[0].Title, "group before insertion point");
        Equal("inserted-1", state.Groups[1].Title, "first consecutively inserted group");
        Equal("inserted-2", state.Groups[2].Title, "second consecutively inserted group");
        Equal("third", state.Groups[3].Title, "existing group shifted after insertions");
        Equal("last", state.Groups[4].Title, "default append remains last");
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

    private static void PhotoBoundaryInsertion()
    {
        var state = new AppState();
        var group = state.AddGroup();
        group.Target[0].Image = "before.jpg";
        group.Target[1].Image = "during.jpg";
        group.Target[2].Image = "after.jpg";

        Equal(2, state.PlacePhotosAtInsertionIndex(
            group.Id,
            false,
            1,
            ["insert-1.jpg", "insert-2.jpg"]), "inserted photo count");
        Equal("before.jpg", group.Target[0].Image, "boundary first image");
        Equal("insert-1.jpg", group.Target[1].Image, "boundary first inserted image");
        Equal("insert-2.jpg", group.Target[2].Image, "boundary second inserted image");
        Equal("during.jpg", group.Target[3].Image, "boundary following image");

        Equal(1, state.PlacePhotosAtInsertionIndex(
            group.Id,
            false,
            0,
            ["first.jpg"]), "first boundary inserted photo count");
        Equal("first.jpg", group.Target[0].Image, "first boundary image");

        Equal(2, state.PlacePhotosAtInsertionIndex(
            group.Id,
            true,
            0,
            ["omit-1.jpg", "omit-2.jpg"]), "empty collection inserted photo count");
        Equal("omit-1.jpg", group.Omit[0].Image, "empty collection first image");
        Equal("omit-2.jpg", group.Omit[1].Image, "empty collection second image");
    }

    private static void PhotoEmptyCellFill()
    {
        var state = new AppState();
        var group = state.AddGroup();
        group.Target[0].Image = "before.jpg";

        Equal(2, state.PlacePhotosBesideCell(
            group.Id,
            false,
            1,
            ["during.jpg", "after.jpg"]), "filled photo count");
        Equal("during.jpg", group.Target[1].Image, "first empty cell image");
        Equal("after.jpg", group.Target[2].Image, "second empty cell image");
    }

    private static void FolderPhotoScope()
    {
        True(WorkspacePath.IsInFolder("잔디깎기/1전.jpg", "잔디깎기"), "direct folder photo");
        True(WorkspacePath.IsInFolder("잔디깎기/하위/2후.jpg", "잔디깎기"), "nested folder photo");
        True(WorkspacePath.IsInFolder("잔디깎기\\하위\\3중.jpg", "잔디깎기"), "normalized separator photo");
        True(WorkspacePath.IsInFolder("잔디깎기/1전.jpg", string.Empty), "root folder photo");
        True(!WorkspacePath.IsInFolder("잔디깎기-백업/1전.jpg", "잔디깎기"), "sibling prefix excluded");
        True(!WorkspacePath.IsInFolder("경계석/1전.jpg", "잔디깎기"), "sibling folder excluded");
        True(WorkspacePath.IsSameOrDescendantFolder("잔디깎기", "잔디깎기"), "same folder included");
        True(WorkspacePath.IsSameOrDescendantFolder("잔디깎기/하위/더하위", "잔디깎기"), "descendant folder included");
        True(!WorkspacePath.IsSameOrDescendantFolder("잔디깎기-백업", "잔디깎기"), "sibling folder prefix excluded");

        var selectedPhoto = new[] { "현장/구역A/1전.jpg" };
        True(FolderPhotoSelection.HasSelectionInFolder(selectedPhoto, "현장/구역A"),
            "selected photo highlights containing folder");
        True(FolderPhotoSelection.HasSelectionInFolder(selectedPhoto, "현장"),
            "selected photo highlights ancestor folder");
        True(FolderPhotoSelection.HasSelectionInFolder(selectedPhoto, string.Empty),
            "selected photo highlights root folder");
        True(!FolderPhotoSelection.HasSelectionInFolder(selectedPhoto, "현장/구역B"),
            "selected photo does not highlight sibling folder");
    }

    private static void FolderRule1SelectionToggle()
    {
        True(!FolderPhotoSelection.ShouldClearOnFolderToggle(isRule1Selection: true),
            "rule1 selection survives folder toggle");
        True(FolderPhotoSelection.ShouldClearOnFolderToggle(isRule1Selection: false),
            "manual selection clears on folder toggle");

        var eligible = new List<string>
        {
            "잔디깎기/1전.jpg",
            "잔디깎기/왼쪽/2전.jpg",
            "잔디깎기/오른쪽/3후.jpg",
            "경계석/4전.jpg"
        };

        var selected = FolderPhotoSelection.Toggle([], eligible, "잔디깎기");
        Equal(3, selected.Count, "parent selects descendants");

        selected = FolderPhotoSelection.Toggle(selected, eligible, "잔디깎기/왼쪽");
        Equal(2, selected.Count, "child deselection count");
        True(FolderPhotoSelection.HasSelectionInFolder(selected, "잔디깎기"), "parent remains selected");
        True(!FolderPhotoSelection.HasSelectionInFolder(selected, "잔디깎기/왼쪽"), "child becomes unselected");

        selected = FolderPhotoSelection.Toggle(selected, eligible, "잔디깎기/오른쪽");
        Equal(1, selected.Count, "second child deselection count");
        True(FolderPhotoSelection.HasSelectionInFolder(selected, "잔디깎기"), "direct parent photo remains");

        selected = FolderPhotoSelection.Toggle(selected, eligible, "잔디깎기");
        Equal(0, selected.Count, "parent deselects remaining scope");
        True(!FolderPhotoSelection.HasSelectionInFolder(selected, "잔디깎기"), "parent becomes unselected");

        selected = FolderPhotoSelection.Toggle(selected, eligible, "경계석");
        selected = FolderPhotoSelection.Toggle(selected, eligible, "잔디깎기/왼쪽");
        Equal(2, selected.Count, "sibling selections accumulate");
        True(FolderPhotoSelection.HasSelectionInFolder(selected, "경계석"), "first sibling retained");
        True(FolderPhotoSelection.HasSelectionInFolder(selected, "잔디깎기/왼쪽"), "second sibling added");
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
