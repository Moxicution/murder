﻿using Bang;
using Bang.Entities;
using Bang.Interactions;
using System.Collections.Immutable;

namespace Murder.Interactions
{
    public readonly struct RemoveEntityOnInteraction : Interaction
    {
        public RemoveEntityOnInteraction() { }

        public void Interact(World world, Entity interactor, Entity interacted)
        {
            if (interacted.TryGetIdTarget()?.Target is int targetId &&
                world.TryGetEntity(targetId) is Entity targetEntity)
            {
                targetEntity.Destroy();
            }

            // Also delete all entities defined in a collection.
            if (interacted.TryGetIdTargetCollection()?.Targets is ImmutableDictionary<string, int> targets)
            {
                foreach (int entityId in targets.Values)
                {
                    if (world.TryGetEntity(entityId) is Entity otherTarget)
                    {
                        otherTarget.Destroy();
                    }
                }
            }
        }
    }
}
