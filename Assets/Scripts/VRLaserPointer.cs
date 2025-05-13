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
    [SerializeField] private float maxRayDistance = 100f;   // レーザーの最大検出距離
    [SerializeField] private float maxVisualDistance = 5f;  // 視覚的なレーザーの長さ
    [SerializeField] private float rayWidth = 0.01f;        // レーザーの幅
    [SerializeField] private Color rayColor = new Color(0.0f, 0.5f, 1.0f, 0.5f); // レーザーの色

    [Header("ポインタードット設定")]
    [SerializeField] private float dotScale = 0.02f;        // ポインタードットのスケール
    [SerializeField] private Color dotColor = Color.white;  // 通常時のドットの色
    [SerializeField] private Color dotPressedColor = Color.red; // 押下時のドットの色

    [Header("入力設定")]
    [SerializeField] private InputActionReference triggerAction; // トリガーボタン
    [SerializeField] private InputActionReference aButtonAction; // Aボタン

    [Header("UI設定")]
    [SerializeField] private GraphicRaycaster uiRaycaster; // UIレイキャスター
    [SerializeField] private EventSystem eventSystem;      // イベントシステム
    [SerializeField] private Canvas[] targetCanvasList;    // 対象のCanvas一覧

    [Header("パネル可視性コントローラー")]
    [SerializeField] private PanelVisibilityController panelVisibilityController;

    [Header("高度な設定")]
    [SerializeField] private bool processAllCanvases = true; // すべてのCanvasを処理するかどうか
    [SerializeField] private bool useClosestHit = true;      // 最も近いヒットを使用するかどうか

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

    // Aボタン関連の変数
    private static readonly object _buttonLock = new object(); // ロックオブジェクト
    private bool aButtonProcessing = false; // Aボタン処理中フラグ
    private float aButtonDebounceTime = 0.5f; // デバウンス時間を長めに設定
    // 前回のボタン押下時間を記録
    private float lastAButtonTime = 0f;

    // ドラッグ関連の変数
    private Vector2 dragStartPosition;
    private GameObject draggedObject;
    private bool dragThresholdMet = false;
    private const float dragThreshold = 5f; // ドラッグ開始するための最小移動距離

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

            // PanelVisibilityControllerを探す（もしSerializeFieldで設定されていなければ）
            if (panelVisibilityController == null)
            {
                panelVisibilityController = FindFirstObjectByType<PanelVisibilityController>();
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
                eventSystem = FindFirstObjectByType<EventSystem>();
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

            // LineRendererの設定をクリアに
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = rayWidth;
            lineRenderer.endWidth = rayWidth * 0.5f; // 先細りにする

            // 色を明確に設定
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.0f); // 終点は透明に

            // マテリアルを適切に設定
            Material laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (laserMaterial == null)
            {
                Debug.LogWarning("[LaserPointer] URPシェーダーが見つかりません。代替シェーダーを試します。");
                laserMaterial = new Material(Shader.Find("Unlit/Color"));
                if (laserMaterial == null)
                {
                    Debug.LogWarning("[LaserPointer] 代替シェーダーも見つかりません。新しいマテリアルを作成します。");
                    laserMaterial = new Material(Shader.Find("Standard"));
                }
            }

            if (laserMaterial != null)
            {
                laserMaterial.color = rayColor;
                lineRenderer.material = laserMaterial;
                Debug.Log($"[LaserPointer] レーザーマテリアル作成: Color={rayColor}");
            }
            else
            {
                Debug.LogError("[LaserPointer] レーザーマテリアルを作成できませんでした");
            }

            lineRenderer.positionCount = 2;

            // 初期位置を設定
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * maxVisualDistance;

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
            // ポインタードットを作成
            pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (pointerDot == null)
            {
                Debug.LogError("[LaserPointer] Failed to create pointer dot");
                return;
            }

            pointerDot.name = "LaserDot";
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

            // レーザーを更新
            UpdateLaser();

            // ドラッグ処理の更新
            UpdateDrag();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] Update エラー: {e.Message}");
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
            Vector3 startPos = transform.position;
            Vector3 direction = transform.forward;

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
                    currentTarget = selectedHit.target;
                    hitDistance = selectedHit.distance;

                    // デバッグ出力
                    Debug.Log($"[LaserPointer] UIヒット: Canvas={selectedHit.canvas.name}, Target={currentTarget?.name}, 距離={selectedHit.distance}");

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
            float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
            Vector3 endPos = startPos + direction * visualDistance;
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
                // ドラッグ距離の計算
                float dragDistance = Vector2.Distance(dragStartPosition, pointerData.position);

                // ドラッグの閾値を超えたらドラッグと判定
                if (!dragThresholdMet && dragDistance > dragThreshold)
                {
                    dragThresholdMet = true;
                    isDragging = true;

                    Debug.Log($"[LaserPointer] ドラッグ開始: オブジェクト={draggedObject.name}, 距離={dragDistance}");

                    // ドラッグ開始イベントを発行
                    cachedPointerData.pointerDrag = draggedObject;
                    cachedPointerData.dragging = true;
                    ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.beginDragHandler);
                }

                // ドラッグ中の処理
                if (isDragging)
                {
                    // ドラッグイベントを発行
                    cachedPointerData.position = pointerData.position;
                    cachedPointerData.delta = pointerData.position - dragStartPosition;
                    ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.dragHandler);

                    // スクロールバーや関連UIコンポーネントに対する特別処理
                    ScrollRect scrollRect = draggedObject.GetComponent<ScrollRect>();
                    if (scrollRect == null && draggedObject.transform.parent != null)
                    {
                        scrollRect = draggedObject.transform.parent.GetComponent<ScrollRect>();
                        if (scrollRect == null)
                        {
                            // 親階層をたどってScrollRectを探す
                            Transform parent = draggedObject.transform.parent;
                            while (parent != null && scrollRect == null)
                            {
                                scrollRect = parent.GetComponent<ScrollRect>();
                                parent = parent.parent;
                            }
                        }
                    }

                    if (scrollRect != null)
                    {
                        // スクロールを実行
                        scrollRect.OnDrag(cachedPointerData);

                        if (Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"[LaserPointer] スクロール中: Delta={cachedPointerData.delta}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateDrag エラー: {e.Message}");
        }
    }

    // 特定のCanvasとの交差判定
    private bool CheckUIRaycast(Vector3 startPos, Vector3 direction, Canvas canvas, out UIHitInfo hitInfo)
    {
        hitInfo = new UIHitInfo();
        hitInfo.canvas = canvas;
        hitInfo.distance = float.MaxValue;
        hitInfo.target = null;
        
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
            if (cachedPointerData.pointerPress == go)
            {
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
                // ドラッグ処理の開始準備
                dragStartPosition = pointerData.position;
                draggedObject = currentTarget;
                dragThresholdMet = false;

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

            // ドラッグ終了処理
            if (isDragging && draggedObject != null)
            {
                Debug.Log($"[LaserPointer] ドラッグ終了: オブジェクト={draggedObject.name}");

                // ドラッグ終了処理
                cachedPointerData.position = pointerData.position;
                cachedPointerData.dragging = false;

                ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.endDragHandler);

                // ドロップイベントの処理
                if (currentTarget != null)
                {
                    ExecuteEvents.Execute(currentTarget, cachedPointerData, ExecuteEvents.dropHandler);
                }

                isDragging = false;
                draggedObject = null;
                dragThresholdMet = false;

                // ドラッグ終了時、現在のターゲットがあればドットを表示
                if (currentTarget != null && pointerDot != null)
                {
                    pointerDot.SetActive(true);
                }
            }

            if (currentTarget != null)
            {
                // ポインタアップの処理
                HandlePointerUp(currentTarget);
            }
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