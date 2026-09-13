using System.Collections.Generic;

namespace STRBlender.Core.Domain.Models
{
    /// Holds every generated family member for one C1-centered pedigree,
    /// keyed by role (e.g. "Mother", "PaternalGrandfather", "Sibling1").
    /// Scoped to a single simulation run — built fresh each time, not persisted.
    public class Pedigree
    {
        private readonly Dictionary<string, Profile> _members = new();

        public void Set(string role, Profile profile) => _members[role] = profile;

        public bool TryGet(string role, out Profile profile) =>
            _members.TryGetValue(role, out profile!);

        public Profile Get(string role) => _members[role];

        public bool Contains(string role) => _members.ContainsKey(role);
    }
}
