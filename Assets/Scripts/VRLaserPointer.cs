using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    void Start()
    {
        // PanelVisibilityControllerを探す（もしSerializeFieldで設定されていなければ）
        if (panelVisibilityController == null)
        {
            panelVisibilityController = FindFirstObjectByType<PanelVisibilityController>();
            if (panelVisibilityController == null)
            {
                Debug.LogWarning("PanelVisibilityControllerが見つかりません！");
            }
        }

        // EventSystemを探す（もしSerializeFieldで設定されていなければ）
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("EventSystemが見つかりません！");
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
        }
        else
        {
            Debug.LogError("EventSystemが見つからないため、PointerEventDataを初期化できません");
        }

        // 入力アクションを有効化
        if (triggerAction != null) triggerAction.action.Enable();
        if (aButtonAction != null)
        {
            aButtonAction.action.Enable();
            aButtonAction.action.performed += OnAButtonPressed;
        }

        // CanvasListを初期化
        UpdateVisibleCanvasList();
    }

    void OnDestroy()
    {
        // Aボタンのイベントを解除
        if (aButtonAction != null)
        {
            aButtonAction.action.performed -= OnAButtonPressed;
        }
    }

    private void InitializeLineRenderer()
    {
        // LineRendererがなければ追加
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // LineRendererの設定
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth * 0.5f; // 先細りにする
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.0f); // 終点は透明に
        lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.positionCount = 2;
    }

    private void InitializeHitPoint()
    {
        // ポインタードットを作成
        pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointerDot.name = "LaserDot";
        pointerDot.transform.SetParent(transform);
        pointerDot.transform.localScale = Vector3.one * dotScale;

        // 衝突判定は不要なので削除
        Destroy(pointerDot.GetComponent<Collider>());

        // マテリアルを設定
        dotRenderer = pointerDot.GetComponent<Renderer>();
        dotRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        dotRenderer.material.color = dotColor;

        hitPoint = pointerDot.transform;
        pointerDot.SetActive(false);
    }

    // 表示中のCanvasリストを更新
    private void UpdateVisibleCanvasList()
    {
        visibleCanvasList.Clear();

        if (targetCanvasList != null && targetCanvasList.Length > 0)
        {
            foreach (Canvas canvas in targetCanvasList)
            {
                if (canvas != null)
                {
                    // PanelVisibilityControllerで制御されているパネルかどうか確認
                    CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
                    if (canvasGroup != null && canvasGroup.alpha > 0f && canvasGroup.interactable)
                    {
                        visibleCanvasList.Add(canvas);
                    }
                    else if (canvasGroup == null)
                    {
                        // CanvasGroupがなければそのまま追加
                        visibleCanvasList.Add(canvas);
                    }
                }
            }
        }
    }

    void Update()
    {
        // トリガー入力を確認
        CheckTriggerInput();

        // レーザーを更新
        UpdateLaser();
    }

    private void CheckTriggerInput()
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

    private void UpdateLaser()
    {
        if (lineRenderer == null)
        {
            Debug.LogError("LineRendererが初期化されていません");
            return;
        }

        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;

        // レーザーの開始位置を設定
        lineRenderer.SetPosition(0, startPos);

        // 前フレームのターゲットを保存
        lastTarget = currentTarget;
        currentTarget = null;

        // レイの最大距離を初期化
        float hitDistance = maxRayDistance;

        // 表示中のCanvasリストを更新
        UpdateVisibleCanvasList();

        // 物理オブジェクトとの交差判定
        RaycastHit physicsHit;
        bool hitPhysics = Physics.Raycast(startPos, direction, out physicsHit, maxRayDistance);
        if (hitPhysics && physicsHit.distance < hitDistance)
        {
            hitDistance = physicsHit.distance;
        }

        // UI要素との交差判定
        if (uiRaycaster != null && visibleCanvasList.Count > 0 && eventSystem != null)
        {
            // 各Canvasに対して交差判定を行う
            foreach (Canvas canvas in visibleCanvasList)
            {
                if (CheckUIRaycast(startPos, direction, canvas, ref hitDistance))
                {
                    // UIとの交差があれば処理終了
                    return;
                }
            }
        }

        // ドラッグ中のレーザー先端位置の更新
        if (isDragging)
        {
            float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
            Vector3 endPos = startPos + direction * visualDistance;
            lineRenderer.SetPosition(1, endPos);
            pointerDot.SetActive(false);
        }
        // 物理オブジェクトにヒットした場合
        else if (hitPhysics && physicsHit.distance < maxRayDistance)
        {
            float visualDistance = Mathf.Min(physicsHit.distance, maxVisualDistance);
            Vector3 visualEndPoint = startPos + direction * visualDistance;
            lineRenderer.SetPosition(1, visualEndPoint);

            // ヒットポイントの位置は実際のヒット位置に
            hitPoint.position = physicsHit.point;
            pointerDot.SetActive(true);
        }
        else
        {
            // どれにもヒットしなかった場合、表示用の距離を使用
            lineRenderer.SetPosition(1, startPos + direction * maxVisualDistance);
            pointerDot.SetActive(false);
        }

        // UI要素から外れた場合
        if (lastTarget != null && currentTarget == null && !isDragging)
        {
            HandlePointerExit(lastTarget);
        }
    }

    // 特定のCanvasとの交差判定
    private bool CheckUIRaycast(Vector3 startPos, Vector3 direction, Canvas canvas, ref float hitDistance)
    {
        if (canvas == null || uiRaycaster == null || eventSystem == null)
            return false;

        // レイとキャンバスの交点を計算
        Ray ray = new Ray(startPos, direction);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return false;

        Plane canvasPlane = new Plane(-canvasRect.forward, canvasRect.position);
        float rayDistance;

        if (canvasPlane.Raycast(ray, out rayDistance) && rayDistance < hitDistance)
        {
            Vector3 worldPos = startPos + direction * rayDistance;

            // 世界座標からスクリーン座標へ
            Camera canvasCamera = canvas.worldCamera;
            if (canvasCamera == null) canvasCamera = Camera.main;

            if (canvasCamera != null)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPos);

                // UIレイキャスト用のデータを更新
                pointerData.position = screenPoint;
                pointerData.delta = Vector2.zero;
                pointerData.scrollDelta = Vector2.zero;
                pointerData.dragging = false;
                pointerData.useDragThreshold = true;
                pointerData.pointerId = 0;

                // UIレイキャストを実行
                List<RaycastResult> results = new List<RaycastResult>();
                uiRaycaster.Raycast(pointerData, results);

                if (results.Count > 0)
                {
                    // UIヒット結果をより詳細にデバッグ
                    RaycastResult topResult = results[0];
                    GameObject targetObject = topResult.gameObject;

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

                    hitDistance = rayDistance;

                    // レーザーの終点位置を設定
                    float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
                    Vector3 visualEndPoint = startPos + direction * visualDistance;
                    lineRenderer.SetPosition(1, visualEndPoint);

                    // ドラッグ中はドットを非表示に
                    if (!isDragging)
                    {
                        hitPoint.position = worldPos;
                        pointerDot.SetActive(true);

                        // トリガーが押されている場合はドットの色を変更
                        dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;
                    }
                    else
                    {
                        pointerDot.SetActive(false);
                    }

                    // UIターゲットを保存
                    currentTarget = targetObject;

                    // 通常のポインタEnter/Exit処理
                    HandlePointerEnterExit(currentTarget, lastTarget);

                    return true; // UIとの交差あり
                }
            }
        }

        return false; // UIとの交差なし
    }

    private void HandlePointerEnterExit(GameObject current, GameObject last)
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

    private void HandlePointerEnter(GameObject go)
    {
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

    private void HandlePointerExit(GameObject go)
    {
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

    private void HandlePointerDown(GameObject go)
    {
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

    private void HandlePointerUp(GameObject go)
    {
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
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerClickHandler);
        }

        cachedPointerData.pointerPress = null;
    }

    private void TriggerDown()
    {
        // デバッグ情報
        Debug.Log("[LaserPointer] トリガー押下: ターゲット = " + (currentTarget != null ? currentTarget.name : "なし"));

        // ドット色を変更
        if (!isDragging && pointerDot.activeSelf)
        {
            dotRenderer.material.color = dotPressedColor;
        }

        if (currentTarget != null)
        {
            // ポインタダウンの処理
            HandlePointerDown(currentTarget);
        }
    }

    private void TriggerUp()
    {
        // ドット色を元に戻す
        if (!isDragging && pointerDot.activeSelf)
        {
            dotRenderer.material.color = dotColor;
        }

        if (currentTarget != null)
        {
            // ポインタアップの処理
            HandlePointerUp(currentTarget);
        }
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        if (panelVisibilityController == null) return;

        // 視野角内かどうかを確認
        bool isInViewRange = panelVisibilityController.IsInViewRange();

        if (isInViewRange)
        {
            Debug.Log("[LaserPointer] Aボタンが押されました - パネル表示を切り替えます");
            // 強制的にパネルの表示/非表示を切り替える
            panelVisibilityController.ToggleForcedVisibility();
        }
    }
}