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
    
    // 新しいタブ設定
    private enum SetupTab { Basic, Advanced, Presets }
    private SetupTab currentTab = SetupTab.Basic;
    private string[] tabNames = { "基本設定", "詳細設定", "プリセット" };

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
            
            if (leftController != null)
            {
                Debug.Log($"左コントローラーを検出: {leftController.name}");
            }
            
            if (rightController != null)
            {
                Debug.Log($"右コントローラーを検出: {rightController.name}");
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("VRレーザーポインター セットアップ", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // タブの表示
        currentTab = (SetupTab)GUILayout.Toolbar((int)currentTab, tabNames);
        EditorGUILayout.Space();
        
        // 選択されたタブに応じて異なるUIを表示
        switch (currentTab)
        {
            case SetupTab.Basic:
                DrawBasicTab();
                break;
            case SetupTab.Advanced:
                DrawAdvancedTab();
                break;
            case SetupTab.Presets:
                DrawPresetsTab();
                break;
        }
        
        EditorGUILayout.Space();
        GUILayout.Label("レーザーポインターのセットアップ", EditorStyles.boldLabel);
        GUI.enabled = (setupLeft && leftController != null) || (setupRight && rightController != null);
        
        if (GUILayout.Button("レーザーポインターをセットアップ"))
        {
            SetupLaserPointers();
        }
        
        GUI.enabled = true;
    }
    
    private void DrawBasicTab()
    {
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
    }
    
    private void DrawAdvancedTab()
    {
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
        
        // 既存のレーザーポインターの検索と更新
        GUILayout.Label("既存レーザーの更新", EditorStyles.boldLabel);
        
        if (GUILayout.Button("シーン内の全レーザーを更新"))
        {
            UpdateAllLaserPointers();
        }
    }
    
    private void DrawPresetsTab()
    {
        GUILayout.Label("プリセット設定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("プリセットを選択すると、レーザーの見た目が一括設定されます。", MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("MetaQuest3スタイル (青)"))
        {
            selectedColor = new Color(0.0f, 0.8f, 1.0f, 0.7f);
            rayStartWidth = 0.003f;
            rayEndWidth = 0.0005f;
            maxVisualDistance = 0.8f;
            EditorUtility.DisplayDialog("プリセット適用", "MetaQuest3スタイル (青) が適用されました。", "OK");
        }
        
        if (GUILayout.Button("MetaQuest3スタイル (紫)"))
        {
            selectedColor = new Color(0.8f, 0.0f, 1.0f, 0.7f);
            rayStartWidth = 0.003f;
            rayEndWidth = 0.0005f;
            maxVisualDistance = 0.8f;
            EditorUtility.DisplayDialog("プリセット適用", "MetaQuest3スタイル (紫) が適用されました。", "OK");
        }
        
        if (GUILayout.Button("標準スタイル (長め)"))
        {
            selectedColor = new Color(0.0f, 0.8f, 1.0f, 0.7f);
            rayStartWidth = 0.005f;
            rayEndWidth = 0.002f;
            maxVisualDistance = 2.0f;
            EditorUtility.DisplayDialog("プリセット適用", "標準スタイル (長め) が適用されました。", "OK");
        }
        
        if (GUILayout.Button("細い精密スタイル"))
        {
            selectedColor = new Color(1.0f, 0.2f, 0.2f, 0.7f);
            rayStartWidth = 0.001f;
            rayEndWidth = 0.0001f;
            maxVisualDistance = 1.5f;
            EditorUtility.DisplayDialog("プリセット適用", "細い精密スタイルが適用されました。", "OK");
        }
    }

    private void UpdateAllLaserPointers()
    {
        // シーン内の全VRLaserPointerを検索して更新
        VRLaserPointer[] pointers = FindObjectsOfType<VRLaserPointer>();
        int updateCount = 0;
        
        foreach (VRLaserPointer pointer in pointers)
        {
            if (pointer != null)
            {
                UpdateLaserPointerSettings(pointer);
                updateCount++;
            }
        }
        
        if (updateCount > 0)
        {
            EditorUtility.DisplayDialog("更新完了", $"{updateCount}個のレーザーポインターを更新しました。", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("更新情報", "シーン内に更新可能なレーザーポインターが見つかりませんでした。", "OK");
        }
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
            
            // 設定を更新
            UpdateLaserPointerSettings(existingPointer);
            return true;
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
            
            // VRLaserPointerコンポーネントを取得
            VRLaserPointer laserPointer = laserObj.GetComponent<VRLaserPointer>();
            if (laserPointer != null)
            {
                // rayOriginを設定
                SerializedObject serializedPointer = new SerializedObject(laserPointer);
                serializedPointer.FindProperty("rayOrigin").objectReferenceValue = controller.transform;
                serializedPointer.ApplyModifiedProperties();
                
                // レーザー設定を更新
                UpdateLaserPointerSettings(laserPointer);
            }
            
            Debug.Log($"新しいレーザーポインターを作成しました: {laserObj.name}");
            return true;
        }
        else
        {
            // プレハブが見つからない場合は新規作成
            laserObj = new GameObject(isLeft ? "LeftLaserPointer" : "RightLaserPointer");
            laserObj.transform.SetParent(controller.transform, false);
            
            // VRLaserPointerコンポーネントを追加
            VRLaserPointer laserPointer = laserObj.AddComponent<VRLaserPointer>();
            
            // rayOriginを設定
            SerializedObject serializedPointer = new SerializedObject(laserPointer);
            serializedPointer.FindProperty("rayOrigin").objectReferenceValue = controller.transform;
            serializedPointer.ApplyModifiedProperties();
            
            // レーザー設定を更新
            UpdateLaserPointerSettings(laserPointer);
            
            Debug.Log($"新しいレーザーポインターゲームオブジェクトを作成しました: {laserObj.name}");
            return true;
        }
    }
    
    private void UpdateLaserPointerSettings(VRLaserPointer laserPointer)
    {
        if (laserPointer == null) return;
        
        // VRLaserPointerの設定を更新
        SerializedObject serializedPointer = new SerializedObject(laserPointer);
        
        // 設定を適用
        serializedPointer.FindProperty("rayStartWidth").floatValue = rayStartWidth;
        serializedPointer.FindProperty("rayEndWidth").floatValue = rayEndWidth;
        serializedPointer.FindProperty("maxVisualDistance").floatValue = maxVisualDistance;
        serializedPointer.FindProperty("rayColor").colorValue = selectedColor;
        serializedPointer.FindProperty("rayEndColor").colorValue = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0f);
        
        serializedPointer.ApplyModifiedProperties();
        
        // LineRendererの設定を更新
        LineRenderer lineRenderer = laserPointer.gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = laserPointer.gameObject.AddComponent<LineRenderer>();
        }
        
        // LineRenderer基本設定
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = rayStartWidth;
        lineRenderer.endWidth = rayEndWidth;
        lineRenderer.startColor = selectedColor;
        lineRenderer.endColor = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0f);
        lineRenderer.positionCount = 2;
        
        // シャドウ関連の設定を最適化
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;
        
        // マテリアル設定
        Material material = lineRenderer.material;
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
            }
            
            if (material != null)
            {
                material.color = selectedColor;
                material.SetColor("_BaseColor", selectedColor);
                material.SetColor("_EmissionColor", selectedColor * 1.5f);
                material.EnableKeyword("_EMISSION");
                lineRenderer.material = material;
            }
        }
        else
        {
            material.color = selectedColor;
            material.SetColor("_BaseColor", selectedColor);
            material.SetColor("_EmissionColor", selectedColor * 1.5f);
            material.EnableKeyword("_EMISSION");
        }
        
        // 初期位置設定
        Transform rayOrigin = laserPointer.GetRayOrigin();
        if (rayOrigin != null)
        {
            Vector3 startPos = rayOrigin.position;
            Vector3 endPos = startPos + rayOrigin.forward * maxVisualDistance;
            
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
        }
        
        EditorUtility.SetDirty(laserPointer);
        EditorUtility.SetDirty(lineRenderer);
    }
}
