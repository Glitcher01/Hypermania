using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Design;
using Design.Animation;
using MemoryPack;
using Netcode.Rollback;
using UnityEngine;
using Utils;

namespace Game.Sim
{
    [MemoryPackable]
    public partial class GameState : IState<GameState>
    {
        /// <summary>
        /// Data structures and other objects that the GameState might need to perform simulation that should not be
        /// serialized. All data structures/objects must be cleared by the end of the frame! You would typically put a
        /// heap-allocated object here to avoid allocating one every frame.
        /// </summary>
        public class GameStateCache
        {
            /// <summary>
            /// Physics context used to find collisions between boxes.
            /// </summary>
            public Physics<BoxProps> Physics;

            /// <summary>
            /// Cached list used to sort and process collisions, cleared at the end of every frame
            /// </summary>
            public List<Physics<BoxProps>.Collision> Collisions;
            public Dictionary<(int, int), Physics<BoxProps>.Collision> HurtHitCollisions;
        }

        [MemoryPackIgnore]
        public const int MAX_COLLIDERS = 100;

        public Frame Frame;
        public FighterState[] Fighters;

        /// <summary>
        /// Use this static builder instead of the constructor for creating new GameStates. This is because MemoryPack,
        /// which we use to serialize the GameState, places some funky restrictions on the constructor's paratmeter
        /// list.
        /// </summary>
        /// <param name="characterConfigs">Character configs to use</param>
        /// <returns>The created GameState</returns>
        public static (GameState, GameStateCache) Create(CharacterConfig[] characters)
        {
            GameState state = new GameState();
            state.Frame = Frame.FirstFrame;
            state.Fighters = new FighterState[characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                float xPos = i - ((float)characters.Length - 1) / 2;
                FighterFacing facing = xPos > 0 ? FighterFacing.Left : FighterFacing.Right;
                state.Fighters[i] = FighterState.Create(new Vector2(xPos, 0f), facing);
            }
            GameStateCache cache = new GameStateCache();
            cache.Physics = new Physics<BoxProps>(MAX_COLLIDERS);
            // There could be more than MAX_COLLIDERS collisions, but it is a good value to start with to ensure no
            // reallocations are done
            cache.Collisions = new List<Physics<BoxProps>.Collision>(MAX_COLLIDERS);
            cache.HurtHitCollisions = new Dictionary<(int, int), Physics<BoxProps>.Collision>();
            return (state, cache);
        }

        public void Advance(
            (GameInput input, InputStatus status)[] inputs,
            CharacterConfig[] characters,
            GameStateCache cache
        )
        {
            if (inputs.Length != characters.Length || characters.Length != Fighters.Length)
            {
                throw new InvalidOperationException("invalid inputs and characters to advance game state with");
            }
            Frame += 1;

            // This function internally appies changes to the fighter's Mode, which will be used to derive the animation
            // state later.
            for (int i = 0; i < Fighters.Length; i++)
            {
                Fighters[i].ApplyInputIntent(inputs[i].input, characters[i]);
            }

            // If a player applies inputs to start a move at the start of the frame, we wish to apply those inputs and
            // start the move immediately. Thus, we call CalculateSetAnimationState on the beginning (to handle state
            // changing based on the input), as well as on the end (to handle state changing based on
            // movement/collision, etc.) of a frame.
            for (int i = 0; i < Fighters.Length; i++)
            {
                Fighters[i].CalculateSetAnimationState(Frame);
            }

            // Each fighter then adds their hit/hurtboxes to the physics context, which will solve and find all
            // collisions. It is our job to then handle them.
            for (int i = 0; i < Fighters.Length; i++)
            {
                Fighters[i].AddBoxes(Frame, characters[i], cache.Physics, i);
            }

            // AdvanceProjectiles();

            cache.Physics.GetCollisions(cache.Collisions);

            // First, solve collisions that would result in player damage. There can only be one such collision per
            // (A, B) ordered pair, where A and B are players, projectiles, or other game objects. For now, we take the
            // first collision that happens this way. In the future, which collision we take should be given by a hitbox
            // priority (a stronger hitting move should be preferred over a projectile)
            //
            // If there are no collisions of that type, then find collisions between hitboxes. This would result in a
            // clank.
            //
            // Finally, if there are no clanks or player damage collisions, make sure that the characters are not
            // colliding. If they are, push them apart.
            Physics<BoxProps>.Collision? clank = null;
            Physics<BoxProps>.Collision? collide = null;
            foreach (var c in cache.Collisions)
            {
                (int, int) hitPair = (-1, -1);
                if (c.BoxA.Data.Kind == HitboxKind.Hitbox && c.BoxB.Data.Kind == HitboxKind.Hurtbox)
                {
                    hitPair = (c.BoxA.Owner, c.BoxB.Owner);
                }
                else if (c.BoxA.Data.Kind == HitboxKind.Hurtbox && c.BoxB.Data.Kind == HitboxKind.Hitbox)
                {
                    hitPair = (c.BoxB.Owner, c.BoxA.Owner);
                }
                else if (c.BoxA.Data.Kind == HitboxKind.Hitbox && c.BoxB.Data.Kind == HitboxKind.Hitbox)
                {
                    clank = c;
                }
                else if (c.BoxA.Data.Kind == HitboxKind.Hurtbox && c.BoxB.Data.Kind == HitboxKind.Hurtbox)
                {
                    collide = c;
                }
                // TODO: sort by priority or something
                if (hitPair != (-1, -1))
                {
                    cache.HurtHitCollisions[hitPair] = c;
                }
            }

            if (cache.HurtHitCollisions.Count > 0)
            {
                foreach (var c in cache.HurtHitCollisions.Values)
                {
                    HandleCollision(c);
                }
            }
            else if (clank.HasValue)
            {
                HandleCollision(clank.Value);
            }
            else if (collide.HasValue)
            {
                HandleCollision(collide.Value);
            }

            // Clear the physics context for the next frame, which will then re-add boxes and solve for collisions again
            cache.Physics.Clear();
            cache.Collisions.Clear();
            cache.HurtHitCollisions.Clear();

            // Apply any velocities set during movement or through knockback.
            for (int i = 0; i < Fighters.Length; i++)
            {
                Fighters[i].UpdatePosition(Frame);
            }

            // The second call to CalculateSetAnimationState: apply any changes as a result of collision calculations
            // and/or movement calculations.
            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].CalculateSetAnimationState(Frame);
            }

            // Tick the state machine, decreasing any forms of hitstun/blockstun and/or move timers, allowing us to
            // become actionable next frame, etc.
            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].TickStateMachine(Frame);
            }
        }

        private void HandleCollision(Physics<BoxProps>.Collision c)
        {
            if (c.BoxA.Data.Kind == HitboxKind.Hitbox && c.BoxB.Data.Kind == HitboxKind.Hurtbox)
            {
                Fighters[c.BoxB.Owner].ApplyHit(c.BoxA.Data);
            }
            if (c.BoxA.Data.Kind == HitboxKind.Hurtbox && c.BoxB.Data.Kind == HitboxKind.Hitbox)
            {
                Fighters[c.BoxA.Owner].ApplyHit(c.BoxB.Data);
            }
            if (c.BoxA.Data.Kind == HitboxKind.Hitbox && c.BoxB.Data.Kind == HitboxKind.Hitbox)
            {
                // TODO: check if moves are allowed to clank
                Fighters[c.BoxA.Owner].ApplyClank();
                Fighters[c.BoxB.Owner].ApplyClank();
            }
            if (c.BoxA.Data.Kind == HitboxKind.Hurtbox && c.BoxB.Data.Kind == HitboxKind.Hurtbox)
            {
                // TODO: more advanced pushing/hitbox handling, e.g. if someone airborne they shouldn't be able to be
                // pushed
                if (Fighters[c.BoxA.Owner].Position.x < Fighters[c.BoxB.Owner].Position.x)
                {
                    Fighters[c.BoxA.Owner].Position.x -= c.OverlapX / 2;
                    Fighters[c.BoxB.Owner].Position.x += c.OverlapX / 2;
                }
                else
                {
                    Fighters[c.BoxA.Owner].Position.x += c.OverlapX / 2;
                    Fighters[c.BoxB.Owner].Position.x -= c.OverlapX / 2;
                }
            }
        }

        [ThreadStatic]
        private static ArrayBufferWriter<byte> _writer;
        private static ArrayBufferWriter<byte> Writer
        {
            get
            {
                if (_writer == null)
                    _writer = new ArrayBufferWriter<byte>(256);
                return _writer;
            }
        }

        public ulong Checksum()
        {
            Writer.Clear();
            MemoryPackSerializer.Serialize(Writer, this);
            ReadOnlySpan<byte> bytes = Writer.WrittenSpan;

            // 64-bit FNV-1a over the serialized bytes
            const ulong OFFSET = 14695981039346656037UL;
            const ulong PRIME = 1099511628211UL;

            ulong hash = OFFSET;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= PRIME;
            }
            return hash;
        }
    }
}
