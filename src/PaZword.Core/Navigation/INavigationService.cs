namespace PaZword.Core.Navigation;

/// <summary>
/// Minimal navigation abstraction. Pages register by name; no WinUI types cross this boundary,
/// keeping PaZword.Core free of UI references and AOT-safe.
/// </summary>
public interface INavigationService
{
    /// <summary>Navigates to the page registered under <paramref name="pageKey"/>.</summary>
    bool NavigateTo(string pageKey, object? parameter = null);

    /// <summary>Navigates back if there is a back-stack entry.</summary>
    bool GoBack();

    /// <summary>Returns <see langword="true"/> if there is at least one entry on the back-stack.</summary>
    bool CanGoBack { get; }
}
