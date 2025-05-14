using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LaserMaterialFixHelper : EditorWindow
{
    [MenuItem("Tools/VR/レーザーポインターマテリアル修正")]
    public static void ShowWindow()
    {
        GetWindow<LaserMaterialFixHelper>("レーザーマテリアル修正");
    }

    private Color blueColor = new Color(0.0f, 0.8f, 1.0f, 0.7f);
    private Color purpleColor = new Color(0.8f, 0.0f, 1.0f, 0.7f);
    private Color selectedColor;
    private float rayStartWidth = 0.003f;
    private float rayEndWidth = 0.0005f;
    private float maxVisualDistance = 0.8f;

    private void OnEnable()
    {
        selectedColor = blueColor;
    }

    private void OnGUI()
    {
        GUILayout.Label("レーザーポインター マテリアル修正ツール", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("カラー設定", EditorStyles.boldLabel);
        selectedColor = EditorGUILayout.ColorField("レーザー色", selectedColor);

        // プリセット色
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("青色設定"))
        {
            selectedColor = blueColor;
        }
        if (GUILayout.Button("紫色設定"))
        {
            selectedColor = purpleColor;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("サイズ設定", EditorStyles.boldLabel);
        rayStartWidth = EditorGUILayout.Slider("開始幅", rayStartWidth, 0.001f, 0.01f);
        rayEndWidth = EditorGUILayout.Slider("終端幅", rayEndWidth, 0.0001f, 0.005f);
        maxVisualDistance = EditorGUILayout.Slider("長さ", maxVisualDistance, 0.2f, 2.0f);

        EditorGUILayout.Space();
        GUILayout.Label("適用", EditorStyles.boldLabel);

        if (GUILayout.Button("すべてのレーザーポインターに適用"))
        {
            ApplyToAllLaserPointers();
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("選択したコントローラーのレーザーに適用"))
        {
            ApplyToSelectedControllers();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("適用するとシーン内のすべてのVRLaserPointerのマテリアルと設定が更新されます。", MessageType.Info);
    }

    private void ApplyToAllLaserPointers()
    {
        // シーン内のすべてのVRLaserPointerを検索
        VRLaserPointer[] laserPointers = FindObjectsOfType<VRLaserPointer>();
        if (laserPointers.Length == 0)
        {
            EditorUtility.DisplayDialog("結果", "レーザーポインターが見つかりませんでした。", "OK");
            return;
        }

        int updatedCount = 0;
        foreach (VRLaserPointer pointer in laserPointers)
        {
            if (UpdateLaserMaterial(pointer))
            {
                updatedCount++;
            }
        }

        EditorUtility.DisplayDialog("結果", $"{updatedCount}個のレーザーポインターを更新しました。", "OK");
    }

    private void ApplyToSelectedControllers()
    {
        // 選択されたオブジェクトを取得
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("警告", "オブジェクトが選択されていません。Controller ObjectまたはVRLaserPointerコンポーネントを持つオブジェクトを選択してください。", "OK");
            return;
        }

        List<VRLaserPointer> pointers = new List<VRLaserPointer>();

        // 選択されたオブジェクトとその子オブジェクトからVRLaserPointerを探す
        foreach (GameObject obj in selected)
        {
            // 直接アタッチされている場合
            VRLaserPointer pointer = obj.GetComponent<VRLaserPointer>();
            if (pointer != null)
            {
                pointers.Add(pointer);
            }

            // 子オブジェクトを探索
            VRLaserPointer[] childPointers = obj.GetComponentsInChildren<VRLaserPointer>();
            pointers.AddRange(childPointers);
        }

        if (pointers.Count == 0)
        {
            EditorUtility.DisplayDialog("警告", "選択されたオブジェクトにVRLaserPointerコンポーネントがありません。", "OK");
            return;
        }

        int updatedCount = 0;
        foreach (VRLaserPointer pointer in pointers)
        {
            if (UpdateLaserMaterial(pointer))
            {
                updatedCount++;
            }
        }

        EditorUtility.DisplayDialog("結果", $"{updatedCount}個のレーザーポインターを更新しました。", "OK");
    }

    private bool UpdateLaserMaterial(VRLaserPointer pointer)
    {
        if (pointer == null) return false;

        Undo.RecordObject(pointer, "Update Laser Settings");

        // SerializedObjectを使って変更を適用
        SerializedObject serializedPointer = new SerializedObject(pointer);
        serializedPointer.FindProperty("rayStartWidth").floatValue = rayStartWidth;
        serializedPointer.FindProperty("rayEndWidth").floatValue = rayEndWidth;
        serializedPointer.FindProperty("maxVisualDistance").floatValue = maxVisualDistance;
        
        SerializedProperty colorProp = serializedPointer.FindProperty("rayColor");
        colorProp.colorValue = selectedColor;
        
        SerializedProperty endColorProp = serializedPointer.FindProperty("rayEndColor");
        Color endColor = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.0f);
        endColorProp.colorValue = endColor;
        
        serializedPointer.ApplyModifiedProperties();

        // LineRendererの設定も更新
        LineRenderer lineRenderer = pointer.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            Undo.RecordObject(lineRenderer, "Update LineRenderer");
            
            lineRenderer.startWidth = rayStartWidth;
            lineRenderer.endWidth = rayEndWidth;
            
            lineRenderer.startColor = selectedColor;
            lineRenderer.endColor = endColor;

            // マテリアル更新または新規作成
            UpdateMaterial(lineRenderer);
            
            EditorUtility.SetDirty(lineRenderer);
        }
        else
        {
            // LineRendererがない場合は追加して設定
            lineRenderer = pointer.gameObject.AddComponent<LineRenderer>();
            
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = rayStartWidth;
            lineRenderer.endWidth = rayEndWidth;
            lineRenderer.startColor = selectedColor;
            lineRenderer.endColor = endColor;
            lineRenderer.positionCount = 2;
            
            // 初期位置を設定
            Transform rayOrigin = pointer.GetRayOrigin();
            Vector3 startPos = rayOrigin != null ? rayOrigin.position : pointer.transform.position;
            Vector3 direction = rayOrigin != null ? rayOrigin.forward : pointer.transform.forward;
            Vector3 endPos = startPos + direction * maxVisualDistance;
            
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);
            
            // マテリアルの作成と設定
            UpdateMaterial(lineRenderer);
            
            Debug.Log($"LineRendererを追加: {pointer.gameObject.name}");
        }

        EditorUtility.SetDirty(pointer);
        return true;
    }

    private void UpdateMaterial(LineRenderer lineRenderer)
    {
        if (lineRenderer == null) return;
        
        // 既存のマテリアルがあれば更新、なければ新規作成
        Material laserMaterial = lineRenderer.material;
        bool createNewMaterial = false;
        
        if (laserMaterial == null)
        {
            createNewMaterial = true;
        }
        else
        {
            try
            {
                // マテリアルの更新記録
                Undo.RecordObject(laserMaterial, "Update Material Color");
                
                // マテリアルの色を設定
                laserMaterial.color = selectedColor;
                laserMaterial.SetColor("_BaseColor", selectedColor); // URP用
                laserMaterial.SetColor("_EmissionColor", selectedColor * 1.5f); // 発光効果
                laserMaterial.EnableKeyword("_EMISSION"); // 発光を有効化
                
                EditorUtility.SetDirty(laserMaterial);
            }
            catch (System.Exception)
            {
                // マテリアルの更新に失敗した場合は新規作成
                Debug.LogWarning("既存のマテリアルの更新に失敗しました。新しいマテリアルを作成します。");
                createNewMaterial = true;
            }
        }
        
        if (createNewMaterial)
        {
            // 新しいマテリアルを作成
            Material newMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (newMaterial == null)
            {
                newMaterial = new Material(Shader.Find("Unlit/Color"));
                if (newMaterial == null)
                {
                    newMaterial = new Material(Shader.Find("Standard"));
                }
            }
            
            if (newMaterial != null)
            {
                // マテリアルの設定
                newMaterial.color = selectedColor;
                newMaterial.SetColor("_BaseColor", selectedColor); // URP用
                newMaterial.SetColor("_EmissionColor", selectedColor * 1.5f); // 発光効果
                newMaterial.EnableKeyword("_EMISSION"); // 発光を有効化
                
                // マテリアルをレンダラーに設定
                lineRenderer.material = newMaterial;
                
                Debug.Log("新しいマテリアルを作成しました。");
            }
            else
            {
                Debug.LogError("新しいマテリアルを作成できませんでした。使用可能なシェーダーが見つかりません。");
            }
        }
    }
}
