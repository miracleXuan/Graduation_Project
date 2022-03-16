using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTSEngine.Entities;
using RTSEngine.ResourceExtension;
using RTSEngine.Event;
using RTSEngine.UnitExtension;
using RTSEngine.Model;

namespace RTSEngine.EntityComponent
{
    public class ResourceCollector : FactionEntityTargetProgressComponent<IResource>, IResourceCollector
    {
        #region Class Attributes
        protected IUnit unit { private set; get; }

        [SerializeField, Tooltip("What types of resources can be collected?")]
        private CollectableResourceData[] collectableResources = new CollectableResourceData[0];
        private IReadOnlyDictionary<ResourceTypeInfo, CollectableResourceData> collectableResourcesDic = null;

        protected IResourceManager resourceMgr { private set; get; } 
        #endregion

        #region Initializing/Terminating
        protected sealed override void OnProgressInit()
        {
            this.resourceMgr = gameMgr.GetService<IResourceManager>(); 

            unit = Entity as IUnit;

            if (unit.IsFree)
            {
                logger.LogError($"[{typeof(ResourceCollector).Name} - {Entity.Code}] This component can not be attached to a free unit. The unit must belong to a faction slot!");
                return;
            }

            // Allows for constant access time to collectable resource data rather than having to go through the list each time
            collectableResourcesDic = collectableResources.ToDictionary(cr => cr.type, cr => cr);
        }
        #endregion

        #region Handling Events: Collected Resource
        private void HandleTargetHealthUpdated(IEntity resource, HealthUpdateArgs args)
        {
            if (args.Source != unit
                || resource != Target.instance)
                return;

            if (Target.instance.CanAutoCollect && !unit.IsFree)
            {
                resourceMgr.UpdateResource(
                    unit.FactionID,
                    new ResourceInput
                    {
                        type = Target.instance.ResourceType,
                        value = new ResourceTypeValue 
                        { 
                            amount = -args.Value,
                            capacity = 0
                        }
                    },
                    add: true);
                return;
            }


            unit.DropOffSource.UpdateCollectedResources(Target.instance.ResourceType, -args.Value);

            // Stop the collection audio
            audioMgr.StopSFX(unit.AudioSourceComponent);

            AttemptDropOff();
        }

        private void HandleTargetResourceDead(IEntity resource, DeadEventArgs e)
        {
            DisableProgress();

            if (unit.DropOffSource.IsValid() && !(resource as IResource).CanAutoCollect)
                unit.DropOffSource.AttemptStartDropOff(force: false, resourceType: (resource as IResource).ResourceType);
        }
        #endregion

        #region Updating Component State
        protected override bool MustStopProgress()
        {
            return (Target.instance.Health.IsDead && (Target.instance.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive))
                || (!Target.instance.CanCollectOutsideBorder && !RTSHelper.IsSameFaction(Target.instance, factionEntity))
                || (InProgress && !IsTargetInRange(transform.position, Target) && (Target.instance.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive));
        }

        protected override bool CanEnableProgress()
        {
            return IsTargetInRange(transform.position, Target)
                && (Target.instance.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive || unit.DropOffSource.State == DropOffState.goingBack);
        }

        protected override bool CanProgress() => Target.instance.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive;

        protected override bool MustDisableProgress() => !Target.instance.IsValid() || (!Target.instance.CanAutoCollect && unit.DropOffSource.State != DropOffState.inactive);

        // This defines that if the DropOffSource sent a SetIdle request to the entity, to start its drop off process, the collector component will not be disabled in the process
        public override bool CanStopOnSetIdleSource(IEntityTargetComponent idleSource) => idleSource != unit.DropOffSource;

        protected override bool CanStop() => InProgress && (Target.instance.CanAutoCollect || unit.DropOffSource.State == DropOffState.inactive);

        protected override void OnProgressStop()
        {
            inProgressObject =  null;

            sourceEffect = null;
            targetEffect = null;

            unit.DropOffSource?.Cancel();

            if (LastTarget.instance.IsValid())
            {
                LastTarget.instance.WorkerMgr.Remove(unit);

                LastTarget.instance.Health.EntityHealthUpdated -= HandleTargetHealthUpdated;
                LastTarget.instance.Health.EntityDead -= HandleTargetResourceDead;
            }
        }
        #endregion

        #region Drop Off Handling
        private void AttemptDropOff()
        {
            if (Target.instance.CanAutoCollect)
                return;

            if (unit.DropOffSource.AttemptStartDropOff(force: false, resourceType: Target.instance.ResourceType))
            {
                // Hide the source and target effect objects during drop off.
                ToggleSourceTargetEffect(false);

                DisableProgress();
                return;
            }
            else if(unit.DropOffSource.State != DropOffState.goingBack)
                // Cancel drop off if it was pending
                unit.DropOffSource.Cancel();
        }
        #endregion

        #region Handling Progress
        protected override void OnInProgressEnabled()
        {
            audioMgr.PlaySFX(unit.AudioSourceComponent, Target.instance.CollectionAudio, true);

            //unit is coming back after dropping off resources?
            if (!Target.instance.CanAutoCollect && unit.DropOffSource.State == DropOffState.goingBack)
                unit.DropOffSource.Cancel();
            else
                globalEvent.RaiseEntityComponentTargetStartGlobal(this, new TargetDataEventArgs(Target));

            unit.MovementComponent.UpdateRotationTarget(Target.instance, Target.instance.transform.position);
        }

        protected override void OnProgress()
        {
            Target.instance.Health.Add(new HealthUpdateArgs(-collectableResourcesDic[Target.instance.ResourceType].amount, unit));
        }
        #endregion

        #region Searching/Updating Target
        public override bool CanSearch => true;

        public override float GetProgressRange()
            => Target.instance.WorkerMgr.GetOccupiedPosition(unit, out _)
            ? mvtMgr.StoppingDistance
            : base.GetProgressRange();
        public override Vector3 GetProgressCenter() 
            => Target.instance.WorkerMgr.GetOccupiedPosition(unit, out Vector3 workerPosition)
            ? workerPosition
            : base.GetProgressCenter();

        public override bool IsTargetInRange(Vector3 sourcePosition, TargetData<IEntity> target)
        {
            if (!target.instance.IsValid() ||
                !(target.instance is IResource))
            {
                return false;
            }

            return (target.instance as IResource).WorkerMgr.GetOccupiedPosition(unit, out Vector3 workerPosition)
                ? Vector3.Distance(sourcePosition, workerPosition) <= mvtMgr.StoppingDistance
                : base.IsTargetInRange(sourcePosition, target);
        }

        public bool IsResourceTypeCollectable(ResourceTypeInfo resourceType)
        {
            return collectableResourcesDic != null
                ? collectableResourcesDic.ContainsKey(resourceType)
                : collectableResources.Select(cr => cr.type == resourceType).Any();
        }

        public override ErrorMessage IsTargetValid (TargetData<IEntity> testTarget, bool playerCommand)
        {
            TargetData<IResource> potentialTarget = testTarget;

            if (!potentialTarget.instance.IsValid())
                return ErrorMessage.invalid;
            else if (!factionEntity.IsInteractable)
                return ErrorMessage.invalid;
            else if (!potentialTarget.instance.IsInteractable)
                return ErrorMessage.uninteractable;
            else if (!potentialTarget.instance.CanCollect)
                return ErrorMessage.resourceNotCollectable;
            else if (!potentialTarget.instance.CanCollectOutsideBorder && !potentialTarget.instance.IsFriendlyFaction(factionEntity))
                return ErrorMessage.resourceTargetOutsideTerritory;
            else if (!IsResourceTypeCollectable(potentialTarget.instance.ResourceType))
                return ErrorMessage.entityCompTargetPickerUndefined;
            else if (potentialTarget.instance.Health.IsDead)
                return ErrorMessage.dead;
            // Check if this is not the same resource that is being collected before checking if it has max collectors (in case player is asking the unit to collect the resource it is actively collecting).
            else if (!factionEntity.CanMove() && !IsTargetInRange(transform.position, potentialTarget))
                return ErrorMessage.entityCompTargetOutOfRange;
            else if (unit.DropOffSource.HasReachedMaxCapacity(potentialTarget.instance.ResourceType))
            {
                unit.DropOffSource.AttemptStartDropOff(force: true, potentialTarget.instance.ResourceType);
                return ErrorMessage.dropOffMaxCapacityReached;
            }

            return potentialTarget.instance.WorkerMgr.CanMove(
                unit,
                new AddableUnitData
                {
                    allowDifferentFaction = true
                });
        }

        protected override void OnTargetPostLocked(bool playerCommand, bool sameTarget)
        {
            if (!logger.RequireTrue(Target.instance.CanAutoCollect || unit.DropOffSource != null,
                $"[{GetType().Name} - {Entity.Code}] A component that extends {typeof(IDropOffSource).Name} interface must be attached to this resource collector since resources can not be auto collected!"))
                return;

            var handler = collectableResourcesDic[Target.instance.ResourceType];
            // In this component, the in progress object depends on the type of resource that is being collected.
            inProgressObject = handler.enableObject;
            progressOverrideController = handler.animatorOverrideController;
            progressEnabledAudio = handler.enableAudio;
            sourceEffect = handler.sourceEffect;
            targetEffect = handler.targetEffect;

            if (Target.instance.WorkerMgr.Move(
                unit,
                new AddableUnitData(sourceTargetComponent: this, playerCommand, allowDifferentFaction: Target.instance.CanCollectOutsideBorder)) != ErrorMessage.none)
            {
                unit.DropOffSource?.Cancel();
                Stop();
                return;
            }

            // For the worker component manager, make sure that enough worker positions is available even in the local method.
            // Since they are only updated in the local method, meaning that the SetTarget method would always relay the input in case a lot of consecuive calls are made...
            //... on the same resource from multiple collectors.

            if (sameTarget)
            {
                AttemptDropOff();
                return;
            }

            Target.instance.Health.EntityHealthUpdated += HandleTargetHealthUpdated;
            Target.instance.Health.EntityDead += HandleTargetResourceDead;

            globalEvent.RaiseEntityComponentTargetLockedGlobal(this, new TargetDataEventArgs(Target));

            AttemptDropOff();
        }
        #endregion
    }
}
