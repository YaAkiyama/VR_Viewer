// PanoramaPlayerクラスを作成
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

            // 初期動画を設定
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

        // VideoPlayerの設定
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = location.renderTexture;
        videoPlayer.clip = location.panoramaVideo;
        videoPlayer.isLooping = true;

        // スカイボックスマテリアルの設定
        location.skyboxMaterial.mainTexture = location.renderTexture;
        RenderSettings.skybox = location.skyboxMaterial;

        // 動画再生
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