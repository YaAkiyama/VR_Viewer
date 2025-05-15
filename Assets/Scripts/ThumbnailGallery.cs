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
    [SerializeField] private float startPadding = 10f;     // 最初のサムネイルの前の余白
    [SerializeField] private bool autoAdjustPosition = true; // X位置を自動調整するかどうか

    [Header("オフセット設定")]
    [SerializeField] private bool useAdvancedOffsetCalculation = true; // 高度なオフセット計算を使用するかどうか
    [SerializeField] private float offsetBaseValue = -150f; // ベースオフセット値（要素1つの時）
    [SerializeField] private float offsetScaleFactor = -110f; // 要素数増加に伴うスケーリング係数
    [SerializeField] private float offsetCurveExponent = 0.5f; // 曲線の指数（0.5は平方根カーブ）

    [Header("スクロール設定")]
    [SerializeField] private bool limitScrollBounds = true; // スクロール範囲を制限するかどうか
    [SerializeField] private float scrollEndPadding = 0f;   // スクロール端の余白（正の値でスクロール範囲が狭くなる）
    [SerializeField] private bool centerCurrentThumbnail = true; // 現在選択中のサムネイルを中央に表示するかどうか
    [SerializeField] private float scrollAnimationDuration = 0.3f; // スクロールアニメーション時間（秒）
    [SerializeField] private bool scrollOnThumbnailClick = false; // サムネイルクリック時にスクロールするかどうか

    [Header("参照")]
    [SerializeField] private MapMarkerManager markerManager; // マーカーマネージャー

    // 内部で計算される位置調整値
    private float xPositionOffset = -380f; // X位置の調整（負の値で左に移動）

    // サムネイルオブジェクトを保存するリスト
    private List<GameObject> thumbnailObjects = new List<GameObject>();
    private int currentSelectedIndex = -1;
    private ScrollRect scrollRect;
    private float minScrollPosition = 0f;  // 左端（最初のサムネイル）
    private float maxScrollPosition = 1f;  // 右端（最後のサムネイル）

    // スクロールアニメーション用
    private bool isScrolling = false;
    private float scrollStartTime;
    private float scrollStartPos;
    private float scrollTargetPos;

    // サムネイルクリック検知用
    private bool thumbnailWasClicked = false;

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

    void Update()
    {
        // スクロールアニメーションの処理を無効化
        /*
        // スクロールアニメーションの処理
        if (isScrolling)
        {
            float elapsedTime = Time.time - scrollStartTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / scrollAnimationDuration);

            // イージング関数（EaseOutCubic）を適用
            float t = 1f - Mathf.Pow(1f - normalizedTime, 3);

            // 現在のスクロール位置を計算
            float currentScrollPos = Mathf.Lerp(scrollStartPos, scrollTargetPos, t);
            scrollRect.horizontalNormalizedPosition = currentScrollPos;

            // アニメーション終了判定
            if (normalizedTime >= 1f)
            {
                isScrolling = false;
            }
        }
        */

        // 他のUpdate内の処理があればそのまま残す
    }

    // X位置オフセットを自動計算（改良版）
    private void CalculateXPositionOffset()
    {
        if (!autoAdjustPosition) return;

        // マーカーの数を取得
        int markerCount = markerManager.GetMarkerData().Count;

        if (markerCount <= 0)
        {
            xPositionOffset = 0f;
            return;
        }

        if (useAdvancedOffsetCalculation)
        {
            // 高度な計算方法（べき乗曲線による近似）
            // 提供されたデータポイントに基づく計算方法
            float offsetValue = offsetBaseValue + offsetScaleFactor * Mathf.Pow(markerCount, offsetCurveExponent);

            // ビューポートの幅による調整
            float viewportWidth = scrollRect ? scrollRect.viewport.rect.width : 800f;
            float defaultViewportWidth = 800f;
            float viewportScaleFactor = defaultViewportWidth / viewportWidth;

            xPositionOffset = offsetValue * viewportScaleFactor;
        }
        else
        {
            // 単純な計算方法（直線的な関係）
            xPositionOffset = offsetBaseValue * markerCount;
        }

        Debug.Log($"自動計算されたxPositionOffset: {xPositionOffset}（マーカー数: {markerCount}）");
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
        if (!limitScrollBounds || isScrolling) return;

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

        // X位置オフセットを自動計算
        if (autoAdjustPosition)
        {
            CalculateXPositionOffset();
        }

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
        // サムネイルクリックフラグをセット（これは残す）
        thumbnailWasClicked = true;

        // マーカーマネージャーに通知（これも残す）
        markerManager.OnMarkerClicked(markerIndex);

        // スクロール関連の処理は追加しない
    }

    // マーカー選択時の処理（イベント受信）
    private void OnMarkerSelected(int markerIndex)
    {
        // 選択中のサムネイルを更新（この機能は残す）
        UpdateSelectedThumbnail(markerIndex);

        // 以下のスクロール関連のコードは一時的にコメントアウトまたは削除
        /*
        // サムネイルクリックの場合はスクロールしない設定の場合
        bool shouldScroll = true;

        if (thumbnailWasClicked && !scrollOnThumbnailClick)
        {
            shouldScroll = false;
        }

        // フラグをリセット
        thumbnailWasClicked = false;

        // 条件に応じてスクロール
        if (shouldScroll)
        {
            // 選択されたサムネイルが見えるようにスクロール
            ScrollToThumbnail(markerIndex);
        }
        */

        // フラグのリセットだけは維持
        thumbnailWasClicked = false;
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
        // 一時的に処理をすべて無効化
        return; // 早期リターンでメソッドの残りの部分を実行しない

        /*
        if (scrollRect == null) return;

        // 以下の元のコード全体をコメントアウト
        // マーカーデータをPoint Number順に取得
        List<MapMarker> markers = markerManager.GetMarkerData()
            .OrderBy(m => m.pointNumber)
            .ToList();

        // markerIndexに一致するマーカーの位置（順番）を特定
        int thumbnailIndex = markers.FindIndex(m => m.pointNumber == markerIndex);

        if (thumbnailIndex >= 0 && thumbnailIndex < thumbnailObjects.Count)
        {
            // 元のスクロール処理をすべてコメントアウト
        }
        */
    }

    // スクロールアニメーションを開始
    private void AnimateScrollTo(float targetNormalizedPos)
    {
        // 一時的に処理をすべて無効化
        return; // 早期リターンでメソッドの残りの部分を実行しない

        /*
        // アニメーション状態を初期化
        isScrolling = true;
        scrollStartTime = Time.time;
        scrollStartPos = scrollRect.horizontalNormalizedPosition;
        scrollTargetPos = targetNormalizedPos;

        // アニメーション時間がゼロなら即座に移動
        if (scrollAnimationDuration <= 0)
        {
            scrollRect.horizontalNormalizedPosition = targetNormalizedPos;
            isScrolling = false;
        }
        */
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
        autoAdjustPosition = false; // 手動設定の場合は自動調整を無効化
        RebuildGallery();
    }

    // 位置を調整するためのユーティリティメソッド
    public void AdjustXPosition(float adjustment)
    {
        xPositionOffset += adjustment;
        autoAdjustPosition = false; // 手動調整の場合は自動調整を無効化
        RebuildGallery();
    }

    // サムネイルの中央表示設定を変更
    public void SetCenterCurrentThumbnail(bool center, float animationDuration = 0.3f)
    {
        centerCurrentThumbnail = center;
        scrollAnimationDuration = animationDuration;

        // 現在選択中のサムネイルがあれば、位置を更新
        if (currentSelectedIndex >= 0 && markerManager != null)
        {
            // 順番でソートしたマーカーリストを取得
            List<MapMarker> markers = markerManager.GetMarkerData()
                .OrderBy(m => m.pointNumber)
                .ToList();

            if (currentSelectedIndex < markers.Count)
            {
                // 現在のマーカーのpointNumberを取得
                int currentMarkerIndex = markers[currentSelectedIndex].pointNumber;
                // スクロール位置を更新
                ScrollToThumbnail(currentMarkerIndex);
            }
        }
    }

    // サムネイルクリック時のスクロール設定を変更
    public void SetScrollOnThumbnailClick(bool scroll)
    {
        scrollOnThumbnailClick = scroll;
    }

    // 自動位置調整設定を変更
    public void SetAutoAdjustPosition(bool autoAdjust)
    {
        if (autoAdjustPosition != autoAdjust)
        {
            autoAdjustPosition = autoAdjust;
            if (autoAdjust)
            {
                // 自動調整に切り替える場合は再計算
                CalculateXPositionOffset();
            }
            RebuildGallery();
        }
    }

    // オフセット計算パラメータを設定
    public void SetOffsetCalculationParameters(float baseValue, float scaleFactor, float curveExponent)
    {
        offsetBaseValue = baseValue;
        offsetScaleFactor = scaleFactor;
        offsetCurveExponent = curveExponent;

        if (autoAdjustPosition)
        {
            CalculateXPositionOffset();
            RebuildGallery();
        }
    }

    // オフセット計算パラメータの最適化
    public void OptimizeOffsetParameters()
    {
        // 提供されたデータポイントをもとに、最適なパラメータを計算
        // これは簡略化のため、手動で調整した値に近い値を設定する
        offsetBaseValue = -150f;  // 1つの場合のベース値
        offsetScaleFactor = -120f; // スケーリング係数
        offsetCurveExponent = 0.5f; // 平方根に近い曲線

        if (autoAdjustPosition)
        {
            CalculateXPositionOffset();
            RebuildGallery();
        }
    }
}