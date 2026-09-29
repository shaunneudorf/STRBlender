using System.Collections.Generic;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Common.Kits
{
    /// All data for the GlobalFiler kit in one place: loci, channel/dye
    /// layout, MW regression parameters, and allelic ladder alleles (from
    /// the GlobalFiler Express PCR Amplification Kit User Guide). Adding
    /// another CE kit means writing one file like this one.
    public static class GlobalFilerKit
    {
        public static KitDefinition Build()
        {
            var loci = new List<string>
            {
                "D3S1358","vWA","D16S539","CSF1PO","TPOX","YINDEL","AMEL",
                "D8S1179","D21S11","D18S51","DYS391","D2S441","D19S433","TH01",
                "FGA","D22S1045","D5S818","D13S317","D7S820","SE33","D10S1248",
                "D1S1656","D12S391","D2S1338"
            };

            var channels = new Dictionary<string, ChannelConfig>
            {
                ["Blue"] = new ChannelConfig(new List<string> { "D3S1358", "vWA", "D16S539", "CSF1PO", "TPOX" }, RgbColor.Blue, 100),
                ["Green"] = new ChannelConfig(new List<string> { "YINDEL", "AMEL", "D8S1179", "D21S11", "D18S51", "DYS391" }, RgbColor.Green, 150),
                ["Yellow"] = new ChannelConfig(new List<string> { "D2S441", "D19S433", "TH01", "FGA" }, RgbColor.Goldenrod, 150),
                ["Red"] = new ChannelConfig(new List<string> { "D22S1045", "D5S818", "D13S317", "D7S820", "SE33" }, RgbColor.Red, 175),
                ["Purple"] = new ChannelConfig(new List<string> { "D10S1248", "D1S1656", "D12S391", "D2S1338" }, RgbColor.Purple, 150),
            };

            // k/b/a/efficiency are sourced from GlobalFiler's own kit/panel
            // definition. There's no stored min/max MW here — the locus bar
            // is derived live from the ladder below (KitDefinition.
            // GetLocusBounds), so it always hugs the actual ladder allele
            // range rather than any padded official spec value.
            // YINDEL has no entry here because its MW is handled as a
            // fixed-value special case in KitDefinition.CalculateMw (only
            // alleles "1"/"2" exist).
            var locusParams = new Dictionary<string, LocusParams>
            {
                ["D3S1358"] = new(60.14, 4.08, 1.05),
                ["vWA"] = new(112.22, 4.05, 1.0),
                ["D16S539"] = new(207.44, 4.05, 1.0),
                ["CSF1PO"] = new(259.29, 3.97, 1.15),
                ["TPOX"] = new(318.25, 4.05, 0.85),
                ["YINDEL"] = new(0, 0.0, 1.0),
                ["AMEL"] = new(106.0, 0.0, 1.0),
                ["D8S1179"] = new(93.73, 4.1, 1.12),
                ["D21S11"] = new(86.11, 4.04, 1.25),
                ["D18S51"] = new(233.0, 4.05, 1.35),
                ["DYS391"] = new(337.1, 4.03, 1.12),
                ["D2S441"] = new(43.86, 4.11, 0.85),
                ["D19S433"] = new(94.67, 3.93, 0.85),
                ["TH01"] = new(163.19, 4.02, 0.8),
                ["FGA"] = new(171.15, 4.02, 0.9),
                ["D22S1045"] = new(64.63, 2.98, 1.1),
                ["D5S818"] = new(110.28, 4.06, 1.2),
                ["D13S317"] = new(178.67, 4.03, 1.35),
                ["D7S820"] = new(238.71, 3.99, 1.1),
                ["SE33"] = new(289.39, 4.04, 1.5),
                ["D10S1248"] = new(53.55, 4.00, 1.25),
                ["D1S1656"] = new(121.63, 4.22, 1.4),
                ["D12S391"] = new(160.58, 3.97, 1.15),
                ["D2S1338"] = new(236.45, 4.02, 1.4),
            };

            // Allelic ladder alleles per locus, from the kit's user guide.
            var ladder = new Dictionary<string, List<string>>
            {
                ["D3S1358"] = Split("9,10,11,12,13,14,15,16,17,18,19,20"),
                ["vWA"] = Split("11,12,13,14,15,16,17,18,19,20,21,22,23,24"),
                ["D16S539"] = Split("5,8,9,10,11,12,13,14,15"),
                ["CSF1PO"] = Split("6,7,8,9,10,11,12,13,14,15"),
                ["TPOX"] = Split("5,6,7,8,9,10,11,12,13,14,15"),
                ["YINDEL"] = Split("1,2"),
                ["AMEL"] = Split("X,Y"),
                ["D8S1179"] = Split("5,6,7,8,9,10,11,12,13,14,15,16,17,18,19"),
                ["D21S11"] = Split("24,24.2,25,26,27,28,28.2,29,29.2,30,30.2,31,31.2,32,32.2,33,33.2,34,34.2,35,35.2,36,37,38"),
                ["D18S51"] = Split("7,9,10,10.2,11,12,13,13.2,14,14.2,15,16,17,18,19,20,21,22,23,24,25,26,27"),
                ["DYS391"] = Split("7,8,9,10,11,12,13"),
                ["D2S441"] = Split("8,9,10,11,11.3,12,13,14,15,16,17"),
                ["D19S433"] = Split("6,7,8,9,10,11,12,12.2,13,13.2,14,14.2,15,15.2,16,16.2,17,17.2,18,18.2,19.2"),
                ["TH01"] = Split("4,5,6,7,8,9,9.3,10,11,13.3"),
                ["FGA"] = Split("13,14,15,16,17,18,19,20,21,22,23,24,25,26,26.2,27,28,29,30,30.2,31.2,32.2,33.2,42.2,43.2,44.2,45.2,46.2,47.2,48.2,50.2,51.2"),
                ["D22S1045"] = Split("8,9,10,11,12,13,14,15,16,17,18,19"),
                ["D5S818"] = Split("7,8,9,10,11,12,13,14,15,16,17,18"),
                ["D13S317"] = Split("5,6,7,8,9,10,11,12,13,14,15,16"),
                ["D7S820"] = Split("6,7,8,9,10,11,12,13,14,15"),
                ["SE33"] = Split("4.2,6.3,8,9,11,12,13,14,15,16,17,18,19,20,20.2,21,21.2,22.2,23.2,24.2,25.2,26.2,27.2,28.2,29.2,30.2,31.2,32.2,33.2,34.2,35,35.2,36,37"),
                ["D10S1248"] = Split("8,9,10,11,12,13,14,15,16,17,18,19"),
                ["D1S1656"] = Split("9,10,11,12,13,14,14.3,15,15.3,16,16.3,17,17.3,18.3,19.3,20.3"),
                ["D12S391"] = Split("14,15,16,17,18,19,19.3,20,21,22,23,24,25,26,27"),
                ["D2S1338"] = Split("11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28"),
            };

            return new KitDefinition("GLOBALFILER", loci, channels, locusParams, ladder);
        }

        private static List<string> Split(string csv) => new List<string>(csv.Split(','));
    }
}
