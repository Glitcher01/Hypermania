using System.Collections.Generic;
using Design;
using Game.Sim;
using UnityEngine;
using Utils;

namespace Game.View
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class FighterView : MonoBehaviour
    {
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private CharacterConfig _characterConfig;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.speed = 0f;

            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Init(CharacterConfig characterConfig)
        {
            _characterConfig = characterConfig;
            _animator.runtimeAnimatorController = characterConfig.AnimationController;
        }

        public void Render(Frame frame, in FighterState state)
        {
            Vector3 pos = transform.position;
            pos.x = state.Position.x;
            pos.y = state.Position.y;
            transform.position = pos;

            _spriteRenderer.flipX = state.FacingDirection.x < 0f;

            CharacterAnimation animation = state.AnimState;
            int totalTicks = _characterConfig.GetHitboxData(animation).TotalTicks;

            // Default loop the animation, this is okay because any animations that aren't supposed to loop will be
            // ended by the FighterState
            int ticks = (frame - state.AnimSt) % totalTicks;

            _animator.Play(animation.ToString(), 0, (float)ticks / totalTicks);
            _animator.Update(0f); // force pose evaluation this frame while paused
        }
    }
}
