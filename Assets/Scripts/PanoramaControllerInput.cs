using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class PanoramaControllerInput : MonoBehaviour
{
    private Panorama360Controller panoramaController;

    // 入力アクションリファレンス
    [SerializeField] private InputActionReference rightPrimaryButtonAction;
    [SerializeField] private InputActionReference leftPrimaryButtonAction;

    private bool rightButtonPressed = false;
    private bool leftButtonPressed = false;

    void Start()
    {
        panoramaController = GetComponent<Panorama360Controller>();

        // 入力アクションのコールバックを設定
        if (rightPrimaryButtonAction != null)
        {
            rightPrimaryButtonAction.action.performed += OnRightPrimaryButtonPressed;
            rightPrimaryButtonAction.action.canceled += OnRightPrimaryButtonReleased;
            rightPrimaryButtonAction.action.Enable();
        }

        if (leftPrimaryButtonAction != null)
        {
            leftPrimaryButtonAction.action.performed += OnLeftPrimaryButtonPressed;
            leftPrimaryButtonAction.action.canceled += OnLeftPrimaryButtonReleased;
            leftPrimaryButtonAction.action.Enable();
        }
    }

    void OnDestroy()
    {
        // 入力アクションのコールバックを解除
        if (rightPrimaryButtonAction != null)
        {
            rightPrimaryButtonAction.action.performed -= OnRightPrimaryButtonPressed;
            rightPrimaryButtonAction.action.canceled -= OnRightPrimaryButtonReleased;
            rightPrimaryButtonAction.action.Disable();
        }

        if (leftPrimaryButtonAction != null)
        {
            leftPrimaryButtonAction.action.performed -= OnLeftPrimaryButtonPressed;
            leftPrimaryButtonAction.action.canceled -= OnLeftPrimaryButtonReleased;
            leftPrimaryButtonAction.action.Disable();
        }
    }

    private void OnRightPrimaryButtonPressed(InputAction.CallbackContext context)
    {
        if (!rightButtonPressed)
        {
            rightButtonPressed = true;
            panoramaController.NextPanorama();
        }
    }

    private void OnRightPrimaryButtonReleased(InputAction.CallbackContext context)
    {
        rightButtonPressed = false;
    }

    private void OnLeftPrimaryButtonPressed(InputAction.CallbackContext context)
    {
        if (!leftButtonPressed)
        {
            leftButtonPressed = true;
            panoramaController.PreviousPanorama();
        }
    }

    private void OnLeftPrimaryButtonReleased(InputAction.CallbackContext context)
    {
        leftButtonPressed = false;
    }
}