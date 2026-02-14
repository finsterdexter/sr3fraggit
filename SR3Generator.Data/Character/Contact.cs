using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Data.Character
{
    public class Contact
    {
        public required string Name { get; set; }
        public ContactLevel Level { get; set; }
        public Dictionary<Guid, Contact> FriendsOfAFriend { get; set; } = new Dictionary<Guid, Contact>();

    }

    public enum ContactLevel
    {
        Contact = 1,
        Buddy = 2,
        FriendForLife = 3,
    }
}
