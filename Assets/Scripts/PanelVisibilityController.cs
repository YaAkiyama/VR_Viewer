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
    private bool isInViewRange = true;
    private Coroutine fadeCoroutine;

    void Start()
    {
        // カメラの参照を取得
        if (cameraTransform == null)
        {
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                cameraTransform = xrOrigin.Camera.transform;
            }
            else
            {
                cameraTransform = Camera.main.transform;
            }
        }

        // 各パネルにCanvasGroupを設定
        foreach (var panel in controlledPanels)
        {
            if (panel == null) continue;

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            panelCanvasGroups[panel] = canvasGroup;
            canvasGroup.alpha = 1f;
        }
    }

    void Update()
    {
        if (cameraTransform == null) return;

        // カメラの前方向ベクトルから角度を計算
        float cameraXRotation = cameraTransform.eulerAngles.x;

        // 0-360度範囲から-180から180度範囲に変換
        if (cameraXRotation > 180f)
            cameraXRotation -= 360f;

        // 視野範囲内かどうかをチェック
        bool newIsInViewRange = (cameraXRotation >= minViewAngleX && cameraXRotation <= maxViewAngleX);

        // 状態が変わった場合のみフェード処理を開始
        if (newIsInViewRange != isInViewRange)
        {
            isInViewRange = newIsInViewRange;

            // 既存のフェードコルーチンがあれば停止
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // 新しいフェードコルーチンを開始
            fadeCoroutine = StartCoroutine(FadePanels(isInViewRange));
        }
    }

    // パネルをフェードイン/アウトさせるコルーチン
    private IEnumerator FadePanels(bool fadeIn)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        Dictionary<CanvasGroup, float> startAlphas = new Dictionary<CanvasGroup, float>();

        // 開始アルファ値を記録
        foreach (var canvasGroup in panelCanvasGroups.Values)
        {
            if (canvasGroup != null)
            {
                startAlphas[canvasGroup] = canvasGroup.alpha;
            }
        }

        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * fadeSpeed;
            float t = Mathf.Clamp01(time);

            foreach (var canvasGroup in panelCanvasGroups.Values)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlphas[canvasGroup], targetAlpha, t);
                }
            }

            yield return null;
        }

        // 最終値を確実に設定し、インタラクションを調整
        foreach (var canvasGroup in panelCanvasGroups.Values)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
                canvasGroup.interactable = fadeIn;
                canvasGroup.blocksRaycasts = fadeIn;
            }
        }
    }

    // パネルを追加
    public void AddPanel(GameObject panel)
    {
        if (panel == null || controlledPanels.Contains(panel)) return;

        controlledPanels.Add(panel);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        panelCanvasGroups[panel] = canvasGroup;
        canvasGroup.alpha = isInViewRange ? 1f : 0f;
        canvasGroup.interactable = isInViewRange;
        canvasGroup.blocksRaycasts = isInViewRange;
    }

    // パネルを削除
    public void RemovePanel(GameObject panel)
    {
        if (panel == null || !controlledPanels.Contains(panel)) return;

        controlledPanels.Remove(panel);

        if (panelCanvasGroups.ContainsKey(panel))
        {
            panelCanvasGroups.Remove(panel);
        }
    }

    // 視野角範囲を設定
    public void SetViewAngleRange(float min, float max)
    {
        minViewAngleX = min;
        maxViewAngleX = max;
    }
}