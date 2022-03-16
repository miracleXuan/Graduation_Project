using UnityEngine;

using RTSEngine.EntityComponent;
using RTSEngine.Game;
using System.Collections.Generic;
using RTSEngine.Entities;

namespace RTSEngine.Upgrades
{
    [System.Serializable]
    public struct EntityComponentUpgradeElementSource
    {
        [Space(), SerializeField, EntityComponentCode(0, "sourceEntity"), Tooltip("Code of the component to upgrade (if there is one).")]
        private string sourceComponentCode;

        public string GetSourceCode(IEntity sourceEntity)
        {
            return GetSourceComponent(sourceEntity).IsValid() ? GetSourceComponent(sourceEntity).Code : "";
        }

        public IEntityComponent GetSourceComponent(IEntity sourceEntity)
        {
            RTSHelper.TryGetEntityComponentWithCode(sourceEntity, sourceComponentCode, out IEntityComponent component);
            return component;
        }

        [SerializeField, EnforceType(typeof(IEntityComponent)), Tooltip("The upgrade entity component target.")]
        private GameObject upgradeTarget;
        public IEntityComponent UpgradeTarget => upgradeTarget.IsValid() ? upgradeTarget.GetComponent<IEntityComponent>() : null;

        [Space(), SerializeField, Tooltip("Upgrades to trigger/launch when this upgrade is completed.")]
        private TriggerUpgrade[] triggerUpgrades;
        public IEnumerable<TriggerUpgrade> TriggerUpgrades => triggerUpgrades;
    }

    public class EntityComponentUpgrade : Upgrade
    {
        [SerializeField, Tooltip("Define the possible component upgrades that can be launched from this component.")]
        private EntityComponentUpgradeElementSource[] upgrades = new EntityComponentUpgradeElementSource[0];
        public IEnumerable<EntityComponentUpgradeElementSource> AllUpgrades => upgrades;

        public EntityComponentUpgradeElementSource GetUpgrade(int index)
        {
            if (index.IsValidIndex(upgrades))
                return upgrades[index];

            string errorMsg = $"[EntityComponentUprade - {SourceEntity?.Code}] Unable to fetch upgrade of invalid index {index}";
            if (RTSHelper.LoggingService.IsValid())
                RTSHelper.LoggingService.LogError(errorMsg, source: this);
            else
                Debug.LogError($"[RTSEditorHelper] {errorMsg}");
            return default;
        }

        public override void LaunchLocal(IGameManager gameMgr, int upgradeIndex, int factionID)
        {
            gameMgr.GetService<IEntityComponentUpgradeManager>().LaunchLocal(this, GetUpgrade(upgradeIndex), factionID);
        }
    }
}
