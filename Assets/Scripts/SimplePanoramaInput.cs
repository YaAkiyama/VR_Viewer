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
        // クールダウン時間中は処理しない
        if (Time.time < nextButtonTime) return;

        // キーボード入力をサポート
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

        // Meta Questコントローラー入力
        if (Gamepad.current != null)
        {
            // 右コントローラー
            if (Gamepad.current.buttonSouth.isPressed)
            {
                if (markerManager != null)
                {
                    markerManager.SelectNextMarker();
                    nextButtonTime = Time.time + buttonCooldown;
                }
            }

            // 左コントローラー
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