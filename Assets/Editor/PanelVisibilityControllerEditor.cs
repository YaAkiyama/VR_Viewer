#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PanelVisibilityController))]
public class PanelVisibilityControllerEditor : Editor
{
    private GameObject newPanel;
    private GameObject thumbnailCanvas;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PanelVisibilityController controller = (PanelVisibilityController)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("パネル管理", EditorStyles.boldLabel);

        newPanel = EditorGUILayout.ObjectField("追加するパネル", newPanel, typeof(GameObject), true) as GameObject;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("パネルを追加"))
        {
            if (newPanel != null)
            {
                controller.AddPanel(newPanel);
                newPanel = null;
                EditorUtility.SetDirty(target);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "追加するパネルを選択してください", "OK");
            }
        }

        if (GUILayout.Button("現在のパネルを再設定"))
        {
            // Startメソッドを手動で呼び出すにはReflectionが必要なため、
            // ここでは単純に変更をマークして再生中ならば自動的に再設定されるようにします
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.EndHorizontal();

        // サムネイルキャンバスの設定
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("サムネイルキャンバス設定", EditorStyles.boldLabel);

        thumbnailCanvas = EditorGUILayout.ObjectField("サムネイルキャンバス", thumbnailCanvas, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("サムネイルキャンバスを設定"))
        {
            if (thumbnailCanvas != null)
            {
                SerializedProperty thumbnailCanvasProp = serializedObject.FindProperty("thumbnailCanvas");
                thumbnailCanvasProp.objectReferenceValue = thumbnailCanvas;
                serializedObject.ApplyModifiedProperties();
                
                // 再生中の場合はコントローラーにも反映
                if (Application.isPlaying)
                {
                    controller.SetThumbnailCanvas(thumbnailCanvas);
                }
                
                EditorUtility.SetDirty(target);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "サムネイルキャンバスを選択してください", "OK");
            }
        }

        if (GUILayout.Button("シーンからサムネイルキャンバスを自動検出"))
        {
            GameObject foundCanvas = GameObject.Find("ThumbnailCanvas");
            if (foundCanvas != null)
            {
                thumbnailCanvas = foundCanvas;
                
                SerializedProperty thumbnailCanvasProp = serializedObject.FindProperty("thumbnailCanvas");
                thumbnailCanvasProp.objectReferenceValue = thumbnailCanvas;
                serializedObject.ApplyModifiedProperties();
                
                // 再生中の場合はコントローラーにも反映
                if (Application.isPlaying)
                {
                    controller.SetThumbnailCanvas(thumbnailCanvas);
                }
                
                EditorUtility.SetDirty(target);
                EditorUtility.DisplayDialog("情報", "サムネイルキャンバスを自動検出しました", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "シーン内に「ThumbnailCanvas」という名前のGameObjectが見つかりませんでした", "OK");
            }
        }

        // パネルアクティブ状態の制御を追加
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("パネルアクティブ制御", EditorStyles.boldLabel);

        // Unity PlayMode時のみボタンを有効にする
        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("パネルアクティブをトグル"))
        {
            controller.TogglePanelActive();
        }

        GUI.enabled = true; // ボタンの有効状態をリセット

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("プリセット", EditorStyles.boldLabel);

        if (GUILayout.Button("標準視野角範囲 (-70° 〜 +70°)"))
        {
            SerializedProperty minProp = serializedObject.FindProperty("minViewAngleX");
            SerializedProperty maxProp = serializedObject.FindProperty("maxViewAngleX");

            minProp.floatValue = -70f;
            maxProp.floatValue = 70f;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("狭い視野角範囲 (-45° 〜 +45°)"))
        {
            SerializedProperty minProp = serializedObject.FindProperty("minViewAngleX");
            SerializedProperty maxProp = serializedObject.FindProperty("maxViewAngleX");

            minProp.floatValue = -45f;
            maxProp.floatValue = 45f;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("広い視野角範囲 (-90° 〜 +90°)"))
        {
            SerializedProperty minProp = serializedObject.FindProperty("minViewAngleX");
            SerializedProperty maxProp = serializedObject.FindProperty("maxViewAngleX");

            minProp.floatValue = -90f;
            maxProp.floatValue = 90f;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif