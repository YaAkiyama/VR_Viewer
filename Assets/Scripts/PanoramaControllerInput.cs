using UnityEngine;
using UnityEngine.InputSystem;

public class PanoramaControllerInput : MonoBehaviour
{
    private Panorama360Controller panoramaController;
    private MapMarkerManager markerManager;

    // ���̓A�N�V�������t�@�����X
    [SerializeField] private InputActionReference rightPrimaryButtonAction;
    [SerializeField] private InputActionReference leftPrimaryButtonAction;

    private bool rightButtonPressed = false;
    private bool leftButtonPressed = false;

    void Start()
    {
        panoramaController = GetComponent<Panorama360Controller>();

        // MapMarkerManager�̎Q�Ƃ��擾
        markerManager = FindFirstObjectByType<MapMarkerManager>();
        if (markerManager == null)
        {
            Debug.LogError("MapMarkerManager��������܂���BPanoramaControllerInput���g�p����ɂ�MapMarkerManager���K�v�ł��B");
            enabled = false; // �G���[������ꍇ�̓R���|�[�l���g�𖳌���
            return;
        }

        // ���̓A�N�V�����̃R�[���o�b�N��ݒ�
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
        // ���̓A�N�V�����̃R�[���o�b�N������
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
            markerManager.SelectNextMarker(); // panoramaController.NextPanorama()����ύX
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
            markerManager.SelectPreviousMarker(); // panoramaController.PreviousPanorama����ύX
        }
    }

    private void OnLeftPrimaryButtonReleased(InputAction.CallbackContext context)
    {
        leftButtonPressed = false;
    }
}