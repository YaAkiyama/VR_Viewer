using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePanoramaInput : MonoBehaviour
{
    private Panorama360Controller panoramaController;
    private MapMarkerManager markerManager;

    [SerializeField] private float buttonCooldown = 0.3f;
    private float nextButtonTime = 0;

    void Start()
    {
        panoramaController = GetComponent<Panorama360Controller>();
        markerManager = FindFirstObjectByType<MapMarkerManager>();
    }

    void Update()
    {
        // �N�[���_�E�����Ԓ��͏������Ȃ�
        if (Time.time < nextButtonTime) return;

        // �L�[�{�[�h���͂��T�|�[�g
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                if (markerManager != null)
                {
                    markerManager.SelectNextMarker();
                    nextButtonTime = Time.time + buttonCooldown;
                }
            }

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                if (markerManager != null)
                {
                    markerManager.SelectPreviousMarker();
                    nextButtonTime = Time.time + buttonCooldown;
                }
            }
        }

        // Meta Quest�R���g���[���[����
        if (Gamepad.current != null)
        {
            // �E�R���g���[���[
            if (Gamepad.current.buttonSouth.isPressed)
            {
                if (markerManager != null)
                {
                    markerManager.SelectNextMarker();
                    nextButtonTime = Time.time + buttonCooldown;
                }
            }

            // ���R���g���[���[
            if (Gamepad.current.buttonWest.isPressed)
            {
                if (markerManager != null)
                {
                    markerManager.SelectPreviousMarker();
                    nextButtonTime = Time.time + buttonCooldown;
                }
            }
        }
    }
}