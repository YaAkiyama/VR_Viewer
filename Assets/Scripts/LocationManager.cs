// LocationManagerクラス（修正なし）
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class LocationData
{
    public int id;                   // 通番
    public VideoClip panoramaVideo;  // 360度動画
    public Material skyboxMaterial;  // スカイボックス用マテリアル
    public RenderTexture renderTexture;  // レンダーテクスチャ
    public float mapPositionX;       // マップ上のX座標
    public float mapPositionY;       // マップ上のY座標
    public Sprite thumbnailImage;    // ポジションパネル用サムネイル画像
}
public class LocationManager : MonoBehaviour
{
    [SerializeField] private List<LocationData> locations = new List<LocationData>();
    [SerializeField] private Sprite mapBackgroundImage; // マップの背景画像
    [SerializeField] private Sprite positionMarkerImage; // 位置表示用マーカー画像
    [SerializeField] private Sprite currentPositionMarkerImage; // 現在地マーカー画像

    private int currentLocationIndex = 0;

    public List<LocationData> Locations => locations;
    public Sprite MapBackgroundImage => mapBackgroundImage;
    public Sprite PositionMarkerImage => positionMarkerImage;
    public Sprite CurrentPositionMarkerImage => currentPositionMarkerImage;
    public int CurrentLocationIndex
    {
        get => currentLocationIndex;
        set
        {
            if (value >= 0 && value < locations.Count)
            {
                currentLocationIndex = value;
                OnLocationChanged?.Invoke(currentLocationIndex);
            }
        }
    }

    public LocationData CurrentLocation => locations.Count > 0 ? locations[currentLocationIndex] : null;

    // 場所が変更された時のイベント
    public delegate void LocationChangedHandler(int newLocationIndex);
    public event LocationChangedHandler OnLocationChanged;

    void Start()
    {
        // 初期位置を設定
        if (locations.Count > 0)
        {
            CurrentLocationIndex = 0;
        }
    }
}