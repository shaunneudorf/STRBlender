using System.Collections.Generic;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Common.Kits
{
    /// All data for the PowerPlex Fusion kit in one place, entirely its own —
    /// loci, channel/dye layout, MW regression parameters, and allelic ladder
    /// alleles, all sourced from Fusion's own kit/panel definition (no
    /// cross-kit borrowing). The locus bar's range is never stored here — it's
    /// derived live from the ladder below (see KitDefinition.GetLocusBounds).
    /// Y-linked loci (DYS391) need no special handling: like GlobalFiler's
    /// DYS391 they are ordinary entries in every collection below.
    public static class PowerPlexFusionKit
    {
        public static KitDefinition Build()
        {
            var loci = new List<string>
            {
                // Blue
                "AMEL","D3S1358","D1S1656","D2S441","D10S1248","D13S317","Penta E",
                // Green
                "D16S539","D18S51","D2S1338","CSF1PO","Penta D",
                // Yellow
                "TH01","vWA","D21S11","D7S820","D5S818","TPOX","DYS391",
                // Orange
                "D8S1179","D12S391","D19S433","FGA","D22S1045"
            };

            // Fusion's fifth (size standard) dye is not a marker channel and is not modeled.
            // The panel table calls the fourth marker dye "Red"; here it is Orange.
            var channels = new Dictionary<string, ChannelConfig>
            {
                ["Blue"] = new ChannelConfig(new List<string> { "AMEL", "D3S1358", "D1S1656", "D2S441", "D10S1248", "D13S317", "Penta E" }, RgbColor.Blue, 100),
                ["Green"] = new ChannelConfig(new List<string> { "D16S539", "D18S51", "D2S1338", "CSF1PO", "Penta D" }, RgbColor.Green, 150),
                ["Yellow"] = new ChannelConfig(new List<string> { "TH01", "vWA", "D21S11", "D7S820", "D5S818", "TPOX", "DYS391" }, RgbColor.Goldenrod, 150),
                ["Orange"] = new ChannelConfig(new List<string> { "D8S1179", "D12S391", "D19S433", "FGA", "D22S1045" }, RgbColor.Orange, 175),
            };

            // LocusParams(offset, bp per repeat, efficiency).
            // Offset and bp-per-repeat are Fusion's own size regression
            // (size = offset + bp-per-repeat * allele); offset equals each
            // locus's smallest ladder allele size minus bp-per-repeat * that allele.
            // NOTE: the third value (efficiency) was not supplied, so it is a
            // neutral 1.00 everywhere. Replace with Fusion's own values if you
            // have them.
            // AMEL: the panel bin is 80.0-92.0 bp, so X is placed at 84.0 and
            // Y at 88.0 (4 bp apart, as in the other kits), centred in that
            // range. KitDefinition.CalculateMw reads X from this entry's offset.
            var locusParams = new Dictionary<string, LocusParams>
            {
                ["AMEL"] = new(84.0, 0.0, 1.00),
                ["D3S1358"] = new(57.0, 4.0, 1.00),
                ["D1S1656"] = new(115.0, 4.0, 1.00),
                ["D2S441"] = new(175.5, 4.0, 1.00),
                ["D10S1248"] = new(216.0, 4.0, 1.00),
                ["D13S317"] = new(275.2, 4.0, 1.00),
                ["Penta E"] = new(329.5, 5.0, 1.00),
                ["D16S539"] = new(57.0, 4.0, 1.00),
                ["D18S51"] = new(101.1, 4.0, 1.00),
                ["D2S1338"] = new(178.0, 4.0, 1.00),
                ["CSF1PO"] = new(289.0, 4.0, 1.00),
                ["Penta D"] = new(360.0, 5.0, 1.00),
                ["TH01"] = new(57.0, 4.0, 1.00),
                ["vWA"] = new(82.0, 4.0, 1.00),
                ["D21S11"] = new(102.0, 4.0, 1.00),
                ["D7S820"] = new(247.0, 4.0, 1.00),
                ["D5S818"] = new(292.5, 4.0, 1.00),
                ["TPOX"] = new(371.0, 4.0, 1.00),
                ["DYS391"] = new(423.6, 4.0, 1.00),
                ["D8S1179"] = new(44.0, 4.0, 1.00),
                ["D12S391"] = new(75.4, 4.0, 1.00),
                ["D19S433"] = new(170.7, 4.0, 1.00),
                ["FGA"] = new(201.0, 4.0, 1.00),
                ["D22S1045"] = new(399.0, 3.0, 1.00),
            };

            // Fusion's own allelic ladder, transcribed from the panel table.
            var ladder = new Dictionary<string, List<string>>
            {
                ["AMEL"] = Split("X,Y"),
                ["D3S1358"] = Split("9,10,11,12,13,14,15,16,17,18,19,20"),
                ["D1S1656"] = Split("9,10,11,12,13,14,14.3,15,15.3,16,16.3,17,17.3,18,18.3,19,19.3,20.3"),
                ["D2S441"] = Split("8,9,10,11,11.3,12,13,14,15,16,17"),
                ["D10S1248"] = Split("8,9,10,11,12,13,14,15,16,17,18,19"),
                ["D13S317"] = Split("5,6,7,8,9,10,11,12,13,14,15,16,17"),
                ["Penta E"] = Split("5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24"),
                ["D16S539"] = Split("4,5,6,7,8,9,10,11,12,13,14,15,16"),
                ["D18S51"] = Split("7,8,9,10,10.2,11,12,13,13.2,14,15,16,17,18,19,20,21,22,23,24,25,26,27"),
                ["D2S1338"] = Split("10,12,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28"),
                ["CSF1PO"] = Split("5,6,7,8,9,10,11,12,13,14,15,16"),
                ["Penta D"] = Split("2.2,3.2,5,6,7,8,9,10,11,12,13,14,15,16,17"),
                ["TH01"] = Split("3,4,5,6,7,8,9,9.3,10,11,13.3"),
                ["vWA"] = Split("10,11,12,13,14,15,16,17,18,19,20,21,22,23,24"),
                ["D21S11"] = Split("24,24.2,25,25.2,26,27,28,28.2,29,29.2,30,30.2,31,31.2,32,32.2,33,33.2,34,34.2,35,35.2,36,37,38"),
                ["D7S820"] = Split("5,6,7,8,9,10,11,12,13,14,15,16"),
                ["D5S818"] = Split("6,7,8,9,10,11,12,13,14,15,16,17,18"),
                ["TPOX"] = Split("4,5,6,7,8,9,10,11,12,13,14,15,16"),
                ["DYS391"] = Split("5,6,7,8,9,10,11,12,13,14,15,16"),
                ["D8S1179"] = Split("7,8,9,10,11,12,13,14,15,16,17,18,19"),
                ["D12S391"] = Split("14,15,16,17,17.3,18,18.3,19,20,21,22,23,24,25,26,27"),
                ["D19S433"] = Split("5.2,6.2,8,9,10,11,12,12.2,13,13.2,14,14.2,15,15.2,16,16.2,17,17.2,18,18.2"),
                ["FGA"] = Split("14,15,16,17,18,18.2,19,19.2,20,20.2,21,21.2,22,22.2,23,23.2,24,24.2,25,25.2,26,27,28,29,30,31.2,32.2,33.2,42.2,43.2,44.2,45.2,46.2,48.2,50.2"),
                ["D22S1045"] = Split("7,8,9,10,11,12,13,14,15,16,17,18,19,20"),
            };

            return new KitDefinition("POWERPLEX FUSION", loci, channels, locusParams, ladder);
        }

        private static List<string> Split(string csv) => new List<string>(csv.Split(','));
    }
}
