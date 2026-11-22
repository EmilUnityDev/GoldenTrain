using SlotsLogic.Match3Slots.Scripts.Combinations;

namespace SlotsLogic.Match3Slots.Scripts.Interfaces
{
    public interface IDataStorage
    {
        bool HasExistingData();
        void Save(AllColumnsData data);
        AllColumnsData Load();
    }
}