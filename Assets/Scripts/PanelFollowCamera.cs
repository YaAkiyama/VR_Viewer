using UnityEngine;
using Unity.XR.CoreUtils; // XROrigin�p�̐��������O���

public class PanelFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distance = 0.5f; // �J��������̋���
    [SerializeField] private Vector2 screenOffset = new Vector2(-0.3f, 0.3f); // ��ʏ�̈ʒu�����iX:���E�AY:�㉺�j

    // �㉺�̐�����ǉ�
    [SerializeField] private float minVerticalOffset = 0.1f;  // �Œ�̍����i�J�������S����̋����j
    [SerializeField] private float maxVerticalOffset = 0.4f;  // �ō��̍����i�J�������S����̋����j

    private void Start()
    {
        // Camera Rig���烁�C���J�������擾
        if (cameraTransform == null)
        {
            // XR Origin����J�������������o�i�ŐV�̃��\�b�h���g�p�j
            var xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
                cameraTransform = xrOrigin.Camera.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // �J�����̎p�����l�����ď㉺�̌��E���v�Z
        float currentVerticalOffset = screenOffset.y;

        // �㉺�ʒu�𐧌�
        currentVerticalOffset = Mathf.Clamp(currentVerticalOffset, minVerticalOffset, maxVerticalOffset);

        // �J�����̑O�����ɔz�u�i�������w��j
        Vector3 forwardPosition = cameraTransform.position + cameraTransform.forward * distance;

        // ��ʂ̍���Ɉʒu�����i�����������������ʒu���g�p�j
        Vector3 offsetPosition = forwardPosition +
                               cameraTransform.right * screenOffset.x +
                               cameraTransform.up * currentVerticalOffset;

        transform.position = offsetPosition;

        // �J�����Ɠ��������ɐݒ�i��ɐ��ʂ������j
        transform.rotation = cameraTransform.rotation;
    }
}