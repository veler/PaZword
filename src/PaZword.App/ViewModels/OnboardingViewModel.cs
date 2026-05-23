using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaZword.App.Navigation;
using PaZword.App.Services;
using PaZword.Core.Navigation;

namespace PaZword.App.ViewModels;

/// <summary>
/// Drives the 3-screen first-run flow locked by D-015:
///   0 → WelcomeIntro    "Welcome to PaZword"
///   1 → KdbxExplainer   "What is a KDBX vault?"
///   2 → ChooseAction    "Create / Open / Migrate"
/// </summary>
public sealed partial class OnboardingViewModel : ObservableObject
{
    private readonly INavigationService _nav;
    private int _currentIndex;

    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            if (SetProperty(ref _currentIndex, value))
            {
                OnPropertyChanged(nameof(IsFirstScreen));
                OnPropertyChanged(nameof(IsFinalScreen));
                OnPropertyChanged(nameof(IsIntroVisible));
                OnPropertyChanged(nameof(IsExplainerVisible));
                OnPropertyChanged(nameof(IsChooseActionVisible));
                OnPropertyChanged(nameof(Pip0Filled));
                OnPropertyChanged(nameof(Pip1Filled));
                OnPropertyChanged(nameof(Pip2Filled));
                NextCommand.NotifyCanExecuteChanged();
                BackCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsFirstScreen => CurrentIndex == 0;
    public bool IsFinalScreen => CurrentIndex == 2;

    public bool IsIntroVisible => CurrentIndex == 0;
    public bool IsExplainerVisible => CurrentIndex == 1;
    public bool IsChooseActionVisible => CurrentIndex == 2;

    public bool Pip0Filled => CurrentIndex >= 0;
    public bool Pip1Filled => CurrentIndex >= 1;
    public bool Pip2Filled => CurrentIndex >= 2;

    public OnboardingViewModel(INavigationService nav)
    {
        _nav = nav;
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => CurrentIndex++;
    private bool CanGoNext() => CurrentIndex < 2;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back() => CurrentIndex--;
    private bool CanGoBack() => CurrentIndex > 0;

    [RelayCommand]
    private void Skip()
    {
        AppSettings.OnboardingCompleted = true;
        _nav.Reset(PageKeys.Welcome);
    }

    [RelayCommand]
    private void FinishToWelcome()
    {
        AppSettings.OnboardingCompleted = true;
        _nav.Reset(PageKeys.Welcome);
    }

    [RelayCommand]
    private void FinishToMigration()
    {
        AppSettings.OnboardingCompleted = true;
        _nav.Reset(PageKeys.MigrationPlaceholder);
    }
}
