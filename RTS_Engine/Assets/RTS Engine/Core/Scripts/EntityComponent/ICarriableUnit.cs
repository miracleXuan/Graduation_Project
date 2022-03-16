using RTSEngine.Model;
using RTSEngine.UnitExtension;

namespace RTSEngine.EntityComponent
{
    public interface ICarriableUnit : IEntityTargetComponent
    {
        IUnitCarrier CurrCarrier { get; }
        ModelCacheAwareTransformInput CurrSlot { get; }
        int CurrSlotID { get; }
        bool AllowMovementToExitCarrier { get; }

        AddableUnitData GetAddableData(bool playerCommand);
        ErrorMessage SetTarget(IUnitCarrier carrier, AddableUnitData addableData);
    }
}