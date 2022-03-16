using UnityEngine;

using RTSEngine.Entities;

namespace RTSEngine.EntityComponent
{
        [System.Serializable]
        public class AdvancedFactionEntityTargetPicker : FactionEntityTargetPicker
        {
            [SerializeField, Tooltip("Allow to target units?")]
            private bool targetUnits = true;

            [SerializeField, Tooltip("Allow to target units that are in range of the faction entity but not stored in it?")]
            private bool targetExternal = true;
            [SerializeField, Tooltip("Allow to target units stored inside the same faction entity?")]
            private bool targetStored = true;

            [SerializeField, Tooltip("Allow to target buildings?")]
            private bool targetBuildings = true;

        public bool IsValidTarget(IEntityTargetComponent component, IFactionEntity factionEntity)
        {
            if (factionEntity.IsUnit())
            {
                if (!targetUnits)
                    return false;

                IUnit unit = factionEntity as IUnit;
                IFactionEntity healerEntity = component.Entity as IFactionEntity;

                // Unit has an active carrier where it stored
                if (unit.CarriableUnit.IsValid() && unit.CarriableUnit.CurrCarrier.IsValid())
                {
                    // If the carrier is different than the source or it is the source but we can not target stored units
                    if (!healerEntity.UnitCarrier.IsUnitStored(unit) || !targetStored)
                        return false;
                }
                else if (!targetExternal) // Unit is not carried by a UnitCarrier but we can not target non-stored units
                    return false;
            }
            else if (factionEntity.IsBuilding() && !targetBuildings)
                return false;

            return base.IsValidTarget(factionEntity);
        }
    }
}
