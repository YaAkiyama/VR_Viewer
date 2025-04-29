using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PositionPanel : MonoBehaviour
{
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private GameObject thumbnailPrefab;
    [SerializeField] private float panelDistance = 1.0f;
    [SerializeField] private Vector3 panelOffset = new Vector3(0, -0.3f, 0);
    [SerializeField] private float thumbnailSpacing = 10f;
    [SerializeField] private float thumbnailWidth = 150f;
    [SerializeField] private float thumbnailHeight = 100f;

    private LocationManager locationManager;
    private Transform cameraTransform;
    private List<GameObject> thumbnails = new List<GameObject>();
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    private Transform dragControllerTransform = null;
    private bool lastPressState = false;

    void Start()
    {
        locationManager = FindAnyObjectByType<LocationManager>();
        cameraTransform = Camera.main.transform;

        if (locationManager != null)
        {
            // サムネイルの作成
            SetupThumbnails();

            // 位置変更イベントを登録
            locationManager.OnLocationChanged += OnLocationChanged;

            // 初期選択位置を強調表示
            UpdateSelectedThumbnail();
        }
    }

    void Update()
    {
        // パネルの位置をカメラに追従
        FollowCamera();

        // ドラッグスクロール処理
        HandleDragScroll();
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

    private void SetupThumbnails()
    {
        // 既存のサムネイルをクリア
        foreach (var thumbnail in thumbnails)
        {
            Destroy(thumbnail);
        }
        thumbnails.Clear();

        // コンテンツサイズを設定
        float totalWidth = locationManager.Locations.Count * (thumbnailWidth + thumbnailSpacing) - thumbnailSpacing;
        contentRect.sizeDelta = new Vector2(totalWidth, contentRect.sizeDelta.y);

        // 各位置のサムネイルを作成
        for (int i = 0; i < locationManager.Locations.Count; i++)
        {
            LocationData location = locationManager.Locations[i];

            GameObject thumbnail = Instantiate(thumbnailPrefab, contentRect);
            RectTransform thumbnailRect = thumbnail.GetComponent<RectTransform>();

            // 配置（左から順に）
            thumbnailRect.anchorMin = new Vector2(0, 0.5f);
            thumbnailRect.anchorMax = new Vector2(0, 0.5f);
            thumbnailRect.pivot = new Vector2(0, 0.5f);
            thumbnailRect.anchoredPosition = new Vector2(i * (thumbnailWidth + thumbnailSpacing), 0);
            thumbnailRect.sizeDelta = new Vector2(thumbnailWidth, thumbnailHeight);

            // 画像設定
            Image thumbnailImage = thumbnail.GetComponent<Image>();
            thumbnailImage.sprite = location.thumbnailImage;

            // 選択枠設定（初めは全て非表示）
            Transform selectionFrame = thumbnail.transform.Find("SelectionFrame");
            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(false);
            }

            // インタラクション設定
            XRSimpleInteractable interactable = thumbnail.AddComponent<XRSimpleInteractable>();
            int index = i;
            interactable.selectEntered.AddListener((args) => {
                locationManager.CurrentLocationIndex = index;
            });

            // ドラッグ機能のセットアップ
            interactable.hoverEntered.AddListener((args) => {
                if (isDragging && args.interactorObject is XRRayInteractor)
                {
                    // インタラクターのTransformを保存
                    dragControllerTransform = args.interactorObject.transform;
                }
            });

            thumbnails.Add(thumbnail);
        }
    }

    private void HandleDragScroll()
    {
        // 最新の入力システムを使用したコントローラー入力の検出
        var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None);

        foreach (var rayInteractor in rayInteractors)
        {
            bool currentPressState = false;

            // 選択アクションを検出する
            var interactor = rayInteractor.GetComponent<XRBaseInteractor>();
            if (interactor != null)
            {
                // 選択状態かどうかを確認
                currentPressState = interactor.isSelectActive;

                // トリガーの状態変化を検出
                bool pressed = currentPressState && !lastPressState;
                bool released = !currentPressState && lastPressState;

                // トリガーが押された時の処理
                if (pressed && !isDragging)
                {
                    isDragging = true;
                    dragControllerTransform = rayInteractor.transform;
                    dragStartPosition = GetWorldToCanvasPosition(dragControllerTransform.position);
                }
                // トリガーが離された時の処理
                else if (released && isDragging && dragControllerTransform == rayInteractor.transform)
                {
                    isDragging = false;
                    dragControllerTransform = null;
                }

                lastPressState = currentPressState;
            }
        }

        // ドラッグ中の処理
        if (isDragging && dragControllerTransform != null)
        {
            Vector2 currentPosition = GetWorldToCanvasPosition(dragControllerTransform.position);
            Vector2 dragDelta = currentPosition - dragStartPosition;

            // スクロール処理
            Vector2 anchoredPosition = contentRect.anchoredPosition;
            anchoredPosition.x += dragDelta.x;

            // スクロール範囲の制限
            float minX = 0;
            float maxX = Mathf.Max(0, contentRect.sizeDelta.x - ((RectTransform)contentRect.parent).rect.width);
            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, -maxX, -minX);

            contentRect.anchoredPosition = anchoredPosition;
            dragStartPosition = currentPosition;
        }
    }

    private Vector2 GetWorldToCanvasPosition(Vector3 worldPosition)
    {
        // ワールド座標からキャンバス上の座標に変換
        Vector2 viewportPosition = Camera.main.WorldToViewportPoint(worldPosition);
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        return new Vector2(
            (viewportPosition.x * canvasRect.sizeDelta.x) - (canvasRect.sizeDelta.x * 0.5f),
            (viewportPosition.y * canvasRect.sizeDelta.y) - (canvasRect.sizeDelta.y * 0.5f)
        );
    }

    private void UpdateSelectedThumbnail()
    {
        // 全てのサムネイルの選択状態をリセット
        for (int i = 0; i < thumbnails.Count; i++)
        {
            Transform selectionFrame = thumbnails[i].transform.Find("SelectionFrame");
            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(i == locationManager.CurrentLocationIndex);
            }
        }
    }

    private void OnLocationChanged(int newLocationIndex)
    {
        UpdateSelectedThumbnail();
    }

    private void OnDestroy()
    {
        if (locationManager != null)
        {
            locationManager.OnLocationChanged -= OnLocationChanged;
        }
    }
}