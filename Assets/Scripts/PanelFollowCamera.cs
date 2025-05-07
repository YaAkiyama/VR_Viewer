using UnityEngine;
using Unity.XR.CoreUtils; // XROrigin用の正しい名前空間

public class PanelFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distance = 0.5f; // カメラからの距離
    [SerializeField] private Vector2 screenOffset = new Vector2(-0.3f, 0.3f); // 画面上の位置調整（X:左右、Y:上下）

    // 上下の制限を追加
    [SerializeField] private float minVerticalOffset = 0.1f;  // 最低の高さ（カメラ中心からの距離）
    [SerializeField] private float maxVerticalOffset = 0.4f;  // 最高の高さ（カメラ中心からの距離）

    private void Start()
    {
        // Camera Rigからメインカメラを取得
        if (cameraTransform == null)
        {
            // XR Originからカメラを自動検出（最新のメソッドを使用）
            var xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
                cameraTransform = xrOrigin.Camera.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // カメラの姿勢を考慮して上下の限界を計算
        float currentVerticalOffset = screenOffset.y;

        // 上下位置を制限
        currentVerticalOffset = Mathf.Clamp(currentVerticalOffset, minVerticalOffset, maxVerticalOffset);

        // カメラの前方向に配置（距離を指定）
        Vector3 forwardPosition = cameraTransform.position + cameraTransform.forward * distance;

        // 画面の左上に位置調整（制限をかけた垂直位置を使用）
        Vector3 offsetPosition = forwardPosition +
                               cameraTransform.right * screenOffset.x +
                               cameraTransform.up * currentVerticalOffset;

        transform.position = offsetPosition;

        // カメラと同じ向きに設定（常に正面を向く）
        transform.rotation = cameraTransform.rotation;
    }
}