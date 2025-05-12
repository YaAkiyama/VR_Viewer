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

    void Start()
    {
        // PanelVisibilityController��T���i����SerializeField�Őݒ肳��Ă��Ȃ���΁j
        if (panelVisibilityController == null)
        {
            panelVisibilityController = FindFirstObjectByType<PanelVisibilityController>();
            if (panelVisibilityController == null)
            {
                Debug.LogWarning("PanelVisibilityController��������܂���I");
            }
        }

        // EventSystem��T���i����SerializeField�Őݒ肳��Ă��Ȃ���΁j
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("EventSystem��������܂���I");
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
        }
        else
        {
            Debug.LogError("EventSystem��������Ȃ����߁APointerEventData���������ł��܂���");
        }

        // ���̓A�N�V������L����
        if (triggerAction != null) triggerAction.action.Enable();
        if (aButtonAction != null)
        {
            aButtonAction.action.Enable();
            aButtonAction.action.performed += OnAButtonPressed;
        }

        // CanvasList��������
        UpdateVisibleCanvasList();
    }

    void OnDestroy()
    {
        // A�{�^���̃C�x���g������
        if (aButtonAction != null)
        {
            aButtonAction.action.performed -= OnAButtonPressed;
        }
    }

    private void InitializeLineRenderer()
    {
        // LineRenderer���Ȃ���Βǉ�
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // LineRenderer�̐ݒ�
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth * 0.5f; // ��ׂ�ɂ���
        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.0f); // �I�_�͓�����
        lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.positionCount = 2;
    }

    private void InitializeHitPoint()
    {
        // �|�C���^�[�h�b�g���쐬
        pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointerDot.name = "LaserDot";
        pointerDot.transform.SetParent(transform);
        pointerDot.transform.localScale = Vector3.one * dotScale;

        // �Փ˔���͕s�v�Ȃ̂ō폜
        Destroy(pointerDot.GetComponent<Collider>());

        // �}�e���A����ݒ�
        dotRenderer = pointerDot.GetComponent<Renderer>();
        dotRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        dotRenderer.material.color = dotColor;

        hitPoint = pointerDot.transform;
        pointerDot.SetActive(false);
    }

    // �\������Canvas���X�g���X�V
    private void UpdateVisibleCanvasList()
    {
        visibleCanvasList.Clear();

        if (targetCanvasList != null && targetCanvasList.Length > 0)
        {
            foreach (Canvas canvas in targetCanvasList)
            {
                if (canvas != null)
                {
                    // PanelVisibilityController�Ő��䂳��Ă���p�l�����ǂ����m�F
                    CanvasGroup canvasGroup = canvas.GetComponent<CanvasGroup>();
                    if (canvasGroup != null && canvasGroup.alpha > 0f && canvasGroup.interactable)
                    {
                        visibleCanvasList.Add(canvas);
                    }
                    else if (canvasGroup == null)
                    {
                        // CanvasGroup���Ȃ���΂��̂܂ܒǉ�
                        visibleCanvasList.Add(canvas);
                    }
                }
            }
        }
    }

    void Update()
    {
        // �g���K�[���͂��m�F
        CheckTriggerInput();

        // ���[�U�[���X�V
        UpdateLaser();
    }

    private void CheckTriggerInput()
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

    private void UpdateLaser()
    {
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer������������Ă��܂���");
            return;
        }

        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;

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
        RaycastHit physicsHit;
        bool hitPhysics = Physics.Raycast(startPos, direction, out physicsHit, maxRayDistance);
        if (hitPhysics && physicsHit.distance < hitDistance)
        {
            hitDistance = physicsHit.distance;
        }

        // UI�v�f�Ƃ̌�������
        if (uiRaycaster != null && visibleCanvasList.Count > 0 && eventSystem != null)
        {
            // �eCanvas�ɑ΂��Č���������s��
            foreach (Canvas canvas in visibleCanvasList)
            {
                if (CheckUIRaycast(startPos, direction, canvas, ref hitDistance))
                {
                    // UI�Ƃ̌���������Ώ����I��
                    return;
                }
            }
        }

        // �h���b�O���̃��[�U�[��[�ʒu�̍X�V
        if (isDragging)
        {
            float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
            Vector3 endPos = startPos + direction * visualDistance;
            lineRenderer.SetPosition(1, endPos);
            pointerDot.SetActive(false);
        }
        // �����I�u�W�F�N�g�Ƀq�b�g�����ꍇ
        else if (hitPhysics && physicsHit.distance < maxRayDistance)
        {
            float visualDistance = Mathf.Min(physicsHit.distance, maxVisualDistance);
            Vector3 visualEndPoint = startPos + direction * visualDistance;
            lineRenderer.SetPosition(1, visualEndPoint);

            // �q�b�g�|�C���g�̈ʒu�͎��ۂ̃q�b�g�ʒu��
            hitPoint.position = physicsHit.point;
            pointerDot.SetActive(true);
        }
        else
        {
            // �ǂ�ɂ��q�b�g���Ȃ������ꍇ�A�\���p�̋������g�p
            lineRenderer.SetPosition(1, startPos + direction * maxVisualDistance);
            pointerDot.SetActive(false);
        }

        // UI�v�f����O�ꂽ�ꍇ
        if (lastTarget != null && currentTarget == null && !isDragging)
        {
            HandlePointerExit(lastTarget);
        }
    }

    // �����Canvas�Ƃ̌�������
    private bool CheckUIRaycast(Vector3 startPos, Vector3 direction, Canvas canvas, ref float hitDistance)
    {
        if (canvas == null || uiRaycaster == null || eventSystem == null)
            return false;

        // ���C�ƃL�����o�X�̌�_���v�Z
        Ray ray = new Ray(startPos, direction);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return false;

        Plane canvasPlane = new Plane(-canvasRect.forward, canvasRect.position);
        float rayDistance;

        if (canvasPlane.Raycast(ray, out rayDistance) && rayDistance < hitDistance)
        {
            Vector3 worldPos = startPos + direction * rayDistance;

            // ���E���W����X�N���[�����W��
            Camera canvasCamera = canvas.worldCamera;
            if (canvasCamera == null) canvasCamera = Camera.main;

            if (canvasCamera != null)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldPos);

                // UI���C�L���X�g�p�̃f�[�^���X�V
                pointerData.position = screenPoint;
                pointerData.delta = Vector2.zero;
                pointerData.scrollDelta = Vector2.zero;
                pointerData.dragging = false;
                pointerData.useDragThreshold = true;
                pointerData.pointerId = 0;

                // UI���C�L���X�g�����s
                List<RaycastResult> results = new List<RaycastResult>();
                uiRaycaster.Raycast(pointerData, results);

                if (results.Count > 0)
                {
                    // UI�q�b�g���ʂ����ڍׂɃf�o�b�O
                    RaycastResult topResult = results[0];
                    GameObject targetObject = topResult.gameObject;

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
                    lineRenderer.SetPosition(1, visualEndPoint);

                    // �h���b�O���̓h�b�g���\����
                    if (!isDragging)
                    {
                        hitPoint.position = worldPos;
                        pointerDot.SetActive(true);

                        // �g���K�[��������Ă���ꍇ�̓h�b�g�̐F��ύX
                        dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;
                    }
                    else
                    {
                        pointerDot.SetActive(false);
                    }

                    // UI�^�[�Q�b�g��ۑ�
                    currentTarget = targetObject;

                    // �ʏ�̃|�C���^Enter/Exit����
                    HandlePointerEnterExit(currentTarget, lastTarget);

                    return true; // UI�Ƃ̌�������
                }
            }
        }

        return false; // UI�Ƃ̌����Ȃ�
    }

    private void HandlePointerEnterExit(GameObject current, GameObject last)
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

    private void HandlePointerEnter(GameObject go)
    {
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

    private void HandlePointerExit(GameObject go)
    {
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

    private void HandlePointerDown(GameObject go)
    {
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

    private void HandlePointerUp(GameObject go)
    {
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
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerClickHandler);
        }

        cachedPointerData.pointerPress = null;
    }

    private void TriggerDown()
    {
        // �f�o�b�O���
        Debug.Log("[LaserPointer] �g���K�[����: �^�[�Q�b�g = " + (currentTarget != null ? currentTarget.name : "�Ȃ�"));

        // �h�b�g�F��ύX
        if (!isDragging && pointerDot.activeSelf)
        {
            dotRenderer.material.color = dotPressedColor;
        }

        if (currentTarget != null)
        {
            // �|�C���^�_�E���̏���
            HandlePointerDown(currentTarget);
        }
    }

    private void TriggerUp()
    {
        // �h�b�g�F�����ɖ߂�
        if (!isDragging && pointerDot.activeSelf)
        {
            dotRenderer.material.color = dotColor;
        }

        if (currentTarget != null)
        {
            // �|�C���^�A�b�v�̏���
            HandlePointerUp(currentTarget);
        }
    }

    private void OnAButtonPressed(InputAction.CallbackContext context)
    {
        if (panelVisibilityController == null) return;

        // ����p�����ǂ������m�F
        bool isInViewRange = panelVisibilityController.IsInViewRange();

        if (isInViewRange)
        {
            Debug.Log("[LaserPointer] A�{�^����������܂��� - �p�l���\����؂�ւ��܂�");
            // �����I�Ƀp�l���̕\��/��\����؂�ւ���
            panelVisibilityController.ToggleForcedVisibility();
        }
    }
}