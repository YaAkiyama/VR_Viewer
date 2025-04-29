// MapPanel�N���X�iXR Interaction�̕������C���j
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MapPanel : MonoBehaviour
{
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private Image mapBackgroundImage;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private GameObject currentPositionMarkerPrefab;
    [SerializeField] private float panelDistance = 1.0f;
    [SerializeField] private Vector3 panelOffset = new Vector3(0, 0, 0);

    private LocationManager locationManager;
    private Transform cameraTransform;
    private List<GameObject> positionMarkers = new List<GameObject>();
    private GameObject currentPositionMarker;

    void Start()
    {
        locationManager = FindAnyObjectByType<LocationManager>();
        cameraTransform = Camera.main.transform;

        // �}�b�v�w�i�̐ݒ�
        if (locationManager != null && mapBackgroundImage != null)
        {
            mapBackgroundImage.sprite = locationManager.MapBackgroundImage;
            mapBackgroundImage.color = new Color(1, 1, 1, 0.9f); // 10%����

            // �}�[�J�[�̔z�u
            SetupMarkers();

            // ���ݒn�}�[�J�[�̔z�u
            UpdateCurrentPositionMarker();

            // �ʒu�ύX�C�x���g��o�^
            locationManager.OnLocationChanged += OnLocationChanged;
        }
    }

    void Update()
    {
        // �p�l���̈ʒu���J�����ɒǏ]
        FollowCamera();

        // ���ݒn�}�[�J�[�̌������J�����ɍ��킹��
        UpdateCurrentPositionMarkerRotation();
    }

    private void FollowCamera()
    {
        if (cameraTransform != null)
        {
            // �J�����̌����ɍ��킹�ăp�l�����Œ苗���ɔz�u
            Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * panelDistance;
            targetPosition += cameraTransform.right * panelOffset.x;
            targetPosition += cameraTransform.up * panelOffset.y;
            targetPosition += cameraTransform.forward * panelOffset.z;

            transform.position = targetPosition;

            // �J�����̕���������
            transform.LookAt(cameraTransform);
            transform.rotation = Quaternion.LookRotation(-transform.forward, cameraTransform.up);
        }
    }

    private void SetupMarkers()
    {
        // �����̃}�[�J�[���N���A
        foreach (var marker in positionMarkers)
        {
            Destroy(marker);
        }
        positionMarkers.Clear();

        // �e�ʒu�Ƀ}�[�J�[��z�u
        foreach (var location in locationManager.Locations)
        {
            GameObject marker = Instantiate(markerPrefab, mapRect);
            RectTransform markerRect = marker.GetComponent<RectTransform>();

            // �}�b�v��̍��W�ɔz�u�i���㌴�_�j
            markerRect.anchorMin = new Vector2(0, 1);
            markerRect.anchorMax = new Vector2(0, 1);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = new Vector2(location.mapPositionX, -location.mapPositionY);

            // �摜�ݒ�
            Image markerImage = marker.GetComponent<Image>();
            markerImage.sprite = locationManager.PositionMarkerImage;
            markerImage.color = new Color(1, 1, 1, 0.9f); // 10%����

            // �C���^���N�V�����ݒ�i�C���ӏ��j
            IXRSelectInteractable interactable = marker.AddComponent<XRSimpleInteractable>();
            int locationId = location.id;

            // �V�����C�x���g�o�^���@
            interactable.selectEntered.AddListener((args) => {
                SelectLocation(locationId);
            });

            positionMarkers.Add(marker);
        }
    }

    private void UpdateCurrentPositionMarker()
    {
        if (currentPositionMarker == null)
        {
            currentPositionMarker = Instantiate(currentPositionMarkerPrefab, mapRect);
            Image markerImage = currentPositionMarker.GetComponent<Image>();
            markerImage.sprite = locationManager.CurrentPositionMarkerImage;
        }

        if (locationManager.CurrentLocation != null)
        {
            RectTransform markerRect = currentPositionMarker.GetComponent<RectTransform>();

            // ���݂̈ʒu�ɔz�u
            markerRect.anchorMin = new Vector2(0, 1);
            markerRect.anchorMax = new Vector2(0, 1);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = new Vector2(
                locationManager.CurrentLocation.mapPositionX,
                -locationManager.CurrentLocation.mapPositionY
            );
        }
    }

    private void UpdateCurrentPositionMarkerRotation()
    {
        if (currentPositionMarker != null && cameraTransform != null)
        {
            // Y����]�̂ݍl��
            float cameraYRotation = cameraTransform.eulerAngles.y;
            currentPositionMarker.transform.rotation = Quaternion.Euler(0, 0, -cameraYRotation);
        }
    }

    private void SelectLocation(int locationId)
    {
        // ID�Ɉ�v����ʒu�C���f�b�N�X������
        for (int i = 0; i < locationManager.Locations.Count; i++)
        {
            if (locationManager.Locations[i].id == locationId)
            {
                locationManager.CurrentLocationIndex = i;
                return;
            }
        }
    }

    private void OnLocationChanged(int newLocationIndex)
    {
        UpdateCurrentPositionMarker();
    }

    private void OnDestroy()
    {
        if (locationManager != null)
        {
            locationManager.OnLocationChanged -= OnLocationChanged;
        }
    }
}