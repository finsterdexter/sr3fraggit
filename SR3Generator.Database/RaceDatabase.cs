using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Database
{
    public static class RaceDatabase
    {
        public static List<Race> PlayerRaces { get; set; }

        static RaceDatabase()
        {
            PlayerRaces = new List<Race>
            {
                new Race {Name = RaceName.Human, AttributeMods = new List<AttributeMod>(), Extras = new List<string>()},
                new Race {Name = RaceName.Elf, 
                    AttributeMods = new List<AttributeMod>()
                    {
                        new(AttributeName.Quickness, 1),
                        new(AttributeName.Charisma, 2)
                    }, 
                    Extras = new List<string>()
                    {
                        "Low-light Vision"
                    }
                },
                new Race {Name = RaceName.Dwarf, 
                    AttributeMods = new List<AttributeMod>()
                    {
                        new(AttributeName.Body, 1),
                        new(AttributeName.Strength, 2),
                        new(AttributeName.Willpower, 1)
                    }, 
                    Extras = new List<string>()
                    {
                        "Thermographic Vision", 
                        "Resistance (+2 Body) to any disease or toxin"
                    }
                },
                new Race {Name = RaceName.Ork, 
                    AttributeMods = new List<AttributeMod>()
                    {
                        new(AttributeName.Body, 3),
                        new(AttributeName.Strength, 2),
                        new(AttributeName.Charisma, -1),
                        new(AttributeName.Intelligence, -1)
                    }, 
                    Extras = new List<string>()
                    {
                        "Low-light Vision",
                    }
                },
                new Race {Name = RaceName.Troll,
                    AttributeMods = new List<AttributeMod>()
                    {
                        new(AttributeName.Body, 5),
                        new(AttributeName.Quickness, -1),
                        new(AttributeName.Strength, 4),
                        new(AttributeName.Intelligence, -2),
                        new(AttributeName.Charisma, -2),
                    }, 
                    Extras = new List<string>()
                    {
                        "Thermographic Vision",
                        "+1 Reach for Armed/Unarmed Combat",
                        "Dermal Armor (+1 Body)"
                    }
                }
            };
        }
    }
}
