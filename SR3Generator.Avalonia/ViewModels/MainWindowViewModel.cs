using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;

namespace SR3Generator.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    [ObservableProperty]
    private string _title = "SR3 Character Generator";

    public MainWindowViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
    }

    [RelayCommand]
    private void NewCharacter()
    {
        _characterService.NewCharacter();
    }
}
