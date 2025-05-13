using UnityEngine;
using UnityEngine.UI;

public class PanelVisibilityController : MonoBehaviour
{
    [Header("パネル設定")]
    public Canvas mapCanvas;
    public Canvas thumbnailCanvas;

    [Header("参照")]
    public VRLaserPointer laserPointer;

    private void Start()
    {
        // 各キャンバスがGraphicRaycasterコンポーネントを持っているか確認
        EnsureGraphicRaycaster(mapCanvas);
        EnsureGraphicRaycaster(thumbnailCanvas);

        // レーザーポインターにキャンバスのレイヤーを設定
        if (laserPointer != null)
        {
            // キャンバスのレイヤーをインタラクションレイヤーに追加
            laserPointer.interactionLayers |= (1 << mapCanvas.gameObject.layer);
            laserPointer.interactionLayers |= (1 << thumbnailCanvas.gameObject.layer);
        }
    }

    // GraphicRaycasterがない場合は追加
    private void EnsureGraphicRaycaster(Canvas canvas)
    {
        if (canvas == null) return;

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log($"GraphicRaycaster added to {canvas.name}");
        }
    }

    // パネルの表示/非表示を切り替え
    public void ToggleMapCanvas()
    {
        if (mapCanvas != null)
        {
            mapCanvas.enabled = !mapCanvas.enabled;
        }
    }

    public void ToggleThumbnailCanvas()
    {
        if (thumbnailCanvas != null)
        {
            thumbnailCanvas.enabled = !thumbnailCanvas.enabled;
        }
    }
}