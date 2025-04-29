// PanoramaPlayer�N���X���쐬
using UnityEngine;
using UnityEngine.Video;

public class PanoramaPlayer : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Material skyboxMaterial;

    private LocationManager locationManager;

    private void Start()
    {
        locationManager = FindAnyObjectByType<LocationManager>();

        if (locationManager != null)
        {
            locationManager.OnLocationChanged += OnLocationChanged;

            // ���������ݒ�
            if (locationManager.CurrentLocation != null)
            {
                PlayVideo(locationManager.CurrentLocation);
            }
        }

        if (skyboxMaterial == null)
        {
            skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
        }
    }

    private void OnLocationChanged(int newLocationIndex)
    {
        PlayVideo(locationManager.CurrentLocation);
    }

    private void PlayVideo(LocationData location)
    {
        if (location == null || location.panoramaVideo == null)
            return;

        // VideoPlayer�̐ݒ�
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = location.renderTexture;
        videoPlayer.clip = location.panoramaVideo;
        videoPlayer.isLooping = true;

        // �X�J�C�{�b�N�X�}�e���A���̐ݒ�
        location.skyboxMaterial.mainTexture = location.renderTexture;
        RenderSettings.skybox = location.skyboxMaterial;

        // ����Đ�
        videoPlayer.Play();
    }

    private void OnDestroy()
    {
        if (locationManager != null)
        {
            locationManager.OnLocationChanged -= OnLocationChanged;
        }
    }
}