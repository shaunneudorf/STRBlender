using System;
using System.Collections.Generic;

namespace STRBlender.Core.Domain.Common
{
    public static class LocusDefinitions
    {
        private static Dictionary<string, LocusParams> _activeLoci = new();

        static LocusDefinitions()
        {
            _activeLoci = IdplusLoci;
        }

        public static void SetKit(string kitName)
        {
            _activeLoci = kitName?.ToUpper() == "GLOBALFILER" ? GlobalfilerLoci : IdplusLoci;
        }

        public static double? CalculateMw(string locus, string allele)
        {
            if (locus == "AMEL")
            {
                return allele == "X" ? 106.0 : allele == "Y" ? 110.0 : null;
            }
            if (locus == "YINDEL")
            {
                return allele == "1" ? 81.075 : allele == "2" ? 86.345 : null;
            }

            if (!_activeLoci.TryGetValue(locus, out var p))
                return null;

            if (!double.TryParse(allele, out double alleleNum))
                return null;

            return AlleleToMw(alleleNum, p.K, p.B, p.A);
        }

        public static double GetEfficiency(string locus)
        {
            return _activeLoci.TryGetValue(locus, out var p) ? p.Efficiency : 1.0;
        }

        public static (double? MinMw, double? MaxMw) GetLocusBounds(string locus)
        {
            if (_activeLoci.TryGetValue(locus, out var p))
                return (p.MinMW, p.MaxMW);
            return (null, null);
        }

        private static double AlleleToMw(double allele, double k, double b, double a)
        {
            int integerPart = (int)allele;
            double fractionalPart = allele - integerPart;
            return (integerPart - k) * a + (fractionalPart * 10) + b;
        }

        // ====================== LOCUS DATA ======================
        private static readonly Dictionary<string, LocusParams> IdplusLoci = new()
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

        private static readonly Dictionary<string, LocusParams> GlobalfilerLoci = new()
        {
            ["D3S1358"] = new(9, 96.48, 4.15, 1.0, 90.5, 146.5),
            ["vWA"] = new(11, 156.57, 4.07, 1.0, 151.0, 215.0),
            ["D16S539"] = new(5, 227.36, 4.06, 1.0, 221.5, 273.5),
            ["CSF1PO"] = new(6, 283.215, 4.00, 1.0, 277.0, 325.0),
            ["TPOX"] = new(5, 338.405, 4.03, 1.0, 332.5, 384.5),
            ["YINDEL"] = new(0, 81.075, 0.0, 1.0, 78.0, 92.0),
            ["AMEL"] = new(0, 106.0, 0.0, 1.00, 104.0, 110.0),
            ["D8S1179"] = new(5, 114.165, 4.06, 1.0, 111.0, 176.5),
            ["D21S11"] = new(24, 183.015, 4.08, 1.0, 179.5, 246.5),
            ["D18S51"] = new(7, 261.25, 4.05, 1.0, 255.5, 347.5),
            ["DYS391"] = new(7, 365.15, 4.03, 1.0, 359.5, 395.5),
            ["D2S441"] = new(8, 76.575, 4.10, 1.0, 75.0, 113.5),
            ["D19S433"] = new(6, 118.535, 3.95, 1.0, 115.5, 173.5),
            ["TH01"] = new(4, 179.205, 4.05, 1.0, 174.5, 219.5),
            ["FGA"] = new(13, 223.465, 4.03, 1.0, 221.0, 380.0),
            ["D22S1045"] = new(8, 88.31, 3.99, 1.0, 83.5, 126.5),
            ["D5S818"] = new(7, 138.59, 4.08, 1.0, 133.5, 189.5),
            ["D13S317"] = new(5, 198.975, 4.06, 1.0, 197.0, 249.0),
            ["D7S820"] = new(6, 262.575, 4.00, 1.0, 250.0, 304.0),
            ["SE33"] = new(8, 321.58, 4.06, 1.0, 306.0, 450.0),
            ["D10S1248"] = new(8, 85.385, 4.05, 1.0, 83.0, 123.0),
            ["D1S1656"] = new(9, 159.99, 4.02, 1.0, 125.0, 205.0),
            ["D12S391"] = new(14, 216.575, 4.05, 1.0, 207.0, 295.0),
            ["D2S1338"] = new(11, 281.73, 4.05, 1.0, 297.0, 375.0),
        };
    }

    public class LocusParams
    {
        public double K { get; }
        public double B { get; }
        public double A { get; }
        public double Efficiency { get; }
        public double? MinMW { get; }
        public double? MaxMW { get; }

        public LocusParams(double k, double b, double a, double efficiency, double? minMW, double? maxMW)
        {
            K = k;
            B = b;
            A = a;
            Efficiency = efficiency;
            MinMW = minMW;
            MaxMW = maxMW;
        }
    }
}