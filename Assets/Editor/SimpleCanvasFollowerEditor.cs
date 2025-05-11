#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleCanvasFollower))]
public class SimpleCanvasFollowerEditor : Editor
{
    private Transform selectedReferenceCanvas;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SimpleCanvasFollower follower = (SimpleCanvasFollower)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("クイック設定", EditorStyles.boldLabel);

        // ボタンを追加
        if (GUILayout.Button("回転をリセット"))
        {
            follower.ResetRotation();
            EditorUtility.SetDirty(target);
        }

        // 相対配置設定
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("相対配置設定", EditorStyles.boldLabel);

        selectedReferenceCanvas = EditorGUILayout.ObjectField("参照Canvas", selectedReferenceCanvas, typeof(Transform), true) as Transform;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("相対配置を有効化"))
        {
            if (selectedReferenceCanvas != null)
            {
                follower.SetRelativePositioning(true, selectedReferenceCanvas);
                EditorUtility.SetDirty(target);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "参照Canvasを選択してください", "OK");
            }
        }

        if (GUILayout.Button("相対配置を無効化"))
        {
            follower.SetRelativePositioning(false);
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.EndHorizontal();

        // スマート設定
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("スマート設定", EditorStyles.boldLabel);

        if (GUILayout.Button("ThumbnailCanvasをMapCanvasの下に配置"))
        {
            Transform mapCanvas = GameObject.Find("Canvas")?.transform;
            if (mapCanvas != null && follower.gameObject.name.Contains("Thumbnail"))
            {
                follower.SetRelativePositioning(true, mapCanvas, new Vector3(0, -0.15f, 0));
                EditorUtility.SetDirty(target);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "MapCanvasが見つからないか、このオブジェクトがThumbnailCanvasではありません", "OK");
            }
        }

        // 軸固定プリセット
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("軸固定プリセット", EditorStyles.boldLabel);

        if (GUILayout.Button("X軸のみ固定 (0度)"))
        {
            SerializedProperty fixedXProp = serializedObject.FindProperty("fixedXRotation");
            fixedXProp.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();

            follower.SetLockXOnly();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("Y軸のみ固定 (0度)"))
        {
            SerializedProperty fixedYProp = serializedObject.FindProperty("fixedYRotation");
            fixedYProp.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();

            follower.SetLockYOnly();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("XとY軸を固定 (0, 0)"))
        {
            SerializedProperty fixedXProp = serializedObject.FindProperty("fixedXRotation");
            SerializedProperty fixedYProp = serializedObject.FindProperty("fixedYRotation");

            fixedXProp.floatValue = 0f;
            fixedYProp.floatValue = 0f;

            serializedObject.ApplyModifiedProperties();

            follower.SetLockXAndY();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("すべての軸の固定解除（完全追従）"))
        {
            follower.SetUnlockAllAxes();
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("設定をログ出力"))
        {
            follower.LogCurrentSettings();
        }
    }
}
#endif