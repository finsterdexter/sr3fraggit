using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SR3Generator.Creation.Validation
{
    public class ValidationIssue
    {
        public ValidationIssueLevel Level { get; set; }
        public ValidationIssueCategory Category { get; set; }
        public required string Message { get; set; }
    }

    public enum ValidationIssueLevel
    {
        Error,
        Warning,
        Info
    }

    public enum ValidationIssueCategory
    {
        Misc,
        Attributes,
        Skills,
        Resources,
        Magic,
        Equipment,
        Race,
        Cyberdeck,
    }
}
