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

    private Dictionary<GameObject, CanvasGroup> panelCanvasGroups = new Dictionary<GameObject, CanvasGroup>();
    private bool isInViewRange = true;    // 視野角内にいるかどうか
    private bool forcedVisible = true;    // 強制表示状態（true=表示、false=非表示）
    private bool isForced = false;        // 強制状態フラグ（true=強制状態有効、false=通常状態）
    private bool isFading = false;        // フェード中かどうか
    private Coroutine fadeCoroutine;

    // プロパティを公開
    public float MinViewAngleX => minViewAngleX;
    public float MaxViewAngleX => maxViewAngleX;

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

                // 初期状態は表示
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                Debug.Log($"[PanelVisibilityController] パネル初期化: {panel.name}, Alpha=1.0");
            }

            // 初期状態設定: 通常状態（視野角による）
            isForced = false;
            isInViewRange = true;  // 初期状態では視野角内と仮定

            Debug.Log("[PanelVisibilityController] 初期化完了: 通常状態（視野角による）");
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
                          $"強制状態={isForced}, " +
                          $"強制表示={forcedVisible}");
            }

            // 視野角範囲内かどうかをチェック
            bool newIsInViewRange = (cameraXRotation >= minViewAngleX && cameraXRotation <= maxViewAngleX);

            // 視野角状態が変化した場合
            if (newIsInViewRange != isInViewRange)
            {
                bool oldIsInViewRange = isInViewRange;
                isInViewRange = newIsInViewRange;

                // 強制状態と視野角状態が一致した場合、強制状態を解除
                if (isForced && forcedVisible == isInViewRange)
                {
                    isForced = false;
                    Debug.Log("[PanelVisibilityController] 視野角状態と一致したため強制状態を解除");
                }

                // 強制状態でない場合のみフェード処理
                if (!isForced && !isFading && oldIsInViewRange != newIsInViewRange)
                {
                    // 既存のフェードコルーチンがあれば停止
                    if (fadeCoroutine != null)
                    {
                        StopCoroutine(fadeCoroutine);
                    }

                    // 新しいフェードコルーチンを開始
                    fadeCoroutine = StartCoroutine(FadePanels(isInViewRange));

                    Debug.Log($"[PanelVisibilityController] 通常フェード: {(isInViewRange ? "表示" : "非表示")}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] Update エラー: {e.Message}");
        }
    }

    // パネルをフェードイン/アウトさせるコルーチン
    private IEnumerator FadePanels(bool fadeIn)
    {
        isFading = true;

        Debug.Log($"[PanelVisibilityController] フェード開始: {(fadeIn ? "フェードイン" : "フェードアウト")}");

        float targetAlpha = fadeIn ? 1f : 0f;
        Dictionary<CanvasGroup, float> startAlphas = new Dictionary<CanvasGroup, float>();

        try
        {
            // 開始アルファ値を記録
            foreach (var canvasGroup in panelCanvasGroups.Values)
            {
                if (canvasGroup != null)
                {
                    startAlphas[canvasGroup] = canvasGroup.alpha;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] フェード初期化エラー: {e.Message}");
            isFading = false;
            yield break; // コルーチンを終了
        }

        // フェードアニメーション
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * fadeSpeed;
            float t = Mathf.Clamp01(time);

            try
            {
                foreach (var canvasGroup in panelCanvasGroups.Values)
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = Mathf.Lerp(startAlphas[canvasGroup], targetAlpha, t);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PanelVisibilityController] フェード処理中エラー: {e.Message}");
            }

            yield return null;
        }

        try
        {
            // 最終値を確実に設定
            foreach (var canvasGroup in panelCanvasGroups.Values)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = targetAlpha;
                    canvasGroup.interactable = fadeIn;
                    canvasGroup.blocksRaycasts = fadeIn;
                }
            }

            Debug.Log($"[PanelVisibilityController] フェード完了: {(fadeIn ? "フェードイン" : "フェードアウト")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] フェード最終処理エラー: {e.Message}");
        }
        finally
        {
            isFading = false;
        }
    }

    // 強制的にパネルの表示/非表示を切り替える
    public void ToggleForcedVisibility()
    {
        try
        {
            Debug.Log("[PanelVisibilityController] Aボタン押下: ToggleForcedVisibility呼び出し");

            // フェード中は操作を無視
            if (isFading)
            {
                Debug.Log("[PanelVisibilityController] フェード中のため操作を無視");
                return;
            }

            // 既に強制状態の場合
            if (isForced)
            {
                // 強制表示状態を反転
                forcedVisible = !forcedVisible;

                Debug.Log($"[PanelVisibilityController] 強制フェード切替: {(forcedVisible ? "フェードイン" : "フェードアウト")}");

                // 視野角の状態と一致した場合は強制状態を解除
                if (forcedVisible == isInViewRange)
                {
                    isForced = false;
                    Debug.Log("[PanelVisibilityController] 視野角状態と一致したため強制状態を解除");
                }
            }
            else
            {
                // 通常状態から強制状態への切替
                // 現在の視野角状態と反対の状態にする
                forcedVisible = !isInViewRange;
                isForced = true;
                Debug.Log($"[PanelVisibilityController] 通常状態から強制状態へ切替: {(forcedVisible ? "フェードイン" : "フェードアウト")}");
            }

            // 既存のフェードコルーチンがあれば停止
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // 新しいフェードコルーチンを開始
            fadeCoroutine = StartCoroutine(FadePanels(forcedVisible));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] ToggleForcedVisibility エラー: {e.Message}");
        }
    }

    // 強制状態を解除して通常状態に戻す
    public void DisableForcedState()
    {
        try
        {
            if (!isForced) return;

            isForced = false;

            Debug.Log("[PanelVisibilityController] 強制状態を解除: 通常状態に戻ります");

            // 視野角の状態と現在の表示状態が異なる場合、フェードを行う
            if (isInViewRange != forcedVisible && !isFading)
            {
                // 既存のフェードコルーチンがあれば停止
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                // 視野角の状態に合わせてフェード
                fadeCoroutine = StartCoroutine(FadePanels(isInViewRange));
                Debug.Log($"[PanelVisibilityController] 通常状態に戻るフェード: {(isInViewRange ? "表示" : "非表示")}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PanelVisibilityController] DisableForcedState エラー: {e.Message}");
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

            // 現在の表示状態に合わせる
            bool shouldBeVisible = isForced ? forcedVisible : isInViewRange;
            canvasGroup.alpha = shouldBeVisible ? 1f : 0f;
            canvasGroup.interactable = shouldBeVisible;
            canvasGroup.blocksRaycasts = shouldBeVisible;

            Debug.Log($"[PanelVisibilityController] パネル設定完了: {panel.name}, Alpha={canvasGroup.alpha}");
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

    // 強制状態かどうかを返すメソッド
    public bool IsForcedState()
    {
        return isForced;
    }

    // 現在の強制表示状態を返すメソッド
    public bool IsForcedVisible()
    {
        return forcedVisible;
    }

    // フェード中かどうかを返すメソッド
    public bool IsFading()
    {
        return isFading;
    }
}