using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PanelVisibilityController))]
public class PanelVisibilityControllerEditor : Editor
{
    private SerializedProperty leftControllerAnchorProperty;
    private SerializedProperty rightControllerAnchorProperty;
    private SerializedProperty laserPointerPrefabProperty;
    private SerializedProperty panelsProperty;
    
    private bool showControllerSettings = true;
    private bool showPointerSettings = true;
    private bool showPanelSettings = true;
    
    private void OnEnable()
    {
        leftControllerAnchorProperty = serializedObject.FindProperty("leftControllerAnchor");
        rightControllerAnchorProperty = serializedObject.FindProperty("rightControllerAnchor");
        laserPointerPrefabProperty = serializedObject.FindProperty("laserPointerPrefab");
        panelsProperty = serializedObject.FindProperty("panels");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        PanelVisibilityController controller = (PanelVisibilityController)target;
        
        // カスタムスタイル定義
        GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
        headerStyle.fontStyle = FontStyle.Bold;
        
        // コントローラー設定セクション
        EditorGUILayout.Space();
        showControllerSettings = EditorGUILayout.Foldout(showControllerSettings, "VRコントローラー設定", headerStyle);
        if (showControllerSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(leftControllerAnchorProperty, new GUIContent("左コントローラーアンカー", "Left controller transform"));
            EditorGUILayout.PropertyField(rightControllerAnchorProperty, new GUIContent("右コントローラーアンカー", "Right controller transform"));
            EditorGUI.indentLevel--;
        }
        
        // ポインター設定セクション
        EditorGUILayout.Space();
        showPointerSettings = EditorGUILayout.Foldout(showPointerSettings, "ポインター設定", headerStyle);
        if (showPointerSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(laserPointerPrefabProperty, new GUIContent("レーザーポインタープレハブ", "Prefab for laser pointer"));
            
            if (laserPointerPrefabProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("レーザーポインタープレハブが設定されていません。コンポーネントが自動的に追加されます。", MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
        }
        
        // パネル設定セクション
        EditorGUILayout.Space();
        showPanelSettings = EditorGUILayout.Foldout(showPanelSettings, "インタラクティブパネル設定", headerStyle);
        if (showPanelSettings)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(panelsProperty, true);
            
            if (GUILayout.Button("パネルを追加"))
            {
                AddNewPanel(controller);
            }
            
            if (GUILayout.Button("すべてのパネルを非表示"))
            {
                HideAllPanels(controller);
            }
            
            EditorGUI.indentLevel--;
        }
        
        serializedObject.ApplyModifiedProperties();
        
        // 変更が加えられた場合、Editorを再描画
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
    
    // 新しいパネルを追加するヘルパーメソッド
    private void AddNewPanel(PanelVisibilityController controller)
    {
        int index = panelsProperty.arraySize;
        panelsProperty.arraySize++;
        
        SerializedProperty newPanel = panelsProperty.GetArrayElementAtIndex(index);
        newPanel.FindPropertyRelative("panelID").stringValue = "Panel_" + index;
        newPanel.FindPropertyRelative("isVisible").boolValue = true;
        newPanel.FindPropertyRelative("requiresPointing").boolValue = true;
        
        serializedObject.ApplyModifiedProperties();
    }
    
    // すべてのパネルを非表示にするヘルパーメソッド
    private void HideAllPanels(PanelVisibilityController controller)
    {
        for (int i = 0; i < panelsProperty.arraySize; i++)
        {
            SerializedProperty panel = panelsProperty.GetArrayElementAtIndex(i);
            panel.FindPropertyRelative("isVisible").boolValue = false;
        }
        
        serializedObject.ApplyModifiedProperties();
        
        // エディタモードでの変更を即時に反映
        if (Application.isPlaying)
        {
            controller.HideAllPanels();
        }
    }
}
