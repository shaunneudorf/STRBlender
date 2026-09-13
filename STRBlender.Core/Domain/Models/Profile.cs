using System.Collections.Generic;

namespace STRBlender.Core.Domain.Models
{
    public class Profile
    {
        public Dictionary<string, (string A1, string A2)> Loci { get; } = new();
    }
}