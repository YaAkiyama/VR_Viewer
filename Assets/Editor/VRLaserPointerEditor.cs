using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VRLaserPointer))]
public class VRLaserPointerEditor : Editor
{
    private GameObject leftControllerAnchor;
    private GameObject rightControllerAnchor;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VRLaserPointer laserPointer = (VRLaserPointer)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("コントローラー設定", EditorStyles.boldLabel);

        // 自動設定ボタン
        if (GUILayout.Button("コントローラーオリジンを自動検出"))
        {
            // Camera Rigを検索
            var cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (cameraRig != null)
            {
                // 左右のコントローラーを検索
                Transform leftController = cameraRig.transform.Find("LeftControllerAnchor");
                Transform rightController = cameraRig.transform.Find("RightControllerAnchor");

                // このポインターが取り付けられているコントローラーを検出
                bool isLeftController = false;
                bool isRightController = false;

                Transform current = laserPointer.transform;
                while (current != null)
                {
                    if (leftController != null && current == leftController)
                    {
                        isLeftController = true;
                        break;
                    }
                    if (rightController != null && current == rightController)
                    {
                        isRightController = true;
                        break;
                    }
                    current = current.parent;
                }

                // 自動検出したレイオリジンを設定
                if (isLeftController && leftController != null)
                {
                    SerializedProperty originProp = serializedObject.FindProperty("rayOrigin");
                    originProp.objectReferenceValue = leftController;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    Debug.Log("左コントローラーを自動検出しました");
                }
                else if (isRightController && rightController != null)
                {
                    SerializedProperty originProp = serializedObject.FindProperty("rayOrigin");
                    originProp.objectReferenceValue = rightController;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                    Debug.Log("右コントローラーを自動検出しました");
                }
                else
                {
                    // 自動検出できなかった場合、ドロップダウンを表示
                    if (leftController != null)
                        leftControllerAnchor = leftController.gameObject;
                    if (rightController != null)
                        rightControllerAnchor = rightController.gameObject;

                    EditorUtility.DisplayDialog("情報", "コントローラーを自動検出できませんでした。手動で選択してください。", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "[BuildingBlock] Camera Rigが見つかりません。シーン内に存在することを確認してください。", "OK");
            }
        }

        // 左右コントローラー選択フィールド
        EditorGUILayout.Space();
        leftControllerAnchor = EditorGUILayout.ObjectField("左コントローラー", leftControllerAnchor, typeof(GameObject), true) as GameObject;
        rightControllerAnchor = EditorGUILayout.ObjectField("右コントローラー", rightControllerAnchor, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("左コントローラーに設定") && leftControllerAnchor != null)
        {
            SerializedProperty originProp = serializedObject.FindProperty("rayOrigin");
            originProp.objectReferenceValue = leftControllerAnchor.transform;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Debug.Log($"レーザーの発射元を左コントローラーに設定しました: {leftControllerAnchor.name}");
        }

        if (GUILayout.Button("右コントローラーに設定") && rightControllerAnchor != null)
        {
            SerializedProperty originProp = serializedObject.FindProperty("rayOrigin");
            originProp.objectReferenceValue = rightControllerAnchor.transform;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Debug.Log($"レーザーの発射元を右コントローラーに設定しました: {rightControllerAnchor.name}");
        }
        EditorGUILayout.EndHorizontal();

        // マテリアル設定のボタン
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("視覚効果設定", EditorStyles.boldLabel);

        if (GUILayout.Button("レーザーポインター初期化"))
        {
            // LineRendererが存在するか確認
            LineRenderer lineRenderer = laserPointer.gameObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = laserPointer.gameObject.AddComponent<LineRenderer>();
                Debug.Log("LineRendererを追加しました");
            }

            // 基本設定を適用
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = laserPointer.GetRayStartWidth();
            lineRenderer.endWidth = laserPointer.GetRayEndWidth();
            lineRenderer.startColor = laserPointer.GetRayColor();
            lineRenderer.endColor = laserPointer.GetRayEndColor();
            lineRenderer.positionCount = 2;

            // マテリアルを設定
            Material laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (laserMaterial == null)
            {
                laserMaterial = new Material(Shader.Find("Standard"));
            }

            if (laserMaterial != null)
            {
                laserMaterial.color = laserPointer.GetRayColor();
                laserMaterial.SetColor("_BaseColor", laserPointer.GetRayColor()); // URP用
                laserMaterial.SetColor("_EmissionColor", laserPointer.GetRayColor() * 1.5f); // 発光効果
                laserMaterial.EnableKeyword("_EMISSION"); // 発光を有効化
                lineRenderer.material = laserMaterial;
                Debug.Log("レーザーマテリアルを作成しました");
            }

            // 初期位置を設定
            Transform rayOrigin = laserPointer.GetRayOrigin();
            Vector3 startPos = rayOrigin != null ? rayOrigin.position : laserPointer.transform.position;
            Vector3 direction = rayOrigin != null ? rayOrigin.forward : laserPointer.transform.forward;
            Vector3 endPos = startPos + direction * laserPointer.GetMaxVisualDistance();

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            EditorUtility.SetDirty(lineRenderer);
            Debug.Log("レーザーポインターを初期化しました");
        }

        if (GUILayout.Button("発光効果を強化"))
        {
            LineRenderer lineRenderer = laserPointer.gameObject.GetComponent<LineRenderer>();
            if (lineRenderer != null && lineRenderer.material != null)
            {
                lineRenderer.material.EnableKeyword("_EMISSION");

                Color emissionColor = laserPointer.GetRayColor() * 1.5f;
                lineRenderer.material.SetColor("_EmissionColor", emissionColor);
                
                lineRenderer.startWidth = laserPointer.GetRayStartWidth();
                lineRenderer.endWidth = laserPointer.GetRayEndWidth();
                
                // グラデーションを設定
                lineRenderer.startColor = laserPointer.GetRayColor();
                lineRenderer.endColor = laserPointer.GetRayEndColor();
                
                EditorUtility.SetDirty(lineRenderer);
                Debug.Log("レーザーの発光効果を強化しました");
            }
            else
            {
                Debug.LogWarning("LineRendererまたはMaterialが見つかりません");
            }
        }

        // プリセット設定
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("MetaQuest3スタイル (青)"))
        {
            ApplyMetaQuestStyle(laserPointer, true);
        }

        if (GUILayout.Button("MetaQuest3スタイル (紫)"))
        {
            ApplyMetaQuestStyle(laserPointer, false);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyMetaQuestStyle(VRLaserPointer laserPointer, bool useBlueColor)
    {
        // VRLaserPointerの設定
        SerializedObject serializedPointer = new SerializedObject(laserPointer);
        
        // サイズ設定
        serializedPointer.FindProperty("maxVisualDistance").floatValue = 0.8f;
        serializedPointer.FindProperty("rayStartWidth").floatValue = 0.003f;
        serializedPointer.FindProperty("rayEndWidth").floatValue = 0.0005f;
        
        // 色設定
        Color mainColor = useBlueColor 
            ? new Color(0.0f, 0.8f, 1.0f, 0.7f)  // 青
            : new Color(0.8f, 0.0f, 1.0f, 0.7f); // 紫
            
        Color endColor = new Color(mainColor.r, mainColor.g, mainColor.b, 0.0f);
        
        serializedPointer.FindProperty("rayColor").colorValue = mainColor;
        serializedPointer.FindProperty("rayEndColor").colorValue = endColor;
        
        serializedPointer.ApplyModifiedProperties();
        
        // LineRendererの更新
        LineRenderer lineRenderer = laserPointer.gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = laserPointer.gameObject.AddComponent<LineRenderer>();
            Debug.Log("LineRendererを追加しました");
        }
        
        // LineRendererの設定
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 0.003f;
        lineRenderer.endWidth = 0.0005f;
        lineRenderer.startColor = mainColor;
        lineRenderer.endColor = endColor;
        lineRenderer.positionCount = 2;
        
        // レーザーの位置を設定
        Transform rayOrigin = laserPointer.GetRayOrigin();
        Vector3 startPos = rayOrigin != null ? rayOrigin.position : laserPointer.transform.position;
        Vector3 direction = rayOrigin != null ? rayOrigin.forward : laserPointer.transform.forward;
        Vector3 endPos = startPos + direction * 0.8f;
        
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
        
        // マテリアルの更新または作成
        if (lineRenderer.material != null)
        {
            // 既存のマテリアルを更新
            lineRenderer.material.color = mainColor;
            lineRenderer.material.SetColor("_BaseColor", mainColor);
            lineRenderer.material.SetColor("_EmissionColor", mainColor * 1.5f);
            lineRenderer.material.EnableKeyword("_EMISSION");
        }
        else
        {
            // 新しいマテリアルを作成
            Material laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (laserMaterial == null)
            {
                laserMaterial = new Material(Shader.Find("Unlit/Color"));
                if (laserMaterial == null)
                {
                    laserMaterial = new Material(Shader.Find("Standard"));
                }
            }
            
            if (laserMaterial != null)
            {
                laserMaterial.color = mainColor;
                laserMaterial.SetColor("_BaseColor", mainColor);
                laserMaterial.SetColor("_EmissionColor", mainColor * 1.5f);
                laserMaterial.EnableKeyword("_EMISSION");
                lineRenderer.material = laserMaterial;
            }
        }
        
        EditorUtility.SetDirty(laserPointer);
        EditorUtility.SetDirty(lineRenderer);
        
        Debug.Log($"MetaQuest3スタイル ({(useBlueColor ? "青" : "紫")}) を適用しました");
    }
}
