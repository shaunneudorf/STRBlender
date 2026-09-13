using System.Collections.Generic;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Common
{
    public static class KitConfig
    {
        // Loci lists
        public static readonly List<string> IdplusAutosomal = new()
        {
            "D8S1179","D21S11","D7S820","CSF1PO","D3S1358","TH01","D13S317",
            "D16S539","D2S1338","D19S433","vWA","TPOX","D18S51","AMEL",
            "D5S818","FGA"
        };

        public static readonly List<string> GlobalfilerAutosomal = new()
        {
            "D3S1358","vWA","D16S539","CSF1PO","TPOX","YINDEL","AMEL",
            "D8S1179","D21S11","D18S51","DYS391","D2S441","D19S433","TH01",
            "FGA","D22S1045","D5S818","D13S317","D7S820","SE33","D10S1248",
            "D1S1656","D12S391","D2S1338"
        };

        public static readonly Dictionary<string, List<string>> Kits = new()
        {
            ["IDPLUS"] = IdplusAutosomal,
            ["GLOBALFILER"] = GlobalfilerAutosomal,
        };

        // Database options
        public static readonly Dictionary<string, string> DatabaseOptions = new()
        {
            ["GF RCMP Caucasian"] = "RCMP Combined Caucasian.csv",
            ["GF RCMP Cree"] = "RCMP Cree (Saskatchewan).csv",
            ["GF RCMP Ojibwe"] = "RCMP Ojibwe (Northern Ontario).csv",
            ["GF RCMP Salishan"] = "RCMP Salishan (B.C.).csv",
            ["GF NIST African American"] = "NIST1036_AfAm.csv",
            ["GF NIST Asian"] = "NIST1036_Asian.csv",
            ["IDP South Asian"] = "South Asian.csv",
            ["IDP Russian"] = "IDP Russian.csv",
        };

        // Channel configurations
        public static readonly Dictionary<string, ChannelConfig> IdplusChannels = new()
        {
            ["Blue"] = new ChannelConfig(new List<string> { "D8S1179", "D21S11", "D7S820", "CSF1PO" }, RgbColor.Blue, 100),
            ["Green"] = new ChannelConfig(new List<string> { "D3S1358", "TH01", "D13S317", "D16S539", "D2S1338" }, RgbColor.Green, 150),
            ["Yellow"] = new ChannelConfig(new List<string> { "D19S433", "vWA", "TPOX", "D18S51" }, RgbColor.Goldenrod, 150),
            ["Red"] = new ChannelConfig(new List<string> { "AMEL", "D5S818", "FGA" }, RgbColor.Red, 175),
        };

        public static readonly Dictionary<string, ChannelConfig> GlobalfilerChannels = new()
        {
            ["Blue"] = new ChannelConfig(new List<string> { "D3S1358", "vWA", "D16S539", "CSF1PO", "TPOX" }, RgbColor.Blue, 100),
            ["Green"] = new ChannelConfig(new List<string> { "YINDEL", "AMEL", "D8S1179", "D21S11", "D18S51", "DYS391" }, RgbColor.Green, 150),
            ["Yellow"] = new ChannelConfig(new List<string> { "D2S441", "D19S433", "TH01", "FGA" }, RgbColor.Goldenrod, 150),
            ["Red"] = new ChannelConfig(new List<string> { "D22S1045", "D5S818", "D13S317", "D7S820", "SE33" }, RgbColor.Red, 175),
            ["Purple"] = new ChannelConfig(new List<string> { "D10S1248", "D1S1656", "D12S391", "D2S1338" }, RgbColor.Purple, 150),
        };
    }

    public record ChannelConfig(List<string> Loci, RgbColor Color, int Threshold);
}