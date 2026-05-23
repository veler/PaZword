using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaZword.Core.Services;

namespace PaZword.App.ViewModels;

/// <summary>
/// Sample view-model proving CommunityToolkit.Mvvm and constructor injection
/// work under WinUI 3 + NativeAOT.
///
/// AOT note (see docs/sprint-0/aot-notes.md): we use the manual
/// <see cref="ObservableObject.SetProperty"/> pattern instead of the
/// <c>[ObservableProperty]</c> source generator. The generator either requires
/// C# 13 partial-property syntax (CommunityToolkit.Mvvm raises MVVMTK0045 for
/// the field-based form under WinRT scenarios) or a newer toolkit release; until
/// we standardize on a configuration that emits AOT-clean code without analyzer
/// noise, manual <c>SetProperty</c> stays warning-free and trivially auditable.
/// </summary>
public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly DiagnosticsService _diagnostics;
    private string _status = "PaZword v2 — Sprint 0 spike";

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public MainPageViewModel(DiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics;
    }

    [RelayCommand]
    private void Ping()
    {
        _diagnostics.LogStartup();
        Status = $"Ping at {DateTimeOffset.Now:HH:mm:ss}";
    }
}
