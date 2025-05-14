using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ApplyLaserFixTool : EditorWindow
{
    [MenuItem("Tools/VR/レーザー緊急修正ツール")]
    public static void ShowWindow()
    {
        GetWindow<ApplyLaserFixTool>("レーザー緊急修正");
    }

    private Color selectedColor = new Color(0.0f, 0.8f, 1.0f, 0.7f); // 青色
    private bool applyToLeft = true;
    private bool applyToRight = true;
    private bool forceUseControllerTransform = true;
    private bool useControllerForward = true;
    private float rayStartWidth = 0.003f;
    private float rayEndWidth = 0.0005f;
    private float maxVisualDistance = 0.8f;

    private void OnGUI()
    {
        GUILayout.Label("レーザーポインター 緊急修正ツール", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("色と形状設定", EditorStyles.boldLabel);
        selectedColor = EditorGUILayout.ColorField("レーザー色", selectedColor);
        rayStartWidth = EditorGUILayout.Slider("開始幅", rayStartWidth, 0.001f, 0.01f);
        rayEndWidth = EditorGUILayout.Slider("終端幅", rayEndWidth, 0.0001f, 0.005f);
        maxVisualDistance = EditorGUILayout.Slider("長さ", maxVisualDistance, 0.2f, 2f);

        EditorGUILayout.Space();
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
        GUILayout.Label("コントローラー固定設定", EditorStyles.boldLabel);
        forceUseControllerTransform = EditorGUILayout.Toggle("コントローラー位置に固定", forceUseControllerTransform);
        useControllerForward = EditorGUILayout.Toggle("コントローラー方向を使用", useControllerForward);

        EditorGUILayout.Space();
        GUILayout.Label("適用対象", EditorStyles.boldLabel);
        applyToLeft = EditorGUILayout.Toggle("左コントローラー", applyToLeft);
        applyToRight = EditorGUILayout.Toggle("右コントローラー", applyToRight);

        EditorGUILayout.Space();
        if (GUILayout.Button("緊急フィクサーを適用"))
        {
            ApplyFixers();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("このツールはレーザーポインターの色が紫のまま、または位置がずれている問題を緊急で修正します。", MessageType.Warning);
    }

    private void ApplyFixers()
    {
        GameObject cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
        if (cameraRig == null)
        {
            EditorUtility.DisplayDialog("エラー", "[BuildingBlock] Camera Rigが見つかりません。シーン内に存在することを確認してください。", "OK");
            return;
        }

        int fixedCount = 0;

        // 左コントローラーに適用
        if (applyToLeft)
        {
            Transform leftController = cameraRig.transform.Find("LeftControllerAnchor");
            if (leftController != null)
            {
                fixedCount += ApplyFixerToController(leftController.gameObject);
            }
        }

        // 右コントローラーに適用
        if (applyToRight)
        {
            Transform rightController = cameraRig.transform.Find("RightControllerAnchor");
            if (rightController != null)
            {
                fixedCount += ApplyFixerToController(rightController.gameObject);
            }
        }

        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog("成功", $"{fixedCount}個のレーザーポインターに緊急フィクサーを適用しました。", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("情報", "適用対象となるレーザーポインターが見つかりませんでした。", "OK");
        }
    }

    private int ApplyFixerToController(GameObject controller)
    {
        int count = 0;

        // 既存のVRLaserPointerを検索
        VRLaserPointer[] laserPointers = controller.GetComponentsInChildren<VRLaserPointer>(true);
        foreach (VRLaserPointer laserPointer in laserPointers)
        {
            if (laserPointer != null)
            {
                GameObject laserObject = laserPointer.gameObject;
                
                // 既存のフィクサーがあれば削除
                VRLaserFixDirectly existingFixer = laserObject.GetComponent<VRLaserFixDirectly>();
                if (existingFixer != null)
                {
                    DestroyImmediate(existingFixer);
                }
                
                // フィクサーを追加
                VRLaserFixDirectly fixer = laserObject.AddComponent<VRLaserFixDirectly>();
                
                // フィクサーを設定
                var serializedFixer = new SerializedObject(fixer);
                serializedFixer.FindProperty("forceUseControllerTransform").boolValue = forceUseControllerTransform;
                serializedFixer.FindProperty("useControllerForward").boolValue = useControllerForward;
                serializedFixer.FindProperty("laserColor").colorValue = selectedColor;
                serializedFixer.FindProperty("rayStartWidth").floatValue = rayStartWidth;
                serializedFixer.FindProperty("rayEndWidth").floatValue = rayEndWidth;
                serializedFixer.FindProperty("maxVisualDistance").floatValue = maxVisualDistance;
                serializedFixer.ApplyModifiedProperties();
                
                // 変更を保存
                EditorUtility.SetDirty(laserObject);
                
                // 設定を適用
                fixer.ApplyColorAndWidthSettings();
                fixer.UpdateVRLaserPointerSettings();
                
                Debug.Log($"[ApplyLaserFixTool] フィクサーを適用: {laserObject.name}");
                count++;
            }
        }

        // 再帰的に子オブジェクトを検索
        foreach (Transform child in controller.transform)
        {
            if (child.name.Contains("Controller") && !child.name.Contains("LaserPointer"))
            {
                count += ApplyFixerToController(child.gameObject);
            }
        }

        return count;
    }
}
