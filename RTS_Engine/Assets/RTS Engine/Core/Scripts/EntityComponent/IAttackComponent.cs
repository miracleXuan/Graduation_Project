using RTSEngine.Attack;
using RTSEngine.BuildingExtension;
using RTSEngine.Entities;
using RTSEngine.Model;
using System.Collections.Generic;
using UnityEngine;

namespace RTSEngine.EntityComponent
{
    public interface IAttackComponent : IEntityTargetComponent
    {
        IBorder SearchRangeCenter { set; }
        bool Revert { get; }

        bool RequireTarget { get; }

        AttackFormationSelector Formation { get; }
        AttackWeapon Weapon { get; }
        AttackDamage Damage { get; }
        AttackLauncher Launcher { get; }
        AttackLOS LineOfSight { get; }

        bool IsInTargetRange { get; }

        AttackEngagementOptions EngageOptions { get; }

        TargetData<IFactionEntity> Target { get; }

        ModelCacheAwareTransformInput WeaponTransform { get; }
        bool IsLocked { get; }

        ErrorMessage CanSwitchAttack();
        ErrorMessage LockAttackAction(bool locked, bool playerCommand);
        ErrorMessage SetNextLaunchLogActionLocal(IReadOnlyCollection<AttackObjectLaunchLogInput> nextLaunchLog, IFactionEntity target, bool playerCommand);
        ErrorMessage SwitchAttackAction(bool playerCommand);

        void TriggerAttack();
    }
}
