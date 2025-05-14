using UnityEngine;
using Unity.XR.CoreUtils;

public class SimpleCanvasFollower : MonoBehaviour
{
    [Header("�Ǐ]�ݒ�")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 _positionOffset = new Vector3(0f, 0f, 0.5f);

    [Header("��]�ݒ�")]
    [SerializeField] private bool lookAtCamera = true;

    [Tooltip("�I�����������Œ肷�邩�ǂ���")]
    [SerializeField] private bool lockXRotation = false;
    [SerializeField] private bool lockYRotation = false;
    [SerializeField] private bool lockZRotation = false;

    [Tooltip("�Œ肷��ꍇ�̊e���̒l")]
    [SerializeField] private float fixedXRotation = 0f;
    [SerializeField] private float fixedYRotation = 0f;
    [SerializeField] private float fixedZRotation = 0f;

    [Header("���Δz�u�ݒ�")]
    [SerializeField] private bool useRelativePositioning = false;
    [SerializeField] private Transform referenceCanvas;
    [SerializeField] private Vector3 relativeOffset = new Vector3(0f, -0.15f, 0f);

    // �O������A�N�Z�X���邽�߂̃v���p�e�B
    public Vector3 positionOffset
    {
        get { return _positionOffset; }
        set { _positionOffset = value; }
    }

    void Start()
    {
        // �J�����̎Q�Ƃ��擾
        if (cameraTransform == null)
        {
            // XR���̏ꍇ
            var xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                cameraTransform = xrOrigin.Camera.transform;
                Debug.Log("XR�J�������Q�ƂƂ��Đݒ肵�܂���: " + gameObject.name);
            }
            else
            {
                // �ʏ���̏ꍇ
                cameraTransform = Camera.main.transform;

                if (cameraTransform != null)
                {
                    Debug.Log("���C���J�������Q�ƂƂ��Đݒ肵�܂���: " + gameObject.name);
                }
                else
                {
                    Debug.LogError("�J������������܂���B�蓮�ŎQ�Ƃ�ݒ肵�Ă�������: " + gameObject.name);
                }
            }
        }

        // ���Δz�u�̎Q�Ɛݒ���`�F�b�N
        if (useRelativePositioning && referenceCanvas == null)
        {
            Debug.LogWarning("���Δz�u���L���ł����A�Q��Canvas���ݒ肳��Ă��܂���: " + gameObject.name);
        }

        // ������]��ݒ�
        ApplyRotation();
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // �ʒu���v�Z
        Vector3 targetPosition;

        if (useRelativePositioning && referenceCanvas != null)
        {
            // �Q��Canvas����̑��Έʒu���v�Z
            targetPosition = CalculateRelativePosition();
        }
        else
        {
            // �J�����ɑ΂��钼�ڈʒu���v�Z
            targetPosition = CalculateDirectPosition();
        }

        // �ʒu��K�p
        transform.position = targetPosition;

        // ��]��K�p
        ApplyRotation();
    }

    // �J�����ɑ΂��钼�ڈʒu�v�Z
    private Vector3 CalculateDirectPosition()
    {
        return cameraTransform.position +
               cameraTransform.forward * _positionOffset.z +
               cameraTransform.up * _positionOffset.y +
               cameraTransform.right * _positionOffset.x;
    }

    // �Q��Canvas�ɑ΂��鑊�Έʒu�v�Z
    private Vector3 CalculateRelativePosition()
    {
        // �Q��Canvas����̑��Έʒu���v�Z
        Vector3 referencePosition = referenceCanvas.position;

        // �㉺�ʒu�̒����i�Q��Canvas�̍��W�n�Ōv�Z�j
        Vector3 upOffset = referenceCanvas.up * relativeOffset.y;
        Vector3 rightOffset = referenceCanvas.right * relativeOffset.x;
        Vector3 forwardOffset = referenceCanvas.forward * relativeOffset.z;

        return referencePosition + upOffset + rightOffset + forwardOffset;
    }

    // ��]��K�p�i���Œ���l���j
    private void ApplyRotation()
    {
        if (cameraTransform == null) return;

        if (useRelativePositioning && referenceCanvas != null)
        {
            // �Q��Canvas�Ɠ�����]���g�p
            transform.rotation = referenceCanvas.rotation;
        }
        else if (lookAtCamera)
        {
            // ��{�I�ɃJ�����̕����Ɍ�������
            transform.LookAt(cameraTransform);
            transform.Rotate(0, 180, 0); // �p�l�������ʂ������悤�ɒ���

            // ���݂̉�]���擾
            Vector3 currentRotation = transform.rotation.eulerAngles;

            // �e�����ƂɌŒ肷�邩�ǂ����𔻒f���A�K�v�ɉ����ďC��
            float newX = lockXRotation ? fixedXRotation : currentRotation.x;
            float newY = lockYRotation ? fixedYRotation : currentRotation.y;
            float newZ = lockZRotation ? fixedZRotation : currentRotation.z;

            // ���Œ肪�w�肳��Ă���ꍇ�̂݉�]���X�V
            if (lockXRotation || lockYRotation || lockZRotation)
            {
                transform.rotation = Quaternion.Euler(newX, newY, newZ);
            }
        }
    }

    // ���Δz�u��ݒ�
    public void SetRelativePositioning(bool enable, Transform reference = null, Vector3 offset = default)
    {
        useRelativePositioning = enable;

        if (reference != null)
        {
            referenceCanvas = reference;
        }

        if (offset != default)
        {
            relativeOffset = offset;
        }

        // �����ɓK�p
        if (cameraTransform != null)
        {
            LateUpdate();
        }
    }

    // �v���Z�b�g: X�����Œ�
    public void SetLockXOnly()
    {
        lockXRotation = true;
        lockYRotation = false;
        lockZRotation = false;
        ApplyRotation();
    }

    // �v���Z�b�g: Y�����Œ�
    public void SetLockYOnly()
    {
        lockXRotation = false;
        lockYRotation = true;
        lockZRotation = false;
        ApplyRotation();
    }

    // �v���Z�b�g: X��Y�����Œ�
    public void SetLockXAndY()
    {
        lockXRotation = true;
        lockYRotation = true;
        lockZRotation = false;
        ApplyRotation();
    }

    // �v���Z�b�g: ���ׂĂ̎����Œ�
    public void SetLockAllAxes()
    {
        lockXRotation = true;
        lockYRotation = true;
        lockZRotation = true;
        ApplyRotation();
    }

    // �v���Z�b�g: ���ׂĂ̎����Œ����
    public void SetUnlockAllAxes()
    {
        lockXRotation = false;
        lockYRotation = false;
        lockZRotation = false;
        ApplyRotation();
    }

    // ��]�����Z�b�g
    public void ResetRotation()
    {
        ApplyRotation();
        Debug.Log("��]�����Z�b�g���܂���: " + gameObject.name);
    }

    // �f�o�b�O�p: ���݂̐ݒ��\��
    public void LogCurrentSettings()
    {
        Debug.Log($"===== {gameObject.name} �ݒ� =====");
        Debug.Log($"�J�����Q��: {(cameraTransform != null ? cameraTransform.name : "�ݒ�Ȃ�")}");
        Debug.Log($"�ʒu�I�t�Z�b�g: {_positionOffset}");
        Debug.Log($"�J����������: {lookAtCamera}");
        Debug.Log($"X���Œ�: {lockXRotation} (�l: {fixedXRotation})");
        Debug.Log($"Y���Œ�: {lockYRotation} (�l: {fixedYRotation})");
        Debug.Log($"Z���Œ�: {lockZRotation} (�l: {fixedZRotation})");
        Debug.Log($"���Δz�u: {useRelativePositioning}");
        Debug.Log($"�Q��Canvas: {(referenceCanvas != null ? referenceCanvas.name : "�ݒ�Ȃ�")}");
        Debug.Log($"���΃I�t�Z�b�g: {relativeOffset}");
        Debug.Log($"==================================");
    }

    // Gizmo�̕`��i�G�f�B�^��ł̎��o���j
    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null) return;

        // �ʒu���v�Z
        Vector3 targetPosition;

        if (useRelativePositioning && referenceCanvas != null)
        {
            targetPosition = CalculateRelativePosition();
        }
        else
        {
            targetPosition = CalculateDirectPosition();
        }

        // �ʒu������Gizmo��`��
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(targetPosition, 0.01f);

        // �Q�ƃ��C����`��
        if (useRelativePositioning && referenceCanvas != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(referenceCanvas.position, targetPosition);
        }
        else
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cameraTransform.position, targetPosition);
        }
    }
}