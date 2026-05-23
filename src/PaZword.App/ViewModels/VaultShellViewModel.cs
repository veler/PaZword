using CommunityToolkit.Mvvm.ComponentModel;
using PaZword.Core.Vault;

namespace PaZword.App.ViewModels;

/// <summary>
/// Backs <see cref="Pages.VaultShellPage"/>. Owns the inner-frame navigation service
/// and the active <see cref="VaultHandle"/> snapshot for the page header.
/// </summary>
public sealed partial class VaultShellViewModel : ObservableObject
{
    private string _vaultFileName = string.Empty;

    public string VaultFileName
    {
        get => _vaultFileName;
        set => SetProperty(ref _vaultFileName, value);
    }
}
