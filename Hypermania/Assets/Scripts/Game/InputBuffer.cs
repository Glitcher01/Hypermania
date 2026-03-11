using System;
using Design.Configs;
using Game.Sim;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Utils.EnumArray;

namespace Game
{
    public class InputBuffer
    {
        private EnumArray<InputFlags, Binding> _controlScheme;
        private EnumArray<InputFlags, Binding> _p2controlScheme;
        private Boolean _isControllerInput;

        /**
         * Base InputBuffer Constructor
         *
         * Constructs an InputBuffer to accept user input
         *
         * @param config - The Scriptable ControlsConfig Object to Reference
         *
         */
        public InputBuffer(ControlsConfig config)
        {
            _controlScheme = config.GetControlScheme();
            _isControllerInput = config.IsControllerInput();

        }

        private InputFlags _input = InputFlags.None;
        private static (InputFlags dir, InputFlags opp)[] _dirPairs =
        {
            (InputFlags.Left, InputFlags.Right),
            (InputFlags.Up, InputFlags.Down),
        };

        public void Saturate()
        {
            foreach (InputFlags flag in Enum.GetValues(typeof(InputFlags)))
            {
                if (flag == InputFlags.None)
                {
                    continue; // Skips the None InputFlag (Does Not Have a Key Press)
                }
                if (_isControllerInput)
                {
                    if (
                        (
                            _controlScheme[flag].GetPrimaryGamepadButton() != GamepadButtons.None
                            && Gamepad.current[(GamepadButton)(_controlScheme[flag].GetPrimaryGamepadButton())].isPressed
                        )
                        || (
                            _controlScheme[flag].GetAltGamepadButton() != GamepadButtons.None
                            && Gamepad.current[(GamepadButton)(_controlScheme[flag].GetAltGamepadButton())].isPressed
                        )
                    )
                    {
                        _input |= flag;
                    }
                }
                else
                {
                    // Checks if either the primary or alt button set in config is pressed
                    // Ignores keys set to none
                    if (
                        (
                            _controlScheme[flag].GetPrimaryKey() != Key.None
                            && Keyboard.current[_controlScheme[flag].GetPrimaryKey()].isPressed
                        )
                        || (
                            _controlScheme[flag].GetAltKey() != Key.None
                            && Keyboard.current[_controlScheme[flag].GetAltKey()].isPressed
                        )
                    )
                    {
                        _input |= flag;
                    }
                }
            }

            // clean inputs: cancel directionals
            foreach ((InputFlags dir, InputFlags opp) in _dirPairs)
            {
                if ((_input & dir) != 0 && (_input & opp) != 0)
                {
                    _input &= ~dir;
                    _input &= ~opp;
                }
            }
        }

        public void Clear()
        {
            _input = InputFlags.None;
        }

        public GameInput Poll()
        {
            return new GameInput(_input);
        }
    }
}
