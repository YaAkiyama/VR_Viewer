#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapMarkerManager))]
public class MapMarkerManagerEditor : Editor
{
    private bool showRotationSettings = true;
    private bool showVisibilitySettings = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapMarkerManager manager = (MapMarkerManager)target;

        EditorGUILayout.Space();

        // 回転設定セクション
        showRotationSettings = EditorGUILayout.Foldout(showRotationSettings, "現在地マーカー回転設定", true);

        if (showRotationSettings)
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button("進行方向を表示（回転有効）"))
            {
                SerializedProperty rotateProp = serializedObject.FindProperty("rotateCurrentMarker");
                SerializedProperty offsetProp = serializedObject.FindProperty("markerRotationOffset");
                SerializedProperty smoothProp = serializedObject.FindProperty("smoothRotation");

                rotateProp.boolValue = true;
                offsetProp.floatValue = 0f;
                smoothProp.boolValue = true;

                serializedObject.ApplyModifiedProperties();

                manager.SetCurrentMarkerRotation(true, 0f);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("逆方向を表示（180度回転）"))
            {
                SerializedProperty rotateProp = serializedObject.FindProperty("rotateCurrentMarker");
                SerializedProperty offsetProp = serializedObject.FindProperty("markerRotationOffset");

                rotateProp.boolValue = true;
                offsetProp.floatValue = 180f;

                serializedObject.ApplyModifiedProperties();

                manager.SetCurrentMarkerRotation(true, 180f);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("回転を無効化（固定表示）"))
            {
                SerializedProperty rotateProp = serializedObject.FindProperty("rotateCurrentMarker");

                rotateProp.boolValue = false;

                serializedObject.ApplyModifiedProperties();

                manager.SetCurrentMarkerRotation(false);
                EditorUtility.SetDirty(target);
            }

            EditorGUI.indentLevel--;
        }

        // 表示設定セクション
        EditorGUILayout.Space();
        showVisibilitySettings = EditorGUILayout.Foldout(showVisibilitySettings, "マーカー表示設定", true);

        if (showVisibilitySettings)
        {
            EditorGUI.indentLevel++;

            SerializedProperty hideProp = serializedObject.FindProperty("hideOriginalMarker");

            EditorGUILayout.PropertyField(hideProp, new GUIContent("選択中の通常マーカーを隠す"));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("通常マーカーを隠す"))
            {
                hideProp.boolValue = true;
                serializedObject.ApplyModifiedProperties();

                manager.SetHideOriginalMarker(true);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("すべて表示"))
            {
                hideProp.boolValue = false;
                serializedObject.ApplyModifiedProperties();

                manager.SetHideOriginalMarker(false);
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("マーカー表示状態を更新"))
            {
                manager.UpdateMarkerVisibility();
                EditorUtility.SetDirty(target);
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif