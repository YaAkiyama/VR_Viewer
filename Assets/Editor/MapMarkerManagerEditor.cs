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

        // ��]�ݒ�Z�N�V����
        showRotationSettings = EditorGUILayout.Foldout(showRotationSettings, "���ݒn�}�[�J�[��]�ݒ�", true);

        if (showRotationSettings)
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button("�i�s������\���i��]�L���j"))
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

            if (GUILayout.Button("�t������\���i180�x��]�j"))
            {
                SerializedProperty rotateProp = serializedObject.FindProperty("rotateCurrentMarker");
                SerializedProperty offsetProp = serializedObject.FindProperty("markerRotationOffset");

                rotateProp.boolValue = true;
                offsetProp.floatValue = 180f;

                serializedObject.ApplyModifiedProperties();

                manager.SetCurrentMarkerRotation(true, 180f);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("��]�𖳌����i�Œ�\���j"))
            {
                SerializedProperty rotateProp = serializedObject.FindProperty("rotateCurrentMarker");

                rotateProp.boolValue = false;

                serializedObject.ApplyModifiedProperties();

                manager.SetCurrentMarkerRotation(false);
                EditorUtility.SetDirty(target);
            }

            EditorGUI.indentLevel--;
        }

        // �\���ݒ�Z�N�V����
        EditorGUILayout.Space();
        showVisibilitySettings = EditorGUILayout.Foldout(showVisibilitySettings, "�}�[�J�[�\���ݒ�", true);

        if (showVisibilitySettings)
        {
            EditorGUI.indentLevel++;

            SerializedProperty hideProp = serializedObject.FindProperty("hideOriginalMarker");

            EditorGUILayout.PropertyField(hideProp, new GUIContent("�I�𒆂̒ʏ�}�[�J�[���B��"));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("�ʏ�}�[�J�[���B��"))
            {
                hideProp.boolValue = true;
                serializedObject.ApplyModifiedProperties();

                manager.SetHideOriginalMarker(true);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("���ׂĕ\��"))
            {
                hideProp.boolValue = false;
                serializedObject.ApplyModifiedProperties();

                manager.SetHideOriginalMarker(false);
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("�}�[�J�[�\����Ԃ��X�V"))
            {
                manager.UpdateMarkerVisibility();
                EditorUtility.SetDirty(target);
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif