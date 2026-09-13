namespace STRBlender.Core.Domain.Models
{
    public class Peak
    {
        public string Locus { get; set; } = "";
        public string Allele { get; set; } = "";
        public double? Mw { get; set; }
        public double Height { get; set; }
        public double BaseHeight { get; set; }
        public string PeakType { get; set; } = "parent";
    }
}