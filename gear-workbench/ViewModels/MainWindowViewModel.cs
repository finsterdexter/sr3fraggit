namespace GearWorkbench.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public GearTabViewModel Gear { get; } = new();
}
