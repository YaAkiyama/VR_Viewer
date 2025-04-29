using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class SceneSetup : MonoBehaviour
{
    public void SetupScene()
    {
        // ”ñ„§‚ÌFindObjectOfType‚©‚ç„§‚³‚ê‚éFindAnyObjectByType‚ÉC³
        if (FindAnyObjectByType<XROrigin>() == null)
        {
            GameObject cameraRig = GameObject.Instantiate(Resources.Load("Prefabs/CameraRig")) as GameObject;
            cameraRig.name = "CameraRig";
        }
    }
}