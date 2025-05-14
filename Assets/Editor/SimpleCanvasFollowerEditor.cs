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
        EditorGUILayout.LabelField("�N�C�b�N�ݒ�", EditorStyles.boldLabel);

        // �{�^����ǉ�
        if (GUILayout.Button("��]�����Z�b�g"))
        {
            follower.ResetRotation();
            EditorUtility.SetDirty(target);
        }

        // ���Δz�u�ݒ�
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("���Δz�u�ݒ�", EditorStyles.boldLabel);

        selectedReferenceCanvas = EditorGUILayout.ObjectField("�Q��Canvas", selectedReferenceCanvas, typeof(Transform), true) as Transform;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("���Δz�u��L����"))
        {
            if (selectedReferenceCanvas != null)
            {
                follower.SetRelativePositioning(true, selectedReferenceCanvas);
                EditorUtility.SetDirty(target);
            }
            else
            {
                EditorUtility.DisplayDialog("�G���[", "�Q��Canvas��I�����Ă�������", "OK");
            }
        }

        if (GUILayout.Button("���Δz�u�𖳌���"))
        {
            follower.SetRelativePositioning(false);
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.EndHorizontal();

        // �X�}�[�g�ݒ�
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("�X�}�[�g�ݒ�", EditorStyles.boldLabel);

        if (GUILayout.Button("ThumbnailCanvas��MapCanvas�̉��ɔz�u"))
        {
            Transform mapCanvas = GameObject.Find("Canvas")?.transform;
            if (mapCanvas != null && follower.gameObject.name.Contains("Thumbnail"))
            {
                follower.SetRelativePositioning(true, mapCanvas, new Vector3(0, -0.15f, 0));
                EditorUtility.SetDirty(target);
            }
            else
            {
                EditorUtility.DisplayDialog("�G���[", "MapCanvas��������Ȃ����A���̃I�u�W�F�N�g��ThumbnailCanvas�ł͂���܂���", "OK");
            }
        }

        // ���Œ�v���Z�b�g
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("���Œ�v���Z�b�g", EditorStyles.boldLabel);

        if (GUILayout.Button("X���̂݌Œ� (0�x)"))
        {
            SerializedProperty fixedXProp = serializedObject.FindProperty("fixedXRotation");
            fixedXProp.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();

            follower.SetLockXOnly();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("Y���̂݌Œ� (0�x)"))
        {
            SerializedProperty fixedYProp = serializedObject.FindProperty("fixedYRotation");
            fixedYProp.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();

            follower.SetLockYOnly();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("X��Y�����Œ� (0, 0)"))
        {
            SerializedProperty fixedXProp = serializedObject.FindProperty("fixedXRotation");
            SerializedProperty fixedYProp = serializedObject.FindProperty("fixedYRotation");

            fixedXProp.floatValue = 0f;
            fixedYProp.floatValue = 0f;

            serializedObject.ApplyModifiedProperties();

            follower.SetLockXAndY();
            EditorUtility.SetDirty(target);
        }

        if (GUILayout.Button("���ׂĂ̎��̌Œ�����i���S�Ǐ]�j"))
        {
            follower.SetUnlockAllAxes();
            EditorUtility.SetDirty(target);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("�ݒ�����O�o��"))
        {
            follower.LogCurrentSettings();
        }
    }
}
#endif