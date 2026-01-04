using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SR3Generator.Avalonia.Services;
using System;

namespace SR3Generator.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _title = "SR3 Character Generator";

    [ObservableProperty]
    private CharacterShellViewModel _characterShell;

    public MainWindowViewModel(ICharacterBuilderService characterService, IServiceProvider serviceProvider)
    {
        _characterService = characterService;
        _serviceProvider = serviceProvider;
        _characterShell = _serviceProvider.GetRequiredService<CharacterShellViewModel>();
    }

    [RelayCommand]
    private void NewCharacter()
    {
        _characterService.NewCharacter();
        CharacterShell = _serviceProvider.GetRequiredService<CharacterShellViewModel>();
    }
}
