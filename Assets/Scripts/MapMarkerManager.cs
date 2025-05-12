using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.XR.CoreUtils;

public class MapMarkerManager : MonoBehaviour
{
    [Header("�}�[�J�[�ݒ�")]
    [SerializeField] private GameObject markerPrefab;            // �}�[�J�[�v���n�u
    [SerializeField] private GameObject currentPositionPrefab;   // ���ݒn�}�[�J�[�v���n�u
    [SerializeField] private Transform markerContainer;          // �}�[�J�[��z�u����e�I�u�W�F�N�g

    [Header("�}�b�v�ݒ�")]
    [SerializeField] private RectTransform mapRect;              // �n�}��RectTransform

    [Header("�p�m���}�A�g")]
    [SerializeField] private Panorama360Controller panoramaController; // �p�m���}�R���g���[���[

    [Header("���ݒn�}�[�J�[�ݒ�")]
    [SerializeField] private bool rotateCurrentMarker = true;   // ���ݒn�}�[�J�[����]�����邩�ǂ���
    [SerializeField] private float markerRotationOffset = 0f;   // �}�[�J�[��]�̒����l�i�x�j
    [SerializeField] private bool smoothRotation = true;        // ��]���X���[�Y�ɍs�����ǂ���
    [SerializeField] private float rotationSmoothSpeed = 5f;    // ��]�̃X���[�Y�x
    [SerializeField] private bool hideOriginalMarker = true;   // �I�𒆂̒ʏ�}�[�J�[���\���ɂ��邩
    [SerializeField] private bool invertRotation = true;       // ��]�����𔽓]�����邩�ǂ���

    // �ÓI�C�x���g�i���̃R���|�[�l���g����̎Q�Ɨp�j
    public delegate void MarkerSelectedHandler(int markerId);
    public static event MarkerSelectedHandler OnMarkerSelected;

    // �}�[�J�[�f�[�^���X�g
    [SerializeField] private List<MapMarker> markerData = new List<MapMarker>();

    // �����^�C���f�[�^
    private Dictionary<int, GameObject> markerObjects = new Dictionary<int, GameObject>();
    private GameObject currentPositionMarker;
    private int currentMarkerIndex = -1;
    private Transform cameraTransform;
    private float targetRotation = 0f;

    void Start()
    {
        // �p�m���}�R���g���[���[�ւ̎Q�Ɗm�F
        if (panoramaController == null)
        {
            panoramaController = FindFirstObjectByType<Panorama360Controller>();
            if (panoramaController == null)
            {
                Debug.LogError("Panorama360Controller��������܂���");
                return;
            }
        }

        // �J�����ւ̎Q�Ƃ��擾
        var xrOrigin = FindFirstObjectByType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            cameraTransform = xrOrigin.Camera.transform;
        }
        else
        {
            cameraTransform = Camera.main.transform;
            if (cameraTransform == null)
            {
                Debug.LogWarning("�J������������܂���B���ݒn�}�[�J�[�̉�]�@�\�͖����ł��B");
            }
        }

        // �}�[�J�[�̏�������
        CreateAllMarkers();

        // �������ݒn�}�[�J�[�̐����i��\���j
        CreateCurrentPositionMarker();

        // Point Number���ŏ��̃}�[�J�[�������\���Ƃ��Đݒ�
        if (markerData.Count > 0)
        {
            // �}�[�J�[�f�[�^��Point Number���Ƀ\�[�g
            markerData = markerData.OrderBy(m => m.pointNumber).ToList();

            // �\�[�g�ς݂Ȃ̂ōŏ��̃}�[�J�[���ŏ�Point Number
            MapMarker firstMarker = markerData[0];
            OnMarkerClicked(firstMarker.pointNumber);
        }
    }

    void Update()
    {
        // �J������Y����]�ɍ��킹�Č��ݒn�}�[�J�[����]
        UpdateCurrentMarkerRotation();
    }

    // ���ݒn�}�[�J�[�̉�]���X�V
    private void UpdateCurrentMarkerRotation()
    {
        if (!rotateCurrentMarker || currentPositionMarker == null || cameraTransform == null) return;

        // �J������Y����]���擾
        float cameraYRotation = cameraTransform.eulerAngles.y;

        // ��]�����𔽓]������ꍇ�͕����𔽓]
        if (invertRotation)
        {
            cameraYRotation = -cameraYRotation;
        }

        // �}�[�J�[�̉�]�ڕW��ݒ�iZ���Ƀ}�b�s���O�j
        targetRotation = cameraYRotation + markerRotationOffset;

        if (smoothRotation)
        {
            // ���݂̉�]���擾
            Vector3 currentRotation = currentPositionMarker.transform.eulerAngles;

            // Z���̉�]���X���[�Y�ɕύX
            float newZRotation = Mathf.LerpAngle(currentRotation.z, targetRotation, Time.deltaTime * rotationSmoothSpeed);

            // �V������]��K�p
            currentPositionMarker.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, newZRotation);
        }
        else
        {
            // ���ډ�]��K�p
            Vector3 currentRotation = currentPositionMarker.transform.eulerAngles;
            currentPositionMarker.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, targetRotation);
        }
    }

    // ���ׂẴ}�[�J�[���쐬
    private void CreateAllMarkers()
    {
        foreach (var marker in markerData)
        {
            CreateMarker(marker);
        }
    }

    // �}�[�J�[��1�쐬
    private void CreateMarker(MapMarker data)
    {
        GameObject markerObj = Instantiate(markerPrefab, markerContainer);
        RectTransform rt = markerObj.GetComponent<RectTransform>();

        // �}�[�J�[�̈ʒu��ݒ�
        rt.anchoredPosition = data.position;

        // �N���b�N�C�x���g�̐ݒ�
        Button button = markerObj.GetComponent<Button>();
        if (button != null)
        {
            int markerId = data.pointNumber; // ���[�J���ϐ��ɃR�s�[
            button.onClick.AddListener(() => OnMarkerClicked(markerId));
        }

        // �}�[�J�[�ԍ���\������e�L�X�g������ΐݒ�
        Text markerText = markerObj.GetComponentInChildren<Text>();
        if (markerText != null)
        {
            markerText.text = data.pointNumber.ToString();
        }

        // ���ݑI�𒆂̃}�[�J�[���ǂ������`�F�b�N���āA�K�v�Ȃ��\����
        if (currentMarkerIndex == data.pointNumber && hideOriginalMarker)
        {
            markerObj.SetActive(false);
        }

        // �}�[�J�[�I�u�W�F�N�g�������ɕۑ�
        markerObjects[data.pointNumber] = markerObj;
    }

    // ���ݒn�}�[�J�[���쐬
    private void CreateCurrentPositionMarker()
    {
        currentPositionMarker = Instantiate(currentPositionPrefab, markerContainer);
        currentPositionMarker.SetActive(false); // ������Ԃ͔�\��
    }

    // �}�[�J�[�N���b�N���̏���
    public void OnMarkerClicked(int markerId)
    {
        Debug.Log($"�}�[�J�[ {markerId} ���N���b�N����܂���");

        // �N���b�N���ꂽ�}�[�J�[�̃f�[�^���擾
        MapMarker clickedMarker = markerData.Find(m => m.pointNumber == markerId);
        if (clickedMarker == null) return;

        // �ȑO�̑I���}�[�J�[��\���ɖ߂�
        if (currentMarkerIndex >= 0 && hideOriginalMarker && markerObjects.ContainsKey(currentMarkerIndex))
        {
            markerObjects[currentMarkerIndex].SetActive(true);
        }

        // ���݂̃}�[�J�[���X�V
        currentMarkerIndex = markerId;

        // �I�������}�[�J�[���\���ɂ���
        if (hideOriginalMarker && markerObjects.ContainsKey(markerId))
        {
            markerObjects[markerId].SetActive(false);
        }

        // ���ݒn�}�[�J�[�̈ʒu���X�V���ĕ\��
        MoveCurrentPositionMarker(clickedMarker.position);

        // �p�m���}�摜��ύX�i�p�X���ݒ肳��Ă���ꍇ�j
        if (!string.IsNullOrEmpty(clickedMarker.panoramaPath) && panoramaController != null)
        {
            // �p�m���}�R���g���[���[�ɉ摜�؂�ւ��v��
            panoramaController.LoadPanoramaByPath(clickedMarker.panoramaPath);
        }

        // �I���C�x���g�𔭍s
        OnMarkerSelected?.Invoke(markerId);
    }

    // ���ݒn�}�[�J�[���ړ�
    private void MoveCurrentPositionMarker(Vector2 position)
    {
        if (currentPositionMarker != null)
        {
            RectTransform rt = currentPositionMarker.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            currentPositionMarker.SetActive(true);

            // �ŐV�̃J������]�𑦍��ɓK�p
            if (rotateCurrentMarker && cameraTransform != null)
            {
                float cameraYRotation = cameraTransform.eulerAngles.y;

                // ��]�����𔽓]������ꍇ�͕����𔽓]
                if (invertRotation)
                {
                    cameraYRotation = -cameraYRotation;
                }

                targetRotation = cameraYRotation + markerRotationOffset;

                if (!smoothRotation)
                {
                    // �X���[�Y��]�������Ȃ瑦���ɓK�p
                    Vector3 currentRotation = currentPositionMarker.transform.eulerAngles;
                    currentPositionMarker.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, targetRotation);
                }
            }
        }
    }

    // ���̃}�[�J�[��I��
    public void SelectNextMarker()
    {
        if (markerData.Count <= 1) return;

        // ���݂̃}�[�J�[�̃C���f�b�N�X���擾
        int currentIndex = markerData.FindIndex(m => m.pointNumber == currentMarkerIndex);
        if (currentIndex < 0) currentIndex = 0;

        // ���̃}�[�J�[�̃C���f�b�N�X���v�Z
        int nextIndex = (currentIndex + 1) % markerData.Count;

        // ���̃}�[�J�[���N���b�N
        OnMarkerClicked(markerData[nextIndex].pointNumber);
    }

    // �O�̃}�[�J�[��I��
    public void SelectPreviousMarker()
    {
        if (markerData.Count <= 1) return;

        // ���݂̃}�[�J�[�̃C���f�b�N�X���擾
        int currentIndex = markerData.FindIndex(m => m.pointNumber == currentMarkerIndex);
        if (currentIndex < 0) currentIndex = 0;

        // �O�̃}�[�J�[�̃C���f�b�N�X���v�Z
        int prevIndex = (currentIndex - 1 + markerData.Count) % markerData.Count;

        // �O�̃}�[�J�[���N���b�N
        OnMarkerClicked(markerData[prevIndex].pointNumber);
    }

    // �O������}�[�J�[�ʒu��ݒ肷�郁�\�b�h
    public void SetMarkerPosition(int markerId, Vector2 newPosition)
    {
        // �}�[�J�[�f�[�^�̍X�V
        int index = markerData.FindIndex(m => m.pointNumber == markerId);
        if (index >= 0)
        {
            markerData[index].position = newPosition;

            // �}�[�J�[�I�u�W�F�N�g�����݂���Έʒu���X�V
            if (markerObjects.ContainsKey(markerId))
            {
                RectTransform rt = markerObjects[markerId].GetComponent<RectTransform>();
                rt.anchoredPosition = newPosition;
            }

            // ���ݑI�𒆂̃}�[�J�[�Ȃ猻�ݒn�}�[�J�[���X�V
            if (currentMarkerIndex == markerId)
            {
                MoveCurrentPositionMarker(newPosition);
            }
        }
    }

    // ���̃}�[�J�[�̕\��/��\���ݒ��؂�ւ�
    public void SetHideOriginalMarker(bool hide)
    {
        // �ݒ肪�ς��Ȃ��ꍇ�͉������Ȃ�
        if (hideOriginalMarker == hide) return;

        // �ݒ���X�V
        hideOriginalMarker = hide;

        // ���ݑI�𒆂̃}�[�J�[������ꍇ�͕\��/��\�����X�V
        if (currentMarkerIndex >= 0 && markerObjects.ContainsKey(currentMarkerIndex))
        {
            markerObjects[currentMarkerIndex].SetActive(!hide);
        }
    }

    // ���ݒn�}�[�J�[�̉�]�ݒ��ύX
    public void SetCurrentMarkerRotation(bool enable, float offset = 0f, bool invert = true)
    {
        rotateCurrentMarker = enable;
        markerRotationOffset = offset;
        invertRotation = invert;

        // �����K�p
        if (currentPositionMarker != null && currentPositionMarker.activeSelf)
        {
            UpdateCurrentMarkerRotation();
        }
    }

    // �}�[�J�[�f�[�^���X�g���擾
    public List<MapMarker> GetMarkerData()
    {
        return markerData;
    }

    // ���ݑI�𒆂̃}�[�J�[�ԍ����擾
    public int GetCurrentMarkerIndex()
    {
        return currentMarkerIndex;
    }

    // ��]�����̔��]�ݒ��ύX
    public void SetInvertRotation(bool invert)
    {
        if (invertRotation != invert)
        {
            invertRotation = invert;

            // �����K�p
            if (currentPositionMarker != null && currentPositionMarker.activeSelf && rotateCurrentMarker)
            {
                UpdateCurrentMarkerRotation();
            }
        }
    }

    // �}�[�J�[�̕\����Ԃ��X�V����i�v���O�����I�ɑ��̏ꏊ����Ăяo���ꍇ�p�j
    public void UpdateMarkerVisibility()
    {
        if (currentMarkerIndex < 0) return;

        foreach (var entry in markerObjects)
        {
            int pointNumber = entry.Key;
            GameObject markerObj = entry.Value;

            if (pointNumber == currentMarkerIndex)
            {
                markerObj.SetActive(!hideOriginalMarker);
            }
            else
            {
                markerObj.SetActive(true);
            }
        }
    }
}