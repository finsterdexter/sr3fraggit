using Microsoft.Extensions.Options;
using SR3Generator.Data.Character;
using SR3Generator.Database.Connection;
using SR3Generator.Database.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Database
{
    public class SkillDatabase
    {
        public Dictionary<string, Skill> ActiveSkills { get; set; } = new Dictionary<string, Skill>();
        public Dictionary<string, Skill> KnowledgeSkills { get; set; } = new Dictionary<string, Skill>();

        /// <summary>
        /// Looks up a skill or specialization by name.
        /// For base skills, the key is just the name.
        /// For specializations, the key is "{BaseSkillName}|{SpecName}".
        /// This method handles both cases and also searches by iterating values if needed.
        /// </summary>
        public bool TryGetSkillByName(string name, out Skill? skill)
        {
            // Try direct lookup first (works for base skills)
            if (ActiveSkills.TryGetValue(name, out skill) || KnowledgeSkills.TryGetValue(name, out skill))
                return true;

            // For specializations, search by iterating values (handles composite keys)
            skill = ActiveSkills.Values.FirstOrDefault(s => s.Name == name);
            if (skill != null) return true;

            skill = KnowledgeSkills.Values.FirstOrDefault(s => s.Name == name);
            return skill != null;
        }

        /// <summary>
        /// Looks up a specialization by base skill name and specialization name.
        /// Uses the composite key format: "{baseSkillName}|{specName}"
        /// </summary>
        public bool TryGetSpecialization(string baseSkillName, string specName, out Skill? skill)
        {
            var key = $"{baseSkillName}|{specName}";
            if (ActiveSkills.TryGetValue(key, out skill) || KnowledgeSkills.TryGetValue(key, out skill))
                return true;

            skill = null;
            return false;
        }

        /// <summary>
        /// Public constructor for external consumers (DI registration).
        /// </summary>
        public SkillDatabase(IOptions<DatabaseOptions> options)
            : this(new DbConnectionFactory(options), new ReadSkillsQueryHandler())
        {
        }

        /// <summary>
        /// Internal constructor for testing with mocked dependencies.
        /// </summary>
        internal SkillDatabase(DbConnectionFactory dbConnectionFactory,
            ReadSkillsQueryHandler readSkillsQueryHandler)
        {
            var conn = dbConnectionFactory.CreateConnection();
            var (skills, specs) = readSkillsQueryHandler.HandleAsync(new ReadSkillsQuery(), conn, null).Result;

            foreach (var skill in skills)
            {
                if (skill.Type == SkillType.Active)
                    ActiveSkills[skill.Name] = skill;
                else
                    KnowledgeSkills[skill.Name] = skill;
            }

            foreach (var spec in specs)
            {
                // Use composite key to avoid collisions when multiple skills share the same spec name
                // e.g., "Assault Rifles|weapon->" and "Clubs|weapon->" are distinct
                var key = $"{spec.BaseSkillName}|{spec.Name}";
                if (spec.Type == SkillType.Active)
                    ActiveSkills[key] = spec;
                else
                    KnowledgeSkills[key] = spec;
            }
        }
    }
}
