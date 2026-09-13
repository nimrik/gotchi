using Gotchi.Data;

namespace Gotchi.Persistence
{
    public interface ISaveService
    {
        bool HasSave();
        PetSaveData Load();
        void Save(PetSaveData data);
        void Delete();
    }
}
