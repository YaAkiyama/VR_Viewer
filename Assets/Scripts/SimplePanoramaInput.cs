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
        // キーボード入力をサポート
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

        // Meta Questコントローラー入力（クールダウンを使用）
        if (Time.time > nextButtonTime)
        {
            // 右コントローラー
            if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
            {
                panoramaController.NextPanorama();
                nextButtonTime = Time.time + buttonCooldown;
            }

            // 左コントローラー
            if (Gamepad.current != null && Gamepad.current.buttonWest.isPressed)
            {
                panoramaController.PreviousPanorama();
                nextButtonTime = Time.time + buttonCooldown;
            }
        }
    }
}