using CommunityToolkit.Mvvm.ComponentModel;
using SR3Generator.Avalonia.Services;
using System.Threading.Tasks;

namespace SR3Generator.Avalonia.ViewModels;

public partial class KarmaConversionDialogViewModel : ViewModelBase
{
    private readonly IUserSettingsService _settings;

    [ObservableProperty]
    private bool _enabled;

    /// <summary>Nuyen per 1 Karma. Editable as a number; persisted as a long. </summary>
    [ObservableProperty]
    private decimal _rate;

    public KarmaConversionDialogViewModel(IUserSettingsService settings)
    {
        _settings = settings;
        _enabled = settings.KarmaConversionEnabled;
        _rate = settings.KarmaConversionRate;
    }

    public Task SaveAsync() => _settings.SetKarmaConversionAsync(Enabled, (long)Rate);
}
