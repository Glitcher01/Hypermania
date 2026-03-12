using UnityEngine;
using UnityEngine.InputSystem;

public class JoinOnInput : MonoBehaviour
{
    private InputAction anyButtonAction;
    public InputDevice _P1_Input,
        _P2_Input = null;

    private void Awake()
    {
        /* Search for Any Button Press */
        anyButtonAction = new InputAction(binding: "/*/<button>");
        anyButtonAction.performed += OnAnyButtonPressed;
        anyButtonAction.Enable();
    }

    void OnDisable()
    {
        anyButtonAction.Disable();
    }

    private void OnAnyButtonPressed(InputAction.CallbackContext context)
    {
        /* Stop Input Detection if Both Player Inputs are Handled*/
        if (_P1_Input != null && _P2_Input != null)
        {
            return;
        }
        /* Reject Unsupported Devices */
        if (context.control.device is not Gamepad && context.control.device is not Keyboard)
        {
            Debug.Log(context.control.device + " is not a supported device");
            return;
        }
        /* Reject if device is already connected */
        if (context.control.device == _P1_Input || context.control.device == _P2_Input)
        {
            Debug.Log(context.control.device + " has already been connected");
            return;
        }
        /* Prioritize Setting Player 1 Input First */
        if (_P1_Input == null && !context.control.device.Equals(_P2_Input))
        {
            _P1_Input = context.control.device;
            Debug.Log("Player 1: " + _P1_Input.name);
        }
        else if (_P2_Input == null && !context.control.device.Equals(_P1_Input))
        {
            _P2_Input = context.control.device;
            Debug.Log("Player 2: " + _P2_Input.name);
        }

        /* Search for New Device Input*/
        if (_P1_Input == null || _P2_Input == null)
        {
            anyButtonAction = new InputAction(binding: "/*/<button>");
        }
    }

    /**
     * Getter to Return Player's Input Devuce
     *
     * @param index
     */
    public InputDevice GetPlayerInputDevice(int index)
    {
        if (index == 1)
        {
            return _P1_Input;
        }
        if (index == 2)
        {
            return _P2_Input;
        }
        return null;
    }
}
