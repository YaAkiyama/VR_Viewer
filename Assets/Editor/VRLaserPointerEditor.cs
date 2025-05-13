#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(VRLaserPointer))]
public class VRLaserPointerEditor : Editor
{
    private Canvas newCanvas;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VRLaserPointer laserPointer = (VRLaserPointer)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ターゲットCanvas管理", EditorStyles.boldLabel);

        newCanvas = EditorGUILayout.ObjectField("追加するCanvas", newCanvas, typeof(Canvas), true) as Canvas;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Canvasを追加"))
        {
            if (newCanvas != null)
            {
                if (Application.isPlaying)
                {
                    laserPointer.AddTargetCanvas(newCanvas);
                }
                else
                {
                    // 編集モード時はシリアライズされたフィールドを直接更新できないため、
                    // エディタ上で通知
                    EditorUtility.DisplayDialog("情報", "再生モードでのみCanvasを動的に追加できます。\n再生モードでは、Inspector上で追加したいCanvasを指定してこのボタンを押してください。", "OK");
                }
                newCanvas = null;
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "追加するCanvasを選択してください", "OK");
            }
        }

        if (GUILayout.Button("ThumbnailCanvasを検索して追加"))
        {
            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
            Canvas thumbnailCanvas = null;
            
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name.Contains("Thumbnail") || canvas.name.Contains("thumbnail"))
                {
                    thumbnailCanvas = canvas;
                    break;
                }
            }
            
            if (thumbnailCanvas != null)
            {
                if (Application.isPlaying)
                {
                    laserPointer.AddTargetCanvas(thumbnailCanvas);
                    EditorUtility.DisplayDialog("情報", $"{thumbnailCanvas.name}を追加しました", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("情報", $"{thumbnailCanvas.name}が見つかりました。\n再生モードでのみCanvasを動的に追加できます。", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("情報", "ThumbnailCanvasが見つかりませんでした", "OK");
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);

        if (GUILayout.Button("設定をリセット"))
        {
            if (EditorUtility.DisplayDialog("確認", "VRLaserPointerの設定をリセットしますか？", "はい", "いいえ"))
            {
                SerializedProperty maxRayDistanceProp = serializedObject.FindProperty("maxRayDistance");
                SerializedProperty maxVisualDistanceProp = serializedObject.FindProperty("maxVisualDistance");
                SerializedProperty rayWidthProp = serializedObject.FindProperty("rayWidth");
                SerializedProperty rayColorProp = serializedObject.FindProperty("rayColor");
                SerializedProperty dotScaleProp = serializedObject.FindProperty("dotScale");
                SerializedProperty dotColorProp = serializedObject.FindProperty("dotColor");
                SerializedProperty dotPressedColorProp = serializedObject.FindProperty("dotPressedColor");

                maxRayDistanceProp.floatValue = 100f;
                maxVisualDistanceProp.floatValue = 5f;
                rayWidthProp.floatValue = 0.01f;
                rayColorProp.colorValue = new Color(0.0f, 0.5f, 1.0f, 0.5f);
                dotScaleProp.floatValue = 0.02f;
                dotColorProp.colorValue = Color.white;
                dotPressedColorProp.colorValue = Color.red;

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
#endif