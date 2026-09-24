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

            // Same k/b/a/efficiency/min-max values as the original
            // LocusDefinitions.GlobalfilerLoci table.
            var locusParams = new Dictionary<string, LocusParams>
            {
                ["D3S1358"] = new(9, 96.48, 4.15, 1.0, 96.47, 141.54),
                ["vWA"] = new(11, 156.57, 4.07, 1.0, 156.55, 209.44),
                ["D16S539"] = new(5, 227.36, 4.06, 1.0, 227.32, 267.89),
                ["CSF1PO"] = new(6, 283.215, 4.00, 1.0, 283.17, 319.25),
                ["TPOX"] = new(5, 338.405, 4.03, 1.0, 338.30, 378.92),
                ["YINDEL"] = new(0, 0, 0.0, 1.0, 78.91, 88.31),
                ["AMEL"] = new(0, 106.0, 0.0, 1.00, 104.0, 110.69),
                ["D8S1179"] = new(5, 114.165, 4.06, 1.0, 114.15, 171.33),
                ["D21S11"] = new(24, 183.015, 4.08, 1.0, 182.98, 239.82),
                ["D18S51"] = new(7, 261.25, 4.05, 1.0, 261.21, 342.42),
                ["DYS391"] = new(7, 365.15, 4.03, 1.0, 365.11, 389.37),
                ["D2S441"] = new(8, 76.575, 4.10, 1.0, 76.55, 113.68),
                ["D19S433"] = new(6, 118.535, 3.95, 1.0, 118.52, 171.61),
                ["TH01"] = new(4, 179.205, 4.05, 1.0, 179.17, 210.44),
                ["FGA"] = new(13, 223.465, 4.03, 1.0, 218.00, 378.41),
                ["D22S1045"] = new(8, 88.31, 3.99, 1.0, 88.29, 135.00),
                ["D5S818"] = new(7, 138.59, 4.08, 1.0, 138.58, 183.37),
                ["D13S317"] = new(5, 198.975, 4.06, 1.0, 198.96, 243.32),
                ["D7S820"] = new(6, 262.575, 4.00, 1.0, 262.55, 298.49),
                ["SE33"] = new(8, 321.58, 4.06, 1.0, 307.19, 440.0),
                ["D10S1248"] = new(8, 85.385, 4.05, 1.0, 85.37, 129.75),
                ["D1S1656"] = new(9, 159.99, 4.02, 1.0, 159.98, 207.38),
                ["D12S391"] = new(14, 216.575, 4.05, 1.0, 216.54, 268.63),
                ["D2S1338"] = new(11, 281.73, 4.05, 1.0, 281.68, 349.95),
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
