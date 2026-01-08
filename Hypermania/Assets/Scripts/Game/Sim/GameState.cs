using System;
using System.Buffers;
using Design;
using MemoryPack;
using Netcode.Rollback;
using UnityEngine;
using Utils;

namespace Game.Sim
{
    [MemoryPackable]
    public partial class GameState : IState<GameState>
    {
        public Frame Frame;
        public FighterState[] Fighters;

        /// <summary>
        /// Use this static builder instead of the constructor for creating new GameStates. This is because MemoryPack,
        /// which we use to serialize the GameState, places some funky restrictions on the constructor's paratmeter
        /// list.
        /// </summary>
        /// <param name="characterConfigs">Character configs to use</param>
        /// <returns>The created GameState</returns>
        public static GameState Create(CharacterConfig[] characters)
        {
            GameState state = new GameState();
            state.Frame = Frame.FirstFrame;
            state.Fighters = new FighterState[characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                float xPos = i - ((float)characters.Length - 1) / 2;
                Vector2 facing = xPos > 0 ? Vector2.right : Vector2.left;
                state.Fighters[i] = FighterState.Create(new Vector2(xPos, -4.5f), facing, characters[0]);
            }
            return state;
        }

        public void Advance((GameInput input, InputStatus status)[] inputs, CharacterConfig[] characters)
        {
            if (inputs.Length != characters.Length || characters.Length != Fighters.Length)
            {
                throw new InvalidOperationException("invalid inputs and characters to advance game state with");
            }
            Frame += 1;

            // This function internally appies changes to the fighter's Mode, which will be used to derive the animation
            // state later.
            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].ApplyInputIntent(inputs[i].input, characters[i]);
            }

            // This function internally appies changes to the fighter's position, which will be used to derive the
            // animation state later.
            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].UpdatePosition(Frame);
            }

            // If a player applies inputs to start a move at the start of the frame, we wish to apply those inputs and
            // start the move immediately. Thus, we call CalculateSetAnimationState on the beginning (to handle state
            // changing based on the input), as well as on the end (to handle state changing based on a collision, etc.)
            // of a frame.
            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].CalculateSetAnimationState(Frame);
            }

            // UpdateBoxes();

            // AdvanceProjectiles();

            // DetectCollisions();

            // ResolveCollisions();

            // ApplyHitResult();

            // The second call to CalculateSetAnimationState: apply any changes as a result of collision calculations.
            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].CalculateSetAnimationState(Frame);
            }

            for (int i = 0; i < inputs.Length && i < Fighters.Length; i++)
            {
                Fighters[i].TickStateMachine(Frame);
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
