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
    [SerializeField] private Color rayColor = Color.white;
    [SerializeField] private float rayWidth = 0.02f;
    [SerializeField] private bool stopAtFirstHit = true;

    void Start()
    {
        // 自動検出（明示的に設定されていない場合）
        if (leftRayInteractor == null || rightRayInteractor == null)
        {
            TryFindRayInteractors();
        }

        SetupRayInteractor(leftRayInteractor);
        SetupRayInteractor(rightRayInteractor);
    }

    private void TryFindRayInteractors()
    {
        XRRayInteractor[] interactors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);
        foreach (var interactor in interactors)
        {
            string name = interactor.gameObject.name.ToLower();
            if (name.Contains("left") && leftRayInteractor == null)
            {
                leftRayInteractor = interactor;
                Debug.Log("左手のXRRayInteractorを自動検出: " + interactor.gameObject.name);
            }
            else if (name.Contains("right") && rightRayInteractor == null)
            {
                rightRayInteractor = interactor;
                Debug.Log("右手のXRRayInteractorを自動検出: " + interactor.gameObject.name);
            }
        }
    }

    private void SetupRayInteractor(XRRayInteractor rayInteractor)
    {
        if (rayInteractor != null)
        {
            // レーザーの長さを設定
            rayInteractor.maxRaycastDistance = rayLength;

            // レーザーの見た目を設定
            var lineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
            if (lineVisual == null)
            {
                lineVisual = rayInteractor.gameObject.AddComponent<XRInteractorLineVisual>();
                Debug.Log("XRInteractorLineVisualを追加: " + rayInteractor.gameObject.name);
            }

            // 線の太さ
            lineVisual.lineWidth = rayWidth;

            // 終端のドットの設定
            if (reticlePrefab != null)
            {
                lineVisual.reticle = reticlePrefab;
            }
            else if (Resources.Load<GameObject>("Prefabs/LaserDot") != null)
            {
                lineVisual.reticle = Resources.Load<GameObject>("Prefabs/LaserDot");
                Debug.Log("レティクルをResourcesから設定");
            }

            // 線の色設定
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(rayColor, 0.0f);
            colorKeys[1] = new GradientColorKey(rayColor, 1.0f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(0.0f, 1.0f);

            gradient.SetKeys(colorKeys, alphaKeys);
            lineVisual.validColorGradient = gradient;

            // 最初のヒットで止める
            lineVisual.stopLineAtFirstRaycastHit = stopAtFirstHit;

            Debug.Log("レーザーポインターを設定完了: " + rayInteractor.gameObject.name);
        }
    }
}