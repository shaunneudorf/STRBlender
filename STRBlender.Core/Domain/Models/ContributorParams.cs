namespace STRBlender.Core.Domain.Models
{
    public record ContributorParams(
        double Proportion,
        double Degradation,
        string Database,
        string Relationship,
        bool Locked,
        SexType Sex
    );
}