// LaserPointerSetup�N���X�i�C���Łj
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class LaserPointerSetup : MonoBehaviour
{
    [SerializeField] private XRRayInteractor leftRayInteractor;
    [SerializeField] private XRRayInteractor rightRayInteractor;
    [SerializeField] private float rayLength = 5f;
    [SerializeField] private GameObject reticlePrefab;

    void Start()
    {
        SetupRayInteractor(leftRayInteractor);
        SetupRayInteractor(rightRayInteractor);
    }

    private void SetupRayInteractor(XRRayInteractor rayInteractor)
    {
        if (rayInteractor != null)
        {
            // ���[�U�[�̒�����ݒ�
            rayInteractor.maxRaycastDistance = rayLength;

            // ���[�U�[�̌����ڂ�ݒ�
            var lineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
            if (lineVisual != null)
            {
                // ���̑���
                lineVisual.lineWidth = 0.02f;

                // �I�[�̃h�b�g�̐ݒ�
                if (reticlePrefab != null)
                {
                    lineVisual.reticle = reticlePrefab;
                }
                else
                {
                    Debug.LogWarning("���[�U�[�|�C���^�[�p�̃��e�B�N���v���n�u���ݒ肳��Ă��܂���B");
                }

                lineVisual.stopLineAtFirstRaycastHit = true;
            }
        }
    }
}