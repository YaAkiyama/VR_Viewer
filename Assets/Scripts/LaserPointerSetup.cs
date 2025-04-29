// LaserPointerSetupクラス（修正版）
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
            // レーザーの長さを設定
            rayInteractor.maxRaycastDistance = rayLength;

            // レーザーの見た目を設定
            var lineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
            if (lineVisual != null)
            {
                // 線の太さ
                lineVisual.lineWidth = 0.02f;

                // 終端のドットの設定
                if (reticlePrefab != null)
                {
                    lineVisual.reticle = reticlePrefab;
                }
                else
                {
                    Debug.LogWarning("レーザーポインター用のレティクルプレハブが設定されていません。");
                }

                lineVisual.stopLineAtFirstRaycastHit = true;
            }
        }
    }
}