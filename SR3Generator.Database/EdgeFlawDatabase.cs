namespace SR3Generator.Database;

using SR3Generator.Data.Character;

public static class EdgeFlawDatabase
{
    public static List<EdgeFlaw> AllEdgesFlaws { get; set; }

    static EdgeFlawDatabase()
    {
        AllEdgesFlaws = new List<EdgeFlaw>();
        AllEdgesFlaws.AddRange(GetAttributeEdges());
        AllEdgesFlaws.AddRange(GetSkillEdgesFlaws());
        AllEdgesFlaws.AddRange(GetPhysicalEdgesFlaws());
        AllEdgesFlaws.AddRange(GetMentalEdgesFlaws());
        AllEdgesFlaws.AddRange(GetSocialEdgesFlaws());
        AllEdgesFlaws.AddRange(GetMagicalEdgesFlaws());
        AllEdgesFlaws.AddRange(GetMatrixEdgesFlaws());
        AllEdgesFlaws.AddRange(GetMiscellaneousEdgesFlaws());
    }

    private static IEnumerable<EdgeFlaw> GetAttributeEdges()
    {
        yield return new EdgeFlaw
        {
            Name = "Bonus Attribute Point",
            Description = "Gain 1 bonus Attribute Point that can be added to any Mental or Physical Attribute. Can raise above the Racial Modified Limit. Only one Attribute per character may be increased.",
            PointValue = 2,
            Category = EdgeFlawCategory.Attribute,
            Book = "src",
            Page = 17
        };
        yield return new EdgeFlaw
        {
            Name = "Exceptional Attribute",
            Description = "Increase the Racial Modified Limit for one Attribute by 1. This also raises the Attribute Maximum.",
            PointValue = 2,
            Category = EdgeFlawCategory.Attribute,
            Book = "src",
            Page = 17
        };
    }

    private static IEnumerable<EdgeFlaw> GetSkillEdgesFlaws()
    {
        yield return new EdgeFlaw
        {
            Name = "Aptitude",
            Description = "Gain a -1 target modifier for all tests made with a specific skill. Cannot be used when defaulting to that skill.",
            PointValue = 4,
            Category = EdgeFlawCategory.Skill,
            Book = "src",
            Page = 17
        };
        yield return new EdgeFlaw
        {
            Name = "Home Ground",
            Description = "Gain a -1 target modifier for all Active Skill Tests in a specific familiar location. Knowledge Skills apply a -2 modifier.",
            PointValue = 2,
            Category = EdgeFlawCategory.Skill,
            Book = "src",
            Page = 18
        };
        yield return new EdgeFlaw
        {
            Name = "Computer Illiterate",
            Description = "Extreme difficulty working with computers and electronics. +1 modifier to all tests involving computers. May need Success Tests for simple tasks.",
            PointValue = -3,
            Category = EdgeFlawCategory.Skill,
            Book = "src",
            Page = 17
        };
        yield return new EdgeFlaw
        {
            Name = "Incompetence",
            Description = "Receive a +1 target modifier to all tests made with a specific skill.",
            PointValue = -2,
            Category = EdgeFlawCategory.Skill,
            Book = "src",
            Page = 17
        };
    }

    private static IEnumerable<EdgeFlaw> GetPhysicalEdgesFlaws()
    {
        yield return new EdgeFlaw
        {
            Name = "Adrenaline Surge",
            Description = "Use Rule of Six for Initiative in combat, but receive +1 target modifier on Combat and Perception Tests. Cannot be combined with cyberware, bioware, or magic Initiative enhancement.",
            PointValue = 2,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 18
        };
        yield return new EdgeFlaw
        {
            Name = "Double Jointed",
            Description = "Unusually flexible joints. -1 target modifier for Athletics (Escape Artist) Tests. Can squeeze into small spaces.",
            PointValue = 1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20
        };
        yield return new EdgeFlaw
        {
            Name = "High Pain Tolerance",
            Description = "Resist the effects of one box of physical or mental damage per 2 points. Uses same rules as the adept power Pain Resistance.",
            PointValue = 2,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Lightning Reflexes",
            Description = "+1 Reaction for Surprise Tests only (not Initiative). Cumulative with other Reaction bonuses in Surprise Tests. Max +3 Reaction.",
            PointValue = 2,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Natural Immunity (Natural)",
            Description = "Immunity to a single natural disease or toxin. Cannot affect the character.",
            PointValue = 1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20
        };
        yield return new EdgeFlaw
        {
            Name = "Natural Immunity (Man-made)",
            Description = "Immunity to a single man-made drug or poison, including biowarfare toxins. One safe dose every (30 - Body) / 2 hours.",
            PointValue = 3,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20
        };
        yield return new EdgeFlaw
        {
            Name = "Night Vision",
            Description = "Human characters gain low-light vision. Does not apply when rigging or in the Matrix.",
            PointValue = 2,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20,
            Restrictions = "1 point for riggers or deckers"
        };
        yield return new EdgeFlaw
        {
            Name = "Quick Healer",
            Description = "Heal faster than normal characters.",
            PointValue = 2,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Resistance to Pathogens",
            Description = "Increased resistance to diseases.",
            PointValue = 1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Resistance to Toxins",
            Description = "Increased resistance to toxins and poisons.",
            PointValue = 1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Toughness",
            Description = "Especially durable. +1 Body for resisting physical damage.",
            PointValue = 3,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Water Sprite",
            Description = "Can hold breath for extended periods and swim with great skill.",
            PointValue = 1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Will to Live",
            Description = "Harder to kill. +1 to Deadly wound boxes per point.",
            PointValue = 1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31,
            IsLeveled = true,
            Level = 1
        };

        // Physical Flaws
        yield return new EdgeFlaw
        {
            Name = "Allergy (Uncommon, Mild)",
            Description = "Allergic to a rare substance. +1 target modifier when exposed.",
            PointValue = -2,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Allergy (Uncommon, Moderate)",
            Description = "Allergic to a rare substance. Intense pain; add 2 to Power of weapons made from the substance.",
            PointValue = -3,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Allergy (Uncommon, Severe)",
            Description = "Allergic to a rare substance. Light wound every minute of exposure. Add 2 to Power of weapons made from the substance.",
            PointValue = -4,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Allergy (Common, Mild)",
            Description = "Allergic to a common substance. +1 target modifier when exposed.",
            PointValue = -3,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Allergy (Common, Moderate)",
            Description = "Allergic to a common substance. Intense pain; add 2 to Power of weapons made from the substance.",
            PointValue = -4,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Allergy (Common, Severe)",
            Description = "Allergic to a common substance. Light wound every minute of exposure. Add 2 to Power of weapons made from the substance.",
            PointValue = -5,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Bio-Rejection",
            Description = "Immune system rejects all cyberware implants. Must use cloned tissue. Cannot take Sensitive System.",
            PointValue = -5,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            Restrictions = "-2 points for magically active characters"
        };
        yield return new EdgeFlaw
        {
            Name = "Blind",
            Description = "Cannot see. +6 target modifier for visual tests. Cyber-replacement eyes do not correct this. Cannot take Color Blind or Night Blindness.",
            PointValue = -6,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19,
            Restrictions = "-2 points for magically active characters (can use astral perception)"
        };
        yield return new EdgeFlaw
        {
            Name = "Borrowed Time",
            Description = "Character has a fatal condition and will die in a secret number of months (GM rolls 3D6).",
            PointValue = -6,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19
        };
        yield return new EdgeFlaw
        {
            Name = "Color Blind",
            Description = "Sees only in black, white, and gray. +4 target modifier when distinguishing colors is important. Cannot take Blind.",
            PointValue = -1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 19
        };
        yield return new EdgeFlaw
        {
            Name = "Deaf",
            Description = "Cannot hear. +4 target modifier when hearing is a factor. Cannot be cured with cyberware.",
            PointValue = -3,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20,
            Restrictions = "-1 point if does not affect rigging/Matrix actions"
        };
        yield return new EdgeFlaw
        {
            Name = "Infirm",
            Description = "Deteriorating physical fitness. Reduce Attribute Maximum of Physical Attributes by 1 per point.",
            PointValue = -1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Low Pain Tolerance",
            Description = "Particularly sensitive to pain. Wound modifiers are increased by one level.",
            PointValue = -4,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20
        };
        yield return new EdgeFlaw
        {
            Name = "Night Blindness",
            Description = "Effectively blind in darkness. Additional +2 target modifier in Full Darkness or Minimal Light. Cannot take Blind.",
            PointValue = -2,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20,
            Restrictions = "-1 point for riggers or deckers"
        };
        yield return new EdgeFlaw
        {
            Name = "Paraplegic",
            Description = "Paralyzed from the waist down. Can use wheelchair. Move at 1/4 normal walking rate without powered chair.",
            PointValue = -3,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 20
        };
        yield return new EdgeFlaw
        {
            Name = "Quadriplegic",
            Description = "Paralyzed in all four limbs. Requires assistance for most physical tasks. Move at 1/10 normal rate.",
            PointValue = -6,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Sensitive System",
            Description = "Essence Cost of all cyberware is doubled. Cannot take Bio-Rejection.",
            PointValue = -3,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31,
            Restrictions = "-2 points for magically active characters"
        };
        yield return new EdgeFlaw
        {
            Name = "Weak Immune System",
            Description = "More susceptible to diseases and infections.",
            PointValue = -1,
            Category = EdgeFlawCategory.Physical,
            Book = "src",
            Page = 31
        };
    }

    private static IEnumerable<EdgeFlaw> GetMentalEdgesFlaws()
    {
        yield return new EdgeFlaw
        {
            Name = "Bravery",
            Description = "Less susceptible to fear and intimidation. -1 target modifier for resisting fear or intimidation.",
            PointValue = 1,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "College Education",
            Description = "Attended university. Reduces defaulting modifiers for Academic Knowledge Skills.",
            PointValue = 1,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Common Sense",
            Description = "GM can give hints when the player is about to do something stupid.",
            PointValue = 2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Perceptive",
            Description = "Likely to notice small details. -1 target modifier on all Perception Tests including Astral Perception.",
            PointValue = 3,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 22
        };
        yield return new EdgeFlaw
        {
            Name = "Perfect Time",
            Description = "Always knows the current time to the minute. Can be thrown off by unconsciousness or drugs, but recovers quickly.",
            PointValue = 1,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 22
        };
        yield return new EdgeFlaw
        {
            Name = "Photographic Memory",
            Description = "Never forgets anything experienced. Can perfectly recall faces, dates, numbers. GM must provide correct info if player forgets.",
            PointValue = 3,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 23
        };
        yield return new EdgeFlaw
        {
            Name = "Sense of Direction",
            Description = "Never gets lost. Always knows true north and can retrace steps. Does not help if unconscious or transported while unable to sense.",
            PointValue = 1,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24
        };
        yield return new EdgeFlaw
        {
            Name = "Spike Resistance",
            Description = "Increased resistance to simsense spikes and harmful ASIST. +1 effective Willpower per 2 points. Only for riggers or deckers.",
            PointValue = 2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24,
            IsLeveled = true,
            Level = 1,
            Restrictions = "Only riggers or deckers"
        };
        yield return new EdgeFlaw
        {
            Name = "Technical School Education",
            Description = "Attended tech school. Reduces defaulting modifiers for Background Knowledge Skills. Stacks with College Education.",
            PointValue = 1,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24
        };

        // Mental Flaws
        yield return new EdgeFlaw
        {
            Name = "Amnesia",
            Description = "Character has lost some or all memories. GM controls what the character remembers.",
            PointValue = -3,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Combat Monster",
            Description = "Loves combat too much. Must make Willpower (6) Test to break off from a fight or retreat.",
            PointValue = -1,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Combat Paralysis",
            Description = "Freezes up in combat. Must make Willpower (6) Test or lose first Initiative Pass. If wounded, must test again or freeze for entire Combat Turn.",
            PointValue = -4,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Compulsive",
            Description = "Character has a behavior they cannot control (gambling, lying, kleptomania, etc.). GM determines exact value.",
            PointValue = -1,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Flashbacks",
            Description = "Experiences traumatic flashbacks triggered by specific stimuli. GM determines trigger and effects.",
            PointValue = -4,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Impulsive",
            Description = "Acts without thinking. Must make Willpower (6) Test to resist immediate urges.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Illiterate",
            Description = "Cannot read or write. Cannot use any skill requiring reading. Cannot take any Education Edge.",
            PointValue = -3,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 22
        };
        yield return new EdgeFlaw
        {
            Name = "Oblivious",
            Description = "Often fails to notice things. +1 target modifier on all Perception Tests including Astral Perception.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 22
        };
        yield return new EdgeFlaw
        {
            Name = "Pacifist",
            Description = "Cannot take a life except in self-defense. Cannot participate in premeditated murder. Cannot take Total Pacifist.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 22
        };
        yield return new EdgeFlaw
        {
            Name = "Phobia (Uncommon, Mild)",
            Description = "Fear of a rare trigger. +1 target modifier while experiencing reaction.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 23,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Phobia (Uncommon, Moderate)",
            Description = "Fear of a rare trigger. +2 target modifier. Must make Willpower (4) Test to confront the trigger directly.",
            PointValue = -3,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 23,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Phobia (Uncommon, Severe)",
            Description = "Fear of a rare trigger. Collapses or runs away unless Willpower (6) Test succeeds. If test succeeds, +2 target modifier.",
            PointValue = -4,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 23,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Phobia (Common, Mild)",
            Description = "Fear of a common trigger. +1 target modifier while experiencing reaction.",
            PointValue = -3,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 23,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Phobia (Common, Moderate)",
            Description = "Fear of a common trigger. +2 target modifier. Must make Willpower (4) Test to confront the trigger directly.",
            PointValue = -4,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 23,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Phobia (Common, Severe)",
            Description = "Fear of a common trigger. Collapses or runs away unless Willpower (6) Test succeeds. If test succeeds, +2 target modifier.",
            PointValue = -5,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 23,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Sea Legs",
            Description = "After 24 hours on land without a boat, must make Willpower (4) Test or seek a ship. +1 TN per failure. For sea-based characters only.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24
        };
        yield return new EdgeFlaw
        {
            Name = "Sea Madness",
            Description = "After 24 hours at sea with no land in sight, must make Willpower (4) Test or slowly go mad. May sabotage companions. For sea-based characters only.",
            PointValue = -4,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24
        };
        yield return new EdgeFlaw
        {
            Name = "Sensitive Neural Structure",
            Description = "More vulnerable to neural damage from BTLs, black IC, dump shock. Reduce effective Willpower by 1 (-2) or 2 (-4).",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Simsense Vertigo",
            Description = "Disorientation when using simsense tech. +1 target modifier and -1 Initiative. -4 points for riggers or deckers.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24,
            Restrictions = "-4 points for riggers or deckers"
        };
        yield return new EdgeFlaw
        {
            Name = "Total Pacifist",
            Description = "Cannot kill any living creature more intelligent than an insect. Suffers 2D6 weeks of depression if they do. Cannot take Pacifist.",
            PointValue = -5,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24
        };
        yield return new EdgeFlaw
        {
            Name = "Uneducated",
            Description = "Only rudimentary knowledge. Intelligence x 3 Knowledge Skills. Cannot start with Academic or Background Knowledge Skills. Only one language.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24
        };
        yield return new EdgeFlaw
        {
            Name = "Vindictive",
            Description = "Especially vengeful. Must correct any slight, no matter how small. Carries grudges until avenged.",
            PointValue = -2,
            Category = EdgeFlawCategory.Mental,
            Book = "src",
            Page = 24
        };
    }

    private static IEnumerable<EdgeFlaw> GetSocialEdgesFlaws()
    {
        yield return new EdgeFlaw
        {
            Name = "Animal Empathy",
            Description = "Instinctive feel for handling animals. -1 target modifier for influencing or controlling animals. Reluctant to harm animals.",
            PointValue = 2,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 25
        };
        yield return new EdgeFlaw
        {
            Name = "Blandness",
            Description = "Blends into any crowd. +1 target modifier for anyone trying to track or locate the character physically. Not affected by magical or Matrix searches.",
            PointValue = 2,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 25
        };
        yield return new EdgeFlaw
        {
            Name = "Connected",
            Description = "Has a contact who buys or sells contraband at best prices. 3 points = one-way transaction. 5 points = two-way.",
            PointValue = 3,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 25,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Extra Contact",
            Description = "Receive 1 additional Level 1 contact during character creation.",
            PointValue = 1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Friendly Face",
            Description = "Easily fits into new situations. -1 to Social Skill Tests when infiltrating groups or meeting contacts in new cities.",
            PointValue = 1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Friends Abroad",
            Description = "Starts with an extra contact in a foreign land. Can make new foreign contacts during play.",
            PointValue = 3,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Friends in High Places",
            Description = "Has an influential Level 2 contact (corporate VP, government official). Will not risk their position.",
            PointValue = 2,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Good Looking and Knows It",
            Description = "Stunning looks provide social advantages. -2 Social/Etiquette TN vs opposite sex, -1 vs same sex. Friendly initial attitude from opposite sex.",
            PointValue = 1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Human-Looking",
            Description = "Can pass for human. Humans respond with Neutral attitudes. No racism table rolls unless in close proximity. Only elves, dwarfs, and orks.",
            PointValue = 1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26,
            Restrictions = "Only elves, dwarfs, and orks"
        };
        yield return new EdgeFlaw
        {
            Name = "Good Reputation",
            Description = "Enjoys a good reputation. -1 target modifier for Social Skill Tests per point.",
            PointValue = 1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26,
            IsLeveled = true,
            Level = 1
        };

        // Social Flaws
        yield return new EdgeFlaw
        {
            Name = "Bad Reputation",
            Description = "Dark stain on reputation. +1 target modifier on all Social Skill Tests per point.",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 25,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Braggart",
            Description = "Cannot stop boasting. Must make Intelligence (6) Test with 2 successes to back down from a story or boast.",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 25
        };
        yield return new EdgeFlaw
        {
            Name = "Dark Secret",
            Description = "Terrible secret whose revelation would have dreadful consequences. GM must threaten to expose it every 2-3 sessions.",
            PointValue = -2,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 25
        };
        yield return new EdgeFlaw
        {
            Name = "Day Job",
            Description = "Holds down a real job. Provides salary but requires time. 1,000/month (-1), 2,500/month (-2), 5,000+/month (-3).",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 25,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Dependent",
            Description = "Has a loved one who depends on the character for support and aid. Value set by GM based on demands.",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Distinctive Style",
            Description = "Dangerously memorable. -1 target modifier for anyone trying to track or locate the character physically.",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Elf Poser",
            Description = "Human who wants to be an elf. Associates with elves, talks like them, alters appearance. Contempt from real elves if discovered.",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26,
            Restrictions = "Only humans"
        };
        yield return new EdgeFlaw
        {
            Name = "Extra Enemy",
            Description = "Receive 1 additional enemy during character creation.",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Hung Out to Dry",
            Description = "Contacts suddenly refuse to talk. GM may replace with Bad Reputation or Extra Enemies if resolved.",
            PointValue = -4,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Liar",
            Description = "Sounds insincere even when telling the truth. GM rolls 1D6; on 1 the listener thinks the character is lying. Chance increases with each meeting.",
            PointValue = -2,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
        yield return new EdgeFlaw
        {
            Name = "Ugly and Doesn't Care",
            Description = "Bad looks cause social penalties. +2 Social/Etiquette TN vs opposite sex, +1 vs same sex. Suspicious initial attitude.",
            PointValue = -1,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 31
        };
        yield return new EdgeFlaw
        {
            Name = "Uncouth",
            Description = "No social graces. +2 target modifier on all Social Skill Tests including Negotiation and Etiquette.",
            PointValue = -2,
            Category = EdgeFlawCategory.Social,
            Book = "src",
            Page = 26
        };
    }

    private static IEnumerable<EdgeFlaw> GetMagicalEdgesFlaws()
    {
        yield return new EdgeFlaw
        {
            Name = "Astral Chameleon",
            Description = "Astral signature blends into background. Signatures last as if Force was 1 less. +2 to attempts to assense the signature.",
            PointValue = 2,
            Category = EdgeFlawCategory.Magical,
            Book = "src",
            Page = 27,
            Restrictions = "Only Awakened characters"
        };
        yield return new EdgeFlaw
        {
            Name = "Focused Concentration",
            Description = "+1 target modifier only when sustaining spells. Can sustain Sorcery + 1 spells simultaneously. Cannot perform Exclusive Actions while sustaining.",
            PointValue = 2,
            Category = EdgeFlawCategory.Magical,
            Book = "src",
            Page = 27,
            Restrictions = "Only Awakened characters"
        };
        yield return new EdgeFlaw
        {
            Name = "Magic Resistance",
            Description = "+1 die for Spell Resistance Tests per point. Cannot be magically active. Works against beneficial spells too. Cannot lower resistance.",
            PointValue = 1,
            Category = EdgeFlawCategory.Magical,
            Book = "src",
            Page = 27,
            IsLeveled = true,
            Level = 1,
            Restrictions = "Cannot be Awakened"
        };
        yield return new EdgeFlaw
        {
            Name = "Poor Link",
            Description = "Ritual sorcery directed against the character receives +2 target modifier for the Link Test. May also hinder friendly ritual magic.",
            PointValue = 2,
            Category = EdgeFlawCategory.Magical,
            Book = "src",
            Page = 27
        };
        yield return new EdgeFlaw
        {
            Name = "Spirit Affinity",
            Description = "Natural affinity with one type of nature spirit. Spirits are more inclined to do favors. May be reluctant to attack the character.",
            PointValue = 2,
            Category = EdgeFlawCategory.Magical,
            Book = "src",
            Page = 27
        };

        // Magical Flaws
        yield return new EdgeFlaw
        {
            Name = "Astral Impressions",
            Description = "Astral signature sticks out. Signatures last as if Force was 1 higher. -2 to attempts to read the signature.",
            PointValue = -2,
            Category = EdgeFlawCategory.Magical,
            Book = "src",
            Page = 27,
            Restrictions = "Only Awakened characters"
        };
        yield return new EdgeFlaw
        {
            Name = "Spirit Bane",
            Description = "Actively disliked by one type of nature spirit. Spirits harass the character and target them first in combat.",
            PointValue = -2,
            Category = EdgeFlawCategory.Magical,
            Book = "src",
            Page = 27
        };
    }

    private static IEnumerable<EdgeFlaw> GetMatrixEdgesFlaws()
    {
        yield return new EdgeFlaw
        {
            Name = "Codeslinger",
            Description = "Adept at one system operation. +1 die for System Tests with that operation. Only characters capable of decking.",
            PointValue = 2,
            Category = EdgeFlawCategory.Matrix,
            Book = "src",
            Page = 28,
            Restrictions = "Only deckers"
        };
        yield return new EdgeFlaw
        {
            Name = "Cracker",
            Description = "Exceptionally skilled at one System Test type (Access, Control, etc.). +1 die for that test. Cannot have Codeslinger. Otaku cannot take.",
            PointValue = 4,
            Category = EdgeFlawCategory.Matrix,
            Book = "src",
            Page = 28,
            Restrictions = "Only deckers; cannot have Codeslinger"
        };
        yield return new EdgeFlaw
        {
            Name = "Natural Hardening",
            Description = "Neural structure resists feedback. 1 point of natural Hardening, cumulative with cyberdeck Hardening.",
            PointValue = 4,
            Category = EdgeFlawCategory.Matrix,
            Book = "src",
            Page = 28
        };

        // Matrix Flaws
        yield return new EdgeFlaw
        {
            Name = "Codeblock",
            Description = "Always has trouble with one system operation. Lose one die on tests for that operation. Only deckers.",
            PointValue = -1,
            Category = EdgeFlawCategory.Matrix,
            Book = "src",
            Page = 28,
            Restrictions = "Only deckers"
        };
        yield return new EdgeFlaw
        {
            Name = "Choker",
            Description = "Difficulty with one System Test type. Lose one die on that test. Cannot have Cracker. Only deckers.",
            PointValue = -2,
            Category = EdgeFlawCategory.Matrix,
            Book = "src",
            Page = 28,
            Restrictions = "Only deckers; cannot have Cracker"
        };
        yield return new EdgeFlaw
        {
            Name = "Jack Itch",
            Description = "Psychosomatic itch when jacked in. Can only stay jacked in for Willpower minutes before +1 TN, increasing every Willpower minutes. At +8, frenzies and jacks out.",
            PointValue = -1,
            Category = EdgeFlawCategory.Matrix,
            Book = "src",
            Page = 28
        };
        yield return new EdgeFlaw
        {
            Name = "Scorched",
            Description = "Past encounter with psychotropic Black IC. GM determines lasting effects. No Willpower Tests to ignore conditioning.",
            PointValue = -1,
            Category = EdgeFlawCategory.Matrix,
            Book = "src",
            Page = 28
        };
    }

    private static IEnumerable<EdgeFlaw> GetMiscellaneousEdgesFlaws()
    {
        yield return new EdgeFlaw
        {
            Name = "Daredevil",
            Description = "Each session, 1 extra Karma Pool Point for heroically risky actions. Cannot be burned. Once used, gone for the session.",
            PointValue = 3,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 29
        };
        yield return new EdgeFlaw
        {
            Name = "Pirate Family",
            Description = "Extended pirate family in multiple ports. Acts as Friends of Friends. GM may use it to annoy rather than help.",
            PointValue = 3,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 30
        };
        yield return new EdgeFlaw
        {
            Name = "Vehicle Empathy",
            Description = "Understands vehicles intuitively. Reduce vehicle Handling by 1 when in physical contact (manual controls or jacked in).",
            PointValue = 2,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 31
        };

        // Miscellaneous Flaws
        yield return new EdgeFlaw
        {
            Name = "Bad Karma",
            Description = "Must earn double Karma to increase Karma Pool.",
            PointValue = -5,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 29
        };
        yield return new EdgeFlaw
        {
            Name = "Cranial Bomb",
            Description = "Someone planted a cranial bomb in the character's head. GM decides who and what they want. Free; does not cost Resources.",
            PointValue = -6,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 29
        };
        yield return new EdgeFlaw
        {
            Name = "Cursed Karma",
            Description = "When using Karma Pool, roll 1D6. On 1, the point has the exact opposite effect intended.",
            PointValue = -6,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 29
        };
        yield return new EdgeFlaw
        {
            Name = "Gremlins",
            Description = "Equipment tends to malfunction when touched. Roll 2D6; on 2, equipment breaks down. Severity depends on point value.",
            PointValue = -1,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 29,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Hunted",
            Description = "Enemies aggressively hunt the character. If killed, a new one takes its place. -2 (Rank 3), -4 (Rank 4), -6 (Rank 5-6).",
            PointValue = -2,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 29,
            IsLeveled = true,
            Level = 1
        };
        yield return new EdgeFlaw
        {
            Name = "Mysterious Cyberware",
            Description = "Unaware piece of cyberware in the body. GM chooses when it is revealed. May show up on scanners at inconvenient times.",
            PointValue = -3,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 29
        };
        yield return new EdgeFlaw
        {
            Name = "Police Record",
            Description = "Ex-convict with a criminal SIN. Only street-level contacts. Corporate security has records. Lone Star harasses on sight. Must check with parole officer.",
            PointValue = -6,
            Category = EdgeFlawCategory.Miscellaneous,
            Book = "src",
            Page = 30
        };
    }
}
