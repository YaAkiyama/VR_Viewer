using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SimpleLaserPointer : MonoBehaviour
{
    [Header("レーザー設定")]
    public float maxDetectionDistance = 100f;
    public float maxVisualDistance = 50f;
    public float dotScale = 0.02f;
    public Color laserColor = Color.blue;
    public Color dotColor = Color.white;
    public Color dotPressedColor = Color.red;
    public Color dotDraggingColor = Color.yellow;

    [Header("UI検出用")]
    public GraphicRaycaster uiRaycaster;
    public Canvas targetCanvas; // 後方互換性のため残す
    public Canvas[] targetCanvasList; // 新たに追加：複数Canvas対応
    public EventSystem eventSystem;

    [Header("フェードコントローラー参照")]
    public CanvasFadeController canvasFadeController; // 後方互換性のため残す

    // 入力関係
    private bool triggerPressed = false;
    private bool isDragging = false;

    private LineRenderer lineRenderer;
    private GameObject hitPointObj;
    private Transform hitPoint;
    private Renderer dotRenderer;
    private PointerEventData pointerData;

    // UI要素との相互作用用
    [HideInInspector] public GameObject currentTarget;
    private GameObject lastTarget;
    private PointerEventData cachedPointerData;

    // ドラッグ操作用
    private CanvasDragHandler canvasDragHandler;

    // Canvas検出関連
    private List<Canvas> visibleCanvasList = new List<Canvas>();

    void Start()
    {
        // ライン描画の初期設定
        InitializeLineRenderer();

        // ヒットポイントの設定
        InitializeHitPoint();

        // EventSystemの確認
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        // PointerEventDataの初期化
        pointerData = new PointerEventData(eventSystem);
        cachedPointerData = new PointerEventData(eventSystem);

        // Canvasリストを初期化
        UpdateVisibleCanvasList();
    }

    private void InitializeLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.002f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);
        lineRenderer.positionCount = 2;
    }

    private void InitializeHitPoint()
    {
        hitPointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitPointObj.name = "LaserDot";
        hitPointObj.transform.SetParent(transform);
        hitPointObj.transform.localScale = Vector3.one * dotScale;
        Destroy(hitPointObj.GetComponent<Collider>());

        dotRenderer = hitPointObj.GetComponent<Renderer>();
        dotRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        dotRenderer.material.color = dotColor;

        hitPoint = hitPointObj.transform;
        hitPointObj.SetActive(false);
    }

    // 表示中のCanvasリストを更新
    private void UpdateVisibleCanvasList()
    {
        visibleCanvasList.Clear();

        // 後方互換性：単一Canvasがセットされている場合
        if (targetCanvas != null)
        {
            CanvasFadeController fadeCtrl = targetCanvas.GetComponent<CanvasFadeController>();
            if (fadeCtrl == null || fadeCtrl.IsVisible())
            {
                visibleCanvasList.Add(targetCanvas);
            }
        }

        // 複数Canvas対応
        if (targetCanvasList != null && targetCanvasList.Length > 0)
        {
            foreach (Canvas canvas in targetCanvasList)
            {
                if (canvas != null)
                {
                    CanvasFadeController fadeCtrl = canvas.GetComponent<CanvasFadeController>();
                    if (fadeCtrl == null || fadeCtrl.IsVisible())
                    {
                        // まだリストに含まれていない場合のみ追加
                        if (!visibleCanvasList.Contains(canvas))
                        {
                            visibleCanvasList.Add(canvas);
                        }
                    }
                }
            }
        }
    }

    void Update()
    {
        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;

        lineRenderer.SetPosition(0, startPos);

        // 前フレームのターゲットを保存
        lastTarget = currentTarget;

        // レイの最大距離を初期化
        float hitDistance = maxDetectionDistance;
        currentTarget = null;

        // 表示中のCanvasリストを更新
        UpdateVisibleCanvasList();

        // 物理オブジェクトとの交差判定
        RaycastHit physicsHit;
        bool hitPhysics = Physics.Raycast(startPos, direction, out physicsHit, maxDetectionDistance);
        if (hitPhysics && physicsHit.distance < hitDistance)
        {
            hitDistance = physicsHit.distance;
        }

        // UI要素との交差判定（表示されているCanvasのみをチェック）
        if (uiRaycaster != null && visibleCanvasList.Count > 0)
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
            hitPointObj.SetActive(false);
        }
        // 物理オブジェクトにヒットした場合
        else if (hitPhysics && physicsHit.distance < maxDetectionDistance)
        {
            float visualDistance = Mathf.Min(physicsHit.distance, maxVisualDistance);
            Vector3 visualEndPoint = startPos + direction * visualDistance;
            lineRenderer.SetPosition(1, visualEndPoint);

            // ヒットポイントの位置は実際のヒット位置に
            hitPoint.position = physicsHit.point;
            hitPointObj.SetActive(true);
        }
        else
        {
            // どれにもヒットしなかった場合、表示用の距離を使用
            lineRenderer.SetPosition(1, startPos + direction * maxVisualDistance);
            hitPointObj.SetActive(false);
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
        // レイとキャンバスの交点を計算
        Ray ray = new Ray(startPos, direction);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
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
                                Debug.Log("[LaserPointer] 親にSelectable検出: " + parent.name);
                                targetObject = parent.gameObject;
                                break;
                            }
                            parent = parent.parent;
                        }
                    }

                    Debug.Log("[LaserPointer] UIヒット: " + topResult.gameObject.name +
                              ", 対象オブジェクト: " + targetObject.name +
                              ", 距離: " + rayDistance +
                              ", Selectable: " + (targetObject.GetComponent<Selectable>() != null));

                    hitDistance = rayDistance;

                    // UIとの交差ポイントを設定
                    Vector3 hitPos = worldPos;

                    // レーザーの最大表示距離内なら表示、そうでなければ最大表示距離で切る
                    float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
                    Vector3 visualEndPoint = startPos + direction * visualDistance;
                    lineRenderer.SetPosition(1, visualEndPoint);

                    // ドラッグ中はドットを非表示に固定
                    if (!isDragging)
                    {
                        hitPoint.position = hitPos;
                        hitPointObj.SetActive(true);
                    }
                    else
                    {
                        hitPointObj.SetActive(false);
                    }

                    // UIターゲットを保存
                    currentTarget = targetObject;

                    // ドラッグハンドラーの取得を試みる
                    if (canvasDragHandler == null && canvas != null)
                    {
                        canvasDragHandler = canvas.GetComponentInChildren<CanvasDragHandler>();
                    }

                    // ドラッグ処理と通常のUI操作を区別
                    if (isDragging)
                    {
                        // ドラッグ中はドットを非表示にするので色の変更は不要
                    }
                    else
                    {
                        // 通常のポインタEnter/Exit処理
                        HandlePointerEnterExit(currentTarget, lastTarget);
                    }

                    return true; // UIとの交差あり
                }
            }
        }

        return false; // UIとの交差なし
    }

    // ドット位置を取得するメソッド
    public Vector3 GetDotPosition()
    {
        // ドットが表示されている場合はその位置を返す
        if (hitPointObj != null && hitPointObj.activeSelf)
        {
            return hitPoint.position;
        }

        // ドットが表示されていない場合はレーザーの先端位置を計算して返す
        return transform.position + transform.forward * maxDetectionDistance;
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

    // 残りのコード（HandlePointerEnter, HandlePointerExit, HandlePointerDown, TriggerDown, TriggerUp など）はそのまま使用
    // ...

    // 以下の既存コードはそのまま使用
    private void HandlePointerEnter(GameObject go)
    {
        // より詳細なデバッグ情報を追加
        Debug.Log("[LaserPointer] ポインターEnter: " + go.name);

        // PointerEnterイベントを配信
        cachedPointerData.pointerEnter = go;
        cachedPointerData.position = pointerData.position;

        // Selectableコンポーネントにからかいしてライライトダト処理
        Selectable selectable = go.GetComponent<Selectable>();
        if (selectable != null)
        {
            Debug.Log("[LaserPointer] Selectable検出: " + go.name + ", Interactable: " + selectable.interactable);
            selectable.OnPointerEnter(cachedPointerData);

            // ボタンに直感的なフィードバックを適用
            ColorBlock colors = selectable.colors;
            Image image = go.GetComponent<Image>();
            if (image != null)
            {
                // 色の変更を強制的に適用
                Color targetColor = colors.highlightedColor;
                Debug.Log("[LaserPointer] 色を変更: " + colors.normalColor + " -> " + targetColor);
                image.color = targetColor;
                image.CrossFadeColor(targetColor, colors.fadeDuration, true, true);
            }
        }

        // イベントを配信
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerEnterHandler);
    }

    private void HandlePointerExit(GameObject go)
    {
        // PointerExitイベントを配信
        cachedPointerData.pointerEnter = null;
        cachedPointerData.position = pointerData.position;

        // Selectableコンポーネントに対してのハイライト解除
        Selectable selectable = go.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.OnPointerExit(cachedPointerData);

            // ボタンの直感的なフィードバックを元に戻す
            ColorBlock colors = selectable.colors;
            Image image = go.GetComponent<Image>();
            if (image != null)
            {
                image.CrossFadeColor(colors.normalColor, colors.fadeDuration, true, true);
            }
        }

        // イベントを配信
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerExitHandler);

        // トリガーが押されていた場合は解除
        if (triggerPressed)
        {
            HandlePointerUp(go);
        }
    }

    private void HandlePointerDown(GameObject go)
    {
        // DragHandleかどうかを確認し、ドラッグ開始処理
        if (canvasDragHandler != null && canvasDragHandler.IsDragHandle(go))
        {
            StartDragging();
            return;
        }

        cachedPointerData.pointerPress = go;
        cachedPointerData.pressPosition = pointerData.position;
        cachedPointerData.pointerPressRaycast = pointerData.pointerPressRaycast;

        // SelectableコンポーネントやButtonに対しての押下処理
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

        // イベントを配信
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerDownHandler);
    }

    private void HandlePointerUp(GameObject go)
    {
        // PointerUpイベントを配信
        cachedPointerData.position = pointerData.position;

        // SelectableコンポーネントやButtonの処理
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

        // イベントを配信
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerUpHandler);

        // クリックイベントを配信
        if (cachedPointerData.pointerPress == go)
        {
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerClickHandler);
        }

        cachedPointerData.pointerPress = null;
    }

    private void StartDragging()
    {
        isDragging = true;

        // ドラッグ中はドットを非表示にする
        hitPointObj.SetActive(false);

        // ドラッグハンドラーに通知
        if (canvasDragHandler != null)
        {
            canvasDragHandler.OnStartDrag(this);
        }

        Debug.Log("ドラッグ開始");
    }

    private void EndDragging()
    {
        isDragging = false;

        // ドラッグが終了したらドットを再表示する
        hitPointObj.SetActive(true);
        dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;

        // ドラッグハンドラーに通知
        if (canvasDragHandler != null)
        {
            canvasDragHandler.OnEndDrag();
        }

        Debug.Log("ドラッグ終了");
    }

    public void TriggerDown()
    {
        triggerPressed = true;

        // デバッグ情報
        Debug.Log("[LaserPointer] トリガー押下: ターゲット = " + (currentTarget != null ? currentTarget.name : "なし"));

        // ドラッグ中でなければドットの色を変更
        if (!isDragging && hitPointObj.activeSelf)
        {
            dotRenderer.material.color = dotPressedColor;
        }

        if (currentTarget != null)
        {
            // DragHandleのチェックを追加
            bool isDragHandle = false;
            if (canvasDragHandler != null)
            {
                isDragHandle = canvasDragHandler.IsDragHandle(currentTarget);
                Debug.Log("[LaserPointer] ドラッグハンドル検出: " + isDragHandle);
            }

            // ドラッグハンドルなら処理
            if (isDragHandle)
            {
                StartDragging();
            }
            // 通常のUIなら処理
            else
            {
                // ボタンの検出ログ
                Selectable selectable = currentTarget.GetComponent<Selectable>();
                if (selectable != null)
                {
                    Debug.Log("[LaserPointer] ボタン操作: " + currentTarget.name +
                             ", Interactable: " + selectable.interactable +
                             ", Navigation: " + selectable.navigation.mode);
                }

                // ポインタダウンの処理
                HandlePointerDown(currentTarget);

                // ボタンの押み効果（物理的な）
                RectTransform buttonRect = currentTarget.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.localPosition = new Vector3(
                        buttonRect.localPosition.x,
                        buttonRect.localPosition.y,
                        buttonRect.localPosition.z - 0.001f
                    );
                }
            }
        }
    }

    public void TriggerUp()
    {
        triggerPressed = false;

        // ドラッグ中ならドラッグ終了
        if (isDragging)
        {
            EndDragging();
        }
        else
        {
            // ドット色を元に戻す
            if (hitPointObj.activeSelf)
            {
                dotRenderer.material.color = dotColor;
            }

            if (currentTarget != null)
            {
                // ポインタアップの処理
                HandlePointerUp(currentTarget);

                // ボタンを元の位置に戻す（物理的な）
                RectTransform buttonRect = currentTarget.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.localPosition = new Vector3(
                        buttonRect.localPosition.x,
                        buttonRect.localPosition.y,
                        buttonRect.localPosition.z + 0.001f
                    );
                }
            }
        }
    }
}