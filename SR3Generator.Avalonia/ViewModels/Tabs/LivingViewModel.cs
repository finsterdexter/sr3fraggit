namespace SR3Generator.Avalonia.ViewModels.Tabs;

/// <summary>
/// Hosts the "Living" tab: Lifestyle and Contacts side-by-side. Composes the two sub-ViewModels
/// (mirrors the container ViewModels like GearContainerViewModel).
/// </summary>
public class LivingViewModel : ViewModelBase
{
    public LifestyleViewModel LifestyleVM { get; }
    public ContactsViewModel ContactsVM { get; }

    public LivingViewModel(LifestyleViewModel lifestyleVM, ContactsViewModel contactsVM)
    {
        LifestyleVM = lifestyleVM;
        ContactsVM = contactsVM;
    }
}
