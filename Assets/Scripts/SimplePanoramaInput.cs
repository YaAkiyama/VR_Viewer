using UnityEngine;
using UnityEngine.InputSystem;

public class SimplePanoramaInput : MonoBehaviour
{
    private Panorama360Controller panoramaController;

    [SerializeField] private float buttonCooldown = 0.3f;
    private float nextButtonTime = 0;

    void Start()
    {
        panoramaController = GetComponent<Panorama360Controller>();
    }

    void Update()
    {
        // �L�[�{�[�h���͂��T�|�[�g
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                panoramaController.NextPanorama();
            }

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                panoramaController.PreviousPanorama();
            }
        }

        // Meta Quest�R���g���[���[���́i�N�[���_�E�����g�p�j
        if (Time.time > nextButtonTime)
        {
            // �E�R���g���[���[
            if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
            {
                panoramaController.NextPanorama();
                nextButtonTime = Time.time + buttonCooldown;
            }

            // ���R���g���[���[
            if (Gamepad.current != null && Gamepad.current.buttonWest.isPressed)
            {
                panoramaController.PreviousPanorama();
                nextButtonTime = Time.time + buttonCooldown;
            }
        }
    }
}