using System.Collections.Generic;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Common.Kits
{
    /// All data for the IDplus kit in one place, entirely its own — loci,
    /// channel/dye layout, MW regression parameters, and allelic ladder
    /// alleles, all sourced from IDplus's own kit/panel definition (not
    /// shared or derived from GlobalFiler's). This is the pattern every
    /// future kit (PowerPlex, SGM Plus, etc.) should follow: one
    /// self-contained factory file per kit, with no cross-kit borrowing,
    /// since a locus's allele range is a property of that specific kit's
    /// own validated panel, not another kit's data. The locus bar's range
    /// is never stored here — it's derived live from the ladder below (see
    /// KitDefinition.GetLocusBounds).
    public static class IdPlusKit
    {
        public static KitDefinition Build()
        {
            var loci = new List<string>
            {
                "D8S1179","D21S11","D7S820","CSF1PO","D3S1358","TH01","D13S317",
                "D16S539","D2S1338","D19S433","vWA","TPOX","D18S51","AMEL",
                "D5S818","FGA"
            };

            var channels = new Dictionary<string, ChannelConfig>
            {
                ["Blue"] = new ChannelConfig(new List<string> { "D8S1179", "D21S11", "D7S820", "CSF1PO" }, RgbColor.Blue, 100),
                ["Green"] = new ChannelConfig(new List<string> { "D3S1358", "TH01", "D13S317", "D16S539", "D2S1338" }, RgbColor.Green, 150),
                ["Yellow"] = new ChannelConfig(new List<string> { "D19S433", "vWA", "TPOX", "D18S51" }, RgbColor.Goldenrod, 150),
                ["Red"] = new ChannelConfig(new List<string> { "AMEL", "D5S818", "FGA" }, RgbColor.Red, 175),
            };

            // k/b/a/efficiency are IDplus's own MW regression. There's no
            // stored min/max MW here — the locus bar is derived live from
            // the ladder below (KitDefinition.GetLocusBounds), so it always
            // hugs the actual ladder allele range rather than any padded
            // official spec value.
            var locusParams = new Dictionary<string, LocusParams>
            {
                ["D8S1179"] = new(88.67, 4.11, 1.00),
                ["D21S11"] = new(85.54, 4.05, 0.85),
                ["D7S820"] = new(230.25, 4.01, 0.75),
                ["CSF1PO"] = new(278.8, 4.02, 0.86),
                ["D3S1358"] = new(62.43, 4.00, 1.20),
                ["TH01"] = new(144.3, 4.08, 1.26),
                ["D13S317"] = new(183.1, 4.03, 1.46),
                ["D16S539"] = new(231.79, 3.97, 1.40),
                ["D2S1338"] = new(244.57, 4.04, 1.30),
                ["D19S433"] = new(64.15, 4.01, 0.90),
                ["vWA"] = new(107.09, 4.08, 1.04),
                ["TPOX"] = new(196.84, 4.03, 1.07),
                ["D18S51"] = new(232.6, 4.05, 1.16),
                ["AMEL"] = new(106.0, 0.0, 1.00),
                ["D5S818"] = new(103.82, 4.11, 1.10),
                ["FGA"] = new(144.21, 4.05, 0.94),
            };

            // IDplus's own allelic ladder — its own panel definition, not
            // borrowed from GlobalFiler. Several loci genuinely differ in
            // allele range from GlobalFiler's ladder for the same locus name
            // (e.g. D3S1358 here is 12–19, GlobalFiler's is 9–20; D13S317
            // here is 8–15, GlobalFiler's is 5–16), so sharing was wrong,
            // not just imprecise.
            var ladder = new Dictionary<string, List<string>>
            {
                ["D8S1179"] = Split("8,9,10,11,12,13,14,15,16,17,18,19"),
                ["D21S11"] = Split("24,24.2,25,26,27,28,28.2,29,29.2,30,30.2,31,31.2,32,32.2,33,33.2,34,34.2,35,35.2,36,37,38"),
                ["D7S820"] = Split("6,7,8,9,10,11,12,13,14,15"),
                ["CSF1PO"] = Split("6,7,8,9,10,11,12,13,14,15"),
                ["D3S1358"] = Split("12,13,14,15,16,17,18,19"),
                ["TH01"] = Split("4,5,6,7,8,9,9.3,10,11,13.3"),
                ["D13S317"] = Split("8,9,10,11,12,13,14,15"),
                ["D16S539"] = Split("5,8,9,10,11,12,13,14,15"),
                ["D2S1338"] = Split("15,16,17,18,19,20,21,22,23,24,25,26,27,28"),
                ["D19S433"] = Split("9,10,11,12.2,13,13.2,14,14.2,15,15.2,16,16.2,17,17.2"),
                ["vWA"] = Split("11,12,13,14,15,16,17,18,19,20,21,22,23,24"),
                ["TPOX"] = Split("6,7,8,9,10,11,12,13"),
                ["D18S51"] = Split("7,9,10,10.2,11,12,13,13.2,14,14.2,15,16,17,18,19,20,21,22,23,24,25,26,27"),
                ["AMEL"] = Split("X,Y"),
                ["D5S818"] = Split("7,8,9,10,11,12,13,14,15,16"),
                ["FGA"] = Split("17,18,19,20,21,22,23,24,25,26,26.2,27,28,29,30,30.2,31.2,32.2,33.2,42.2,43.2,44.2,45.2,46.2,47.2,48.2,50.2,51.2"),
            };

            return new KitDefinition("IDPLUS", loci, channels, locusParams, ladder);
        }

        private static List<string> Split(string csv) => new List<string>(csv.Split(','));
    }
}
