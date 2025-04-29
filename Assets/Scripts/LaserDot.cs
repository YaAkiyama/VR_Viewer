// LaserDot�N���X�i�V�K�쐬�j
using UnityEngine;

public class LaserDot : MonoBehaviour
{
    [SerializeField] private float dotSize = 0.01f;
    [SerializeField] private Color dotColor = Color.white;

    private MeshRenderer meshRenderer;

    void Start()
    {
        // ���̂̃X�P�[���ݒ�
        transform.localScale = Vector3.one * dotSize;

        // �}�e���A���̐F�ݒ�
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = dotColor;
        }
    }
}