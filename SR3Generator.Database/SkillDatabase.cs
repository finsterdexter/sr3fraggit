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
                if (spec.Type == SkillType.Active)
                    ActiveSkills[spec.Name] = spec;
                else
                    KnowledgeSkills[spec.Name] = spec;
            }
        }
    }
}
