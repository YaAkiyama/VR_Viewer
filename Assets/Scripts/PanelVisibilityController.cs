using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PanelVisibilityController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private List<GameObject> controlledPanels = new List<GameObject>();

    [Header("視野角設定")]
    [SerializeField] private float minViewAngleX = -70f;
    [SerializeField] private float maxViewAngleX = 70f;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField][Range(0.1f, 1f)] private float maxAlpha = 1f; // 最大透過度設定

    [Header("パネルアクティブ設定")]
    [SerializeField] private bool isPanelActive = true; // パネルアクティブフラグ（初期値true）

    private Dictionary<GameObject, CanvasGroup> panelCanvasGroups = new Dictionary<GameObject, CanvasGroup>();
    private bool isInViewRange = true;    // 視野角内にいるかどうか
    private bool isFading = false;        // フェード中かどうか
    private Coroutine fadeCoroutine;

    // プロパティを公開
    public float MinViewAngleX => minViewAngleX;
    public float MaxViewAngleX => maxViewAngleX;
    public bool IsPanelActive => isPanelActive; // パネルアクティブ状態の取得プロパティ

    void Start()
    {
        try
        {
            Debug.Log("[PanelVisibilityController] 初期化開始...");

            // カメラの参照を取得
            if (cameraTransform == null)
            {
                var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null && xrOrigin.Camera != null)
                {
                    cameraTransform = xrOrigin.Camera.transform;
                    Debug.Log("[PanelVisibilityController] カメラをXROriginから取得");
                }
                else
                {
                    cameraTransform = Camera.main.transform;
                    Debug.Log("[PanelVisibilityController] カメラをCamera.mainから取得");
                }
            }

            if (cameraTransform != null)
            {
                Debug.Log($"[PanelVisibilityController] カメラ情報: Position={cameraTransform.position}, Rotation={cameraTransform.rotation.eulerAngles}");
            }
            else
            {
                Debug.LogError("[PanelVisibilityController] カメラの参照を取得できませんでした");
            }

            // パネルの初期化
            Debug.Log($"[PanelVisibilityController] コントロール対象パネル数: {controlledPanels.Count}");

            foreach (var panel in controlledPanels)
            {
                if (panel == null)
                {
                    Debug.LogWarning("[PanelVisibilityController] Nullパネルがリストに含まれています");
                    continue;
                }

                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = panel.AddComponent<CanvasGroup>();
                    Debug.Log($"[PanelVisibilityController] CanvasGroupを追加: {panel.name}");
                }

                panelCanvasGroups[panel] = canvasGroup;

                // 初期状態は表示（maxAlphaを使用）
                canvasGroup.alpha = maxAlpha;
                canvasGroup.interactable = isPanelActive; // パネルアクティブ状態に合わせて設定
                canvasGroup.blocksRaycasts = isPanelActive; // パネルアクティブ状態に合わせて設定

                Debug.Log($"[PanelVisibilityController] パネル初期化: {panel.name}, Alpha={maxAlpha}, Interactable={isPanelActive}");
            }

            // 初期状態を設定: 視野角内として初期化
            isInViewRange = true;

            Debug.Log("[PanelVisibilityController] 初期化完了");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] 初期化エラー: {e.Message}\n{e.StackTrace}");
        }
    }

    void Update()
    {
        try
        {
            if (cameraTransform == null)
            {
                // カメラが見つからない場合は再取得を試みる
                var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null && xrOrigin.Camera != null)
                {
                    cameraTransform = xrOrigin.Camera.transform;
                    Debug.Log("[PanelVisibilityController] カメラを再取得しました");
                }
                else
                {
                    cameraTransform = Camera.main.transform;
                }

                if (cameraTransform == null)
                {
                    return;
                }
            }

            // パネルがアクティブでない場合は視野角チェックをスキップ
            if (!isPanelActive)
            {
                // フェード中は何もしない（フェード完了まで待つ）
                if (!isFading)
                {
                    return;
                }
            }

            // カメラの前方向ベクトルから角度を計算
            float cameraXRotation = cameraTransform.eulerAngles.x;

            // 0-360度範囲から-180から180度範囲に変換
            if (cameraXRotation > 180f)
                cameraXRotation -= 360f;

            // 毎秒1回程度、状態をログに出力
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[PanelVisibilityController] 状態: カメラX角度={cameraXRotation}, " +
                          $"視野角範囲={minViewAngleX}～{maxViewAngleX}, " +
                          $"視野角内={isInViewRange}, " +
                          $"フェード中={isFading}, " + 
                          $"パネルアクティブ={isPanelActive}");
            }

            // パネルがアクティブでない場合は視野角による表示制御をスキップ
            if (!isPanelActive)
            {
                return;
            }

            // 視野角範囲内かどうかをチェック
            bool newIsInViewRange = (cameraXRotation >= minViewAngleX && cameraXRotation <= maxViewAngleX);

            // 視野角状態が変化したか、フェードが必要かチェック
            if (newIsInViewRange != isInViewRange || NeedsFade(newIsInViewRange))
            {
                isInViewRange = newIsInViewRange;

                // 現在実行中のフェードがあれば停止
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    isFading = false;
                }

                // 新しいフェードを開始
                fadeCoroutine = StartCoroutine(DynamicFade());

                Debug.Log($"[PanelVisibilityController] 視野角状態変更: {(isInViewRange ? "範囲内" : "範囲外")}、フェード開始");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] Update エラー: {e.Message}");
        }
    }

    // パネルの現在のアルファ値と目標値を確認し、フェードが必要かチェック
    private bool NeedsFade(bool inViewRange)
    {
        // パネルがアクティブでない場合はフェード不要
        if (!isPanelActive)
        {
            return false;
        }

        float targetAlpha = inViewRange ? maxAlpha : 0f;

        foreach (var canvasGroup in panelCanvasGroups.Values)
        {
            if (canvasGroup != null)
            {
                // 現在のアルファ値と目標のアルファ値に差がある場合はフェードが必要
                if (Mathf.Abs(canvasGroup.alpha - targetAlpha) > 0.01f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // 動的にフェード状態を変更できるコルーチン
    private IEnumerator DynamicFade()
    {
        isFading = true;

        Debug.Log($"[PanelVisibilityController] 動的フェード開始");

        // フェード処理の初期状態を記録
        Dictionary<CanvasGroup, float> startAlphas = new Dictionary<CanvasGroup, float>();
        foreach (var canvasGroup in panelCanvasGroups.Values)
        {
            if (canvasGroup != null)
            {
                startAlphas[canvasGroup] = canvasGroup.alpha;
            }
        }

        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed; // フェード完了までの時間
        
        // パネルアクティブ状態と視野角状態に基づいて目標アルファ値を決定
        float targetAlpha = isPanelActive && isInViewRange ? maxAlpha : 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            // 各パネルのフェード処理
            foreach (var canvasGroup in panelCanvasGroups.Values)
            {
                if (canvasGroup != null)
                {
                    // 開始アルファ値からターゲットアルファ値に補間
                    canvasGroup.alpha = Mathf.Lerp(startAlphas[canvasGroup], targetAlpha, t);

                    // インタラクションの設定（パネルアクティブかつ視野角内の場合のみインタラクション可能）
                    bool shouldInteract = isPanelActive && (canvasGroup.alpha > 0.1f);
                    canvasGroup.interactable = shouldInteract;
                    canvasGroup.blocksRaycasts = shouldInteract;
                }
            }

            yield return null;

            // フェード中にパネルアクティブ状態が変わった場合、ターゲットアルファを再設定
            float newTargetAlpha = isPanelActive && isInViewRange ? maxAlpha : 0f;
            if (newTargetAlpha != targetAlpha)
            {
                // ただし、パネルが非アクティブになった場合は必ずフェードアウトに向かう
                if (!isPanelActive)
                {
                    newTargetAlpha = 0f;
                }

                Debug.Log($"[PanelVisibilityController] フェード中に状態が変化: パネルアクティブ={isPanelActive}, 視野角内={isInViewRange}, 新ターゲットAlpha={newTargetAlpha}");
                
                targetAlpha = newTargetAlpha;
                
                // 現在の値を開始値として記録し直す
                startAlphas.Clear();
                foreach (var canvasGroup in panelCanvasGroups.Values)
                {
                    if (canvasGroup != null)
                    {
                        startAlphas[canvasGroup] = canvasGroup.alpha;
                    }
                }

                // 時間をリセット
                elapsedTime = 0f;
            }
        }

        // 最終状態を確実に設定
        bool finalInteractable = isPanelActive && (targetAlpha > 0.1f);
        foreach (var canvasGroup in panelCanvasGroups.Values)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
                canvasGroup.interactable = finalInteractable;
                canvasGroup.blocksRaycasts = finalInteractable;
            }
        }

        Debug.Log($"[PanelVisibilityController] フェード完了: Alpha={targetAlpha}, Interactable={finalInteractable}");
        isFading = false;
    }

    // パネルアクティブ状態を切り替えるメソッド（A ボタン用）
    public void TogglePanelActive()
    {
        // 現在の値を反転
        bool previousState = isPanelActive;
        isPanelActive = !isPanelActive;
        
        Debug.Log($"[PanelVisibilityController] パネルアクティブ状態切替: {previousState} -> {isPanelActive}");
        
        // 非アクティブに変更された場合、強制的にフェードアウト
        if (previousState && !isPanelActive)
        {
            // フェード処理が既に実行中なら停止
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                isFading = false;
            }
            
            // 非アクティブ化に伴うフェードアウトを開始
            fadeCoroutine = StartCoroutine(DynamicFade());
            Debug.Log("[PanelVisibilityController] パネル非アクティブ化に伴うフェードアウト開始");
        }
    }

    // パネルを追加
    public void AddPanel(GameObject panel)
    {
        try
        {
            if (panel == null)
            {
                Debug.LogWarning("[PanelVisibilityController] AddPanel: パネルがnullです");
                return;
            }

            if (controlledPanels.Contains(panel))
            {
                Debug.Log($"[PanelVisibilityController] {panel.name} は既に追加されています");
                return;
            }

            controlledPanels.Add(panel);
            Debug.Log($"[PanelVisibilityController] パネルを追加: {panel.name}");

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
                Debug.Log($"[PanelVisibilityController] {panel.name} にCanvasGroupを追加");
            }

            panelCanvasGroups[panel] = canvasGroup;

            // 現在の視野角状態に合わせて表示/非表示を設定
            bool shouldBeVisible = isPanelActive && isInViewRange;
            canvasGroup.alpha = shouldBeVisible ? maxAlpha : 0f;
            canvasGroup.interactable = shouldBeVisible;
            canvasGroup.blocksRaycasts = shouldBeVisible;

            Debug.Log($"[PanelVisibilityController] パネル設定完了: {panel.name}, Alpha={canvasGroup.alpha}, Interactable={shouldBeVisible}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] AddPanel エラー: {e.Message}");
        }
    }

    // パネルを削除
    public void RemovePanel(GameObject panel)
    {
        try
        {
            if (panel == null)
            {
                Debug.LogWarning("[PanelVisibilityController] RemovePanel: パネルがnullです");
                return;
            }

            if (!controlledPanels.Contains(panel))
            {
                Debug.Log($"[PanelVisibilityController] {panel.name} はリストに含まれていません");
                return;
            }

            controlledPanels.Remove(panel);
            Debug.Log($"[PanelVisibilityController] パネルを削除: {panel.name}");

            if (panelCanvasGroups.ContainsKey(panel))
            {
                panelCanvasGroups.Remove(panel);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] RemovePanel エラー: {e.Message}");
        }
    }

    // 視野角範囲を設定
    public void SetViewAngleRange(float min, float max)
    {
        minViewAngleX = min;
        maxViewAngleX = max;
        Debug.Log($"[PanelVisibilityController] 視野角範囲設定: {min}～{max}");
    }

    // 視野角内かどうかを返すメソッド
    public bool IsInViewRange()
    {
        try
        {
            if (cameraTransform == null)
            {
                Debug.LogWarning("[PanelVisibilityController] IsInViewRange: カメラがnullです");
                return false;
            }

            // カメラの前方向ベクトルから角度を計算
            float cameraXRotation = cameraTransform.eulerAngles.x;

            // 0-360度範囲から-180から180度範囲に変換
            if (cameraXRotation > 180f)
                cameraXRotation -= 360f;

            // 視野範囲内かどうかをチェック
            return (cameraXRotation >= minViewAngleX && cameraXRotation <= maxViewAngleX);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] IsInViewRange エラー: {e.Message}");
            return false;
        }
    }

    // フェード中かどうかを返すメソッド
    public bool IsFading()
    {
        return isFading;
    }

    // 最大透過度を設定するメソッド
    public void SetMaxAlpha(float alpha)
    {
        maxAlpha = Mathf.Clamp01(alpha);
        Debug.Log($"[PanelVisibilityController] 最大透過度設定: {maxAlpha}");
    }

    // パネルアクティブ状態を設定するメソッド
    public void SetPanelActive(bool active)
    {
        if (isPanelActive != active)
        {
            bool previousState = isPanelActive;
            isPanelActive = active;
            
            Debug.Log($"[PanelVisibilityController] パネルアクティブ状態設定: {previousState} -> {isPanelActive}");
            
            // 非アクティブに変更された場合、強制的にフェードアウト
            if (previousState && !isPanelActive)
            {
                // フェード処理が既に実行中なら停止
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    isFading = false;
                }
                
                // 非アクティブ化に伴うフェードアウトを開始
                fadeCoroutine = StartCoroutine(DynamicFade());
                Debug.Log("[PanelVisibilityController] パネル非アクティブ化に伴うフェードアウト開始");
            }
        }
    }

    // 互換性のために残す - 強制状態かどうかを返すメソッド
    public bool IsForcedState()
    {
        // パネルアクティブがfalseの場合は強制的に非表示状態
        return !isPanelActive;
    }

    // 互換性のために残す - 強制的に表示されているかを返すメソッド
    public bool IsForcedVisible()
    {
        // パネルアクティブがfalseの場合は強制的に非表示
        // パネルアクティブがtrueの場合は視野角内にいるかどうかで判断
        return isPanelActive && isInViewRange;
    }
}