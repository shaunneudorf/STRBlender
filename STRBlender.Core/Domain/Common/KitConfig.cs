using System.Collections.Generic;
using System.Linq;
using STRBlender.Core.Domain.Models;

namespace STRBlender.Core.Domain.Common
{
    /// Kept as a thin, backward-compatible facade so existing call sites
    /// (Kits[...], IdplusChannels, GlobalfilerChannels) keep working
    /// unchanged. The actual data now lives in one place per kit —
    /// KitRegistry / KitDefinition — this class just re-shapes it into the
    /// old lookup forms. New code should prefer KitRegistry.Get(kitName)
    /// directly.
    public static class KitConfig
    {
        public static readonly Dictionary<string, List<string>> Kits =
            KitRegistry.KitNames.ToDictionary(name => name, name => KitRegistry.Get(name).Loci);

        public static readonly Dictionary<string, ChannelConfig> IdplusChannels =
            KitRegistry.Get("IDPLUS").Channels;

        public static readonly Dictionary<string, ChannelConfig> GlobalfilerChannels =
            KitRegistry.Get("GLOBALFILER").Channels;

        // Database options
        public static readonly Dictionary<string, string> DatabaseOptions = new()
        {
            ["FBI Caucasian"] = "FBI_Caucasian.csv",
            ["FBI African American"] = "FBI_African American.csv",
            ["FBI SE Hispanic"] = "FBI_SE Hispanic.csv",
            ["FBI SW Hispanic"] = "FBI_SW Hispanic.csv",
            ["FBI Apache"] = "FBI_Apache.csv",
            ["FBI Navajo"] = "FBI_Navajo.csv",
            ["FBI Bahamian"] = "FBI_Bahamian.csv",
            ["FBI Jamaican"] = "FBI_Jamaican.csv",
            ["FBI Trinidadian"] = "FBI_Trinidadian.csv",
            ["FBI Chamorro"] = "FBI_Chamorro.csv",
            ["FBI Filipino"] = "FBI_Filipino.csv",
            ["NIST African American"] = "NIST1036_AfAm.csv",
            ["NIST Asian"] = "NIST1036_Asian.csv",
            ["NIST Caucasian"] = "NIST1036_Cauc.csv",
            ["NIST Hispanic"] = "NIST1036_Hisp.csv",
            ["Xinjiang Uyghur"] = "Xinjiang Uyghur.csv",
            ["Chinese Qiang"] = "Chinese Qiang.csv",
            ["Kenyan Bantu"] = "Kenyan Bantu.csv",
            ["Ethiopian Tigray"] = "Ethiopian Tigray.csv",
        };
    }

    public record ChannelConfig(List<string> Loci, RgbColor Color, int Threshold);
}
