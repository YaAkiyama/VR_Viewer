using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class VRLaserPointer : MonoBehaviour
{
    [Header("���[�U�[�ݒ�")]
    [SerializeField] private float maxRayDistance = 100f;   // ���[�U�[�̍ő匟�o����
    [SerializeField] private float maxVisualDistance = 5f;  // ���o�I�ȃ��[�U�[�̒���
    [SerializeField] private float rayWidth = 0.01f;        // ���[�U�[�̕�
    [SerializeField] private Color rayColor = new Color(0.0f, 0.5f, 1.0f, 0.5f); // ���[�U�[�̐F

    [Header("�|�C���^�[�h�b�g�ݒ�")]
    [SerializeField] private float dotScale = 0.02f;        // �|�C���^�[�h�b�g�̃X�P�[��
    [SerializeField] private Color dotColor = Color.white;  // �ʏ펞�̃h�b�g�̐F
    [SerializeField] private Color dotPressedColor = Color.red; // �������̃h�b�g�̐F

    [Header("���͐ݒ�")]
    [SerializeField] private InputActionReference triggerAction; // �g���K�[�{�^��
    [SerializeField] private InputActionReference aButtonAction; // A�{�^��

    [Header("UI�ݒ�")]
    [SerializeField] private GraphicRaycaster uiRaycaster; // UI���C�L���X�^�[
    [SerializeField] private EventSystem eventSystem;      // �C�x���g�V�X�e��
    [SerializeField] private Canvas[] targetCanvasList;    // �Ώۂ�Canvas�ꗗ

    [Header("�p�l�������R���g���[���[")]
    [SerializeField] private PanelVisibilityController panelVisibilityController;

    // ���[�U�[�p�̃R���|�[�l���g
    private LineRenderer lineRenderer;
    private GameObject pointerDot;
    private Transform hitPoint;
    private Renderer dotRenderer;

    // UI�v�f�Ƃ̑��ݍ�p�p
    private PointerEventData pointerData;
    private PointerEventData cachedPointerData;
    private GameObject currentTarget;
    private GameObject lastTarget;

    // ��ԊǗ�
    private bool triggerPressed = false;
    private bool isDragging = false;
    private List<Canvas> visibleCanvasList = new List<Canvas>();

    // �h���b�O�֘A�̕ϐ�
    private Vector2 dragStartPosition;
    private GameObject draggedObject;
    private bool dragThresholdMet = false;
    private const float dragThreshold = 5f; // �h���b�O�J�n���邽�߂̍ŏ��ړ�����

    void Start()
    {
        try
        {
            Debug.Log("[LaserPointer] Start initializing...");

            // PanelVisibilityController��T���i����SerializeField�Őݒ肳��Ă��Ȃ���΁j
            if (panelVisibilityController == null)
            {
                panelVisibilityController = FindFirstObjectByType<PanelVisibilityController>();
                if (panelVisibilityController == null)
                {
                    Debug.LogWarning("[LaserPointer] PanelVisibilityController��������܂���I");
                }
                else
                {
                    Debug.Log("[LaserPointer] PanelVisibilityController���������o");
                }
            }

            // EventSystem��T���i����SerializeField�Őݒ肳��Ă��Ȃ���΁j
            if (eventSystem == null)
            {
                eventSystem = FindFirstObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    Debug.LogWarning("[LaserPointer] EventSystem��������܂���I");
                }
                else
                {
                    Debug.Log("[LaserPointer] EventSystem���������o");
                }
            }

            // UIRaycaster�̃`�F�b�N
            if (uiRaycaster == null)
            {
                Debug.LogWarning("[LaserPointer] uiRaycaster���ݒ肳��Ă��܂���");

                // �V�[������GraphicRaycaster���������o
                GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
                if (raycasters.Length > 0)
                {
                    uiRaycaster = raycasters[0];
                    Debug.Log($"[LaserPointer] GraphicRaycaster���������o: {uiRaycaster.gameObject.name}");
                }
            }

            // ���[�U�[�p��LineRenderer��ݒ�
            InitializeLineRenderer();

            // �|�C���^�[�h�b�g�𐶐�
            InitializeHitPoint();

            // PointerEventData�̏�����
            if (eventSystem != null)
            {
                pointerData = new PointerEventData(eventSystem);
                cachedPointerData = new PointerEventData(eventSystem);
                Debug.Log("[LaserPointer] PointerEventData��������");
            }
            else
            {
                Debug.LogError("[LaserPointer] EventSystem��������Ȃ����߁APointerEventData���������ł��܂���");
            }

            // ���̓A�N�V������L����
            if (triggerAction != null)
            {
                triggerAction.action.Enable();
                Debug.Log("[LaserPointer] �g���K�[�A�N�V�����L����: " + triggerAction.action.name);
            }
            else
            {
                Debug.LogError("[LaserPointer] triggerAction���ݒ肳��Ă��܂���");
            }

            if (aButtonAction != null)
            {
                // �����̃C�x���g���N���A���čēo�^
                try
                {
                    aButtonAction.action.performed -= OnAButtonPressed;
                }
                catch (System.Exception)
                {
                    // ���o�^�̏ꍇ�A��O����������\��������̂Ŗ���
                }

                aButtonAction.action.Enable();
                aButtonAction.action.performed += OnAButtonPressed;
                Debug.Log("[LaserPointer] A�{�^���A�N�V�����L����: " + aButtonAction.action.name);

                // �A�N�V�����̏�Ԃ��m�F
                InputAction action = aButtonAction.action;
                Debug.Log($"[LaserPointer] A Button Action: Enabled={action.enabled}, Phase={action.phase}, BindingCount={action.bindings.Count}");

                // �o�C���f�B���O���f�o�b�O�o��
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    Debug.Log($"[LaserPointer] Binding {i}: Path={action.bindings[i].path}, Action={action.bindings[i].action}");
                }
            }
            else
            {
                Debug.LogError("[LaserPointer] aButtonAction���ݒ肳��Ă��܂���");
            }

            // CanvasList��������
            if (targetCanvasList != null && targetCanvasList.Length > 0)
            {
                Debug.Log($"[LaserPointer] �Ώ�Canvas��: {targetCanvasList.Length}");
                foreach (var canvas in targetCanvasList)
                {
                    if (canvas != null)
                    {
                        Debug.Log($"[LaserPointer] Canvas: {canvas.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[LaserPointer] Null canvas in targetCanvasList");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[LaserPointer] targetCanvasList���ݒ肳��Ă��Ȃ�����ł�");
            }

            UpdateVisibleCanvasList();

            Debug.Log("[LaserPointer] ����������");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] �������G���[: {e.Message}\n{e.StackTrace}");
        }
    }

    void OnDestroy()
    {
        try
        {
            // A�{�^���̃C�x���g������
            if (aButtonAction != null)
            {
                aButtonAction.action.performed -= OnAButtonPressed;
                Debug.Log("[LaserPointer] A�{�^���C�x���g������");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] OnDestroy �G���[: {e.Message}");
        }
    }

    private void InitializeLineRenderer()
    {
        try
        {
            // LineRenderer���Ȃ���Βǉ�
            lineRenderer = gameObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                Debug.Log("[LaserPointer] LineRenderer added to game object");
            }

            // LineRenderer�̐ݒ���N���A��
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = rayWidth;
            lineRenderer.endWidth = rayWidth * 0.5f; // ��ׂ�ɂ���

            // �F�𖾊m�ɐݒ�
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.0f); // �I�_�͓�����

            // �}�e���A����K�؂ɐݒ�
            Material laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (laserMaterial == null)
            {
                Debug.LogWarning("[LaserPointer] URP�V�F�[�_�[��������܂���B��փV�F�[�_�[�������܂��B");
                laserMaterial = new Material(Shader.Find("Unlit/Color"));
                if (laserMaterial == null)
                {
                    Debug.LogWarning("[LaserPointer] ��փV�F�[�_�[��������܂���B�V�����}�e���A�����쐬���܂��B");
                    laserMaterial = new Material(Shader.Find("Standard"));
                }
            }

            if (laserMaterial != null)
            {
                laserMaterial.color = rayColor;
                lineRenderer.material = laserMaterial;
                Debug.Log($"[LaserPointer] ���[�U�[�}�e���A���쐬: Color={rayColor}");
            }
            else
            {
                Debug.LogError("[LaserPointer] ���[�U�[�}�e���A�����쐬�ł��܂���ł���");
            }

            lineRenderer.positionCount = 2;

            // �����ʒu��ݒ�
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + transform.forward * maxVisualDistance;

            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            Debug.Log($"[LaserPointer] LineRenderer����������: �J�n�ʒu={startPos}, �I���ʒu={endPos}, Forward={transform.forward}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] LineRenderer�������G���[: {e.Message}\n{e.StackTrace}");
        }
    }

    private void InitializeHitPoint()
    {
        try
        {
            // �|�C���^�[�h�b�g���쐬
            pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            if (pointerDot == null)
            {
                Debug.LogError("[LaserPointer] Failed to create pointer dot");
                return;
            }

            pointerDot.name = "LaserDot";
            // ���[���h��Ԃɔz�u���A�e�q�֌W��ݒ肵�Ȃ�
            pointerDot.transform.SetParent(null);
            pointerDot.transform.localScale = Vector3.one * dotScale;

            // �Փ˔���͕s�v�Ȃ̂ō폜
            Collider dotCollider = pointerDot.GetComponent<Collider>();
            if (dotCollider != null)
            {
                Destroy(dotCollider);
            }

            // �}�e���A����ݒ�
            dotRenderer = pointerDot.GetComponent<Renderer>();
            if (dotRenderer != null)
            {
                Material dotMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (dotMaterial == null)
                {
                    Debug.LogWarning("[LaserPointer] URP�V�F�[�_�[��������܂���B��փV�F�[�_�[�������܂��B");
                    dotMaterial = new Material(Shader.Find("Standard"));
                }

                if (dotMaterial != null)
                {
                    dotMaterial.color = dotColor;
                    dotRenderer.material = dotMaterial;
                    Debug.Log($"[LaserPointer] �h�b�g�}�e���A���쐬: Color={dotColor}");
                }
                else
                {
                    Debug.LogError("[LaserPointer] �h�b�g�}�e���A�����쐬�ł��܂���ł���");
                }
            }
            else
            {
                Debug.LogError("[LaserPointer] Dot renderer is null");
            }

            hitPoint = pointerDot.transform;
            pointerDot.SetActive(false);

            Debug.Log("[LaserPointer] �|�C���^�[�h�b�g����������");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] �|�C���^�[�h�b�g�������G���[: {e.Message}\n{e.StackTrace}");
        }
    }

    // �\������Canvas���X�g���X�V
    private void UpdateVisibleCanvasList()
    {
        try
        {
            visibleCanvasList.Clear();

            if (targetCanvasList == null || targetCanvasList.Length == 0)
            {
                Debug.LogWarning("[LaserPointer] targetCanvasList���ݒ肳��Ă��Ȃ�����ł�");
                return;
            }

            foreach (Canvas canvas in targetCanvasList)
            {
                if (canvas == null) continue;

                // PanelVisibilityController�Ő��䂳��Ă���p�l�����ǂ����m�F
                CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    if (canvasGroup.alpha > 0f && canvasGroup.interactable)
                    {
                        visibleCanvasList.Add(canvas);
                        Debug.Log($"[LaserPointer] �\��Canvas�ɒǉ�: {canvas.name}, Alpha={canvasGroup.alpha}, Interactable={canvasGroup.interactable}");
                    }
                    else
                    {
                        Debug.Log($"[LaserPointer] ��\��Canvas: {canvas.name}, Alpha={canvasGroup.alpha}, Interactable={canvasGroup.interactable}");
                    }
                }
                else
                {
                    // CanvasGroup���Ȃ���΂��̂܂ܒǉ�
                    visibleCanvasList.Add(canvas);
                    Debug.Log($"[LaserPointer] CanvasGroup�Ȃ�Canvas�ǉ�: {canvas.name}");
                }
            }

            Debug.Log($"[LaserPointer] �\��Canvas��: {visibleCanvasList.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateVisibleCanvasList �G���[: {e.Message}");
        }
    }

    void Update()
    {
        try
        {
            // �g���K�[���͂��m�F
            CheckTriggerInput();

            // ���[�U�[���X�V
            UpdateLaser();

            // �h���b�O�����̍X�V
            UpdateDrag();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] Update �G���[: {e.Message}");
        }
    }

    private void CheckTriggerInput()
    {
        try
        {
            if (triggerAction != null)
            {
                bool isTriggerPressed = triggerAction.action.IsPressed();

                // �����ꂽ�u��
                if (isTriggerPressed && !triggerPressed)
                {
                    TriggerDown();
                }
                // �����ꂽ�u��
                else if (!isTriggerPressed && triggerPressed)
                {
                    TriggerUp();
                }

                triggerPressed = isTriggerPressed;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] CheckTriggerInput �G���[: {e.Message}");
        }
    }

    private void UpdateLaser()
    {
        try
        {
            if (lineRenderer == null)
            {
                Debug.LogError("[LaserPointer] LineRenderer is null");
                return;
            }

            // �R���g���[���[�̈ʒu�ƕ������擾
            Vector3 startPos = transform.position;
            Vector3 direction = transform.forward;

            // �f�o�b�O - 10�t���[�����Ƃɏ����o��
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[LaserPointer] �R���g���[���[: Position={startPos}, Forward={direction}, Rotation={transform.rotation.eulerAngles}");
            }

            // ���[�U�[�̊J�n�ʒu��ݒ�
            lineRenderer.SetPosition(0, startPos);

            // �O�t���[���̃^�[�Q�b�g��ۑ�
            lastTarget = currentTarget;
            currentTarget = null;

            // ���C�̍ő勗����������
            float hitDistance = maxRayDistance;

            // �\������Canvas���X�g���X�V
            UpdateVisibleCanvasList();

            // �����I�u�W�F�N�g�Ƃ̌�������
            bool hitPhysics = false;
            RaycastHit physicsHit = new RaycastHit();

            try
            {
                hitPhysics = Physics.Raycast(startPos, direction, out physicsHit, maxRayDistance);
                if (hitPhysics && physicsHit.distance < hitDistance)
                {
                    hitDistance = physicsHit.distance;
                    Debug.Log($"[LaserPointer] �����I�u�W�F�N�g�q�b�g: {physicsHit.collider.name}, ����={physicsHit.distance}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LaserPointer] Physics Raycast error: {e.Message}");
            }

            // UI�v�f�Ƃ̌�������
            bool hitUI = false;
            if (visibleCanvasList.Count > 0)
            {
                // UI Raycast�̃f�o�b�O���
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[LaserPointer] UI Raycast: Canvas Count={visibleCanvasList.Count}");
                }

                // �eCanvas�ɑ΂��Č���������s��
                foreach (Canvas canvas in visibleCanvasList)
                {
                    if (canvas == null) continue;

                    try
                    {
                        if (CheckUIRaycast(startPos, direction, canvas, ref hitDistance))
                        {
                            hitUI = true;
                            Debug.Log($"[LaserPointer] UI�q�b�g: Canvas={canvas.name}, Target={currentTarget?.name}");
                            break;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[LaserPointer] UI Raycast error on {canvas.name}: {e.Message}");
                    }
                }
            }

            // UI�v�f����O�ꂽ�ꍇ
            if (lastTarget != null && currentTarget == null && !isDragging)
            {
                HandlePointerExit(lastTarget);
            }

            // ���[�U�[�̏I�_�ʒu�ƒ�����ݒ�
            float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
            Vector3 endPos = startPos + direction * visualDistance;
            lineRenderer.SetPosition(1, endPos);

            // �|�C���^�[�h�b�g�̏���
            if (pointerDot != null)
            {
                if ((hitUI || (hitPhysics && physicsHit.distance < maxRayDistance)) && !isDragging)
                {
                    Vector3 hitPos = hitUI ? endPos : physicsHit.point;

                    if (hitPoint != null)
                    {
                        hitPoint.position = hitPos;
                        pointerDot.SetActive(true);

                        if (dotRenderer != null)
                        {
                            dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;
                        }

                        if (Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"[LaserPointer] �h�b�g�\��: Position={hitPos}, Active={pointerDot.activeSelf}");
                        }
                    }
                }
                else
                {
                    pointerDot.SetActive(false);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateLaser error: {e.Message}\n{e.StackTrace}");
        }
    }

    // �h���b�O�����̍X�V
    private void UpdateDrag()
    {
        try
        {
            if (triggerPressed && draggedObject != null)
            {
                // �h���b�O�����̌v�Z
                float dragDistance = Vector2.Distance(dragStartPosition, pointerData.position);

                // �h���b�O��臒l�𒴂�����h���b�O�Ɣ���
                if (!dragThresholdMet && dragDistance > dragThreshold)
                {
                    dragThresholdMet = true;
                    isDragging = true;

                    Debug.Log($"[LaserPointer] �h���b�O�J�n: �I�u�W�F�N�g={draggedObject.name}, ����={dragDistance}");

                    // �h���b�O�J�n�C�x���g�𔭍s
                    cachedPointerData.pointerDrag = draggedObject;
                    cachedPointerData.dragging = true;
                    ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.beginDragHandler);
                }

                // �h���b�O���̏���
                if (isDragging)
                {
                    // �h���b�O�C�x���g�𔭍s
                    cachedPointerData.position = pointerData.position;
                    cachedPointerData.delta = pointerData.position - dragStartPosition;
                    ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.dragHandler);

                    // �X�N���[���o�[��֘AUI�R���|�[�l���g�ɑ΂�����ʏ���
                    ScrollRect scrollRect = draggedObject.GetComponent<ScrollRect>();
                    if (scrollRect == null && draggedObject.transform.parent != null)
                    {
                        scrollRect = draggedObject.transform.parent.GetComponent<ScrollRect>();
                    }

                    if (scrollRect != null)
                    {
                        // �X�N���[�������s
                        scrollRect.OnDrag(cachedPointerData);

                        if (Time.frameCount % 30 == 0)
                        {
                            Debug.Log($"[LaserPointer] �X�N���[����: Delta={cachedPointerData.delta}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] UpdateDrag �G���[: {e.Message}");
        }
    }

    // �����Canvas�Ƃ̌�������
    private bool CheckUIRaycast(Vector3 startPos, Vector3 direction, Canvas canvas, ref float hitDistance)
    {
        // NULL�`�F�b�N�������Ɏ��{
        if (canvas == null)
        {
            Debug.LogWarning("[LaserPointer] canvas is null");
            return false;
        }

        if (uiRaycaster == null)
        {
            Debug.LogWarning("[LaserPointer] uiRaycaster is null");
            return false;
        }

        if (eventSystem == null)
        {
            Debug.LogWarning("[LaserPointer] eventSystem is null");
            return false;
        }

        try
        {
            // ���C�ƃL�����o�X�̌�_���v�Z
            Ray ray = new Ray(startPos, direction);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            if (canvasRect == null)
            {
                Debug.LogWarning($"[LaserPointer] {canvas.name} ��RectTransform��null�ł�");
                return false;
            }

            // �L�����o�X�̕��ʂ��v�Z�iNaN�l�`�F�b�N��ǉ��j
            Vector3 canvasNormal = -canvasRect.forward;
            if (float.IsNaN(canvasNormal.x) || float.IsNaN(canvasNormal.y) || float.IsNaN(canvasNormal.z))
            {
                Debug.LogError($"[LaserPointer] Canvas {canvas.name} normal has NaN values: {canvasNormal}");
                return false;
            }

            Plane canvasPlane = new Plane(canvasNormal, canvasRect.position);
            float rayDistance;

            if (canvasPlane.Raycast(ray, out rayDistance) && rayDistance < hitDistance)
            {
                Vector3 worldPos = startPos + direction * rayDistance;

                // ���E���W����X�N���[�����W��
                Camera canvasCamera = canvas.worldCamera;
                if (canvasCamera == null)
                {
                    canvasCamera = Camera.main;
                    if (canvasCamera == null)
                    {
                        Debug.LogError("[LaserPointer] No camera found for UI raycasting");
                        return false;
                    }
                }

                // WorldToScreenPoint�ɖ�肪����ꍇ�ɑΏ�
                Vector2 screenPoint;
                try
                {
                    screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPos);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LaserPointer] WorldToScreenPoint failed: {e.Message}");
                    return false;
                }

                // UI���C�L���X�g�p�̃f�[�^���X�V
                if (pointerData == null)
                {
                    Debug.LogError("[LaserPointer] pointerData is null");
                    return false;
                }

                pointerData.position = screenPoint;
                pointerData.delta = Vector2.zero;
                pointerData.scrollDelta = Vector2.zero;
                pointerData.dragging = false;
                pointerData.useDragThreshold = true;
                pointerData.pointerId = 0;

                // UI���C�L���X�g�����s
                List<RaycastResult> results = new List<RaycastResult>();
                try
                {
                    uiRaycaster.Raycast(pointerData, results);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LaserPointer] UI Raycast failed: {e.Message}");
                    return false;
                }

                if (results.Count > 0)
                {
                    // UI�q�b�g����
                    RaycastResult topResult = results[0];
                    GameObject targetObject = topResult.gameObject;

                    if (targetObject == null)
                    {
                        Debug.LogWarning("[LaserPointer] �q�b�g����GameObject��null�ł�");
                        return false;
                    }

                    // �{�^���̎q�I�u�W�F�N�g�i�e�L�X�g�Ȃǁj���q�b�g�����ꍇ�A�e�̃{�^����T��
                    Selectable parentSelectable = null;
                    if (targetObject.GetComponent<Selectable>() == null)
                    {
                        // �e�K�w�����ǂ���Selectable���I�u�W�F�N�g��T��
                        Transform parent = targetObject.transform.parent;
                        while (parent != null)
                        {
                            parentSelectable = parent.GetComponent<Selectable>();
                            if (parentSelectable != null)
                            {
                                targetObject = parent.gameObject;
                                break;
                            }
                            parent = parent.parent;
                        }
                    }

                    hitDistance = rayDistance;

                    // ���[�U�[�̏I�_�ʒu��ݒ�
                    float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
                    Vector3 visualEndPoint = startPos + direction * visualDistance;

                    // ���[�U�[�̕`����X�V
                    if (lineRenderer != null)
                    {
                        lineRenderer.SetPosition(1, visualEndPoint);
                    }

                    // �q�b�g�ʒu�Ƀh�b�g��\��
                    if (hitPoint != null && pointerDot != null)
                    {
                        hitPoint.position = worldPos;
                        pointerDot.SetActive(!isDragging); // �h���b�O���̓h�b�g���\��

                        // �g���K�[��������Ă���ꍇ�̓h�b�g�̐F��ύX
                        if (dotRenderer != null)
                        {
                            dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;
                        }
                    }

                    // UI�^�[�Q�b�g��ۑ�
                    currentTarget = targetObject;

                    // �ʏ�̃|�C���^Enter/Exit����
                    HandlePointerEnterExit(currentTarget, lastTarget);

                    return true; // UI�Ƃ̌�������
                }
            }

            return false; // UI�Ƃ̌����Ȃ�
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] CheckUIRaycast �G���[: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    private void HandlePointerEnterExit(GameObject current, GameObject last)
    {
        try
        {
            // �O�̃^�[�Q�b�g����Exit
            if (last != null && last != current)
            {
                HandlePointerExit(last);
            }

            // �V�����^�[�Q�b�g��Enter
            if (current != null && current != last)
            {
                HandlePointerEnter(current);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerEnterExit �G���[: {e.Message}");
        }
    }

    private void HandlePointerEnter(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerEnter: go��null�ł�");
                return;
            }

            Debug.Log($"[LaserPointer] �|�C���^�[Enter: {go.name}");

            // PointerEnter�C�x���g�𔭐M
            cachedPointerData.pointerEnter = go;
            cachedPointerData.position = pointerData.position;

            // Selectable�R���|�[�l���g�ɑ΂��ăn�C���C�g����
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerEnter(cachedPointerData);

                // �{�^���ɒ����I�ȃt�B�[�h�o�b�N��K�p
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    // �F�̕ύX�������I�ɓK�p
                    Color targetColor = colors.highlightedColor;
                    image.color = targetColor;
                    image.CrossFadeColor(targetColor, colors.fadeDuration, true, true);
                }
            }

            // �C�x���g�𔭐M
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerEnterHandler);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerEnter �G���[: {e.Message}");
        }
    }

    private void HandlePointerExit(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerExit: go��null�ł�");
                return;
            }

            Debug.Log($"[LaserPointer] �|�C���^�[Exit: {go.name}");

            // PointerExit�C�x���g�𔭐M
            cachedPointerData.pointerEnter = null;
            cachedPointerData.position = pointerData.position;

            // Selectable�R���|�[�l���g�ɑ΂��Ẵn�C���C�g����
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerExit(cachedPointerData);

                // �{�^���̃t�B�[�h�o�b�N�����ɖ߂�
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.CrossFadeColor(colors.normalColor, colors.fadeDuration, true, true);
                }
            }

            // �C�x���g�𔭐M
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerExitHandler);

            // �g���K�[��������Ă����ꍇ�͉���
            if (triggerPressed)
            {
                HandlePointerUp(go);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerExit �G���[: {e.Message}");
        }
    }

    private void HandlePointerDown(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerDown: go��null�ł�");
                return;
            }

            Debug.Log($"[LaserPointer] �|�C���^�[Down: {go.name}");

            cachedPointerData.pointerPress = go;
            cachedPointerData.pressPosition = pointerData.position;
            cachedPointerData.pointerPressRaycast = pointerData.pointerPressRaycast;

            // Selectable�R���|�[�l���g�ɑ΂��Ẳ�������
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerDown(cachedPointerData);

                // �{�^���ɒ����I�ȃt�B�[�h�o�b�N��K�p
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.CrossFadeColor(colors.pressedColor, colors.fadeDuration, true, true);
                }
            }

            // �C�x���g�𔭐M
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerDownHandler);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerDown �G���[: {e.Message}");
        }
    }

    private void HandlePointerUp(GameObject go)
    {
        try
        {
            if (go == null)
            {
                Debug.LogWarning("[LaserPointer] HandlePointerUp: go��null�ł�");
                return;
            }

            Debug.Log($"[LaserPointer] �|�C���^�[Up: {go.name}");

            // PointerUp�C�x���g�𔭐M
            cachedPointerData.position = pointerData.position;

            // Selectable�R���|�[�l���g�̏���
            Selectable selectable = go.GetComponent<Selectable>();
            if (selectable != null)
            {
                selectable.OnPointerUp(cachedPointerData);

                // �{�^���̃t�B�[�h�o�b�N����
                ColorBlock colors = selectable.colors;
                Image image = go.GetComponent<Image>();
                if (image != null)
                {
                    image.CrossFadeColor(colors.highlightedColor, colors.fadeDuration, true, true);
                }
            }

            // �C�x���g�𔭐M
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerUpHandler);

            // �N���b�N�C�x���g�𔭐M
            if (cachedPointerData.pointerPress == go)
            {
                Debug.Log($"[LaserPointer] �N���b�N: {go.name}");
                ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerClickHandler);
            }

            cachedPointerData.pointerPress = null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] HandlePointerUp �G���[: {e.Message}");
        }
    }

    private void TriggerDown()
    {
        try
        {
            // �f�o�b�O���
            Debug.Log("[LaserPointer] �g���K�[����: �^�[�Q�b�g = " + (currentTarget != null ? currentTarget.name : "�Ȃ�"));

            // �h�b�g�F��ύX
            if (pointerDot != null && pointerDot.activeSelf && dotRenderer != null)
            {
                dotRenderer.material.color = dotPressedColor;
            }

            if (currentTarget != null)
            {
                // �h���b�O�����̊J�n����
                dragStartPosition = pointerData.position;
                draggedObject = currentTarget;
                dragThresholdMet = false;

                // �|�C���^�_�E���̏���
                HandlePointerDown(currentTarget);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] TriggerDown �G���[: {e.Message}");
        }
    }

    private void TriggerUp()
    {
        try
        {
            // �h�b�g�F�����ɖ߂�
            if (pointerDot != null && pointerDot.activeSelf && dotRenderer != null)
            {
                dotRenderer.material.color = dotColor;
            }

            // �h���b�O�I������
            if (isDragging && draggedObject != null)
            {
                Debug.Log($"[LaserPointer] �h���b�O�I��: �I�u�W�F�N�g={draggedObject.name}");

                // �h���b�O�I������
                cachedPointerData.position = pointerData.position;
                cachedPointerData.dragging = false;

                ExecuteEvents.Execute(draggedObject, cachedPointerData, ExecuteEvents.endDragHandler);

                // �h���b�v�C�x���g�̏���
                if (currentTarget != null)
                {
                    ExecuteEvents.Execute(currentTarget, cachedPointerData, ExecuteEvents.dropHandler);
                }

                isDragging = false;
                draggedObject = null;
                dragThresholdMet = false;

                // �h���b�O�I�����A���݂̃^�[�Q�b�g������΃h�b�g��\��
                if (currentTarget != null && pointerDot != null)
                {
                    pointerDot.SetActive(true);
                }
            }

            if (currentTarget != null)
            {
                // �|�C���^�A�b�v�̏���
                HandlePointerUp(currentTarget);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] TriggerUp �G���[: {e.Message}");
        }
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        try
        {
            // �f�o�b�O���O���m���ɕ\��
            Debug.LogError("[LaserPointer] A�{�^����������܂����I"); // �G���[���x���ŏo�͂��Ċm���ɕ\��

            if (panelVisibilityController == null)
            {
                Debug.LogError("[LaserPointer] PanelVisibilityController is null");

                // �Ď擾�����݂�
                panelVisibilityController = FindFirstObjectByType<PanelVisibilityController>();
                if (panelVisibilityController == null)
                {
                    Debug.LogError("[LaserPointer] PanelVisibilityController��������܂���");
                    return;
                }
            }

            // ���݂̏�Ԃ����O�ɋL�^
            bool isInViewRange = panelVisibilityController.IsInViewRange();
            bool isForcedState = panelVisibilityController.IsForcedState();
            bool isForcedVisible = panelVisibilityController.IsForcedVisible();

            Debug.Log($"[LaserPointer] A�{�^���������̏��: ����p��={isInViewRange}, �������={isForcedState}, �����\��={isForcedVisible}");

            // �����I�Ƀp�l���̕\��/��\����؂�ւ���
            panelVisibilityController.ToggleForcedVisibility();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LaserPointer] A�{�^���������ɃG���[: {e.Message}\n{e.StackTrace}");
        }
    }
}