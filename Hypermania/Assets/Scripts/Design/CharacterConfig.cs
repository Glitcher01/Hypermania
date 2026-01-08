using System;
using Design.Animation;
using Game;
using Game.View;
using UnityEngine;

namespace Design
{
    [CreateAssetMenu(menuName = "Hypermania/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        public Character Character;
        public FighterView Prefab;
        public AnimatorOverrideController AnimationController;
        public float Speed;
        public float JumpVelocity;
        public HitboxData Walk;
        public HitboxData Idle;
        public HitboxData LightAttack;
        public HitboxData Jump;

        // TODO: many more

        public FrameData GetFrameData(CharacterAnimation anim, int tick)
        {
            HitboxData data = GetHitboxData(anim);
            // By default loop the animation, but this should never happen because we would have switched to a different
            // state in the fighter state for ones that should not loop
            tick = ((tick % data.TotalTicks) + data.TotalTicks) % data.TotalTicks;
            return data.Frames[tick];
        }

        public HitboxData GetHitboxData(CharacterAnimation anim)
        {
            switch (anim)
            {
                case CharacterAnimation.Walk:
                    return Walk;
                case CharacterAnimation.Idle:
                    return Idle;
                case CharacterAnimation.Jump:
                    return Jump;
                case CharacterAnimation.LightAttack:
                    return LightAttack;
                default:
                    throw new InvalidOperationException(
                        $"Tried to get hitbox data for {anim} (not registered). Did you add a new type of animation and forget to add it to CharacterConfig.GetHitboxData()?"
                    );
            }
        }
    }
}
