﻿using Assimp;
using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using Murder.Assets.Graphics;
using Murder.Attributes;
using Murder.Components;
using Murder.Core.Geometry;
using Murder.Core.Graphics;
using Murder.Diagnostics;
using Murder.Helpers;
using Murder.Messages;
using Murder.Services;
using Murder.Utilities;

namespace Murder.Systems
{
    [Filter(ContextAccessorFilter.AllOf, typeof(ITransformComponent), typeof(AgentSpriteComponent), typeof(FacingComponent))]
    [ShowInEditor]
    public class AgentAnimatorSystem : IMonoRenderSystem
    {
        public ValueTask Draw(RenderContext render, Context context)
        {
            foreach (var e in context.Entities)
            {
                IMurderTransformComponent transform = e.GetGlobalTransform();
                if (!render.Camera.SafeBounds.Contains(transform.Vector2))
                {
                    continue;
                }

                AgentSpriteComponent sprite = e.GetAgentSprite();
                FacingComponent facing = e.GetFacing();

                var ySort = RenderServices.YSort(transform.Y + sprite.YSortOffset);
                Vector2 impulse = Vector2.Zero;

                if (e.TryGetAgentImpulse() is AgentImpulseComponent imp) impulse = imp.Impulse;

                float start = NoiseHelper.Simple01(e.EntityId * 10) * 5f;
                var prefix = sprite.IdlePrefix;

                if (impulse.HasValue)
                    prefix = sprite.WalkPrefix;

                AnimationOverloadComponent? overload = null;
                if (e.TryGetAnimationOverload() is AnimationOverloadComponent o)
                {
                    overload = o;
                    prefix = $"{o.CurrentAnimation}_";
                    start = o.Start;
                }


                if (Game.Data.GetAsset<AsepriteAsset>(sprite.AnimationGuid) is AsepriteAsset asepriteAsset)
                {
                    var suffix = facing.Direction.ToCardinal();
                    bool flip = false;

                    if (!asepriteAsset.Animations.ContainsKey(prefix + facing.Direction))
                        (suffix, flip) = facing.Direction.ToCardinalFlipped();

                    float speed = overload?.Duration ?? -1;
                    AnimationSpeedOverload? speedOverload = e.TryGetAnimationSpeedOverload();

                    if (speedOverload is not null)
                    {
                        if (speed > 0)
                            speed = speed * speedOverload.Value.Rate;
                        else
                        {
                            if (asepriteAsset.Animations.TryGetValue(prefix + suffix, out var animation))
                            {
                                speed = animation.AnimationDuration / speedOverload.Value.Rate;
                            }
                        }
                    }

                    Microsoft.Xna.Framework.Vector3 blend;
                    // Handle flashing
                    if (e.HasFlashSprite())
                    {
                        blend = RenderServices.BLEND_WASH;
                    }
                    else
                    {
                        blend = RenderServices.BLEND_NORMAL;
                    }

                    var complete = RenderServices.RenderSprite(
                        render.GameplayBatch,
                        render.Camera,
                        transform.ToVector2(),
                        prefix + suffix,
                        asepriteAsset,
                        start,
                        speed,
                        Vector2.Zero,
                        flip,
                        0,
                        Vector2.One,
                        Color.White.WithAlpha(1f),
                        blend,
                        ySort
                        );

                    if (complete && overload != null)
                    {
                        if (!overload.Value.Loop)
                        {
                            e.RemoveAnimationOverload();
                            e.SendMessage<AnimationCompleteMessage>();
                        }
                        else
                        {
                            e.ReplaceComponent(overload.Value.PlayNext());
                        }

                        if (speedOverload is not null && speedOverload.Value.Persist)
                        {
                            e.RemoveAnimationSpeedOverload();
                        }
                    }
                }
            }

            return default;
        }
    }
}
