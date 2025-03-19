using System.Management.Automation;
using System.Management.Automation.Subsystem;

namespace PlaceholderPredictor;

/// <summary>
/// Register the predictor on module loading and unregister it on module un-loading.
/// </summary>
public class Init : IModuleAssemblyInitializer, IModuleAssemblyCleanup
{
    private const string BookmarkIdentifier = "2F841EC3-CA0A-4515-B969-F0704D746330";
    private const string SnippetIdentifier = "336D15EF-9315-46C7-BC0A-015C7748A22E";

    /// <summary>
    /// Gets called when assembly is loaded.
    /// </summary>
    public void OnImport()
    {
        var bmPredictor = new PlaceholderPredictor(
            BookmarkIdentifier, 
            "Bookmarks",
            "Predicts bm: bookmarks from ps-bookmarks.json in user profile",
            "ps-bookmarks.json",
            "bm",
            prependPrevious: true);

        var bookmarks = new FileInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ps-bookmarks.json"
        ));

        if (!bookmarks.Exists)
        {
            bookmarks.Create().Dispose();
        }

        var snipPredictor = new PlaceholderPredictor(
            BookmarkIdentifier,
            "Snippets",
            "Predicts snip: snippets from ps-snippets.json in user profile",
            "ps-snippets.json",
            "snip",
            prependPrevious: false);

        var snippets = new FileInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ps-snippets.json"
        ));

        if (!snippets.Exists)
        {
            snippets.Create().Dispose();
        }

        SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, bmPredictor);
        SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, snipPredictor);
    }

    /// <summary>
    /// Gets called when the binary module is unloaded.
    /// </summary>
    public void OnRemove(PSModuleInfo psModuleInfo)
    {
        SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, new Guid(BookmarkIdentifier));
        SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, new Guid(SnippetIdentifier));
    }
}