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
        }

        if (GUILayout.Button("右コントローラーに設定") && rightControllerAnchor != null)
        {
            SerializedProperty originProp = serializedObject.FindProperty("rayOrigin");
            originProp.objectReferenceValue = rightControllerAnchor.transform;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
        EditorGUILayout.EndHorizontal();

        // マテリアル設定のボタン
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("視覚効果設定", EditorStyles.boldLabel);

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

        if (GUILayout.Button("MetaQuest3スタイル (青)"))
        {
            SerializedProperty visualDistProp = serializedObject.FindProperty("maxVisualDistance");
            SerializedProperty startWidthProp = serializedObject.FindProperty("rayStartWidth");
            SerializedProperty endWidthProp = serializedObject.FindProperty("rayEndWidth");
            SerializedProperty colorProp = serializedObject.FindProperty("rayColor");
            SerializedProperty endColorProp = serializedObject.FindProperty("rayEndColor");

            visualDistProp.floatValue = 0.8f;
            startWidthProp.floatValue = 0.003f;
            endWidthProp.floatValue = 0.0005f;
            colorProp.colorValue = new Color(0.0f, 0.8f, 1.0f, 0.7f);
            endColorProp.colorValue = new Color(0.0f, 0.8f, 1.0f, 0.0f);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Debug.Log("MetaQuest3スタイル (青) を適用しました");
        }

        if (GUILayout.Button("MetaQuest3スタイル (紫)"))
        {
            SerializedProperty visualDistProp = serializedObject.FindProperty("maxVisualDistance");
            SerializedProperty startWidthProp = serializedObject.FindProperty("rayStartWidth");
            SerializedProperty endWidthProp = serializedObject.FindProperty("rayEndWidth");
            SerializedProperty colorProp = serializedObject.FindProperty("rayColor");
            SerializedProperty endColorProp = serializedObject.FindProperty("rayEndColor");

            visualDistProp.floatValue = 0.8f;
            startWidthProp.floatValue = 0.003f;
            endWidthProp.floatValue = 0.0005f;
            colorProp.colorValue = new Color(0.8f, 0.0f, 1.0f, 0.7f);
            endColorProp.colorValue = new Color(0.8f, 0.0f, 1.0f, 0.0f);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Debug.Log("MetaQuest3スタイル (紫) を適用しました");
        }
    }
}
