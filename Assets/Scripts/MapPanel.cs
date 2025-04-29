// MapPanelクラス（XR Interactionの部分を修正）
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

        // マップ背景の設定
        if (locationManager != null && mapBackgroundImage != null)
        {
            mapBackgroundImage.sprite = locationManager.MapBackgroundImage;
            mapBackgroundImage.color = new Color(1, 1, 1, 0.9f); // 10%透過

            // マーカーの配置
            SetupMarkers();

            // 現在地マーカーの配置
            UpdateCurrentPositionMarker();

            // 位置変更イベントを登録
            locationManager.OnLocationChanged += OnLocationChanged;
        }
    }

    void Update()
    {
        // パネルの位置をカメラに追従
        FollowCamera();

        // 現在地マーカーの向きをカメラに合わせる
        UpdateCurrentPositionMarkerRotation();
    }

    private void FollowCamera()
    {
        if (cameraTransform != null)
        {
            // カメラの向きに合わせてパネルを固定距離に配置
            Vector3 targetPosition = cameraTransform.position + cameraTransform.forward * panelDistance;
            targetPosition += cameraTransform.right * panelOffset.x;
            targetPosition += cameraTransform.up * panelOffset.y;
            targetPosition += cameraTransform.forward * panelOffset.z;

            transform.position = targetPosition;

            // カメラの方向を向く
            transform.LookAt(cameraTransform);
            transform.rotation = Quaternion.LookRotation(-transform.forward, cameraTransform.up);
        }
    }

    private void SetupMarkers()
    {
        // 既存のマーカーをクリア
        foreach (var marker in positionMarkers)
        {
            Destroy(marker);
        }
        positionMarkers.Clear();

        // 各位置にマーカーを配置
        foreach (var location in locationManager.Locations)
        {
            GameObject marker = Instantiate(markerPrefab, mapRect);
            RectTransform markerRect = marker.GetComponent<RectTransform>();

            // マップ上の座標に配置（左上原点）
            markerRect.anchorMin = new Vector2(0, 1);
            markerRect.anchorMax = new Vector2(0, 1);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = new Vector2(location.mapPositionX, -location.mapPositionY);

            // 画像設定
            Image markerImage = marker.GetComponent<Image>();
            markerImage.sprite = locationManager.PositionMarkerImage;
            markerImage.color = new Color(1, 1, 1, 0.9f); // 10%透過

            // インタラクション設定（修正箇所）
            IXRSelectInteractable interactable = marker.AddComponent<XRSimpleInteractable>();
            int locationId = location.id;

            // 新しいイベント登録方法
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

            // 現在の位置に配置
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
            // Y軸回転のみ考慮
            float cameraYRotation = cameraTransform.eulerAngles.y;
            currentPositionMarker.transform.rotation = Quaternion.Euler(0, 0, -cameraYRotation);
        }
    }

    private void SelectLocation(int locationId)
    {
        // IDに一致する位置インデックスを検索
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