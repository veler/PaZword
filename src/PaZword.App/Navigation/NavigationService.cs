using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using PaZword.Core.Navigation;
using System.Diagnostics.CodeAnalysis;

namespace PaZword.App.Navigation;

/// <summary>
/// Default <see cref="INavigationService"/> implementation wrapping a WinUI
/// <see cref="Frame"/>. Page types are registered explicitly at app start
/// (or, for inner frames like the vault shell, when the host page loads).
/// </summary>
/// <remarks>
/// <para>
/// AOT discipline (see <c>docs/sprint-0/aot-notes.md</c>): every page type
/// registered here is taken via <see cref="DynamicallyAccessedMembersAttribute"/>
/// with <see cref="DynamicallyAccessedMemberTypes.PublicConstructors"/> so the
/// trimmer keeps the parameterless constructors that <see cref="Frame.Navigate(Type)"/>
/// needs to instantiate the page.
/// </para>
/// <para>
/// One instance is registered as a singleton for the root <see cref="Frame"/>
/// in <see cref="MainWindow"/>. Inner frames (e.g. the vault shell's content
/// frame) construct their own non-DI instance for that frame.
/// </para>
/// </remarks>
public sealed class NavigationService : INavigationService
{
    private readonly Dictionary<string, Type> _pages = new(StringComparer.Ordinal);
    private Frame? _frame;

    /// <summary>Attach this service to a <see cref="Frame"/>. Must be called before navigation.</summary>
    public void Initialize(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _frame = frame;
    }

    /// <inheritdoc/>
    public void RegisterPage(string pageKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type pageType)
    {
        ArgumentException.ThrowIfNullOrEmpty(pageKey);
        ArgumentNullException.ThrowIfNull(pageType);
        _pages[pageKey] = pageType;
    }

    /// <summary>Strongly-typed registration sugar used at startup.</summary>
    public void RegisterPage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPage>(string pageKey)
        where TPage : Page
        => RegisterPage(pageKey, typeof(TPage));

    /// <inheritdoc/>
    public bool NavigateTo(string pageKey, object? parameter = null, NavigationTransition transition = NavigationTransition.Default)
    {
        ArgumentException.ThrowIfNullOrEmpty(pageKey);
        var frame = _frame ?? throw new InvalidOperationException("NavigationService not initialized. Call Initialize(Frame) first.");
        if (!_pages.TryGetValue(pageKey, out var type))
            throw new InvalidOperationException($"Page key '{pageKey}' is not registered.");
        return frame.Navigate(type, parameter, MapTransition(transition));
    }

    /// <inheritdoc/>
    public bool GoBack()
    {
        var frame = _frame ?? throw new InvalidOperationException("NavigationService not initialized.");
        if (!frame.CanGoBack) return false;
        frame.GoBack();
        return true;
    }

    /// <inheritdoc/>
    public void Reset(string? initialPageKey = null, object? parameter = null)
    {
        var frame = _frame ?? throw new InvalidOperationException("NavigationService not initialized.");
        frame.BackStack.Clear();
        if (initialPageKey is not null)
        {
            NavigateTo(initialPageKey, parameter, NavigationTransition.Suppress);
            // Reset() should leave the user unable to go back to where they were.
            frame.BackStack.Clear();
        }
    }

    /// <inheritdoc/>
    public bool CanGoBack => _frame?.CanGoBack ?? false;

    private static NavigationTransitionInfo MapTransition(NavigationTransition transition) => transition switch
    {
        NavigationTransition.DrillIn => new DrillInNavigationTransitionInfo(),
        NavigationTransition.Entrance => new EntranceNavigationTransitionInfo(),
        NavigationTransition.Suppress => new SuppressNavigationTransitionInfo(),
        _ => new EntranceNavigationTransitionInfo(),
    };
}
