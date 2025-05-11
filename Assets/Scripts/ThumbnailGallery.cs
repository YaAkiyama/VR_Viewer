using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class ThumbnailGallery : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private RectTransform galleryContent; // サムネイルを配置する親オブジェクト
    [SerializeField] private GameObject thumbnailPrefab;   // サムネイル表示用プレハブ
    [SerializeField] private float thumbnailSpacing = 10f; // サムネイル間の間隔
    [SerializeField] private float thumbnailWidth = 120f;  // サムネイルの幅
    [SerializeField] private float thumbnailHeight = 90f;  // サムネイルの高さ

    [Header("位置調整")]
    [SerializeField] private float xPositionOffset = -380f; // X位置の調整（負の値で左に移動）
    [SerializeField] private float startPadding = 10f;     // 最初のサムネイルの前の余白

    [Header("スクロール設定")]
    [SerializeField] private bool limitScrollBounds = true; // スクロール範囲を制限するかどうか
    [SerializeField] private float scrollEndPadding = 0f;   // スクロール端の余白（正の値でスクロール範囲が狭くなる）

    [Header("参照")]
    [SerializeField] private MapMarkerManager markerManager; // マーカーマネージャー

    // サムネイルオブジェクトを保存するリスト
    private List<GameObject> thumbnailObjects = new List<GameObject>();
    private int currentSelectedIndex = -1;
    private ScrollRect scrollRect;
    private float minScrollPosition = 0f;  // 左端（最初のサムネイル）
    private float maxScrollPosition = 1f;  // 右端（最後のサムネイル）

    void Awake()
    {
        // ScrollRectコンポーネントの取得
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRectコンポーネントが見つかりません");
        }
    }

    void Start()
    {
        if (markerManager == null)
        {
            markerManager = FindFirstObjectByType<MapMarkerManager>();
            if (markerManager == null)
            {
                Debug.LogError("MapMarkerManagerが見つかりません");
                return;
            }
        }

        // サムネイルギャラリーを初期化
        InitializeGallery();

        // スクロール範囲の計算
        CalculateScrollBounds();

        // マーカー変更イベントを監視
        MapMarkerManager.OnMarkerSelected += OnMarkerSelected;

        // スクロールイベントの監視を追加
        if (scrollRect != null && limitScrollBounds)
        {
            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
    }

    void OnDestroy()
    {
        // イベント監視を解除
        MapMarkerManager.OnMarkerSelected -= OnMarkerSelected;

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
    }

    // スクロール範囲を計算
    public void CalculateScrollBounds()
    {
        if (galleryContent == null || scrollRect == null || scrollRect.viewport == null) return;

        // ビューポートとコンテンツの幅を取得
        float viewportWidth = scrollRect.viewport.rect.width;
        float contentWidth = galleryContent.rect.width;

        // コンテンツがビューポートより小さい場合はスクロール不要
        if (contentWidth <= viewportWidth)
        {
            minScrollPosition = 0f;
            maxScrollPosition = 0f;
            return;
        }

        // ScrollRectのnormalizedPositionは、左端が1、右端が0になる場合があるため、調整
        if (scrollRect.horizontalNormalizedPosition == 0 && galleryContent.anchoredPosition.x == 0)
        {
            // 左端が0、右端が1の場合
            minScrollPosition = 0f;
            maxScrollPosition = 1f;
        }
        else
        {
            // 左端が1、右端が0の場合
            minScrollPosition = 1f;
            maxScrollPosition = 0f;
        }

        // 余白を考慮
        if (scrollEndPadding > 0 && contentWidth > viewportWidth + scrollEndPadding * 2)
        {
            float paddingRatio = scrollEndPadding / (contentWidth - viewportWidth);

            if (minScrollPosition < maxScrollPosition)
            {
                minScrollPosition += paddingRatio;
                maxScrollPosition -= paddingRatio;
            }
            else
            {
                minScrollPosition -= paddingRatio;
                maxScrollPosition += paddingRatio;
            }
        }

        Debug.Log($"スクロール範囲計算: min={minScrollPosition}, max={maxScrollPosition}");
    }

    // スクロール位置変更時のイベントハンドラ
    private void OnScrollValueChanged(Vector2 position)
    {
        if (!limitScrollBounds) return;

        // 水平スクロール位置を制限
        float horizontalPos = position.x;

        // 左端と右端を制限
        if (minScrollPosition < maxScrollPosition) // 左端が0、右端が1の場合
        {
            if (horizontalPos < minScrollPosition)
            {
                horizontalPos = minScrollPosition;
            }
            else if (horizontalPos > maxScrollPosition)
            {
                horizontalPos = maxScrollPosition;
            }
        }
        else // 左端が1、右端が0の場合
        {
            if (horizontalPos > minScrollPosition)
            {
                horizontalPos = minScrollPosition;
            }
            else if (horizontalPos < maxScrollPosition)
            {
                horizontalPos = maxScrollPosition;
            }
        }

        // 値が変わっていれば更新
        if (Mathf.Abs(horizontalPos - position.x) > 0.001f)
        {
            scrollRect.horizontalNormalizedPosition = horizontalPos;
        }
    }

    // サムネイルギャラリーの初期化
    private void InitializeGallery()
    {
        // 既存のサムネイルをクリア
        ClearThumbnails();

        // マーカーデータを取得
        List<MapMarker> markers = markerManager.GetMarkerData();
        if (markers == null || markers.Count == 0) return;

        // ポイント番号でソート
        markers = markers.OrderBy(m => m.pointNumber).ToList();

        // サムネイルを作成
        // 開始位置に全体的なX位置の調整と開始余白を追加
        float xPosition = xPositionOffset + startPadding;

        foreach (MapMarker marker in markers)
        {
            // サムネイルオブジェクトを作成
            GameObject thumbnailObj = Instantiate(thumbnailPrefab, galleryContent);
            RectTransform rt = thumbnailObj.GetComponent<RectTransform>();

            // 位置とサイズを設定（X位置に調整を適用）
            rt.anchoredPosition = new Vector2(xPosition, 0);
            rt.sizeDelta = new Vector2(thumbnailWidth, thumbnailHeight);

            // サムネイル画像を設定
            SetThumbnailImage(thumbnailObj, marker.thumbnailPath);

            // マーカー番号を表示するテキストがあれば設定
            TMPro.TextMeshProUGUI indexText = thumbnailObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (indexText != null)
            {
                indexText.text = marker.pointNumber.ToString();
            }

            // クリックイベントを設定
            int markerIndex = marker.pointNumber; // ローカル変数にコピー
            Button button = thumbnailObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnThumbnailClicked(markerIndex));
            }

            // 次のサムネイルの位置を計算
            xPosition += thumbnailWidth + thumbnailSpacing;

            // サムネイルオブジェクトをリストに追加
            thumbnailObjects.Add(thumbnailObj);
        }

        // コンテンツサイズを設定
        // 最後のサムネイルの後にも余白を追加
        // X位置の調整を考慮して、コンテンツの幅を適切に計算
        float totalWidth = xPosition - xPositionOffset + thumbnailSpacing;
        galleryContent.sizeDelta = new Vector2(totalWidth, galleryContent.sizeDelta.y);

        // 現在選択中のマーカーに合わせてハイライト
        UpdateSelectedThumbnail(markerManager.GetCurrentMarkerIndex());
    }

    // サムネイル画像を設定
    private void SetThumbnailImage(GameObject thumbnailObj, string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        // パスを正規化
        if (path.StartsWith("Assets/Resources/"))
        {
            path = path.Substring("Assets/Resources/".Length);
        }

        // 拡張子を削除
        if (System.IO.Path.HasExtension(path))
        {
            path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),
                                    System.IO.Path.GetFileNameWithoutExtension(path));
        }

        // ThumbnailImageという名前の子オブジェクトを探す
        Transform thumbnailImageTransform = thumbnailObj.transform.Find("ThumbnailImage");
        if (thumbnailImageTransform != null)
        {
            Image thumbnailImage = thumbnailImageTransform.GetComponent<Image>();
            if (thumbnailImage != null)
            {
                // スプライトをロード
                Sprite thumbnailSprite = LoadSpriteFromResources(path);
                if (thumbnailSprite != null)
                {
                    thumbnailImage.sprite = thumbnailSprite;
                    thumbnailImage.preserveAspect = true;

                    // 透明度を設定
                    thumbnailImage.color = new Color(1f, 1f, 1f, 1f); // 不透明に設定
                }
                else
                {
                    Debug.LogError($"スプライトのロード失敗: {path}");
                }
            }
        }
    }

    // サムネイルクリック時の処理
    private void OnThumbnailClicked(int markerIndex)
    {
        // マーカーマネージャーに通知
        markerManager.OnMarkerClicked(markerIndex);
    }

    // マーカー選択時の処理（イベント受信）
    private void OnMarkerSelected(int markerIndex)
    {
        UpdateSelectedThumbnail(markerIndex);

        // 選択されたサムネイルが見えるようにスクロール
        ScrollToThumbnail(markerIndex);
    }

    // 選択中のサムネイルを更新
    private void UpdateSelectedThumbnail(int markerIndex)
    {
        // マーカーデータをPoint Number順に取得
        List<MapMarker> markers = markerManager.GetMarkerData()
            .OrderBy(m => m.pointNumber)
            .ToList();

        // markerIndexに一致するマーカーの位置（順番）を特定
        int thumbnailIndex = markers.FindIndex(m => m.pointNumber == markerIndex);

        if (thumbnailIndex >= 0 && thumbnailIndex < thumbnailObjects.Count)
        {
            // 前の選択をリセット
            for (int i = 0; i < thumbnailObjects.Count; i++)
            {
                Transform backgroundTransform = thumbnailObjects[i].transform.Find("Background");
                if (backgroundTransform != null)
                {
                    Image backgroundImage = backgroundTransform.GetComponent<Image>();
                    if (backgroundImage != null)
                    {
                        backgroundImage.enabled = (i == thumbnailIndex);
                    }
                }
            }

            currentSelectedIndex = thumbnailIndex;
        }
    }

    // 選択されたサムネイルが見えるようにスクロール
    private void ScrollToThumbnail(int markerIndex)
    {
        if (scrollRect == null) return;

        // マーカーデータをPoint Number順に取得
        List<MapMarker> markers = markerManager.GetMarkerData()
            .OrderBy(m => m.pointNumber)
            .ToList();

        // markerIndexに一致するマーカーの位置（順番）を特定
        int thumbnailIndex = markers.FindIndex(m => m.pointNumber == markerIndex);

        if (thumbnailIndex >= 0 && thumbnailIndex < thumbnailObjects.Count)
        {
            GameObject thumbnailObj = thumbnailObjects[thumbnailIndex];
            RectTransform thumbnailRect = thumbnailObj.GetComponent<RectTransform>();

            // サムネイルの位置（コンテンツの左端からの距離）
            float thumbnailPosX = thumbnailRect.anchoredPosition.x;

            // コンテンツ全体の幅
            float contentWidth = galleryContent.rect.width;

            // ビューポートの幅
            float viewportWidth = scrollRect.viewport.rect.width;

            // X位置の調整を考慮して正規化されたスクロール位置を計算
            // xPositionOffsetを考慮して調整
            float adjustedPosX = thumbnailPosX - xPositionOffset;
            float normalizedPos = adjustedPosX / (contentWidth - viewportWidth);

            // スクロールの向きに応じて調整
            if (contentWidth > viewportWidth)
            {
                // 左端が0、右端が1の場合
                if (scrollRect.horizontalNormalizedPosition == 0 && galleryContent.anchoredPosition.x == 0)
                {
                    // そのまま使用
                }
                else
                {
                    // 左端が1、右端が0の場合は反転
                    normalizedPos = 1 - normalizedPos;
                }

                // 値を0〜1の範囲に収める
                normalizedPos = Mathf.Clamp01(normalizedPos);

                // スクロール位置を設定
                scrollRect.horizontalNormalizedPosition = normalizedPos;
            }
        }
    }

    // サムネイルをクリア
    private void ClearThumbnails()
    {
        foreach (GameObject obj in thumbnailObjects)
        {
            Destroy(obj);
        }
        thumbnailObjects.Clear();
    }

    // リソースからスプライトをロード
    private Sprite LoadSpriteFromResources(string path)
    {
        // テクスチャをロード
        Texture2D texture = Resources.Load<Texture2D>(path);

        if (texture == null)
        {
            Debug.LogError($"テクスチャのロード失敗: {path}");
            return null;
        }

        // テクスチャからスプライトを作成
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                                   new Vector2(0.5f, 0.5f));

        return sprite;
    }

    // ギャラリーの再初期化（外部から呼び出し可能）
    public void RebuildGallery()
    {
        InitializeGallery();
        CalculateScrollBounds();
    }

    // スクロール制限を設定
    public void SetScrollLimits(bool enableLimits, float padding = 0f)
    {
        limitScrollBounds = enableLimits;
        scrollEndPadding = padding;
        CalculateScrollBounds();
    }

    // X位置のオフセットを設定（エディタ拡張やプログラムから調整用）
    public void SetXPositionOffset(float offset)
    {
        xPositionOffset = offset;
        RebuildGallery();
    }

    // 位置を調整するためのユーティリティメソッド
    public void AdjustXPosition(float adjustment)
    {
        xPositionOffset += adjustment;
        RebuildGallery();
    }
}