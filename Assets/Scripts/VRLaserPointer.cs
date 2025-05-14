using UnityEngine;
using System.Collections;

public class VRLaserPointer : MonoBehaviour
{
    // レーザーの見た目の設定
    public Color laserColor = Color.red;
    public float laserWidth = 0.01f;
    public float laserMaxLength = 10.0f;
    public AnimationCurve laserWidthCurve;

    // レーザーの終点ドットの設定
    public GameObject dotPrefab;
    public float dotScale = 0.1f;
    
    // コンポーネント参照
    private LineRenderer lineRenderer;
    private GameObject dot;

    // 状態変数
    private bool isHitting = false;
    private RaycastHit hitInfo;

    void Start()
    {
        // LineRendererの初期化
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth * 0.5f; // 先細りに
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);
        lineRenderer.positionCount = 2;

        // レーザードットの初期化
        if (dotPrefab != null)
        {
            dot = Instantiate(dotPrefab, transform.position, Quaternion.identity);
            dot.transform.localScale = Vector3.one * dotScale;
        }
        else
        {
            // ドットプレハブがない場合はプリミティブ球体で代用
            dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dot.GetComponent<Collider>());
            dot.transform.localScale = Vector3.one * dotScale;
            dot.GetComponent<Renderer>().material.color = laserColor;
        }
        
        dot.SetActive(false);
    }

    void Update()
    {
        UpdateLaserBeam();
    }
    
    // レーザービームの更新
    private void UpdateLaserBeam()
    {
        // レーザーの始点は常にコントローラー（このオブジェクト）の位置
        Vector3 startPosition = transform.position;
        lineRenderer.SetPosition(0, startPosition);
        
        // レイキャストで衝突判定
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out hitInfo, laserMaxLength))
        {
            isHitting = true;
            // 実際に当たった位置にレーザーの終点を設定
            lineRenderer.SetPosition(1, hitInfo.point);
            
            // ドットを衝突位置に表示
            dot.SetActive(true);
            dot.transform.position = hitInfo.point;
            dot.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hitInfo.normal);
        }
        else
        {
            isHitting = false;
            // 何も当たらなかった場合は、最大長さまでレーザーを伸ばす
            lineRenderer.SetPosition(1, transform.position + transform.forward * laserMaxLength);
            
            // ドットは非表示
            dot.SetActive(false);
        }
        
        // レーザーの見た目を更新
        UpdateLaserAppearance();
    }
    
    // レーザーの見た目の更新
    private void UpdateLaserAppearance()
    {
        // レーザーの先細り効果をWidthCurveで実現（もし設定されていれば）
        if (laserWidthCurve != null && laserWidthCurve.length > 0)
        {
            lineRenderer.widthCurve = laserWidthCurve;
        }
        else
        {
            // デフォルトでは先細りに
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth * 0.5f;
        }
        
        // 色の設定
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);
        
        // ドットの色も更新
        if (dot != null && dot.GetComponent<Renderer>() != null)
        {
            dot.GetComponent<Renderer>().material.color = laserColor;
        }
    }
    
    // 衝突情報を外部から取得できるメソッド
    public bool IsHitting()
    {
        return isHitting;
    }
    
    public RaycastHit GetHitInfo()
    {
        return hitInfo;
    }
}
