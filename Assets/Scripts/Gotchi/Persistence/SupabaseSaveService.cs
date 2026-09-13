using System;
using Gotchi.Data;

namespace Gotchi.Persistence
{
    // Placeholder for the production backend. Intended implementation: a `pets` table keyed by
    // the anonymous device/player id, read/written through Supabase's REST (PostgREST) endpoint
    // with UnityWebRequest and the anon key, and row-level security so a player can only
    // touch their own row. Not wired up for the MVP by design — see 04-tech-plan.md.
    public class SupabaseSaveService : ISaveService
    {
        private readonly string _projectUrl;
        private readonly string _anonKey;

        public SupabaseSaveService(string projectUrl, string anonKey)
        {
            _projectUrl = projectUrl;
            _anonKey = anonKey;
        }

        public bool HasSave() => throw NotReady();
        public PetSaveData Load() => throw NotReady();
        public void Save(PetSaveData data) => throw NotReady();
        public void Delete() => throw NotReady();

        private static NotImplementedException NotReady() =>
            new NotImplementedException("SupabaseSaveService is a stub. Use LocalJsonSaveService for the MVP.");
    }
}
