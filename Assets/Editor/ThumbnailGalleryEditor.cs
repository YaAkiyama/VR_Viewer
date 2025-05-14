#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ThumbnailGallery))]
public class ThumbnailGalleryEditor : Editor
{
    private float adjustmentValue = 10f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ThumbnailGallery gallery = (ThumbnailGallery)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("X位置調整", EditorStyles.boldLabel);

        // 調整用スライダー
        adjustmentValue = EditorGUILayout.FloatField("調整量", adjustmentValue);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button($"左に {adjustmentValue} 移動"))
        {
            SerializedProperty offsetProp = serializedObject.FindProperty("xPositionOffset");
            offsetProp.floatValue -= adjustmentValue;
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                gallery.RebuildGallery();
            }

            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button($"右に {adjustmentValue} 移動"))
        {
            SerializedProperty offsetProp = serializedObject.FindProperty("xPositionOffset");
            offsetProp.floatValue += adjustmentValue;
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                gallery.RebuildGallery();
            }

            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.EndHorizontal();

        // 位置リセットボタン
        if (GUILayout.Button("位置をリセット (0)"))
        {
            SerializedProperty offsetProp = serializedObject.FindProperty("xPositionOffset");
            offsetProp.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                gallery.RebuildGallery();
            }

            EditorUtility.SetDirty(target);
        }

        // -380プリセット
        if (GUILayout.Button("左に380移動 (-380)"))
        {
            SerializedProperty offsetProp = serializedObject.FindProperty("xPositionOffset");
            offsetProp.floatValue = -380f;
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                gallery.RebuildGallery();
            }

            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("スクロール設定", EditorStyles.boldLabel);

        if (GUILayout.Button("スクロール範囲を再計算"))
        {
            if (Application.isPlaying)
            {
                gallery.CalculateScrollBounds();
            }
            else
            {
                EditorUtility.DisplayDialog("通知", "この機能は実行時にのみ使用できます", "OK");
            }
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("スクロール制限を有効化"))
        {
            SerializedProperty limitProp = serializedObject.FindProperty("limitScrollBounds");
            limitProp.boolValue = true;
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                gallery.SetScrollLimits(true);
            }

            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("スクロール制限を無効化"))
        {
            SerializedProperty limitProp = serializedObject.FindProperty("limitScrollBounds");
            limitProp.boolValue = false;
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                gallery.SetScrollLimits(false);
            }

            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("ギャラリーを再構築"))
        {
            if (Application.isPlaying)
            {
                gallery.RebuildGallery();
            }
            else
            {
                EditorUtility.DisplayDialog("通知", "この機能は実行時にのみ使用できます", "OK");
            }
        }
    }
}
#endif