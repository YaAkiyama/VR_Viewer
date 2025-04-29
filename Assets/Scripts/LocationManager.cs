// LocationManager�N���X�i�C���Ȃ��j
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class LocationData
{
    public int id;                   // �ʔ�
    public VideoClip panoramaVideo;  // 360�x����
    public Material skyboxMaterial;  // �X�J�C�{�b�N�X�p�}�e���A��
    public RenderTexture renderTexture;  // �����_�[�e�N�X�`��
    public float mapPositionX;       // �}�b�v���X���W
    public float mapPositionY;       // �}�b�v���Y���W
    public Sprite thumbnailImage;    // �|�W�V�����p�l���p�T���l�C���摜
}
public class LocationManager : MonoBehaviour
{
    [SerializeField] private List<LocationData> locations = new List<LocationData>();
    [SerializeField] private Sprite mapBackgroundImage; // �}�b�v�̔w�i�摜
    [SerializeField] private Sprite positionMarkerImage; // �ʒu�\���p�}�[�J�[�摜
    [SerializeField] private Sprite currentPositionMarkerImage; // ���ݒn�}�[�J�[�摜

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

    // �ꏊ���ύX���ꂽ���̃C�x���g
    public delegate void LocationChangedHandler(int newLocationIndex);
    public event LocationChangedHandler OnLocationChanged;

    void Start()
    {
        // �����ʒu��ݒ�
        if (locations.Count > 0)
        {
            CurrentLocationIndex = 0;
        }
    }
}