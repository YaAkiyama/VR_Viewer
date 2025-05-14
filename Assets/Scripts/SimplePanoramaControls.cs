using UnityEngine;

public class SimplePanoramaControls : MonoBehaviour
{
    private Panorama360Controller panoramaController;
    private MapMarkerManager markerManager;

    // キーボード入力を使用する場合のフラグ
    [SerializeField] private bool useKeyboardControls = true;

    // ボタン入力を使用する場合のボタン名
    [SerializeField] private string nextButton = "Fire1"; // デフォルトは左クリックやコントローラーのトリガー
    [SerializeField] private string prevButton = "Fire2"; // デフォルトは右クリックや別のボタン

    // 時間間隔による自動切り替え
    [SerializeField] private float autoChangeInterval = 0f; // 0=無効、正数=秒間隔
    private float timer = 0f;

    void Start()
    {
        panoramaController = GetComponent<Panorama360Controller>();

        // MapMarkerManagerの参照を取得
        markerManager = FindFirstObjectByType<MapMarkerManager>();
        if (markerManager == null)
        {
            Debug.LogError("MapMarkerManagerが見つかりません。SimplePanoramaControlsを使用するにはMapMarkerManagerが必要です。");
            enabled = false; // エラーがある場合はコンポーネントを無効化
            return;
        }
    }

    void Update()
    {
        // キーボード入力
        if (useKeyboardControls)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                markerManager.SelectNextMarker();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                markerManager.SelectPreviousMarker();
            }
        }

        // ボタン入力（VRコントローラー用）
        if (Input.GetButtonDown(nextButton))
        {
            markerManager.SelectNextMarker();
        }

        if (Input.GetButtonDown(prevButton))
        {
            markerManager.SelectPreviousMarker();
        }

        // 自動切り替え
        if (autoChangeInterval > 0)
        {
            timer += Time.deltaTime;
            if (timer >= autoChangeInterval)
            {
                timer = 0f;
                markerManager.SelectNextMarker();
            }
        }
    }
}