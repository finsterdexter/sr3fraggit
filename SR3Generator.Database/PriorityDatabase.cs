using SR3Generator.Data.Character.Creation;
using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Database
{
    public static class PriorityDatabase
    {
        public static List<Priority> CurrentPriorities { get; set; }

        static PriorityDatabase()
        {
            CurrentPriorities = new List<Priority>
            {
                new(PriorityType.Attributes, PriorityRank.A),
                new(PriorityType.Skills, PriorityRank.B),
                new(PriorityType.Resources, PriorityRank.C),
                new(PriorityType.Magic, PriorityRank.D),
                new(PriorityType.Race, PriorityRank.E)
            };

        }

        public static int GetAttributePoints(this Priority priority)
        {
            if (priority.Type != PriorityType.Attributes)
                throw new InvalidOperationException("Cannot get attribute points for non-attribute priority");
            switch (priority.Rank)
            {
                case PriorityRank.A:
                    return 30;
                case PriorityRank.B:
                    return 27;
                case PriorityRank.C:
                    return 24;
                case PriorityRank.D:
                    return 21;
                case PriorityRank.E:
                    return 18;
                default:
                    return 0;
            }
        }

        public static int GetSkillPoints(this Priority priority)
        {
            if (priority.Type != PriorityType.Skills)
                throw new InvalidOperationException("Cannot get skill points for non-skill priority");
            switch (priority.Rank)
            {
                case PriorityRank.A:
                    return 50;
                case PriorityRank.B:
                    return 40;
                case PriorityRank.C:
                    return 34;
                case PriorityRank.D:
                    return 30;
                case PriorityRank.E:
                    return 27;
                default:
                    return 0;
            }
        }

        public static int GetNuyen(this Priority priority)
        {
            if (priority.Type != PriorityType.Resources)
                throw new InvalidOperationException("Cannot get nuyen for non-resource priority");
            switch (priority.Rank)
            {
                case PriorityRank.A:
                    return 1000000;
                case PriorityRank.B:
                    return 400000;
                case PriorityRank.C:
                    return 90000;
                case PriorityRank.D:
                    return 20000;
                case PriorityRank.E:
                    return 5000;
                default:
                    return 0;
            }
        }

        public static List<Race> GetAllowedRaces(this Priority priority)
        {
            if (priority.Type != PriorityType.Race)
                throw new InvalidOperationException("Cannot get races for non-race priority");
            switch (priority.Rank)
            {
                case PriorityRank.A:
                case PriorityRank.B:
                case PriorityRank.C:
                    return new List<Race>()
                    { 
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Dwarf),
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Elf),
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human),
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Ork),
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Troll)
                    };
                case PriorityRank.D:
                    return new List<Race>()
                    {
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Dwarf),
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human),
                        RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Ork),
                    };
                case PriorityRank.E:
                default:
                    return new List<Race>() { RaceDatabase.PlayerRaces.First(r => r.Name == RaceName.Human) };
            }
        }

        public static List<MagicAspect> GetAllowedMagicAspects(this Priority priority)
        {
            if (priority.Type != PriorityType.Magic)
                throw new InvalidOperationException("Cannot get magic aspects for non-magic priority");
            switch (priority.Rank)
            {
                case PriorityRank.A:
                    return new List<MagicAspect>
                    {
                        MagicAspectDatabase.PlayerMagicAspects.First(m => m.Name == AspectName.FullMagician),
                    };
                case PriorityRank.B:
                    return new List<MagicAspect>
                    {
                        MagicAspectDatabase.PlayerMagicAspects.First(m => m.Name == AspectName.Shamanist),
                        MagicAspectDatabase.PlayerMagicAspects.First(m => m.Name == AspectName.Elementalist),
                        MagicAspectDatabase.PlayerMagicAspects.First(m => m.Name == AspectName.Sorcerer),
                        MagicAspectDatabase.PlayerMagicAspects.First(m => m.Name == AspectName.Conjurer),
                        MagicAspectDatabase.PlayerMagicAspects.First(m => m.Name == AspectName.PhysicalAdept)
                    };
                case PriorityRank.C:
                case PriorityRank.D:
                case PriorityRank.E:
                default:
                    return new List<MagicAspect>();
            }
        }

    }
}
