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
        private readonly ReadSkillsQueryHandler _readSkillsQueryHandler;

        public Dictionary<string, Skill> ActiveSkills { get; set; } = new Dictionary<string, Skill>();
        public Dictionary<string, Skill> KnowledgeSkills { get; set; } = new Dictionary<string, Skill>();

        internal SkillDatabase(DbConnectionFactory dbConnectionFactory,
            ReadSkillsQueryHandler readSkillsQueryHandler)
        {
            _readSkillsQueryHandler = readSkillsQueryHandler;

            // TODO: preload skill data from json

            // validate data: all specializations must have a base skill name

            // connect to sqlite db
            // read skills and specs from db
            // load results into ActiveSkills and KnowledgeSkills

            var conn = dbConnectionFactory.CreateConnection();
            var (skills, specs) = _readSkillsQueryHandler.HandleAsync(new ReadSkillsQuery(), conn, null).Result;
        }
    }
}
