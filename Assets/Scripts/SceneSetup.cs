using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class SceneSetup : MonoBehaviour
{
    public void SetupScene()
    {
        // �񐄏���FindObjectOfType���琄�������FindAnyObjectByType�ɏC��
        if (FindAnyObjectByType<XROrigin>() == null)
        {
            GameObject cameraRig = GameObject.Instantiate(Resources.Load("Prefabs/CameraRig")) as GameObject;
            cameraRig.name = "CameraRig";
        }
    }
}