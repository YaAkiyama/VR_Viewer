using UnityEngine;
using UnityEditor;

/// <summary>
/// レーザーポインターの位置と色の問題を修正するための緊急フィクサー
/// </summary>
[ExecuteInEditMode]
public class VRLaserFixDirectly : MonoBehaviour
{
    [Header("レーザー設定")]
    [SerializeField] private bool forceUseControllerTransform = true;
    [SerializeField] private bool useControllerForward = true;
    [SerializeField] private Color laserColor = new Color(0.0f, 0.8f, 1.0f, 0.7f); // 青色
    [SerializeField] private bool applyColorNow = true;

    [Header("レーザーの見た目")]
    [SerializeField] private float rayStartWidth = 0.003f;
    [SerializeField] private float rayEndWidth = 0.0005f;
    [SerializeField] private float maxVisualDistance = 0.8f;
    
    [Header("デバッグ")]
    [SerializeField] private bool showDebugLines = true;
    [SerializeField] private float debugLineLength = 0.2f;
    [SerializeField] private Color debugColor = Color.red;

    // レーザー関連コンポーネント
    private VRLaserPointer laserPointer;
    private LineRenderer lineRenderer;
    private Transform controllerTransform;

    private void OnEnable()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // コンポーネントの取得
        laserPointer = GetComponent<VRLaserPointer>();
        lineRenderer = GetComponent<LineRenderer>();

        // コントローラーの位置を特定
        Transform current = transform;
        while (current.parent != null)
        {
            if (current.name.Contains("Controller") && !current.name.Contains("InHand"))
            {
                controllerTransform = current;
                break;
            }
            current = current.parent;
        }

        // 見つからなかった場合は親を使用
        if (controllerTransform == null && transform.parent != null)
        {
            controllerTransform = transform.parent;
        }

        if (Application.isPlaying && applyColorNow)
        {
            ApplyColorAndWidthSettings();
        }

        Debug.Log($"[LaserFixDirectly] 初期化: Controller={controllerTransform?.name}, LaserPointer={laserPointer != null}");
    }

    private void Update()
    {
        if (laserPointer == null || lineRenderer == null) return;

        // レーザーの開始位置を強制的に設定（コントローラーの位置）
        if (forceUseControllerTransform && controllerTransform != null)
        {
            if (lineRenderer.positionCount >= 2)
            {
                lineRenderer.SetPosition(0, controllerTransform.position);
                
                // レーザーの向きを設定（コントローラーの向き）
                Vector3 direction = useControllerForward 
                    ? controllerTransform.forward 
                    : transform.forward;
                
                Vector3 endPosition = controllerTransform.position + direction * maxVisualDistance;
                lineRenderer.SetPosition(1, endPosition);
            }
        }

        // デバッグライン
        if (showDebugLines && controllerTransform != null)
        {
            Debug.DrawLine(
                controllerTransform.position, 
                controllerTransform.position + controllerTransform.forward * debugLineLength, 
                debugColor
            );
        }
    }

    public void ApplyColorAndWidthSettings()
    {
        if (lineRenderer == null) return;

        // カラーを設定
        Color endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.0f);
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = endColor;

        // 幅を設定
        lineRenderer.startWidth = rayStartWidth;
        lineRenderer.endWidth = rayEndWidth;

        // マテリアルを更新
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = laserColor;
            lineRenderer.material.SetColor("_BaseColor", laserColor);
            lineRenderer.material.SetColor("_EmissionColor", laserColor * 1.5f);
            lineRenderer.material.EnableKeyword("_EMISSION");
        }
        else
        {
            Debug.LogWarning("[LaserFixDirectly] マテリアルがnullです");
        }

        Debug.Log($"[LaserFixDirectly] 色と幅を適用: Color={laserColor}, StartWidth={rayStartWidth}, EndWidth={rayEndWidth}");
    }

    // VRLaserPointerの設定も更新
    public void UpdateVRLaserPointerSettings()
    {
        if (laserPointer == null) return;

        // リフレクションを使用してprivateフィールドを更新する
        var type = laserPointer.GetType();
        
        // rayColorフィールド
        var rayColorField = type.GetField("rayColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rayColorField != null) rayColorField.SetValue(laserPointer, laserColor);
        
        // rayEndColorフィールド
        var rayEndColorField = type.GetField("rayEndColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Color endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.0f);
        if (rayEndColorField != null) rayEndColorField.SetValue(laserPointer, endColor);
        
        // rayStartWidthフィールド
        var rayStartWidthField = type.GetField("rayStartWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rayStartWidthField != null) rayStartWidthField.SetValue(laserPointer, rayStartWidth);
        
        // rayEndWidthフィールド
        var rayEndWidthField = type.GetField("rayEndWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rayEndWidthField != null) rayEndWidthField.SetValue(laserPointer, rayEndWidth);
        
        // maxVisualDistanceフィールド
        var maxVisualDistanceField = type.GetField("maxVisualDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxVisualDistanceField != null) maxVisualDistanceField.SetValue(laserPointer, maxVisualDistance);
        
        Debug.Log("[LaserFixDirectly] VRLaserPointer設定を更新しました");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(VRLaserFixDirectly))]
public class VRLaserFixDirectlyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        VRLaserFixDirectly fixDirectly = (VRLaserFixDirectly)target;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("今すぐ色と幅を適用"))
        {
            fixDirectly.ApplyColorAndWidthSettings();
            EditorUtility.SetDirty(target);
        }
        
        if (GUILayout.Button("VRLaserPointerの設定を更新"))
        {
            fixDirectly.UpdateVRLaserPointerSettings();
            EditorUtility.SetDirty(target);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("このコンポーネントはレーザーポインターの位置と色の問題を直接修正するための緊急フィクサーです。", MessageType.Info);
    }
}
#endif
