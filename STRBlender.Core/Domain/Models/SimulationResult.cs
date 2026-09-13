using System.Collections.Generic;

namespace STRBlender.Core.Domain.Models
{
    public class SimulationResult
    {
        public List<Peak> AllPeaks { get; set; } = new();
        public List<List<Peak>> PeakLists { get; set; } = new();
        public List<Profile> Profiles { get; set; } = new();
        public List<Peak> FinalPeaks { get; set; } = new();
        public Pedigree? CachedPedigree { get; set; }
    }
}