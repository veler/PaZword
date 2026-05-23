namespace PaZword.Core.Navigation;

/// <summary>
/// How a navigation transition should animate. App-layer impl maps these to
/// WinUI <c>NavigationTransitionInfo</c> values. Living in Core keeps view-models
/// free of WinUI references and AOT-safe.
/// </summary>
public enum NavigationTransition
{
    /// <summary>Use the framework default (<c>EntranceNavigationTransitionInfo</c>).</summary>
    Default = 0,

    /// <summary>Drill-in (going deeper). Maps to <c>DrillInNavigationTransitionInfo</c>.</summary>
    DrillIn = 1,

    /// <summary>Entrance (peer / back navigation). Maps to <c>EntranceNavigationTransitionInfo</c>.</summary>
    Entrance = 2,

    /// <summary>Suppress the page transition entirely (<c>SuppressNavigationTransitionInfo</c>).</summary>
    Suppress = 3,
}

/// <summary>
/// Minimal navigation abstraction. Pages register by name; no WinUI types cross this boundary,
/// keeping <c>PaZword.Core</c> free of UI references and AOT-safe.
/// </summary>
/// <remarks>
/// View-models depend only on this interface. The concrete implementation lives in
/// <c>PaZword.App.Navigation.NavigationService</c> and wraps a WinUI <c>Frame</c>.
/// Every page type the app navigates to must be registered explicitly at startup via
/// <see cref="RegisterPage"/> — no reflection-based scanning (AOT discipline rule 4).
/// </remarks>
public interface INavigationService
{
    /// <summary>Registers a page type under a navigation key.</summary>
    /// <param name="pageKey">The stable navigation key (e.g. <c>"Welcome"</c>).</param>
    /// <param name="pageType">
    /// The page <see cref="System.Type"/> to instantiate via the registered <c>Frame</c>.
    /// Annotated with <see cref="System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"/>
    /// so the trimmer keeps the parameterless constructor the framework needs.
    /// </param>
    void RegisterPage(
        string pageKey,
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type pageType);

    /// <summary>Navigates to the page registered under <paramref name="pageKey"/>.</summary>
    bool NavigateTo(string pageKey, object? parameter = null, NavigationTransition transition = NavigationTransition.Default);

    /// <summary>Navigates back if there is a back-stack entry.</summary>
    bool GoBack();

    /// <summary>Clears the back-stack and (optionally) navigates to <paramref name="initialPageKey"/>.</summary>
    /// <param name="initialPageKey">Optional first page to navigate to after the reset. If <c>null</c>, the frame is left blank.</param>
    void Reset(string? initialPageKey = null, object? parameter = null);

    /// <summary>Returns <see langword="true"/> if there is at least one entry on the back-stack.</summary>
    bool CanGoBack { get; }
}

