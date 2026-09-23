using System;
using System.Collections.Generic;

namespace STRBlender.Core.Domain.Common
{
    /// Kept as a thin, backward-compatible facade over KitRegistry so
    /// existing call sites (SetKit/CalculateMw/GetEfficiency/GetLocusBounds)
    /// keep working unchanged. It still tracks one mutable "active kit" —
    /// that static-state pattern hasn't changed here, only where the data
    /// itself lives (now KitDefinition, one per kit, via KitRegistry).
    /// New code should prefer resolving a KitDefinition once via
    /// KitRegistry.Get(kitName) and calling its instance methods directly,
    /// which doesn't depend on this shared "currently active" state.
    public static class LocusDefinitions
    {
        private static KitDefinition _activeKit = KitRegistry.Get("IDPLUS");

        public static void SetKit(string kitName)
        {
            _activeKit = KitRegistry.Get(kitName);
        }

        public static double? CalculateMw(string locus, string allele) =>
            _activeKit.CalculateMw(locus, allele);

        public static double GetEfficiency(string locus) =>
            _activeKit.GetEfficiency(locus);

        public static (double? MinMw, double? MaxMw) GetLocusBounds(string locus) =>
            _activeKit.GetLocusBounds(locus);
    }
}
