using System;
using System.Collections.Generic;
using System.Linq;
using STRBlender.Core.Domain.Common;
using STRBlender.Core.Domain.Models;
using STRBlender.Core.Domain.Services;
using STRBlender.Core.Infrastructure.Data;

namespace STRBlender.Core.Application
{
    public static class SimulationService
    {
        /// Desktop path: loads frequency/stutter files from AppPaths on disk.
        public static SimulationResult GenerateProfiles(SimulationParams simParams)
        {
            var reverseRules = StutterCalculator.Load(AppPaths.ReverseStutterPath);
            var forwardRules = StutterCalculator.Load(AppPaths.ForwardStutterPath);

            var freqCache = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            Dictionary<string, Dictionary<string, double>> LoadDb(string database)
            {
                if (freqCache.TryGetValue(database, out var cached))
                    return cached;
                string filename = KitConfig.DatabaseOptions[database];
                string csvPath = AppPaths.GetFrequencyFilePath(filename);
                var freqs = FrequencyLoader.LoadAlleleFrequencies(csvPath, simParams.LociToUse);
                freqCache[database] = freqs;
                return freqs;
            }

            return GenerateProfilesCore(simParams, LoadDb, reverseRules, forwardRules);
        }

        /// Web / in-memory path: caller supplies frequency tables and stutter rules
        /// (e.g. parsed from HttpClient-fetched CSV text). Same peak/pedigree logic.
        public static SimulationResult GenerateProfiles(
            SimulationParams simParams,
            Func<string, Dictionary<string, Dictionary<string, double>>> frequencyProvider,
            StutterCalculator reverseRules,
            StutterCalculator forwardRules)
        {
            return GenerateProfilesCore(simParams, frequencyProvider, reverseRules, forwardRules);
        }

        private static SimulationResult GenerateProfilesCore(
            SimulationParams simParams,
            Func<string, Dictionary<string, Dictionary<string, double>>> frequencyProvider,
            StutterCalculator reverseRules,
            StutterCalculator forwardRules)
        {
            var result = new SimulationResult();
            var contributors = simParams.ContributorParams;

            var c1Contributor = contributors[0];
            var c1Freqs = frequencyProvider(c1Contributor.Database);

            Profile c1Profile;
            if (c1Contributor.Locked && simParams.LastProfilesBackup.Count > 0)
            {
                c1Profile = simParams.LastProfilesBackup[0];
            }
            else
            {
                c1Profile = ProfileGenerator.SampleProfile(c1Freqs, simParams.LociToUse, c1Contributor.Sex);
            }

            bool anyRelated = contributors.Skip(1).Any(c => c.Relationship != "Unrelated");
            Pedigree? pedigree = null;
            if (anyRelated)
            {
                pedigree = (c1Contributor.Locked && simParams.CachedPedigree != null)
                    ? simParams.CachedPedigree
                    : PedigreeGenerator.BuildCoreFamily(c1Profile, c1Freqs, simParams.LociToUse);
            }
            result.CachedPedigree = pedigree;

            foreach (var (i, contributor) in contributors.Select((p, i) => (i, p)))
            {
                Profile profile;

                if (i == 0)
                {
                    profile = c1Profile;
                }
                else if (contributor.Locked && i < simParams.LastProfilesBackup.Count)
                {
                    profile = simParams.LastProfilesBackup[i];
                }
                else if (contributor.Relationship != "Unrelated" &&
                         pedigree != null &&
                         PedigreeGenerator.ResolveRole(pedigree, contributor.Relationship, c1Freqs, simParams.LociToUse, contributor.Sex)
                             is Profile relativeProfile)
                {
                    profile = relativeProfile;
                }
                else
                {
                    var freqs = frequencyProvider(contributor.Database);
                    profile = ProfileGenerator.SampleProfile(freqs, simParams.LociToUse, contributor.Sex);
                }

                result.Profiles.Add(profile);

                var peakList = PeakCalculator.BuildPeakTable(
                    profile,
                    simParams.Template,
                    contributor.Proportion,
                    contributor.Degradation,
                    reverseRules,
                    forwardRules,
                    simParams.LociToUse);

                result.PeakLists.Add(peakList);
                result.AllPeaks.AddRange(peakList);
            }

            var alleleOnlyPeaks = result.AllPeaks.Where(p => p.PeakType == "parent").ToList();

            result.FinalPeaks = PeakCalculator.BuildFinalPeaks(
                alleleOnlyPeaks, reverseRules, forwardRules, simParams.LociToUse);

            return result;
        }
    }
}
