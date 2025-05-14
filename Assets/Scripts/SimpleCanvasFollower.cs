using UnityEngine;
using Unity.XR.CoreUtils;

public class SimpleCanvasFollower : MonoBehaviour
{
    [Header("追従設定")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 _positionOffset = new Vector3(0f, 0f, 0.5f);

    [Header("回転設定")]
    [SerializeField] private bool lookAtCamera = true;

    [Tooltip("選択した軸を固定するかどうか")]
    [SerializeField] private bool lockXRotation = false;
    [SerializeField] private bool lockYRotation = false;
    [SerializeField] private bool lockZRotation = false;

    [Tooltip("固定する場合の各軸の値")]
    [SerializeField] private float fixedXRotation = 0f;
    [SerializeField] private float fixedYRotation = 0f;
    [SerializeField] private float fixedZRotation = 0f;

    [Header("相対配置設定")]
    [SerializeField] private bool useRelativePositioning = false;
    [SerializeField] private Transform referenceCanvas;
    [SerializeField] private Vector3 relativeOffset = new Vector3(0f, -0.15f, 0f);

    // 外部からアクセスするためのプロパティ
    public Vector3 positionOffset
    {
        get { return _positionOffset; }
        set { _positionOffset = value; }
    }

    void Start()
    {
        // カメラの参照を取得
        if (cameraTransform == null)
        {
            // XR環境の場合
            var xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                cameraTransform = xrOrigin.Camera.transform;
                Debug.Log("XRカメラを参照として設定しました: " + gameObject.name);
            }
            else
            {
                // 通常環境の場合
                cameraTransform = Camera.main.transform;

                if (cameraTransform != null)
                {
                    Debug.Log("メインカメラを参照として設定しました: " + gameObject.name);
                }
                else
                {
                    Debug.LogError("カメラが見つかりません。手動で参照を設定してください: " + gameObject.name);
                }
            }
        }

        // 相対配置の参照設定をチェック
        if (useRelativePositioning && referenceCanvas == null)
        {
            Debug.LogWarning("相対配置が有効ですが、参照Canvasが設定されていません: " + gameObject.name);
        }

        // 初期回転を設定
        ApplyRotation();
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 位置を計算
        Vector3 targetPosition;

        if (useRelativePositioning && referenceCanvas != null)
        {
            // 参照Canvasからの相対位置を計算
            targetPosition = CalculateRelativePosition();
        }
        else
        {
            // カメラに対する直接位置を計算
            targetPosition = CalculateDirectPosition();
        }

        // 位置を適用
        transform.position = targetPosition;

        // 回転を適用
        ApplyRotation();
    }

    // カメラに対する直接位置計算
    private Vector3 CalculateDirectPosition()
    {
        return cameraTransform.position +
               cameraTransform.forward * _positionOffset.z +
               cameraTransform.up * _positionOffset.y +
               cameraTransform.right * _positionOffset.x;
    }

    // 参照Canvasに対する相対位置計算
    private Vector3 CalculateRelativePosition()
    {
        // 参照Canvasからの相対位置を計算
        Vector3 referencePosition = referenceCanvas.position;

        // 上下位置の調整（参照Canvasの座標系で計算）
        Vector3 upOffset = referenceCanvas.up * relativeOffset.y;
        Vector3 rightOffset = referenceCanvas.right * relativeOffset.x;
        Vector3 forwardOffset = referenceCanvas.forward * relativeOffset.z;

        return referencePosition + upOffset + rightOffset + forwardOffset;
    }

    // 回転を適用（軸固定を考慮）
    private void ApplyRotation()
    {
        if (cameraTransform == null) return;

        if (useRelativePositioning && referenceCanvas != null)
        {
            // 参照Canvasと同じ回転を使用
            transform.rotation = referenceCanvas.rotation;
        }
        else if (lookAtCamera)
        {
            // 基本的にカメラの方向に向かせる
            transform.LookAt(cameraTransform);
            transform.Rotate(0, 180, 0); // パネルが正面を向くように調整

            // 現在の回転を取得
            Vector3 currentRotation = transform.rotation.eulerAngles;

            // 各軸ごとに固定するかどうかを判断し、必要に応じて修正
            float newX = lockXRotation ? fixedXRotation : currentRotation.x;
            float newY = lockYRotation ? fixedYRotation : currentRotation.y;
            float newZ = lockZRotation ? fixedZRotation : currentRotation.z;

            // 軸固定が指定されている場合のみ回転を更新
            if (lockXRotation || lockYRotation || lockZRotation)
            {
                transform.rotation = Quaternion.Euler(newX, newY, newZ);
            }
        }
    }

    // 相対配置を設定
    public void SetRelativePositioning(bool enable, Transform reference = null, Vector3 offset = default)
    {
        useRelativePositioning = enable;

        if (reference != null)
        {
            referenceCanvas = reference;
        }

        if (offset != default)
        {
            relativeOffset = offset;
        }

        // すぐに適用
        if (cameraTransform != null)
        {
            LateUpdate();
        }
    }

    // プリセット: X軸を固定
    public void SetLockXOnly()
    {
        lockXRotation = true;
        lockYRotation = false;
        lockZRotation = false;
        ApplyRotation();
    }

    // プリセット: Y軸を固定
    public void SetLockYOnly()
    {
        lockXRotation = false;
        lockYRotation = true;
        lockZRotation = false;
        ApplyRotation();
    }

    // プリセット: XとY軸を固定
    public void SetLockXAndY()
    {
        lockXRotation = true;
        lockYRotation = true;
        lockZRotation = false;
        ApplyRotation();
    }

    // プリセット: すべての軸を固定
    public void SetLockAllAxes()
    {
        lockXRotation = true;
        lockYRotation = true;
        lockZRotation = true;
        ApplyRotation();
    }

    // プリセット: すべての軸を固定解除
    public void SetUnlockAllAxes()
    {
        lockXRotation = false;
        lockYRotation = false;
        lockZRotation = false;
        ApplyRotation();
    }

    // 回転をリセット
    public void ResetRotation()
    {
        ApplyRotation();
        Debug.Log("回転をリセットしました: " + gameObject.name);
    }

    // デバッグ用: 現在の設定を表示
    public void LogCurrentSettings()
    {
        Debug.Log($"===== {gameObject.name} 設定 =====");
        Debug.Log($"カメラ参照: {(cameraTransform != null ? cameraTransform.name : "設定なし")}");
        Debug.Log($"位置オフセット: {_positionOffset}");
        Debug.Log($"カメラを向く: {lookAtCamera}");
        Debug.Log($"X軸固定: {lockXRotation} (値: {fixedXRotation})");
        Debug.Log($"Y軸固定: {lockYRotation} (値: {fixedYRotation})");
        Debug.Log($"Z軸固定: {lockZRotation} (値: {fixedZRotation})");
        Debug.Log($"相対配置: {useRelativePositioning}");
        Debug.Log($"参照Canvas: {(referenceCanvas != null ? referenceCanvas.name : "設定なし")}");
        Debug.Log($"相対オフセット: {relativeOffset}");
        Debug.Log($"==================================");
    }

    // Gizmoの描画（エディタ上での視覚化）
    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null) return;

        // 位置を計算
        Vector3 targetPosition;

        if (useRelativePositioning && referenceCanvas != null)
        {
            targetPosition = CalculateRelativePosition();
        }
        else
        {
            targetPosition = CalculateDirectPosition();
        }

        // 位置を示すGizmoを描画
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(targetPosition, 0.01f);

        // 参照ラインを描画
        if (useRelativePositioning && referenceCanvas != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(referenceCanvas.position, targetPosition);
        }
        else
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cameraTransform.position, targetPosition);
        }
    }
}