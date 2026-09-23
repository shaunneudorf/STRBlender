using System;
using System.Collections.Generic;
using STRBlender.Core.Domain.Common.Kits;

namespace STRBlender.Core.Domain.Common
{
    /// Single lookup point for every kit's data. Adding a new kit (PowerPlex,
    /// SGM Plus, etc.) means: write one new factory class under Kits/ (like
    /// GlobalFilerKit/IdPlusKit), then register it in the dictionary below.
    /// Nothing else in the app needs to change.
    public static class KitRegistry
    {
        private static readonly Dictionary<string, KitDefinition> _kits = new()
        {
            ["GLOBALFILER"] = GlobalFilerKit.Build(),
            ["IDPLUS"] = IdPlusKit.Build(),
        };

        static KitRegistry()
        {
            Validate();
        }

        public static KitDefinition Get(string kitName)
        {
            if (kitName != null && _kits.TryGetValue(kitName.ToUpper(), out var kit))
                return kit;
            return _kits["IDPLUS"]; // default, matches LocusDefinitions' previous default
        }

        public static IReadOnlyCollection<string> KitNames => _kits.Keys;

        /// Fails loudly at startup if a kit is missing LocusParams or ladder
        /// data for one of its own loci, instead of silently producing a
        /// null MW / a missing bin at runtime — worth having now that each
        /// kit's data is hand-entered from a manufacturer table.
        private static void Validate()
        {
            foreach (var kit in _kits.Values)
            {
                foreach (var locus in kit.Loci)
                {
                    bool isSpecialCase = locus == "AMEL" || locus == "YINDEL";

                    if (!isSpecialCase && !kit.LocusParamsByLocus.ContainsKey(locus))
                        throw new InvalidOperationException(
                            $"Kit '{kit.Name}' is missing LocusParams for locus '{locus}'.");

                    if (!kit.Ladder.TryGetValue(locus, out var alleles) || alleles.Count == 0)
                        throw new InvalidOperationException(
                            $"Kit '{kit.Name}' is missing allelic ladder alleles for locus '{locus}'.");
                }
            }
        }
    }
}
