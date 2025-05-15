using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VRLaserPointer : MonoBehaviour
{
    [Header("レーザー設定")]
    [SerializeField] private Transform rayOrigin;  // レーザーの発射元（コントローラー）
    [SerializeField] private float maxRayDistance = 100f;   // レーザーの最大検出距離
    [SerializeField] private float maxVisualDistance = 0.8f;  // 視覚的なレーザーの長さ（短く変更）
    [SerializeField] private float rayStartWidth = 0.003f;     // レーザーの開始幅（細く設定）
    [SerializeField] private float rayEndWidth = 0.0005f;       // レーザーの終端幅（先細り効果）
    [SerializeField] private Color rayColor = new Color(0.0f, 0.8f, 1.0f, 0.7f); // レーザーの色（鮮やかに）
    [SerializeField] private Color rayEndColor = new Color(0.0f, 0.8f, 1.0f, 0.0f); // レーザー終端の色（透明に）

    [Header("ポインタードット設定")]
    [SerializeField] private GameObject pointerDotPrefab;   // ポインタードットのプレハブ
    [SerializeField] private float dotScale = 0.01f;        // ポインタードットのスケール（より小さく）
    [SerializeField] private Color dotColor = Color.white;  // 通常時のドットの色
    [SerializeField] private Color dotPressedColor = Color.red; // 押下時のドットの色

    [Header("入力設定")]
    [SerializeField] private InputActionReference triggerAction; // トリガーボタン
    [SerializeField] private InputActionReference aButtonAction; // Aボタン
    [SerializeField] private InputActionReference leftJoystickAction; // 左ジョイスティック
    [SerializeField] private InputActionReference rightJoystickAction; // 右ジョイスティック

    [Header("UI設定")]
    [SerializeField] private GraphicRaycaster uiRaycaster; // UIレイキャスター
    [SerializeField] private EventSystem eventSystem;      // イベントシステム
    [SerializeField] private Canvas[] targetCanvasList;    // 対象のCanvas一覧
    [SerializeField] private ScrollRect thumbnailScrollRect; // サムネイルのScrollRect

    [Header("パネル可視性コントローラー")]
    [SerializeField] private PanelVisibilityController panelVisibilityController;

    [Header("高度な設定")]
    [SerializeField] private bool processAllCanvases = true; // すべてのCanvasを処理するかどうか
    [SerializeField] private bool useClosestHit = true;      // 最も近いヒットを使用するかどうか
    [SerializeField] private float dragThresholdDistance = 0.5f; // ドラッグ開始するための最小移動距離
    [SerializeField] private float scrollSensitivity = 0.5f;   // スクロール感度
    [SerializeField] private float horizontalScrollMultiplier = 5.0f; // 水平スクロール用の乗数
    [SerializeField] private bool invertScrollDirection = true; // スクロール方向を反転するかどうか
    [SerializeField] private bool preventThumbnailSelectionWhileDragging = true; // ドラッグ中のサムネイル選択を防止するかどうか
    [SerializeField] private float joystickScrollSensitivity = 0.03f; // ジョイスティックでのスクロール感度

    // パブリックゲッターメソッド（エディタ拡張用）
    public Color GetRayColor() => rayColor;
    public Color GetRayEndColor() => rayEndColor;
    public float GetRayStartWidth() => rayStartWidth;
    public float GetRayEndWidth() => rayEndWidth;
    public float GetMaxVisualDistance() => maxVisualDistance;
    public Transform GetRayOrigin() => rayOrigin ?? transform;

    // レーザー用のコンポーネント
    private LineRenderer lineRenderer;
    private GameObject pointerDot;
    private Transform hitPoint;
    private Renderer dotRenderer;

    // UI要素との相互作用用
    private PointerEventData pointerData;
    private PointerEventData cachedPointerData;
    private GameObject currentTarget;
    private GameObject lastTarget;

    // 状態管理
    private bool triggerPressed = false;
    private bool isDragging = false;
    private List<Canvas> visibleCanvasList = new List<Canvas>();

    // ジョイスティック入力
    private Vector2 leftJoystickValue;
    private Vector2 rightJoystickValue;
    private bool isJoystickScrolling = false;

    // Aボタン関連の変数
    private static readonly object _buttonLock = new object(); // ロックオブジェクト
    private bool aButtonProcessing = false; // Aボタン処理中フラグ
    private float aButtonDebounceTime = 0.5f; // デバウンス時間を長めに設定
    // 前回のボタン押下時間を記録
    private float lastAButtonTime = 0f;

    // ドラッグ関連の変数
    private Vector2 dragStartPosition;
    private Vector2 dragStartWorldPosition;
    private GameObject draggedObject;
    private bool dragThresholdMet = false;
    private ScrollRect activeScrollRect = null;
    private Vector2 lastPointerPosition;
    private Vector3 lastPointerWorldPosition;
    private Vector2 totalDragDelta = Vector2.zero;
    private Vector2 previousScrollPosition;
    private Canvas hitCanvas;
    private Vector3 initialControllerPosition;
    private Vector3 controllerMovement;
    private bool isInitialDragFrame = true;
    private int framesSinceLastMovement = 0;
    private const int MAX_INACTIVE_FRAMES = 10; // 動きがない場合のフレーム数制限
    private bool isScrolling = false; // スクロール中かどうかを識別するフラグ

    // サムネイル選択処理の追加変数
    private GameObject dragStartThumbnail = null; // ドラッグ開始時のサムネイルオブジェクト
    private GameObject dragEndThumbnail = null;   // ドラッグ終了時のサムネイルオブジェクト
    private bool isThumbnailDrag = false;         // サムネイルをドラッグしているかどうか

    // スクロール種別
    private enum ScrollDirection { Horizontal, Vertical, Both }
    private ScrollDirection activeScrollDirection = ScrollDirection.Horizontal;

    // 複数ヒット管理用
    private struct UIHitInfo
    {
        public float distance;
        public GameObject target;
        public Canvas canvas;
        public Vector3 worldPosition;
        public Vector2 screenPosition;
    }
    private List<UIHitInfo> uiHits = new List<UIHitInfo>();

    void Start()
    {
        try
        {
            Debug.Log("[LaserPointer] Start initializing...");

            // レーザーの発射元を設定 (コントローラーの位置)
            if (rayOrigin == null)
            {
                rayOrigin = transform;
                Debug.Log("[LaserPointer] rayOrigin not set, using self transform");
            }

            // PanelVisibilityControllerを探す（もしSerializeFieldで設定されていなければ）
            if (panelVisibilityController == null)
            {
                panelVisibilityController = FindAnyObjectByType<PanelVisibilityController>();
                if (panelVisibilityController == null)
                {
                    Debug.LogWarning("[LaserPointer] PanelVisibilityControllerが見つかりません！");
                }
                else
                {
                    Debug.Log("[LaserPointer] PanelVisibilityControllerを自動検出");
                }
            }

            // EventSystemを探す（もしSerializeFieldで設定されていなければ）
            if (eventSystem == null)
            {
                eventSystem = FindAnyObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    Debug.LogWarning("[LaserPointer] EventSystemが見つかりません！");
                }
                else
                {
                    Debug.Log("[LaserPointer] EventSystemを自動検出");
                }
            }

            // UIRaycasterのチェック
            if (uiRaycaster == null)
            {
                Debug.LogWarning("[LaserPointer] uiRaycasterが設定されていません");

                // シーン内のGraphicRaycasterを自動検出
                GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
                if (raycasters.Length > 0)
                {
                    uiRaycaster = raycasters[0];
                    Debug.Log($"[LaserPointer] GraphicRaycasterを自動検出: {uiRaycaster.gameObject.name}");
                }
            }

            // ThumbnailScrollRectを検索（設定されていない場合）
            if (thumbnailScrollRect == null)
            {
                // シーン内の「ThumbnailCanvas」を探す
                GameObject thumbnailCanvas = GameObject.Find("ThumbnailCanvas");
                if (thumbnailCanvas != null)
                {
                    // ThumbnailCanvas内のScrollRectを検索
                    thumbnailScrollRect = thumbnailCanvas.GetComponentInChildren<ScrollRect>();
                    if (thumbnailScrollRect != null)
                    {
                        Debug.Log($"[LaserPointer] ThumbnailScrollRectを自動検出: {thumbnailScrollRect.gameObject.name}");
                        // 水平方向のスクロールを有効化、垂直方向を無効化
                        thumbnailScrollRect.horizontal = true;
                        thumbnailScrollRect.vertical = false;
                        
                        // 感度を調整
                        thumbnailScrollRect.scrollSensitivity = 20.0f;
                        
                        // デカレーション（慣性スクロール）を無効化
                        thumbnailScrollRect.decelerationRate = 0f;
                        thumbnailScrollRect.elasticity = 0.1f;
                        
                        Debug.LogError($"[LaserPointer] ThumbnailScrollRect設定最適化: Horizontal={thumbnailScrollRect.horizontal}, Sensitivity={thumbnailScrollRect.scrollSensitivity}");
                    }
                    else
                    {
                        Debug.LogWarning("[LaserPointer] ThumbnailCanvas内にScrollRectが見つかりません");
                    }
                }
            }
            else
            {
                // 設定済みのThumbnailScrollRectの設定を最適化
                thumbnailScrollRect.horizontal = true;
                thumbnailScrollRect.vertical = false;
                thumbnailScrollRect.scrollSensitivity = 20.0f;
                thumbnailScrollRect.decelerationRate = 0f;
                thumbnailScrollRect.elasticity = 0.1f;
                Debug.LogError($"[LaserPointer] 既存ThumbnailScrollRect設定最適化完了");
            }

            // レーザー用のLineRendererを設定
            InitializeLineRenderer();

            // ポインタードットを生成
            InitializeHitPoint();

            // PointerEventDataの初期化
            if (eventSystem != null)
            {
                pointerData = new PointerEventData(eventSystem);
                cachedPointerData = new PointerEventData(eventSystem);
                Debug.Log("[LaserPointer] PointerEventDataを初期化");
            }
            else
            {
                Debug.LogError("[LaserPointer] EventSystemが見つからないため、PointerEventDataを初期化できません");
            }

            // 入力アクションを有効化
            if (triggerAction != null)
            {
                triggerAction.action.Enable();
                Debug.Log("[LaserPointer] トリガーアクション有効化: " + triggerAction.action.name);
            }
            else
            {
                Debug.LogError("[LaserPointer] triggerActionが設定されていません");
            }

            if (aButtonAction != null)
            {
                // まず確実にイベントを解除
                try
                {
                    aButtonAction.action.performed -= OnAButtonPressed;
                }
                catch (System.Exception) { /* 無視 */ }

                // アクションを無効化してから再度有効化
                aButtonAction.action.Disable();
                
                // イベント登録を一度だけ行う
                aButtonAction.action.performed += OnAButtonPressed;
                
                // アクションを有効化
                aButtonAction.action.Enable();
                
                Debug.LogError($"[LaserPointer] Aボタンアクション再設定: {aButtonAction.action.name}, Enabled={aButtonAction.action.enabled}");

                // バインディングをデバッグ出力
                for (int i = 0; i < aButtonAction.action.bindings.Count; i++)
                {
                    Debug.LogError($"[LaserPointer] Binding {i}: Path={aButtonAction.action.bindings[i].path}");
                }
            }
            else
            {
                Debug.LogError("[LaserPointer] aButtonActionが設定されていません");
            }

            // ジョイスティックアクションの設定
            if (leftJoystickAction != null)
            {
                leftJoystickAction.action.Enable();
                Debug.Log("[LaserPointer] 左ジョイスティックアクション有効化: " + leftJoystickAction.action.name);
            }
            else
            {
                Debug.LogWarning("[LaserPointer] leftJoystickActionが設定されていません");
                
                // 入力アクションを探す
                var asset = Resources.FindObjectsOfTypeAll<InputActionAsset>().FirstOrDefault(a => a.name == "InputSystem_Actions");
                if (asset != null)
                {
                    var leftHandMap = asset.FindActionMap("XRI LeftHand");
                    if (leftHandMap != null)
                    {
                        var thumbstickAction = leftHandMap.FindAction("ThumbstickMove");
                        if (thumbstickAction != null)
                        {
                            // InputActionReferenceを作成する
                            leftJoystickAction = ScriptableObject.CreateInstance<InputActionReference>();
                            leftJoystickAction.name = "LeftThumbstickMove";
                            // inputActionReferenceにアクションを設定する方法はないので、代わりにアクションを直接有効化する
                            thumbstickAction.Enable();
                            Debug.LogError($"[LaserPointer] 左ジョイスティックアクションを自動検出して有効化: {thumbstickAction.name}");
                        }
                    }
                }
            }

            if (rightJoystickAction != null)
            {
                rightJoystickAction.action.Enable();
                Debug.Log("[LaserPointer] 右ジョイスティックアクション有効化: " + rightJoystickAction.action.name);
            }
            else
            {
                Debug.LogWarning("[LaserPointer] rightJoystickActionが設定されていません");
                
                // 入力アクションを探す
                var asset = Resources.FindObjectsOfTypeAll<InputActionAsset>().FirstOrDefault(a => a.name == "InputSystem_Actions");
                if (asset != null)
                {
                    var rightHandMap = asset.FindActionMap("XRI RightHand");
                    if (rightHandMap != null)
                    {
                        var thumbstickAction = rightHandMap.FindAction("ThumbstickMove");
                        if (thumbstickAction != null)
                        {
                            // InputActionReferenceを作成する
                            rightJoystickAction = ScriptableObject.CreateInstance<InputActionReference>();
                            rightJoystickAction.name = "RightThumbstickMove";
                            // inputActionReferenceにアクションを設定する方法はないので、代わりにアクションを直接有効化する
                            thumbstickAction.Enable();
                            Debug.LogError($"[LaserPointer] 右ジョイスティックアクションを自動検出して有効化: {thumbstickAction.name}");
                        }
                    }
                }
            }

            // CanvasListを初期化
            if (targetCanvasList == null || targetCanvasList.Length == 0)
            {
                Debug.LogWarning("[LaserPointer] targetCanvasListが設定されていないか空です");
                
                // シーン内のCanvasを自動検出して追加
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                if (canvases.Length > 0)
                {
                    List<Canvas> validCanvases = new List<Canvas>();
                    foreach (var canvas in canvases)
                    {
                        if (canvas.renderMode == RenderMode.WorldSpace)
                        {
                            validCanvases.Add(canvas);
                            Debug.Log($"[LaserPointer] WorldSpace Canvasを自動検出: {canvas.name}");
                        }
                    }
                    
                    if (validCanvases.Count > 0)
                    {
                        targetCanvasList = validCanvases.ToArray();
                        Debug.Log($"[LaserPointer] 自動検出したCanvas数: {targetCanvasList.Length}");
                    }
                }
            }

            UpdateVisibleCanvasList();

            Debug.Log("[LaserPointer] 初期化完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] 初期化エラー: {e.Message}\n{e.StackTrace}");
        }
    }

    void OnDestroy()
    {
        try
        {
            // Aボタンのイベントを解除
            if (aButtonAction != null)
            {
                aButtonAction.action.performed -= OnAButtonPressed;
                Debug.Log("[LaserPointer] Aボタンイベントを解除");
            }

            // ポインタードットを破棄
            if (pointerDot != null)
            {
                Destroy(pointerDot);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] OnDestroy エラー: {e.Message}");
        }
    }

    private void InitializeLineRenderer()
    {
        try
        {
            // LineRendererがなければ追加
            lineRenderer = gameObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                Debug.Log("[LaserPointer] LineRenderer added to game object");
            }

            // LineRendererの設定
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = rayStartWidth;
            lineRenderer.endWidth = rayEndWidth;
            
            // 色をグラデーションに設定
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayEndColor;

            // マテリアルを適切に設定
            Material laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (laserMaterial == null)
            {
                Debug.LogWarning("[LaserPointer] URPシェーダーが見つかりません。代替シェーダーを試します。");
                laserMaterial = new Material(Shader.Find("Unlit/Color"));
                if (laserMaterial == null)
                {
                    Debug.LogWarning("[LaserPointer] 代替シェーダーも見つかりません。Standardシェーダーを使用します。");
                    laserMaterial = new Material(Shader.Find("Standard"));
                }
            }

            if (laserMaterial != null)
            {
                laserMaterial.color = rayColor;
                laserMaterial.SetColor("_BaseColor", rayColor); // URP用
                laserMaterial.SetColor("_EmissionColor", rayColor * 1.5f); // 発光効果
                laserMaterial.EnableKeyword("_EMISSION"); // 発光を有効化
                lineRenderer.material = laserMaterial;
                Debug.Log($"[LaserPointer] レーザーマテリアル作成: Color={rayColor}");
            }
            else
            {
                Debug.LogError("[LaserPointer] レーザーマテリアルを作成できませんでした");
            }

            lineRenderer.positionCount = 2;

            // 初期位置を設定
            Vector3 startPos = rayOrigin != null ? rayOrigin.position : transform.position;
            Vector3 endPos = startPos + (rayOrigin != null ? rayOrigin.forward : transform.forward) * maxVisualDistance;

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            Debug.Log($"[LaserPointer] LineRenderer初期化完了: 開始位置={startPos}, 終了位置={endPos}, Forward={transform.forward}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] LineRenderer初期化エラー: {e.Message}\n{e.StackTrace}");
        }
    }

    private void InitializeHitPoint()
    {
        try
        {
            // ポインタードットをプレハブから生成
            if (pointerDotPrefab != null)
            {
                pointerDot = Instantiate(pointerDotPrefab);
                pointerDot.name = "LaserDot";
                Debug.Log("[LaserPointer] ポインタードットをプレハブから生成");
            }
            else
            {
                // プレハブがない場合はプリミティブから生成
                pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointerDot.name = "LaserDot";
                Debug.Log("[LaserPointer] ポインタードットをプリミティブから生成");
            }

            if (pointerDot == null)
            {
                Debug.LogError("[LaserPointer] Failed to create pointer dot");
                return;
            }

            // ワールド空間に配置し、親子関係を設定しない
            pointerDot.transform.SetParent(null);
            pointerDot.transform.localScale = Vector3.one * dotScale;

            // 衝突判定は不要なので削除
            Collider dotCollider = pointerDot.GetComponent<Collider>();
            if (dotCollider != null)
            {
                Destroy(dotCollider);
            }

            // マテリアルを設定
            dotRenderer = pointerDot.GetComponent<Renderer>();
            if (dotRenderer != null)
            {
                Material dotMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (dotMaterial == null)
                {
                    Debug.LogWarning("[LaserPointer] URPシェーダーが見つかりません。代替シェーダーを試します。");
                    dotMaterial = new Material(Shader.Find("Standard"));
                }

                if (dotMaterial != null)
                {
                    dotMaterial.color = dotColor;
                    dotMaterial.SetColor("_BaseColor", dotColor); // URP用
                    dotMaterial.SetColor("_EmissionColor", dotColor * 1.5f); // 発光効果
                    dotMaterial.EnableKeyword("_EMISSION"); // 発光を有効化
                    dotRenderer.material = dotMaterial;
                    Debug.Log($"[LaserPointer] ドットマテリアル作成: Color={dotColor}");
                }
                else
                {
                    Debug.LogError("[LaserPointer] ドットマテリアルを作成できませんでした");
                }
            }
            else
            {
                Debug.LogError("[LaserPointer] Dot renderer is null");
            }

            hitPoint = pointerDot.transform;
            pointerDot.SetActive(false);

            Debug.Log("[LaserPointer] ポインタードット初期化完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] ポインタードット初期化エラー: {e.Message}\n{e.StackTrace}");
        }
    }

    // 表示中のCanvasリストを更新
    private void UpdateVisibleCanvasList()
    {
        try
        {
            visibleCanvasList.Clear();

            if (targetCanvasList == null || targetCanvasList.Length == 0)
            {
                Debug.LogWarning("[LaserPointer] targetCanvasListが設定されていないか空です");
                return;
            }

            // パネルアクティブ状態を取得
            bool panelActive = panelVisibilityController != null ? panelVisibilityController.IsPanelActive : true;

            foreach (Canvas canvas in targetCanvasList)
            {
                if (canvas == null) continue;

                // パネルが非アクティブの場合はスキップ
                if (!panelActive)
                {
                    Debug.Log($"[LaserPointer] パネル非アクティブのためスキップ: {canvas.name}");
                    continue;
                }

                // CanvasGroupの状態をチェック
                CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    if (canvasGroup.alpha > 0f && canvasGroup.interactable)
                    {
                        visibleCanvasList.Add(canvas);
                        Debug.Log($"[LaserPointer] 表示Canvasに追加: {canvas.name}, Alpha={canvasGroup.alpha}, Interactable={canvasGroup.interactable}");
                    }
                    else
                    {
                        Debug.Log($"[LaserPointer] 非表示Canvas: {canvas.name}, Alpha={canvasGroup.alpha}, Interactable={canvasGroup.interactable}");
                    }
                }
                else
                {
                    // CanvasGroupがなければそのまま追加
                    visibleCanvasList.Add(canvas);
                    Debug.Log($"[LaserPointer] CanvasGroupなしCanvas追加: {canvas.name}");
                }
            }

            // MapCanvasとThumbnailCanvasの両方が含まれているか確認
            bool hasMapCanvas = visibleCanvasList.Any(c => c.name.Contains("MapCanvas"));
            bool hasThumbnailCanvas = visibleCanvasList.Any(c => c.name.Contains("ThumbnailCanvas"));
            
            Debug.Log($"[LaserPointer] 表示Canvas数: {visibleCanvasList.Count}, MapCanvas: {hasMapCanvas}, ThumbnailCanvas: {hasThumbnailCanvas}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateVisibleCanvasList エラー: {e.Message}");
        }
    }

    void Update()
    {
        try
        {
            // トリガー入力を確認
            CheckTriggerInput();

            // ジョイスティック入力を読み取り
            ReadJoystickInput();

            // レーザーを更新
            UpdateLaser();

            // ドラッグ処理の更新
            UpdateDrag();

            // ジョイスティックでのスクロール処理
            UpdateJoystickScroll();

            // VRコントローラーの移動量を更新
            if (triggerPressed && isDragging)
            {
                Vector3 currentControllerPosition = transform.position;
                controllerMovement = currentControllerPosition - initialControllerPosition;
                
                // 移動量が非常に小さい場合は動きがないと判断
                if (controllerMovement.magnitude < 0.001f)
                {
                    framesSinceLastMovement++;
                    if (framesSinceLastMovement > MAX_INACTIVE_FRAMES)
                    {
                        // 一定時間動きがない場合、ドラッグを中断
                        Debug.LogWarning($"[LaserPointer] コントローラー移動なし: {framesSinceLastMovement}フレーム");
                    }
                }
                else
                {
                    framesSinceLastMovement = 0;
                }
            }

            // スクロール中のデバッグ表示
            if (isDragging && triggerPressed && activeScrollRect != null)
            {
                if (Time.frameCount % 10 == 0)
                {
                    Debug.LogError($"[LaserPointer] スクロール状態: ScrollRect={activeScrollRect.name}, " +
                                   $"ScrollPos={activeScrollRect.normalizedPosition}, " +
                                   $"Dragging={isDragging}, Delta={totalDragDelta}, " +
                                   $"ControllerMovement={controllerMovement}, " +
                                   $"InvertDirection={invertScrollDirection}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] Update エラー: {e.Message}");
        }
    }

    private void ReadJoystickInput()
    {
        try
        {
            // 左ジョイスティックの入力を読み取り
            if (leftJoystickAction != null && leftJoystickAction.action != null)
            {
                leftJoystickValue = leftJoystickAction.action.ReadValue<Vector2>();
            }
            else
            {
                // 入力アクションAssetを直接探す
                var asset = Resources.FindObjectsOfTypeAll<InputActionAsset>().FirstOrDefault(a => a.name == "InputSystem_Actions");
                if (asset != null)
                {
                    var leftHandMap = asset.FindActionMap("XRI LeftHand");
                    if (leftHandMap != null)
                    {
                        var thumbstickAction = leftHandMap.FindAction("ThumbstickMove");
                        if (thumbstickAction != null && thumbstickAction.enabled)
                        {
                            leftJoystickValue = thumbstickAction.ReadValue<Vector2>();
                        }
                    }
                }
            }

            // 右ジョイスティックの入力を読み取り
            if (rightJoystickAction != null && rightJoystickAction.action != null)
            {
                rightJoystickValue = rightJoystickAction.action.ReadValue<Vector2>();
            }
            else
            {
                // 入力アクションAssetを直接探す
                var asset = Resources.FindObjectsOfTypeAll<InputActionAsset>().FirstOrDefault(a => a.name == "InputSystem_Actions");
                if (asset != null)
                {
                    var rightHandMap = asset.FindActionMap("XRI RightHand");
                    if (rightHandMap != null)
                    {
                        var thumbstickAction = rightHandMap.FindAction("ThumbstickMove");
                        if (thumbstickAction != null && thumbstickAction.enabled)
                        {
                            rightJoystickValue = thumbstickAction.ReadValue<Vector2>();
                        }
                    }
                }
            }

            // デバッグ用に入力値を出力（値が変わった時のみ）
            if ((leftJoystickValue.magnitude > 0.2f || rightJoystickValue.magnitude > 0.2f) && Time.frameCount % 30 == 0)
            {
                Debug.LogError($"[LaserPointer] ジョイスティック入力: 左={leftJoystickValue}, 右={rightJoystickValue}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] ReadJoystickInput エラー: {e.Message}");
        }
    }

    private void UpdateJoystickScroll()
    {
        // ThumbnailScrollRectが設定されていない場合は処理しない
        if (thumbnailScrollRect == null) return;

        try
        {
            // 左右どちらかのジョイスティックが水平方向に傾いているか確認
            float leftXValue = Mathf.Abs(leftJoystickValue.x) > 0.2f ? leftJoystickValue.x : 0f;
            float rightXValue = Mathf.Abs(rightJoystickValue.x) > 0.2f ? rightJoystickValue.x : 0f;
            
            // 両方のジョイスティックの水平入力を合算
            float combinedXInput = leftXValue + rightXValue;
            
            if (Mathf.Abs(combinedXInput) > 0.2f)
            {
                isJoystickScrolling = true;
                
                // 現在のScrollRectの正規化された位置を取得
                Vector2 normalizedPosition = thumbnailScrollRect.normalizedPosition;
                
                // 移動量を計算（反転フラグを考慮）
                float moveAmount = combinedXInput * joystickScrollSensitivity;
                if (invertScrollDirection)
                {
                    moveAmount = -moveAmount;
                }
                
                // 位置を更新
                normalizedPosition.x += moveAmount;
                normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
                thumbnailScrollRect.normalizedPosition = normalizedPosition;
                
                if (Time.frameCount % 30 == 0)
                {
                    Debug.LogError($"[LaserPointer] ジョイスティックスクロール: 入力={combinedXInput}, 移動量={moveAmount}, 新位置={normalizedPosition}, 反転={invertScrollDirection}");
                }
            }
            else
            {
                isJoystickScrolling = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateJoystickScroll エラー: {e.Message}");
        }
    }

    private void CheckTriggerInput()
    {
        try
        {
            if (triggerAction != null)
            {
                bool isTriggerPressed = triggerAction.action.IsPressed();

                // 押された瞬間
                if (isTriggerPressed && !triggerPressed)
                {
                    TriggerDown();
                }
                // 離された瞬間
                else if (!isTriggerPressed && triggerPressed)
                {
                    TriggerUp();
                }

                triggerPressed = isTriggerPressed;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] CheckTriggerInput エラー: {e.Message}");
        }
    }

    private void UpdateLaser()
    {
        try
        {
            if (lineRenderer == null)
            {
                Debug.LogError("[LaserPointer] LineRenderer is null");
                return;
            }

            // コントローラーの位置と方向を取得
            Vector3 startPos = rayOrigin != null ? rayOrigin.position : transform.position;
            Vector3 direction = rayOrigin != null ? rayOrigin.forward : transform.forward;

            // デバッグ - 10フレームごとに情報を出力
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[LaserPointer] コントローラー: Position={startPos}, Forward={direction}, Rotation={transform.rotation.eulerAngles}");
            }

            // レーザーの開始位置を設定
            lineRenderer.SetPosition(0, startPos);

            // 前フレームのターゲットを保存
            lastTarget = currentTarget;
            currentTarget = null;
            hitCanvas = null;

            // レイの最大距離を初期化
            float hitDistance = maxRayDistance;

            // 表示中のCanvasリストを更新
            UpdateVisibleCanvasList();

            // UI要素のヒット情報をクリア
            uiHits.Clear();

            // 物理オブジェクトとの交差判定
            bool hitPhysics = false;
            RaycastHit physicsHit = new RaycastHit();

            try
            {
                hitPhysics = Physics.Raycast(startPos, direction, out physicsHit, maxRayDistance);
                if (hitPhysics && physicsHit.distance < hitDistance)
                {
                    hitDistance = physicsHit.distance;
                    Debug.Log($"[LaserPointer] 物理オブジェクトヒット: {physicsHit.collider.name}, 距離={physicsHit.distance}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LaserPointer] Physics Raycast error: {e.Message}");
            }

            // UI要素との交差判定
            bool hitUI = false;
            if (visibleCanvasList.Count > 0)
            {
                // UI Raycastのデバッグ情報
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[LaserPointer] UI Raycast: Canvas Count={visibleCanvasList.Count}");
                }

                // 各Canvasに対して交差判定を行う
                foreach (Canvas canvas in visibleCanvasList)
                {
                    if (canvas == null) continue;

                    try
                    {
                        UIHitInfo hitInfo;
                        if (CheckUIRaycast(startPos, direction, canvas, out hitInfo) && hitInfo.target != null)
                        {
                            uiHits.Add(hitInfo);
                            hitUI = true;
                            
                            // すべてのCanvasを処理しない場合は最初のヒットで終了
                            if (!processAllCanvases)
                            {
                                break;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[LaserPointer] UI Raycast error on {canvas.name}: {e.Message}");
                    }
                }

                // UIヒットの処理
                if (hitUI && uiHits.Count > 0)
                {
                    // 最も近いヒットを使用するか、レイヤー順にソートするか
                    if (useClosestHit)
                    {
                        // 距離でソートして最も近いものを選択
                        uiHits.Sort((a, b) => a.distance.CompareTo(b.distance));
                    }
                    else
                    {
                        // Canvasのレイヤー順でソート（ソートはここでは行わない、リスト順を維持）
                    }

                    // 選択されたヒット情報を取得
                    UIHitInfo selectedHit = uiHits[0];
                    
                    // ドラッグ中またはジョイスティックスクロール中のサムネイル選択を防止
                    bool isThumbnail = selectedHit.target != null && 
                                     (selectedHit.target.name.Contains("Thumbnail") || 
                                      (selectedHit.canvas != null && selectedHit.canvas.name.Contains("Thumbnail")));
                    
                    if (!(preventThumbnailSelectionWhileDragging && (isScrolling || isJoystickScrolling) && isThumbnail))
                    {
                        currentTarget = selectedHit.target;
                    }
                    
                    hitDistance = selectedHit.distance;
                    hitCanvas = selectedHit.canvas;

                    // 前回のワールド位置を記録（ドラッグ計算用）
                    lastPointerWorldPosition = selectedHit.worldPosition;
                    
                    // 更新されたポインター位置を保存
                    lastPointerPosition = selectedHit.screenPosition;

                    // デバッグ出力
                    if (Time.frameCount % 30 == 0)
                    {
                        string targetInfo = currentTarget != null ? currentTarget.name : "なし(選択防止中)";
                        Debug.Log($"[LaserPointer] UIヒット: Canvas={selectedHit.canvas.name}, Target={targetInfo}, 距離={selectedHit.distance}, スクロール中={isScrolling}, ジョイスティックスクロール中={isJoystickScrolling}");
                    }

                    // ヒット位置にポインタードットを表示
                    if (hitPoint != null && pointerDot != null && !isDragging)
                    {
                        hitPoint.position = selectedHit.worldPosition;
                        pointerDot.SetActive(true);

                        // トリガーが押されている場合はドットの色を変更
                        if (dotRenderer != null)
                        {
                            dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;
                        }
                    }
                }
            }

            // UI要素から外れた場合
            if (lastTarget != null && currentTarget == null && !isDragging)
            {
                HandlePointerExit(lastTarget);
            }

            // レーザーの終点位置と長さを設定
            float visualDistance;
            Vector3 endPos;

            // ヒットした場合は、ヒット位置までレーザーを表示（ただし最大視覚距離まで）
            if ((hitUI || hitPhysics) && hitDistance < maxRayDistance)
            {
                // ヒット位置が視覚的最大距離より近い場合は、ヒット位置までのレーザーを表示
                if (hitDistance <= maxVisualDistance)
                {
                    visualDistance = hitDistance;
                    endPos = startPos + direction * visualDistance;
                }
                else
                {
                    // ヒットしたが視覚的最大距離より遠い場合は、最大視覚距離までのレーザーを表示
                    visualDistance = maxVisualDistance;
                    endPos = startPos + direction * visualDistance;
                }
            }
            else
            {
                // ヒットしなかった場合、または最大距離以上の場合は、最大視覚距離までのレーザーを表示
                visualDistance = maxVisualDistance;
                endPos = startPos + direction * visualDistance;
            }

            // レーザーの終点を設定
            lineRenderer.SetPosition(1, endPos);

            // ポインタードットの処理
            if (pointerDot != null)
            {
                if ((hitUI || (hitPhysics && physicsHit.distance < maxRayDistance)) && !isDragging)
                {
                    // UIヒットを優先
                    Vector3 hitPos = hitUI ? (uiHits.Count > 0 ? uiHits[0].worldPosition : endPos) : physicsHit.point;

                    if (hitPoint != null)
                    {
                        hitPoint.position = hitPos;
                        pointerDot.SetActive(true);

                        if (dotRenderer != null)
                        {
                            dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;
                        }

                        if (Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"[LaserPointer] ドット表示: Position={hitPos}, Active={pointerDot.activeSelf}");
                        }
                    }
                }
                else
                {
                    pointerDot.SetActive(false);
                }
            }

            // 通常のポインタEnter/Exit処理
            if (currentTarget != lastTarget)
            {
                HandlePointerEnterExit(currentTarget, lastTarget);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateLaser error: {e.Message}\n{e.StackTrace}");
        }
    }

    // ドラッグ処理の更新
    private void UpdateDrag()
    {
        try
        {
            if (triggerPressed && draggedObject != null)
            {
                // コントローラーのワールド位置の移動量を使用
                Vector3 worldMovement = transform.position - initialControllerPosition;
                Vector3 worldMovementAlongX = new Vector3(worldMovement.x, 0, worldMovement.z);
                
                // 初回フレームの場合は移動量をゼロに
                if (isInitialDragFrame)
                {
                    worldMovement = Vector3.zero;
                    isInitialDragFrame = false;
                }
                
                // 2Dスクリーン座標での移動量を計算
                Vector2 currentPointerPosition = pointerData.position;
                Vector2 dragDelta = currentPointerPosition - lastPointerPosition;
                lastPointerPosition = currentPointerPosition;
                
                // ドラッグの正確な計算のために、コントローラーの水平移動を考慮
                if (hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))
                {
                    // 水平方向の移動量を強調（左右移動を検出しやすく）
                    float multiplier = 500.0f;
                    // 反転するかどうかは後で処理するので、ここではそのままの方向で計算
                    dragDelta.x += worldMovementAlongX.x * multiplier;
                }
                
                // 総移動距離を累積
                totalDragDelta += dragDelta;
                
                // デバッグ情報
                if (Time.frameCount % 5 == 0 && hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))
                {
                    Debug.LogError($"[LaserPointer] ドラッグ計算: Screen={dragDelta}, World={worldMovement}, Total={totalDragDelta}, FrameCount={Time.frameCount}");
                }

                // ドラッグの閾値を超えたらドラッグと判定
                if (!dragThresholdMet && 
                   (totalDragDelta.magnitude > dragThresholdDistance || 
                    worldMovementAlongX.magnitude > 0.01f))
                {
                    dragThresholdMet = true;
                    isDragging = true;
                    isScrolling = true; // スクロール開始

                    // まずScrollRectを探す
                    FindAndSetScrollRect();

                    // ドラッグ開始時のスクロール位置を記録
                    if (activeScrollRect != null)
                    {
                        previousScrollPosition = activeScrollRect.normalizedPosition;
                        
                        // スクロール方向を決定
                        DetermineScrollDirection();
                        
                        Debug.LogError($"[LaserPointer] ドラッグ開始: ScrollRect={activeScrollRect.name}, 初期位置={previousScrollPosition}, 方向={activeScrollDirection}, 反転={invertScrollDirection}");
                    }
                    else
                    {
                        Debug.LogError($"[LaserPointer] ドラッグ開始: オブジェクト={draggedObject.name}, ScrollRectなし");
                    }

                    // ドラッグ開始イベントを発行
                    cachedPointerData.pointerDrag = draggedObject;
                    cachedPointerData.dragging = true;
                    cachedPointerData.pressPosition = dragStartPosition;
                    cachedPointerData.position = currentPointerPosition;
                    cachedPointerData.delta = dragDelta;
                    
                    if (activeScrollRect == null)
                    {
                        ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.beginDragHandler);
                    }

                    // サムネイル関連の処理
                    if (hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))
                    {
                        isThumbnailDrag = true;
                        
                        // ドラッグ開始時のサムネイルを記録（現在のターゲット）
                        dragStartThumbnail = currentTarget;
                        
                        if (dragStartThumbnail != null)
                        {
                            Debug.LogError($"[LaserPointer] ドラッグ開始サムネイル: {dragStartThumbnail.name}");
                        }
                    }
                }

                // ドラッグ中の処理
                if (isDragging)
                {
                    // ScrollRect処理
                    if (activeScrollRect != null)
                    {
                        // VRコントローラーのワールド移動量を考慮した更新
                        bool isThumbnailScroll = hitCanvas != null && hitCanvas.name.Contains("Thumbnail");
                        
                        if (isThumbnailScroll)
                        {
                            Vector3 horizontalMovement = Vector3.ProjectOnPlane(worldMovement, Vector3.up);
                            
                            // スクロール方向が反転設定の場合、符号を反転
                            float directionMultiplier = invertScrollDirection ? -1.0f : 1.0f;
                            float xMovement = horizontalMovement.x * 0.5f * directionMultiplier;
                            
                            // より大きな移動量を使用（VRコントローラーの移動に合わせるため）
                            Vector2 forcedDelta = new Vector2(xMovement, 0);
                            
                            // ThumbnailCanvas向けの特殊処理を使用
                            UpdateThumbnailScroll(forcedDelta);
                        }
                        else
                        {
                            // 通常のスクロール更新
                            UpdateScrollRectPosition(dragDelta);
                        }
                    }
                    else
                    {
                        // 通常のドラッグイベント
                        cachedPointerData.position = currentPointerPosition;
                        cachedPointerData.delta = dragDelta;
                        cachedPointerData.dragging = true;
                        ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.dragHandler);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateDrag エラー: {e.Message}");
        }
    }

    // ThumbnailCanvas用のスクロール処理（強化版）
    private void UpdateThumbnailScroll(Vector2 controllerDelta)
    {
        if (activeScrollRect == null) return;
        
        // 現在の正規化されたポジション
        Vector2 normalizedPosition = activeScrollRect.normalizedPosition;

        // コントローラーの入力に基づいてスクロール
        float moveAmount = controllerDelta.x * horizontalScrollMultiplier;
        
//        //反転させるためコメントアウト
//        // invertScrollDirectionフラグに基づいてスクロール方向を反転
//        if (invertScrollDirection)
//        {
//            moveAmount = -moveAmount;
//        }
        
        // 直感的なスクロール制御（パネルをつまんで引っ張るイメージ）
        normalizedPosition.x += moveAmount;
        
        // 値を0-1の範囲に制限
        normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
        
        // 位置を更新
        activeScrollRect.normalizedPosition = normalizedPosition;
        
        // デバッグ出力
        if (Mathf.Abs(moveAmount) > 0.001f)
        {
            Debug.LogError($"[LaserPointer] 特殊スクロール更新: 入力={controllerDelta}, 移動量={moveAmount}, 新位置={normalizedPosition}, 反転={invertScrollDirection}");
        }
    }

    // スクロール方向を決定
    private void DetermineScrollDirection()
    {
        if (activeScrollRect == null) return;

        if (activeScrollRect.horizontal && !activeScrollRect.vertical)
        {
            activeScrollDirection = ScrollDirection.Horizontal;
        }
        else if (!activeScrollRect.horizontal && activeScrollRect.vertical)
        {
            activeScrollDirection = ScrollDirection.Vertical;
        }
        else if (activeScrollRect.horizontal && activeScrollRect.vertical)
        {
            // 両方向可能な場合は初期動きで判断するか、水平方向優先
            activeScrollDirection = ScrollDirection.Both;
        }
        else
        {
            // 両方向無効の場合はデフォルトで水平方向
            activeScrollDirection = ScrollDirection.Horizontal;
        }
    }

    // ScrollRectのポジションを直接更新
    private void UpdateScrollRectPosition(Vector2 dragDelta)
    {
        if (activeScrollRect == null) return;

        // 現在の正規化されたポジション
        Vector2 normalizedPosition = activeScrollRect.normalizedPosition;
        
        // ThumbnailCanvasの場合は特別処理
        bool isThumbnailCanvas = hitCanvas != null && hitCanvas.name.Contains("Thumbnail");
        
        // スクロール方向に応じた移動量計算
        switch (activeScrollDirection)
        {
            case ScrollDirection.Horizontal:
                // invertScrollDirectionフラグに基づいてスクロール方向を反転
                float xDelta = dragDelta.x * scrollSensitivity / activeScrollRect.content.rect.width;
                
                if (invertScrollDirection)
                {
                    xDelta = -xDelta;
                }
                
                // パネルをつまんで引っ張るイメージの操作感
                normalizedPosition.x += xDelta;
                
                // ThumbnailCanvasの場合は強制的にスクロール感度を上げる
                if (isThumbnailCanvas)
                {
                    float additionalXDelta = dragDelta.x * 0.01f;
                    if (invertScrollDirection)
                    {
                        additionalXDelta = -additionalXDelta;
                    }
                    normalizedPosition.x += additionalXDelta; // 追加のスクロール
                }
                break;
                
            case ScrollDirection.Vertical:
                // ScrollRectは垂直方向は下が0、上が1
                // パネルをつまんで引っ張るイメージの操作感
                float yDelta = dragDelta.y * scrollSensitivity / activeScrollRect.content.rect.height;
                if (invertScrollDirection)
                {
                    yDelta = -yDelta;
                }
                normalizedPosition.y += yDelta;
                break;
                
            case ScrollDirection.Both:
                // 両方向に移動
                float xDeltaBoth = dragDelta.x * scrollSensitivity / activeScrollRect.content.rect.width;
                float yDeltaBoth = dragDelta.y * scrollSensitivity / activeScrollRect.content.rect.height;
                
                if (invertScrollDirection)
                {
                    xDeltaBoth = -xDeltaBoth;
                    yDeltaBoth = -yDeltaBoth;
                }
                
                normalizedPosition.x += xDeltaBoth;
                normalizedPosition.y += yDeltaBoth;
                break;
        }

        // 値を0-1の範囲に制限
        normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
        normalizedPosition.y = Mathf.Clamp01(normalizedPosition.y);
        
        // 位置を更新
        activeScrollRect.normalizedPosition = normalizedPosition;
        
        // デバッグ出力
        if (Time.frameCount % 5 == 0 && isThumbnailCanvas)
        {
            Debug.LogError($"[LaserPointer] スクロール更新: Delta={dragDelta}, 新位置={normalizedPosition}, 反転={invertScrollDirection}");
        }
    }

    // ScrollRectを探して設定
    private void FindAndSetScrollRect()
    {
        activeScrollRect = null;

        // 1. 指定されたThumbnailScrollRectを使用
        if (thumbnailScrollRect != null && hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))
        {
            activeScrollRect = thumbnailScrollRect;
            Debug.LogError($"[LaserPointer] 指定されたThumbnailScrollRectを使用: {thumbnailScrollRect.name}");
            return;
        }
        
        // 2. ドラッグ対象がThumbnailCanvasにある場合
        if (hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))
        {
            // ThumbnailCanvas内のScrollRectを検索
            ScrollRect sr = hitCanvas.GetComponentInChildren<ScrollRect>();
            if (sr != null)
            {
                activeScrollRect = sr;
                Debug.LogError($"[LaserPointer] ThumbnailCanvas内のScrollRectを検出: {sr.name}");
                return;
            }
            
            // シーン内で「Thumbnail」を含む名前のScrollRectを検索
            ScrollRect[] allScrollRects = FindObjectsByType<ScrollRect>(FindObjectsSortMode.None);
            foreach (ScrollRect scrollRect in allScrollRects)
            {
                if (scrollRect.name.Contains("Thumbnail") || scrollRect.name.Contains("Scroll"))
                {
                    activeScrollRect = scrollRect;
                    Debug.LogError($"[LaserPointer] シーン内のThumbnailScrollRectを検出: {scrollRect.name}");
                    return;
                }
            }
        }

        // 3. ドラッグ対象自体、または親階層のScrollRectを検索
        ScrollRect directScrollRect = draggedObject.GetComponent<ScrollRect>();
        if (directScrollRect != null)
        {
            activeScrollRect = directScrollRect;
            Debug.LogError($"[LaserPointer] ドラッグ対象のScrollRectを検出: {directScrollRect.name}");
            return;
        }

        // 親階層をたどる
        Transform parent = draggedObject.transform.parent;
        int searchDepth = 0;
        while (parent != null && searchDepth < 10)
        {
            ScrollRect parentScrollRect = parent.GetComponent<ScrollRect>();
            if (parentScrollRect != null)
            {
                activeScrollRect = parentScrollRect;
                Debug.LogError($"[LaserPointer] 親階層のScrollRectを検出: {parentScrollRect.name}, 親={parent.name}");
                return;
            }
            parent = parent.parent;
            searchDepth++;
        }

        // 4. 最後の手段として、特定の名前のオブジェクトを探す
        if (draggedObject.name.Contains("Thumbnail") ||
            (draggedObject.transform.parent != null && draggedObject.transform.parent.name.Contains("Thumbnail")))
        {
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Scroll") || obj.name.Contains("Content"))
                {
                    ScrollRect sr = obj.GetComponent<ScrollRect>();
                    if (sr != null)
                    {
                        activeScrollRect = sr;
                        Debug.LogError($"[LaserPointer] 名前検索でScrollRectを検出: {sr.name}");
                        return;
                    }
                }
            }
        }

        Debug.LogWarning("[LaserPointer] ScrollRectが見つかりませんでした");
    }

    // 特定のCanvasとの交差判定
    private bool CheckUIRaycast(Vector3 startPos, Vector3 direction, Canvas canvas, out UIHitInfo hitInfo)
    {
        hitInfo = new UIHitInfo()
        {
            canvas = canvas,
            distance = float.MaxValue,
            target = null
        };
        
        // NULLチェックを厳密に実施
        if (canvas == null)
        {
            Debug.LogWarning("[LaserPointer] canvas is null");
            return false;
        }

        // GraphicRaycasterを取得
        GraphicRaycaster canvasRaycaster = canvas.GetComponent<GraphicRaycaster>();
        if (canvasRaycaster == null)
        {
            Debug.LogWarning($"[LaserPointer] {canvas.name} にGraphicRaycasterがありません");
            return false;
        }

        if (eventSystem == null)
        {
            Debug.LogWarning("[LaserPointer] eventSystem is null");
            return false;
        }

        try
        {
            // レイとキャンバスの交点を計算
            Ray ray = new Ray(startPos, direction);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            if (canvasRect == null)
            {
                Debug.LogWarning($"[LaserPointer] {canvas.name} のRectTransformがnullです");
                return false;
            }

            // キャンバスの平面を計算（NaN値チェックを追加）
            Vector3 canvasNormal = -canvasRect.forward;
            if (float.IsNaN(canvasNormal.x) || float.IsNaN(canvasNormal.y) || float.IsNaN(canvasNormal.z))
            {
                Debug.LogError($"[LaserPointer] Canvas {canvas.name} normal has NaN values: {canvasNormal}");
                return false;
            }

            Plane canvasPlane = new Plane(canvasNormal, canvasRect.position);
            float rayDistance;

            if (canvasPlane.Raycast(ray, out rayDistance))
            {
                Vector3 worldPos = startPos + direction * rayDistance;

                // 世界座標からスクリーン座標へ
                Camera canvasCamera = canvas.worldCamera;
                if (canvasCamera == null)
                {
                    canvasCamera = Camera.main;
                    if (canvasCamera == null)
                    {
                        Debug.LogError("[LaserPointer] No camera found for UI raycasting");
                        return false;
                    }
                }

                // WorldToScreenPointに問題がある場合に対処
                Vector2 screenPoint;
                try
                {
                    screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPos);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LaserPointer] WorldToScreenPoint failed: {e.Message}");
                    return false;
                }

                // UIレイキャスト用のデータを更新
                if (pointerData == null)
                {
                    Debug.LogError("[LaserPointer] pointerData is null");
                    return false;
                }

                pointerData.position = screenPoint;
                pointerData.delta = Vector2.zero;
                pointerData.scrollDelta = Vector2.zero;
                pointerData.dragging = false;
                pointerData.useDragThreshold = true;
                pointerData.pointerId = 0;

                // UIレイキャストを実行
                List<RaycastResult> results = new List<RaycastResult>();
                try
                {
                    canvasRaycaster.Raycast(pointerData, results);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LaserPointer] UI Raycast failed: {e.Message}");
                    return false;
                }

                if (results.Count > 0)
                {
                    // UIヒット結果
                    RaycastResult topResult = results[0];
                    GameObject targetObject = topResult.gameObject;

                    if (targetObject == null)
                    {
                        Debug.LogWarning("[LaserPointer] ヒットしたGameObjectがnullです");
                        return false;
                    }

                    // ボタンの子オブジェクト（テキストなど）がヒットした場合、親のボタンを探す
                    Selectable parentSelectable = null;
                    if (targetObject.GetComponent<Selectable>() == null)
                    {
                        // 親階層をたどってSelectable持つオブジェクトを探す
                        Transform parent = targetObject.transform.parent;
                        while (parent != null)
                        {
                            parentSelectable = parent.GetComponent<Selectable>();
                            if (parentSelectable != null)
                            {
                                targetObject = parent.gameObject;
                                break;
                            }
                            parent = parent.parent;
                        }
                    }

                    // ヒット情報を設定
                    hitInfo.distance = rayDistance;
                    hitInfo.target = targetObject;
                    hitInfo.worldPosition = worldPos;
                    hitInfo.screenPosition = screenPoint;

                    return true; // UIとの交差あり
                }
            }

            return false; // UIとの交差なし
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] CheckUIRaycast エラー: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    private void HandlePointerEnterExit(GameObject current, GameObject last)
    {
        try
        {
            // 前のターゲットからExit
            if (last != null && last != current)
            {
                HandlePointerExit(last);
            }

            // 新しいターゲットにEnter
            if (current != null && current != last)
            {
                HandlePointerEnter(current);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerEnterExit エラー: {e.Message}");
        }
    }

    private void HandlePointerEnter(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerEnter: goがnullです");
                return;
            }

            Debug.Log($"[LaserPointer] ポインターEnter: {go.name}");

            // ポインターが現在スクロール中ならEnterイベントを無視
            if ((isScrolling || isJoystickScrolling) && preventThumbnailSelectionWhileDragging && 
                (go.name.Contains("Thumbnail") || (hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))))
            {
                Debug.LogWarning($"[LaserPointer] スクロール中のためサムネイルEnterイベントを無視: {go.name}");
                return;
            }

            // PointerEnterイベントを発信
            cachedPointerData.pointerEnter = go;
            cachedPointerData.position = pointerData.position;

            // Selectableコンポーネントに対してハイライト処理
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerEnter(cachedPointerData);

                // ボタンに直感的なフィードバックを適用
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    // 色の変更を強制的に適用
                    Color targetColor = colors.highlightedColor;
                    image.color = targetColor;
                    image.CrossFadeColor(targetColor, colors.fadeDuration, true, true);
                }
            }

            // イベントを発信
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerEnterHandler);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerEnter エラー: {e.Message}");
        }
    }

    private void HandlePointerExit(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerExit: goがnullです");
                return;
            }

            Debug.Log($"[LaserPointer] ポインターExit: {go.name}");

            // PointerExitイベントを発信
            cachedPointerData.pointerEnter = null;
            cachedPointerData.position = pointerData.position;

            // Selectableコンポーネントに対してのハイライト解除
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerExit(cachedPointerData);

                // ボタンのフィードバックを元に戻す
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.CrossFadeColor(colors.normalColor, colors.fadeDuration, true, true);
                }
            }

            // イベントを発信
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerExitHandler);

            // トリガーが押されていた場合は解除
            if (triggerPressed)
            {
                HandlePointerUp(go);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerExit エラー: {e.Message}");
        }
    }

    private void HandlePointerDown(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerDown: goがnullです");
                return;
            }

            Debug.Log($"[LaserPointer] ポインターDown: {go.name}");
            
            // スクロール中のサムネイル選択を防止
            if (preventThumbnailSelectionWhileDragging && (isScrolling || isJoystickScrolling) && 
                (go.name.Contains("Thumbnail") || (hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))))
            {
                Debug.LogWarning($"[LaserPointer] スクロール中のためサムネイルDownイベントを無視: {go.name}");
                return;
            }

            cachedPointerData.pointerPress = go;
            cachedPointerData.pressPosition = pointerData.position;
            cachedPointerData.pointerPressRaycast = pointerData.pointerPressRaycast;

            // Selectableコンポーネントに対しての押下処理
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerDown(cachedPointerData);

                // ボタンに直感的なフィードバックを適用
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.CrossFadeColor(colors.pressedColor, colors.fadeDuration, true, true);
                }
            }

            // イベントを発信
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerDownHandler);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerDown エラー: {e.Message}");
        }
    }

    private void HandlePointerUp(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerUp: goがnullです");
                return;
            }

            Debug.Log($"[LaserPointer] ポインターUp: {go.name}");

            // スクロール中のサムネイル選択を防止（ただし同一サムネイル上での開始/終了は許可）
            bool isSameThumbnail = isThumbnailDrag && dragStartThumbnail != null && dragEndThumbnail != null && 
                                 dragStartThumbnail == dragEndThumbnail;
            
            bool preventClick = preventThumbnailSelectionWhileDragging && (isScrolling || isJoystickScrolling) && 
                               (go.name.Contains("Thumbnail") || (hitCanvas != null && hitCanvas.name.Contains("Thumbnail"))) &&
                               !isSameThumbnail;  // 同一サムネイル上なら許可
            
            if (preventClick && !isSameThumbnail)
            {
                Debug.LogWarning($"[LaserPointer] スクロール中のためサムネイルUpイベントを無視: {go.name}");
                return;
            }

            // PointerUpイベントを発信
            cachedPointerData.position = pointerData.position;

            // Selectableコンポーネントの処理
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerUp(cachedPointerData);

                // ボタンのフィードバック処理
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.CrossFadeColor(colors.highlightedColor, colors.fadeDuration, true, true);
                }
            }

            // イベントを発信
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerUpHandler);

            // クリックイベントを発信
            if (cachedPointerData.pointerPress == go && (!preventClick || isSameThumbnail))
            {
                if (isSameThumbnail)
                {
                    Debug.LogError($"[LaserPointer] 同一サムネイル上でのクリック処理発生: {go.name}");
                }
                
                Debug.Log($"[LaserPointer] クリック: {go.name}");
                ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerClickHandler);
            }

            cachedPointerData.pointerPress = null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerUp エラー: {e.Message}");
        }
    }

    private void TriggerDown()
    {
        try
        {
            // デバッグ情報
            Debug.Log("[LaserPointer] トリガー押下: ターゲット = " + (currentTarget != null ? currentTarget.name : "なし"));

            // ドット色を変更
            if (pointerDot != null && pointerDot.activeSelf && dotRenderer != null)
            {
                dotRenderer.material.color = dotPressedColor;
            }

            if (currentTarget != null)
            {
                // コントローラーの初期位置を記録
                initialControllerPosition = transform.position;
                
                // ドラッグ処理の開始準備
                dragStartPosition = pointerData.position;
                dragStartWorldPosition = lastPointerWorldPosition;
                lastPointerPosition = dragStartPosition;
                draggedObject = currentTarget;
                dragThresholdMet = false;
                totalDragDelta = Vector2.zero;
                isInitialDragFrame = true;
                framesSinceLastMovement = 0;
                isScrolling = false; // ドラッグ開始時点ではスクロールではない
                
                // サムネイル関連の変数をリセット
                isThumbnailDrag = false;
                dragStartThumbnail = null;
                dragEndThumbnail = null;

                // ポインタダウンの処理
                HandlePointerDown(currentTarget);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] TriggerDown エラー: {e.Message}");
        }
    }

    private void TriggerUp()
    {
        try
        {
            // ドット色を元に戻す
            if (pointerDot != null && pointerDot.activeSelf && dotRenderer != null)
            {
                dotRenderer.material.color = dotColor;
            }

            // ドラッグ終了時の現在のターゲットを記録
            if (isThumbnailDrag && currentTarget != null)
            {
                dragEndThumbnail = currentTarget;
                Debug.LogError($"[LaserPointer] ドラッグ終了サムネイル: {dragEndThumbnail.name}");
            }

            // ドラッグ終了処理
            if (isDragging && draggedObject != null)
            {
                Debug.LogError($"[LaserPointer] ドラッグ終了: オブジェクト={draggedObject.name}, サムネイルドラッグ={isThumbnailDrag}");

                // ドラッグ終了処理
                cachedPointerData.position = pointerData.position;
                cachedPointerData.dragging = false;

                // ScrollRect用のドラッグ終了処理
                if (activeScrollRect != null)
                {
                    activeScrollRect.StopMovement();
                    Debug.LogError($"[LaserPointer] ScrollRectのドラッグ終了: {activeScrollRect.name}");
                }
                else
                {
                    ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.endDragHandler);
                }

                // ドロップイベントの処理
                if (currentTarget != null)
                {
                    ExecuteEvents.Execute(currentTarget, cachedPointerData, ExecuteEvents.dropHandler);
                }

                // サムネイルドラッグの場合の特別処理
                bool shouldTriggerClick = false;
                GameObject clickTarget = null;
                
                if (isThumbnailDrag && dragStartThumbnail != null && dragEndThumbnail != null && 
                    dragStartThumbnail == dragEndThumbnail && !preventThumbnailSelectionWhileDragging)
                {
                    // 同じサムネイル上でドラッグを開始・終了した場合は選択として処理
                    shouldTriggerClick = true;
                    clickTarget = dragEndThumbnail;
                    Debug.LogError($"[LaserPointer] 同一サムネイル上でのドラッグ終了: 選択として処理 - {clickTarget.name}");
                }

                isDragging = false;
                draggedObject = null;
                dragThresholdMet = false;
                activeScrollRect = null;
                hitCanvas = null;
                isScrolling = false; // スクロール終了

                // ドラッグ終了時、現在のターゲットがあればドットを表示
                if (currentTarget != null && pointerDot != null)
                {
                    pointerDot.SetActive(true);
                }

                // 同一サムネイル上でのドラッグだった場合、選択アクションを強制実行
                if (shouldTriggerClick && clickTarget != null)
                {
                    // ポインターアップとクリックイベントを発行
                    cachedPointerData.pointerPress = clickTarget;
                    cachedPointerData.position = pointerData.position;
                    
                    Debug.LogError($"[LaserPointer] 強制クリックイベント実行: {clickTarget.name}");
                    ExecuteEvents.Execute(clickTarget, cachedPointerData, ExecuteEvents.pointerClickHandler);
                }
            }

            if (currentTarget != null)
            {
                // ポインタアップの処理
                HandlePointerUp(currentTarget);
            }

            // サムネイル関連のリセット
            isThumbnailDrag = false;
            dragStartThumbnail = null;
            dragEndThumbnail = null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] TriggerUp エラー: {e.Message}");
        }
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        // ロックを使用して同時に複数のスレッドがこのコードを実行できないようにする
        lock (_buttonLock)
        {
            try
            {
                // コントロール名とイベント情報を確認（デバッグ用）
                var control = context.control?.name;
                var time = context.time;
                
                // 重複イベントのチェック - 同じボタンが短時間で連続して押された場合を防止
                if (Time.time - lastAButtonTime < 0.3f)
                {
                    Debug.LogWarning($"[LaserPointer] Aボタン連続押下のため無視: Control={control}, Time={time}, 経過={Time.time - lastAButtonTime}秒");
                    return;
                }
                
                // 処理中フラグのチェック
                if (aButtonProcessing)
                {
                    Debug.LogWarning($"[LaserPointer] Aボタン処理中のため無視: Control={control}, Time={time}");
                    return;
                }
                
                // 最初に処理中フラグを立てる（重要:これによりイベント重複を防止）
                aButtonProcessing = true;
                lastAButtonTime = Time.time;
                
                Debug.LogError($"[LaserPointer] Aボタンイベント発生: Time={time}, Phase={context.phase}, Control={control}");

                if (panelVisibilityController == null)
                {
                    Debug.LogError("[LaserPointer] PanelVisibilityController is null");
                    panelVisibilityController = FindFirstObjectByType<PanelVisibilityController>();
                    if (panelVisibilityController == null)
                    {
                        Debug.LogError("[LaserPointer] PanelVisibilityControllerが見つかりません");
                        aButtonProcessing = false; // フラグをリセット
                        return;
                    }
                }

                // 現在の状態を詳細に記録
                bool isInViewRange = panelVisibilityController.IsInViewRange();
                bool isPanelActive = panelVisibilityController.IsPanelActive;
                bool isFading = panelVisibilityController.IsFading();

                Debug.LogError($"[LaserPointer] Aボタン押下前の状態: 視野角内={isInViewRange}, パネルアクティブ={isPanelActive}, フェード中={isFading}");

                // パネルアクティブ状態を切り替える
                panelVisibilityController.TogglePanelActive();

                // 切替後の状態を記録
                isPanelActive = panelVisibilityController.IsPanelActive;
                Debug.LogError($"[LaserPointer] パネルアクティブ状態切替後: {isPanelActive}");

                // 一定時間後にフラグをリセットするコルーチンを開始
                StartCoroutine(ResetAButtonProcessing());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LaserPointer] Aボタン処理中にエラー: {e.Message}\n{e.StackTrace}");
                // エラー時はフラグをリセット
                aButtonProcessing = false;
            }
        }
    }

    private IEnumerator ResetAButtonProcessing()
    {
        // デバウンス時間待機
        yield return new WaitForSeconds(aButtonDebounceTime);
        
        // フラグをリセット
        aButtonProcessing = false;
        Debug.Log("[LaserPointer] Aボタン処理フラグをリセットしました");
    }
}