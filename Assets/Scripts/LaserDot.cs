// LaserDotクラス（新規作成）
using UnityEngine;

public class LaserDot : MonoBehaviour
{
    [SerializeField] private float dotSize = 0.01f;
    [SerializeField] private Color dotColor = Color.white;

    private MeshRenderer meshRenderer;

    void Start()
    {
        // 球体のスケール設定
        transform.localScale = Vector3.one * dotSize;

        // マテリアルの色設定
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = dotColor;
        }
    }
}