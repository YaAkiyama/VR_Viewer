using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.XR.CoreUtils;

public class MapMarkerManager : MonoBehaviour
{
    [Header("マーカー設定")]
    [SerializeField] private GameObject markerPrefab;            // マーカープレハブ
    [SerializeField] private GameObject currentPositionPrefab;   // 現在地マーカープレハブ
    [SerializeField] private Transform markerContainer;          // マーカーを配置する親オブジェクト

    [Header("マップ設定")]
    [SerializeField] private RectTransform mapRect;              // 地図のRectTransform

    [Header("パノラマ連携")]
    [SerializeField] private Panorama360Controller panoramaController; // パノラマコントローラー

    [Header("現在地マーカー設定")]
    [SerializeField] private bool rotateCurrentMarker = true;   // 現在地マーカーを回転させるかどうか
    [SerializeField] private float markerRotationOffset = 0f;   // マーカー回転の調整値（度）
    [SerializeField] private bool smoothRotation = true;        // 回転をスムーズに行うかどうか
    [SerializeField] private float rotationSmoothSpeed = 5f;    // 回転のスムーズ度
    [SerializeField] private bool hideOriginalMarker = true;   // 選択中の通常マーカーを非表示にするか
    [SerializeField] private bool invertRotation = true;       // 回転方向を反転させるかどうか

    // 静的イベント（他のコンポーネントからの参照用）
    public delegate void MarkerSelectedHandler(int markerId);
    public static event MarkerSelectedHandler OnMarkerSelected;

    // マーカーデータリスト
    [SerializeField] private List<MapMarker> markerData = new List<MapMarker>();

    // ランタイムデータ
    private Dictionary<int, GameObject> markerObjects = new Dictionary<int, GameObject>();
    private GameObject currentPositionMarker;
    private int currentMarkerIndex = -1;
    private Transform cameraTransform;
    private float targetRotation = 0f;

    void Start()
    {
        // パノラマコントローラーへの参照確認
        if (panoramaController == null)
        {
            panoramaController = FindFirstObjectByType<Panorama360Controller>();
            if (panoramaController == null)
            {
                Debug.LogError("Panorama360Controllerが見つかりません");
                return;
            }
        }

        // カメラへの参照を取得
        var xrOrigin = FindFirstObjectByType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            cameraTransform = xrOrigin.Camera.transform;
        }
        else
        {
            cameraTransform = Camera.main.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("カメラが見つかりません。現在地マーカーの回転機能は無効です。");
            }
        }

        // マーカーの初期生成
        CreateAllMarkers();

        // 初期現在地マーカーの生成（非表示）
        CreateCurrentPositionMarker();

        // Point Numberが最小のマーカーを初期表示として設定
        if (markerData.Count > 0)
        {
            // マーカーデータをPoint Number順にソート
            markerData = markerData.OrderBy(m => m.pointNumber).ToList();

            // ソート済みなので最初のマーカーが最小Point Number
            MapMarker firstMarker = markerData[0];
            OnMarkerClicked(firstMarker.pointNumber);
        }
    }

    void Update()
    {
        // カメラのY軸回転に合わせて現在地マーカーを回転
        UpdateCurrentMarkerRotation();
    }

    // 現在地マーカーの回転を更新
    private void UpdateCurrentMarkerRotation()
    {
        if (!rotateCurrentMarker || currentPositionMarker == null || cameraTransform == null) return;

        // カメラのY軸回転を取得
        float cameraYRotation = cameraTransform.eulerAngles.y;

        // 回転方向を反転させる場合は符号を反転
        if (invertRotation)
        {
            cameraYRotation = -cameraYRotation;
        }

        // マーカーの回転目標を設定（Z軸にマッピング）
        targetRotation = cameraYRotation + markerRotationOffset;

        if (smoothRotation)
        {
            // 現在の回転を取得
            Vector3 currentRotation = currentPositionMarker.transform.eulerAngles;

            // Z軸の回転をスムーズに変更
            float newZRotation = Mathf.LerpAngle(currentRotation.z, targetRotation, Time.deltaTime * rotationSmoothSpeed);

            // 新しい回転を適用
            currentPositionMarker.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, newZRotation);
        }
        else
        {
            // 直接回転を適用
            Vector3 currentRotation = currentPositionMarker.transform.eulerAngles;
            currentPositionMarker.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, targetRotation);
        }
    }

    // すべてのマーカーを作成
    private void CreateAllMarkers()
    {
        foreach (var marker in markerData)
        {
            CreateMarker(marker);
        }
    }

    // マーカーを1つ作成
    private void CreateMarker(MapMarker data)
    {
        GameObject markerObj = Instantiate(markerPrefab, markerContainer);
        RectTransform rt = markerObj.GetComponent<RectTransform>();

        // マーカーの位置を設定
        rt.anchoredPosition = data.position;

        // クリックイベントの設定
        Button button = markerObj.GetComponent<Button>();
        if (button != null)
        {
            int markerId = data.pointNumber; // ローカル変数にコピー
            button.onClick.AddListener(() => OnMarkerClicked(markerId));
        }

        // マーカー番号を表示するテキストがあれば設定
        Text markerText = markerObj.GetComponentInChildren<Text>();
        if (markerText != null)
        {
            markerText.text = data.pointNumber.ToString();
        }

        // 現在選択中のマーカーかどうかをチェックして、必要なら非表示に
        if (currentMarkerIndex == data.pointNumber && hideOriginalMarker)
        {
            markerObj.SetActive(false);
        }

        // マーカーオブジェクトを辞書に保存
        markerObjects[data.pointNumber] = markerObj;
    }

    // 現在地マーカーを作成
    private void CreateCurrentPositionMarker()
    {
        currentPositionMarker = Instantiate(currentPositionPrefab, markerContainer);
        currentPositionMarker.SetActive(false); // 初期状態は非表示
    }

    // マーカークリック時の処理
    public void OnMarkerClicked(int markerId)
    {
        Debug.Log($"マーカー {markerId} がクリックされました");

        // クリックされたマーカーのデータを取得
        MapMarker clickedMarker = markerData.Find(m => m.pointNumber == markerId);
        if (clickedMarker == null) return;

        // 以前の選択マーカーを表示に戻す
        if (currentMarkerIndex >= 0 && hideOriginalMarker && markerObjects.ContainsKey(currentMarkerIndex))
        {
            markerObjects[currentMarkerIndex].SetActive(true);
        }

        // 現在のマーカーを更新
        currentMarkerIndex = markerId;

        // 選択したマーカーを非表示にする
        if (hideOriginalMarker && markerObjects.ContainsKey(markerId))
        {
            markerObjects[markerId].SetActive(false);
        }

        // 現在地マーカーの位置を更新して表示
        MoveCurrentPositionMarker(clickedMarker.position);

        // パノラマ画像を変更（パスが設定されている場合）
        if (!string.IsNullOrEmpty(clickedMarker.panoramaPath) && panoramaController != null)
        {
            // パノラマコントローラーに画像切り替え要求
            panoramaController.LoadPanoramaByPath(clickedMarker.panoramaPath);
        }

        // 選択イベントを発行
        OnMarkerSelected?.Invoke(markerId);
    }

    // 現在地マーカーを移動
    private void MoveCurrentPositionMarker(Vector2 position)
    {
        if (currentPositionMarker != null)
        {
            RectTransform rt = currentPositionMarker.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            currentPositionMarker.SetActive(true);

            // 最新のカメラ回転を即座に適用
            if (rotateCurrentMarker && cameraTransform != null)
            {
                float cameraYRotation = cameraTransform.eulerAngles.y;

                // 回転方向を反転させる場合は符号を反転
                if (invertRotation)
                {
                    cameraYRotation = -cameraYRotation;
                }

                targetRotation = cameraYRotation + markerRotationOffset;

                if (!smoothRotation)
                {
                    // スムーズ回転が無効なら即座に適用
                    Vector3 currentRotation = currentPositionMarker.transform.eulerAngles;
                    currentPositionMarker.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, targetRotation);
                }
            }
        }
    }

    // 次のマーカーを選択
    public void SelectNextMarker()
    {
        if (markerData.Count <= 1) return;

        // 現在のマーカーのインデックスを取得
        int currentIndex = markerData.FindIndex(m => m.pointNumber == currentMarkerIndex);
        if (currentIndex < 0) currentIndex = 0;

        // 次のマーカーのインデックスを計算
        int nextIndex = (currentIndex + 1) % markerData.Count;

        // 次のマーカーをクリック
        OnMarkerClicked(markerData[nextIndex].pointNumber);
    }

    // 前のマーカーを選択
    public void SelectPreviousMarker()
    {
        if (markerData.Count <= 1) return;

        // 現在のマーカーのインデックスを取得
        int currentIndex = markerData.FindIndex(m => m.pointNumber == currentMarkerIndex);
        if (currentIndex < 0) currentIndex = 0;

        // 前のマーカーのインデックスを計算
        int prevIndex = (currentIndex - 1 + markerData.Count) % markerData.Count;

        // 前のマーカーをクリック
        OnMarkerClicked(markerData[prevIndex].pointNumber);
    }

    // 外部からマーカー位置を設定するメソッド
    public void SetMarkerPosition(int markerId, Vector2 newPosition)
    {
        // マーカーデータの更新
        int index = markerData.FindIndex(m => m.pointNumber == markerId);
        if (index >= 0)
        {
            markerData[index].position = newPosition;

            // マーカーオブジェクトが存在すれば位置も更新
            if (markerObjects.ContainsKey(markerId))
            {
                RectTransform rt = markerObjects[markerId].GetComponent<RectTransform>();
                rt.anchoredPosition = newPosition;
            }

            // 現在選択中のマーカーなら現在地マーカーも更新
            if (currentMarkerIndex == markerId)
            {
                MoveCurrentPositionMarker(newPosition);
            }
        }
    }

    // 元のマーカーの表示/非表示設定を切り替え
    public void SetHideOriginalMarker(bool hide)
    {
        // 設定が変わらない場合は何もしない
        if (hideOriginalMarker == hide) return;

        // 設定を更新
        hideOriginalMarker = hide;

        // 現在選択中のマーカーがある場合は表示/非表示を更新
        if (currentMarkerIndex >= 0 && markerObjects.ContainsKey(currentMarkerIndex))
        {
            markerObjects[currentMarkerIndex].SetActive(!hide);
        }
    }

    // 現在地マーカーの回転設定を変更
    public void SetCurrentMarkerRotation(bool enable, float offset = 0f, bool invert = true)
    {
        rotateCurrentMarker = enable;
        markerRotationOffset = offset;
        invertRotation = invert;

        // 即時適用
        if (currentPositionMarker != null && currentPositionMarker.activeSelf)
        {
            UpdateCurrentMarkerRotation();
        }
    }

    // マーカーデータリストを取得
    public List<MapMarker> GetMarkerData()
    {
        return markerData;
    }

    // 現在選択中のマーカー番号を取得
    public int GetCurrentMarkerIndex()
    {
        return currentMarkerIndex;
    }

    // 回転方向の反転設定を変更
    public void SetInvertRotation(bool invert)
    {
        if (invertRotation != invert)
        {
            invertRotation = invert;

            // 即時適用
            if (currentPositionMarker != null && currentPositionMarker.activeSelf && rotateCurrentMarker)
            {
                UpdateCurrentMarkerRotation();
            }
        }
    }

    // マーカーの表示状態を更新する（プログラム的に他の場所から呼び出す場合用）
    public void UpdateMarkerVisibility()
    {
        if (currentMarkerIndex < 0) return;

        foreach (var entry in markerObjects)
        {
            int pointNumber = entry.Key;
            GameObject markerObj = entry.Value;

            if (pointNumber == currentMarkerIndex)
            {
                markerObj.SetActive(!hideOriginalMarker);
            }
            else
            {
                markerObj.SetActive(true);
            }
        }
    }
}