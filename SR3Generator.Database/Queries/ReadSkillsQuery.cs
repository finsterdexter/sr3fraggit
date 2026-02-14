using Dapper;
using SR3Generator.Data.Character;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Attribute = SR3Generator.Data.Character.Attribute;

namespace SR3Generator.Database.Queries
{
    internal class ReadSkillsQuery : IQuery<(IEnumerable<Skill> skills, IEnumerable<Skill> specializations)>
    {
    }

    internal class ReadSkillsQueryHandler : IQueryHandler<ReadSkillsQuery, (IEnumerable<Skill> skills, IEnumerable<Skill> specializations)>
    {
        const string skillSql = "SELECT * FROM skills;";
        const string specSql = "SELECT * FROM skill_specializations;";

        public async Task<(IEnumerable<Skill> skills, IEnumerable<Skill> specializations)> HandleAsync(ReadSkillsQuery query, IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            var skills = await dbConnection.QueryAsync<SkillDto>(skillSql, query, dbTransaction);
            var specs = await dbConnection.QueryAsync<SpecializationDto>(specSql, query, dbTransaction);

            var skillResults = new List<Skill>();
            foreach (var skill in skills)
            {
                Skill newSkill;
                switch (skill.atr)
                {
                    case "KNO":
                        newSkill = new Skill(skill.name ?? "", Attribute.AttributeName.Intelligence);
                        newSkill.Type = SkillType.Knowledge;
                        break;
                    case "LAN":
                        newSkill = new Skill(skill.name ?? "", Attribute.AttributeName.Intelligence);
                        newSkill.Type = SkillType.Language;
                        break;
                    case null or "" when skill.id == 393: // IN:Roleplaying Games of 20th Cen - missing atr in db
                        newSkill = new Skill(skill.name ?? "", Attribute.AttributeName.Intelligence);
                        newSkill.Type = SkillType.Knowledge;
                        break;
                    case " Lewis" when skill.id == 408: // AK:Ft Lewis - corrupt atr in db (name got split)
                        newSkill = new Skill("AK:Ft Lewis", Attribute.AttributeName.Intelligence);
                        newSkill.Type = SkillType.Knowledge;
                        break;
                    default:
                        var atr = (Attribute.AttributeAbbr)Enum.Parse(typeof(Attribute.AttributeAbbr), skill.atr!);
                        newSkill = new Skill(skill.name ?? "", Attribute.GetName(atr));
                        newSkill.Type = SkillType.Active;
                        break;
                }
                newSkill.Book = skill.book ?? null!;
                newSkill.Page = skill.page ?? null!;
                newSkill.Notes = skill.notes ?? "";
                newSkill.SkillClass = skill.skill_class ?? "";
                skillResults.Add(newSkill);
            }

            var specResults = new List<Skill>();
            foreach (var spec in specs)
            {
                Skill newSpec;
                var skill = skills.FirstOrDefault(s => s.id == spec.skill_id);
                if (skill is null) continue;
                switch (skill.atr)
                {
                    case "KNO":
                        newSpec = new Skill(spec.name ?? "", Attribute.AttributeName.Intelligence);
                        newSpec.Type = SkillType.Knowledge;
                        break;
                    case "LAN":
                        newSpec = new Skill(spec.name ?? "", Attribute.AttributeName.Intelligence);
                        newSpec.Type = SkillType.Language;
                        break;
                    default:
                        newSpec = new Skill(spec.name ?? "", Attribute.GetName((Attribute.AttributeAbbr)Enum.Parse(typeof(Attribute.AttributeAbbr), skill.atr!)));
                        newSpec.Type = SkillType.Active;
                        break;
                }
                newSpec.IsSpecialization = true;
                newSpec.Book = spec.book ?? null!;
                newSpec.Page = spec.page ?? null!;
                newSpec.Notes = spec.notes ?? "";
                newSpec.BaseSkillName = skill.name;
                newSpec.SkillClass = skill.skill_class ?? "";
                specResults.Add(newSpec);
            }

            return (skillResults, specResults);
        }
    }

    internal class SkillDto
    {
        public int id { get; set; }
        public string? skill_class { get; set; }
        public string? name { get; set; }
        public string? atr { get; set; }
        public string? book { get; set; }
        public string? page { get; set; }
        public string? notes { get; set; }
    }

    internal class SpecializationDto
    {
        public int id { get; set; }
        public int skill_id { get; set; }
        public string? name { get; set; }
        public string? book { get; set; }
        public string? page { get; set; }
        public string? notes { get; set; }
    }
}
