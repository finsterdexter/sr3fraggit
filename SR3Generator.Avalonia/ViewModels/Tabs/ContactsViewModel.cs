using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SR3Generator.Avalonia.Services;
using SR3Generator.Data.Character;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SR3Generator.Avalonia.ViewModels.Tabs;

public partial class ContactsViewModel : ViewModelBase
{
    private readonly ICharacterBuilderService _characterService;

    [ObservableProperty]
    private ObservableCollection<ContactItem> _contacts = new();

    [ObservableProperty]
    private ContactItem? _selectedContact;

    [ObservableProperty]
    private string _newContactName = string.Empty;

    [ObservableProperty]
    private ContactLevel _newContactLevel = ContactLevel.Contact;

    [ObservableProperty]
    private int _freeContactsUsed;

    [ObservableProperty]
    private int _freeContactsAllowed = 2;

    [ObservableProperty]
    private long _nuyenSpentOnContacts;

    public ObservableCollection<ContactLevel> AvailableLevels { get; } = new(
        Enum.GetValues<ContactLevel>()
    );

    public ContactsViewModel(ICharacterBuilderService characterService)
    {
        _characterService = characterService;
        _characterService.CharacterChanged += OnCharacterChanged;
        RefreshFromBuilder();
    }

    private void OnCharacterChanged(object? sender, EventArgs e)
    {
        RefreshFromBuilder();
    }

    private void RefreshFromBuilder()
    {
        var character = _characterService.Builder.Character;

        Contacts.Clear();
        FreeContactsUsed = 0;
        NuyenSpentOnContacts = 0;

        foreach (var contact in character.Contacts.Values)
        {
            var item = new ContactItem(contact);
            Contacts.Add(item);

            // First 2 Level 1 contacts are free
            if (contact.Level == ContactLevel.Contact && FreeContactsUsed < FreeContactsAllowed)
            {
                item.IsFree = true;
                FreeContactsUsed++;
            }
            else
            {
                item.IsFree = false;
                NuyenSpentOnContacts += GetContactCost(contact.Level);
            }
        }
    }

    private long GetContactCost(ContactLevel level)
    {
        return level switch
        {
            ContactLevel.Contact => 5000,
            ContactLevel.Buddy => 10000,
            ContactLevel.FriendForLife => 200000,
            _ => 0
        };
    }

    [RelayCommand]
    private void AddFreeContact()
    {
        if (string.IsNullOrWhiteSpace(NewContactName)) return;
        if (FreeContactsUsed >= FreeContactsAllowed) return;

        var contact = new Contact
        {
            Name = NewContactName,
            Level = ContactLevel.Contact
        };

        _characterService.AddContact(contact);
        NewContactName = string.Empty;
    }

    [RelayCommand]
    private void BuyContact()
    {
        if (string.IsNullOrWhiteSpace(NewContactName)) return;

        var contact = new Contact
        {
            Name = NewContactName,
            Level = NewContactLevel
        };

        _characterService.BuyContact(contact);
        NewContactName = string.Empty;
        NewContactLevel = ContactLevel.Contact;
    }

    [RelayCommand]
    private void RemoveContact()
    {
        if (SelectedContact == null) return;

        var character = _characterService.Builder.Character;
        var contactEntry = character.Contacts.FirstOrDefault(c => c.Value.Name == SelectedContact.Name);
        if (contactEntry.Key != Guid.Empty)
        {
            _characterService.RemoveContact(contactEntry.Key);
        }
    }
}

public partial class ContactItem : ObservableObject
{
    public string Name { get; }
    public ContactLevel Level { get; }
    public string LevelDisplay => Level switch
    {
        ContactLevel.Contact => "Contact (Lvl 1)",
        ContactLevel.Buddy => "Buddy (Lvl 2)",
        ContactLevel.FriendForLife => "Friend for Life (Lvl 3)",
        _ => Level.ToString()
    };
    public string CostDisplay => IsFree ? "Free" : Level switch
    {
        ContactLevel.Contact => "5,000¥",
        ContactLevel.Buddy => "10,000¥",
        ContactLevel.FriendForLife => "200,000¥",
        _ => ""
    };

    [ObservableProperty]
    private bool _isFree;

    public ContactItem(Contact contact)
    {
        Name = contact.Name;
        Level = contact.Level;
    }
}
