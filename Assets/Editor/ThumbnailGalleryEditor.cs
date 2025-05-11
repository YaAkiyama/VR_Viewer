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
        EditorGUILayout.LabelField("X�ʒu����", EditorStyles.boldLabel);

        // �����p�X���C�_�[
        adjustmentValue = EditorGUILayout.FloatField("������", adjustmentValue);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button($"���� {adjustmentValue} �ړ�"))
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

        if (GUILayout.Button($"�E�� {adjustmentValue} �ړ�"))
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

        // �ʒu���Z�b�g�{�^��
        if (GUILayout.Button("�ʒu�����Z�b�g (0)"))
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

        // -380�v���Z�b�g
        if (GUILayout.Button("����380�ړ� (-380)"))
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
        EditorGUILayout.LabelField("�X�N���[���ݒ�", EditorStyles.boldLabel);

        if (GUILayout.Button("�X�N���[���͈͂��Čv�Z"))
        {
            if (Application.isPlaying)
            {
                gallery.CalculateScrollBounds();
            }
            else
            {
                EditorUtility.DisplayDialog("�ʒm", "���̋@�\�͎��s���ɂ̂ݎg�p�ł��܂�", "OK");
            }
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("�X�N���[��������L����"))
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

        if (GUILayout.Button("�X�N���[�������𖳌���"))
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

        if (GUILayout.Button("�M�������[���č\�z"))
        {
            if (Application.isPlaying)
            {
                gallery.RebuildGallery();
            }
            else
            {
                EditorUtility.DisplayDialog("�ʒm", "���̋@�\�͎��s���ɂ̂ݎg�p�ł��܂�", "OK");
            }
        }
    }
}
#endif