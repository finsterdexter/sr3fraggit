using SR3Generator.Data.Gear;
using SR3Generator.Data.Magic;
using static SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Data.Character
{
    public class Character
    {
        public string PlayerName { get; set; } = string.Empty;
        public int TotalKarma { get; set; }
        public int SpentKarma { get; set; }
        public int RemainingKarma { 
            get
            {
                return TotalKarma - SpentKarma;
            }
        }
        public List<KarmaOperation> KarmaOperations { get; set; } = new List<KarmaOperation>();
        public Race Race { get; set; } = null!;
        public Identity Identity { get; set; } = new Identity();
        public List<Lifestyle> Lifestyles { get; set; } = new List<Lifestyle>();
        public long Nuyen { get; set; }
        public Dictionary<AttributeName, Attribute> Attributes { get; set; }
        public Dictionary<DicePoolType, DicePool> DicePools { get; set; } = new Dictionary<DicePoolType, DicePool>();
        public Dictionary<string, Skill> ActiveSkills { get; set; } = new Dictionary<string, Skill>();
        public Dictionary<string, Skill> KnowledgeSkills { get; set; } = new Dictionary<string, Skill>();
        public Dictionary<Guid, Weapon> Weapons { get; set; } = new Dictionary<Guid, Weapon>();
        public Dictionary<Guid, Armor> ArmorClothing { get; set; } = new Dictionary<Guid, Armor>();
        public Dictionary<Guid, Equipment> Gear { get; set; } = new Dictionary<Guid, Equipment>();
        public Dictionary<string, Augmentation> NaturalAugmentations { get; set; } = new Dictionary<string, Augmentation>();

        // Cyberdecks/Programs

        // Magical Data
        //     Spells
        //     Adept Powers
        //     Bonded Spirits
        //         Watchers
        //         Ally Spirit
        public MagicAspect? MagicAspect { get; set; } = null;
        public Dictionary<string, Spell> Spells { get; set; } = new Dictionary<string, Spell>();
        public Dictionary<string, AdeptPower> AdeptPowers { get; set; } = new Dictionary<string, AdeptPower>();
        public Dictionary<Guid, BondedSpirit> BondedSpirits { get; set; } = new Dictionary<Guid, BondedSpirit>();
        public int WatcherSpirits { get; set; }
        public AllySpirit? AllySpirit { get; set; } = null;


        // Foci
        // Vehicles
        // Contacts
        public Dictionary<Guid, Contact> Contacts { get; set; } = new Dictionary<Guid, Contact>();

        public Character()
        {
            Attributes = new Dictionary<AttributeName, Attribute>
            {
                { AttributeName.Body, new Attribute { Name = AttributeName.Body, BaseValue = 1, Type = AttributeType.Physical } },
                { AttributeName.Quickness, new Attribute { Name = AttributeName.Quickness, BaseValue = 1, Type = AttributeType.Physical } },
                { AttributeName.Strength, new Attribute { Name = AttributeName.Strength, BaseValue = 1, Type = AttributeType.Physical } },

                { AttributeName.Charisma, new Attribute { Name = AttributeName.Charisma, BaseValue = 1, Type = AttributeType.Mental } },
                { AttributeName.Intelligence, new Attribute { Name = AttributeName.Intelligence, BaseValue = 1, Type = AttributeType.Mental } },
                { AttributeName.Willpower, new Attribute { Name = AttributeName.Willpower, BaseValue = 1, Type = AttributeType.Mental } },

                { AttributeName.Essence, new Attribute { Name = AttributeName.Essence, BaseValue = 6, Type = AttributeType.Special } },
                { AttributeName.Magic, new Attribute { Name = AttributeName.Magic, BaseValue = 0, Type = AttributeType.Special } },
                { AttributeName.Reaction, new Attribute { Name = AttributeName.Reaction, BaseValue = 1, Type = AttributeType.Combat } },
                { AttributeName.Initiative, new Attribute { Name = AttributeName.Initiative, BaseValue = 1, Type = AttributeType.Combat } },
            };

            DicePools = new Dictionary<DicePoolType, DicePool>()
            {
                { DicePoolType.Karma, new DicePool(DicePoolType.Karma) {Value = 1} },
                { DicePoolType.Combat, new DicePool(DicePoolType.Combat) {Value = 1} },
                { DicePoolType.Spell, new DicePool(DicePoolType.Spell) {Value = 0} },
                { DicePoolType.Hacking, new DicePool(DicePoolType.Hacking) {Value = 0} },
                { DicePoolType.Control, new DicePool(DicePoolType.Control) {Value = 1} },
                { DicePoolType.AstralCombat, new DicePool(DicePoolType.AstralCombat) {Value = 1} },
                { DicePoolType.Task, new DicePool(DicePoolType.Task) {Value = 0} },
            };
        }
    }
}