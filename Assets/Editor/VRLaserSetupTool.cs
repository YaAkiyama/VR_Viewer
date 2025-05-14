using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class VRLaserSetupTool : EditorWindow
{
    [MenuItem("Tools/VR/レーザーポインターセットアップ")]
    public static void ShowWindow()
    {
        GetWindow<VRLaserSetupTool>("レーザーセットアップ");
    }

    private GameObject cameraRig;
    private GameObject leftController;
    private GameObject rightController;
    
    private bool setupLeft = true;
    private bool setupRight = true;
    
    private GameObject laserPrefab;
    private Color selectedColor = new Color(0.0f, 0.8f, 1.0f, 0.7f);
    private float rayStartWidth = 0.003f;
    private float rayEndWidth = 0.0005f;
    private float maxVisualDistance = 0.8f;

    private void OnEnable()
    {
        // デフォルトのレーザープレハブを探す
        laserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/LaserPointer.prefab");
        FindCameraRig();
    }

    private void FindCameraRig()
    {
        // [BuildingBlock] Camera Rigを探す
        cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
        
        if (cameraRig != null)
        {
            // 左右のコントローラーアンカーを探す
            leftController = cameraRig.transform.Find("LeftControllerAnchor")?.gameObject;
            rightController = cameraRig.transform.Find("RightControllerAnchor")?.gameObject;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("VRレーザーポインター セットアップ", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // カメラリグの検出/選択
        GUILayout.Label("カメラリグ設定", EditorStyles.boldLabel);
        cameraRig = EditorGUILayout.ObjectField("Camera Rig", cameraRig, typeof(GameObject), true) as GameObject;
        
        if (GUILayout.Button("Camera Rigを検索"))
        {
            FindCameraRig();
        }
        
        EditorGUILayout.Space();
        
        // 左右コントローラーの検出/選択
        GUILayout.Label("コントローラー設定", EditorStyles.boldLabel);
        
        if (cameraRig != null)
        {
            // コントローラー表示と選択UI
            EditorGUI.BeginDisabledGroup(leftController == null);
            setupLeft = EditorGUILayout.Toggle("左コントローラーにセットアップ", setupLeft);
            leftController = EditorGUILayout.ObjectField("左コントローラー", leftController, typeof(GameObject), true) as GameObject;
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(rightController == null);
            setupRight = EditorGUILayout.Toggle("右コントローラーにセットアップ", setupRight);
            rightController = EditorGUILayout.ObjectField("右コントローラー", rightController, typeof(GameObject), true) as GameObject;
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            EditorGUILayout.HelpBox("Camera Rigが見つかりません。[BuildingBlock] Camera Rigを検索するか、手動で設定してください。", MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        // レーザープレハブ設定
        GUILayout.Label("レーザー設定", EditorStyles.boldLabel);
        laserPrefab = EditorGUILayout.ObjectField("レーザープレハブ", laserPrefab, typeof(GameObject), false) as GameObject;
        
        EditorGUILayout.Space();
        
        // レーザーの外観設定
        GUILayout.Label("外観設定", EditorStyles.boldLabel);
        selectedColor = EditorGUILayout.ColorField("レーザー色", selectedColor);
        rayStartWidth = EditorGUILayout.Slider("開始幅", rayStartWidth, 0.001f, 0.01f);
        rayEndWidth = EditorGUILayout.Slider("終端幅", rayEndWidth, 0.0001f, 0.005f);
        maxVisualDistance = EditorGUILayout.Slider("長さ", maxVisualDistance, 0.2f, 2.0f);
        
        EditorGUILayout.Space();
        
        // プリセット
        GUILayout.Label("プリセット", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("MetaQuest3 青"))
        {
            selectedColor = new Color(0.0f, 0.8f, 1.0f, 0.7f);
            rayStartWidth = 0.003f;
            rayEndWidth = 0.0005f;
            maxVisualDistance = 0.8f;
        }
        if (GUILayout.Button("MetaQuest3 紫"))
        {
            selectedColor = new Color(0.8f, 0.0f, 1.0f, 0.7f);
            rayStartWidth = 0.003f;
            rayEndWidth = 0.0005f;
            maxVisualDistance = 0.8f;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // セットアップボタン
        GUI.enabled = (setupLeft && leftController != null) || (setupRight && rightController != null);
        if (GUILayout.Button("レーザーポインターをセットアップ"))
        {
            SetupLaserPointers();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("このツールは選択したコントローラーにレーザーポインターを追加し、設定を適用します。", MessageType.Info);
    }

    private void SetupLaserPointers()
    {
        int setupCount = 0;
        
        // 左コントローラーをセットアップ
        if (setupLeft && leftController != null)
        {
            if (SetupLaserPointer(leftController, true))
            {
                setupCount++;
            }
        }
        
        // 右コントローラーをセットアップ
        if (setupRight && rightController != null)
        {
            if (SetupLaserPointer(rightController, false))
            {
                setupCount++;
            }
        }
        
        if (setupCount > 0)
        {
            EditorUtility.DisplayDialog("セットアップ完了", $"{setupCount}個のコントローラーにレーザーポインターを設定しました。", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("セットアップエラー", "レーザーポインターのセットアップに失敗しました。", "OK");
        }
    }

    private bool SetupLaserPointer(GameObject controller, bool isLeft)
    {
        if (controller == null) return false;
        
        // 既存のVRLaserPointerコンポーネントをチェック
        VRLaserPointer existingPointer = controller.GetComponent<VRLaserPointer>();
        GameObject laserObj;
        
        if (existingPointer != null)
        {
            // 既存のポインターがある場合は更新
            laserObj = existingPointer.gameObject;
            Debug.Log($"既存のレーザーポインターを更新します: {controller.name}");
        }
        else if (laserPrefab != null)
        {
            // プレハブからインスタンス化
            laserObj = PrefabUtility.InstantiatePrefab(laserPrefab, controller.transform) as GameObject;
            if (laserObj == null)
            {
                Debug.LogError("レーザープレハブのインスタンス化に失敗しました");
                return false;
            }
            
            // 名前を設定
            laserObj.name = isLeft ? "LeftLaserPointer" : "RightLaserPointer";
            
            // 位置とローテーションをリセット
            laserObj.transform.localPosition = Vector3.zero;
            laserObj.transform.localRotation = Quaternion.identity;
            
            Debug.Log($"新しいレーザーポインターを作成しました: {laserObj.name}");
        }
        else
        {
            // プレハブが見つからない場合は新規作成
            laserObj = new GameObject(isLeft ? "LeftLaserPointer" : "RightLaserPointer");
            laserObj.transform.SetParent(controller.transform, false);
            
            Debug.Log($"新しいレーザーポインターゲームオブジェクトを作成しました: {laserObj.name}");
        }
        
        // VRLaserPointerコンポーネントを取得または追加
        VRLaserPointer laserPointer = laserObj.GetComponent<VRLaserPointer>();
        if (laserPointer == null)
        {
            laserPointer = laserObj.AddComponent<VRLaserPointer>();
        }
        
        // 設定を適用
        SerializedObject serializedPointer = new SerializedObject(laserPointer);
        
        // rayOriginを設定
        serializedPointer.FindProperty("rayOrigin").objectReferenceValue = controller.transform;
        
        // 外観設定
        serializedPointer.FindProperty("rayStartWidth").floatValue = rayStartWidth;
        serializedPointer.FindProperty("rayEndWidth").floatValue = rayEndWidth;
        serializedPointer.FindProperty("maxVisualDistance").floatValue = maxVisualDistance;
        
        serializedPointer.FindProperty("rayColor").colorValue = selectedColor;
        Color endColor = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.0f);
        serializedPointer.FindProperty("rayEndColor").colorValue = endColor;
        
        serializedPointer.ApplyModifiedProperties();
        
        // LineRendererを追加・設定（初期化中に自動的に行われるが、念のため）
        LineRenderer lineRenderer = laserObj.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = laserObj.AddComponent<LineRenderer>();
        }
        
        // 基本設定
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = rayStartWidth;
        lineRenderer.endWidth = rayEndWidth;
        lineRenderer.startColor = selectedColor;
        lineRenderer.endColor = endColor;
        
        // マテリアル設定
        Material laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (laserMaterial == null)
        {
            laserMaterial = new Material(Shader.Find("Standard"));
        }
        
        if (laserMaterial != null)
        {
            laserMaterial.color = selectedColor;
            laserMaterial.SetColor("_BaseColor", selectedColor);
            laserMaterial.SetColor("_EmissionColor", selectedColor * 1.5f);
            laserMaterial.EnableKeyword("_EMISSION");
            lineRenderer.material = laserMaterial;
        }
        
        // 変更を保存
        EditorUtility.SetDirty(laserObj);
        
        return true;
    }
}
