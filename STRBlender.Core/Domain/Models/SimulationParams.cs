using System.Collections.Generic;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Models
{
    public class SimulationParams
    {
        public string Kit { get; set; } = "IDPLUS";
        public double Template { get; set; } = 2000;
        public List<string> LociToUse { get; set; } = new();
        public List<ContributorParams> ContributorParams { get; set; } = new();
        public List<Profile> LastProfilesBackup { get; set; } = new();
        public Pedigree? CachedPedigree { get; set; }
    }
}