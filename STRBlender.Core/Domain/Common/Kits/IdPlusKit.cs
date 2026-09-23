using System.Collections.Generic;
using System.Linq;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Common.Kits
{
    /// All data for the IDplus kit in one place. IDplus's loci are a subset
    /// of GlobalFiler's, and the allelic ladder allele sets (which alleles
    /// exist at a locus) are a property of the locus itself rather than the
    /// kit's chemistry — so the ladder is reused from GlobalFilerKit for the
    /// loci the two kits share, while MW regression parameters stay IDplus's
    /// own (its dye chemistry/fragment sizing differs from GlobalFiler's).
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

            // Same k/b/a/efficiency/min-max values as the original
            // LocusDefinitions.IdplusLoci table — IDplus's own regression,
            // distinct from GlobalFiler's even for the same locus names.
            var locusParams = new Dictionary<string, LocusParams>
            {
                ["D8S1179"] = new(8, 121.63, 4.08, 1.00, 118.0, 183.5),
                ["D21S11"] = new(24, 182.42, 4.07, 0.85, 184.5, 247.5),
                ["D7S820"] = new(6, 254.23, 4.03, 0.75, 251.0, 298.5),
                ["CSF1PO"] = new(6, 302.92, 4.05, 0.86, 302.12, 348.63),
                ["D3S1358"] = new(12, 110.24, 4.08, 1.20, 98.0, 148.0),
                ["TH01"] = new(4, 160.58, 4.07, 1.26, 159.0, 205.0),
                ["D13S317"] = new(8, 215.13, 4.07, 1.46, 205.65, 250.16),
                ["D16S539"] = new(5, 251.55, 4.05, 1.40, 255.3, 301.81),
                ["D2S1338"] = new(15, 305.03, 4.06, 1.30, 304.8, 370.31),
                ["D19S433"] = new(9, 99.91, 4.07, 0.90, 101.0, 148.0),
                ["vWA"] = new(11, 151.84, 4.08, 1.04, 151.0, 213.5),
                ["TPOX"] = new(6, 220.92, 4.06, 1.07, 216.99, 260.99),
                ["D18S51"] = new(7, 260.94, 4.05, 1.16, 264.49, 350.0),
                ["AMEL"] = new(0, 106.0, 0.0, 1.00, 104.0, 114.0),
                ["D5S818"] = new(7, 132.56, 4.07, 1.10, 128.0, 180.0),
                ["FGA"] = new(17, 212.60, 4.08, 0.94, 206.25, 360.0),
            };

            var globalFilerLadder = GlobalFilerKit.Build().Ladder;
            var ladder = loci.ToDictionary(l => l, l => globalFilerLadder[l]);

            return new KitDefinition("IDPLUS", loci, channels, locusParams, ladder);
        }
    }
}
