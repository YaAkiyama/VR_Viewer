using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SimpleLaserPointer : MonoBehaviour
{
    [Header("���[�U�[�ݒ�")]
    public float maxDetectionDistance = 100f;
    public float maxVisualDistance = 50f;
    public float dotScale = 0.02f;
    public Color laserColor = Color.blue;
    public Color dotColor = Color.white;
    public Color dotPressedColor = Color.red;
    public Color dotDraggingColor = Color.yellow;

    [Header("UI���o�p")]
    public GraphicRaycaster uiRaycaster;
    public Canvas targetCanvas; // ����݊����̂��ߎc��
    public Canvas[] targetCanvasList; // �V���ɒǉ��F����Canvas�Ή�
    public EventSystem eventSystem;

    [Header("�t�F�[�h�R���g���[���[�Q��")]
    public CanvasFadeController canvasFadeController; // ����݊����̂��ߎc��

    // ���͊֌W
    private bool triggerPressed = false;
    private bool isDragging = false;

    private LineRenderer lineRenderer;
    private GameObject hitPointObj;
    private Transform hitPoint;
    private Renderer dotRenderer;
    private PointerEventData pointerData;

    // UI�v�f�Ƃ̑��ݍ�p�p
    [HideInInspector] public GameObject currentTarget;
    private GameObject lastTarget;
    private PointerEventData cachedPointerData;

    // �h���b�O����p
    private CanvasDragHandler canvasDragHandler;

    // Canvas���o�֘A
    private List<Canvas> visibleCanvasList = new List<Canvas>();

    void Start()
    {
        // ���C���`��̏����ݒ�
        InitializeLineRenderer();

        // �q�b�g�|�C���g�̐ݒ�
        InitializeHitPoint();

        // EventSystem�̊m�F
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        // PointerEventData�̏�����
        pointerData = new PointerEventData(eventSystem);
        cachedPointerData = new PointerEventData(eventSystem);

        // Canvas���X�g��������
        UpdateVisibleCanvasList();
    }

    private void InitializeLineRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.002f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.5f);
        lineRenderer.positionCount = 2;
    }

    private void InitializeHitPoint()
    {
        hitPointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitPointObj.name = "LaserDot";
        hitPointObj.transform.SetParent(transform);
        hitPointObj.transform.localScale = Vector3.one * dotScale;
        Destroy(hitPointObj.GetComponent<Collider>());

        dotRenderer = hitPointObj.GetComponent<Renderer>();
        dotRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        dotRenderer.material.color = dotColor;

        hitPoint = hitPointObj.transform;
        hitPointObj.SetActive(false);
    }

    // �\������Canvas���X�g���X�V
    private void UpdateVisibleCanvasList()
    {
        visibleCanvasList.Clear();

        // ����݊����F�P��Canvas���Z�b�g����Ă���ꍇ
        if (targetCanvas != null)
        {
            CanvasFadeController fadeCtrl = targetCanvas.GetComponent<CanvasFadeController>();
            if (fadeCtrl == null || fadeCtrl.IsVisible())
            {
                visibleCanvasList.Add(targetCanvas);
            }
        }

        // ����Canvas�Ή�
        if (targetCanvasList != null && targetCanvasList.Length > 0)
        {
            foreach (Canvas canvas in targetCanvasList)
            {
                if (canvas != null)
                {
                    CanvasFadeController fadeCtrl = canvas.GetComponent<CanvasFadeController>();
                    if (fadeCtrl == null || fadeCtrl.IsVisible())
                    {
                        // �܂����X�g�Ɋ܂܂�Ă��Ȃ��ꍇ�̂ݒǉ�
                        if (!visibleCanvasList.Contains(canvas))
                        {
                            visibleCanvasList.Add(canvas);
                        }
                    }
                }
            }
        }
    }

    void Update()
    {
        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;

        lineRenderer.SetPosition(0, startPos);

        // �O�t���[���̃^�[�Q�b�g��ۑ�
        lastTarget = currentTarget;

        // ���C�̍ő勗����������
        float hitDistance = maxDetectionDistance;
        currentTarget = null;

        // �\������Canvas���X�g���X�V
        UpdateVisibleCanvasList();

        // �����I�u�W�F�N�g�Ƃ̌�������
        RaycastHit physicsHit;
        bool hitPhysics = Physics.Raycast(startPos, direction, out physicsHit, maxDetectionDistance);
        if (hitPhysics && physicsHit.distance < hitDistance)
        {
            hitDistance = physicsHit.distance;
        }

        // UI�v�f�Ƃ̌�������i�\������Ă���Canvas�݂̂��`�F�b�N�j
        if (uiRaycaster != null && visibleCanvasList.Count > 0)
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
            hitPointObj.SetActive(false);
        }
        // �����I�u�W�F�N�g�Ƀq�b�g�����ꍇ
        else if (hitPhysics && physicsHit.distance < maxDetectionDistance)
        {
            float visualDistance = Mathf.Min(physicsHit.distance, maxVisualDistance);
            Vector3 visualEndPoint = startPos + direction * visualDistance;
            lineRenderer.SetPosition(1, visualEndPoint);

            // �q�b�g�|�C���g�̈ʒu�͎��ۂ̃q�b�g�ʒu��
            hitPoint.position = physicsHit.point;
            hitPointObj.SetActive(true);
        }
        else
        {
            // �ǂ�ɂ��q�b�g���Ȃ������ꍇ�A�\���p�̋������g�p
            lineRenderer.SetPosition(1, startPos + direction * maxVisualDistance);
            hitPointObj.SetActive(false);
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
        // ���C�ƃL�����o�X�̌�_���v�Z
        Ray ray = new Ray(startPos, direction);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
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
                                Debug.Log("[LaserPointer] �e��Selectable���o: " + parent.name);
                                targetObject = parent.gameObject;
                                break;
                            }
                            parent = parent.parent;
                        }
                    }

                    Debug.Log("[LaserPointer] UI�q�b�g: " + topResult.gameObject.name +
                              ", �ΏۃI�u�W�F�N�g: " + targetObject.name +
                              ", ����: " + rayDistance +
                              ", Selectable: " + (targetObject.GetComponent<Selectable>() != null));

                    hitDistance = rayDistance;

                    // UI�Ƃ̌����|�C���g��ݒ�
                    Vector3 hitPos = worldPos;

                    // ���[�U�[�̍ő�\���������Ȃ�\���A�����łȂ���΍ő�\�������Ő؂�
                    float visualDistance = Mathf.Min(hitDistance, maxVisualDistance);
                    Vector3 visualEndPoint = startPos + direction * visualDistance;
                    lineRenderer.SetPosition(1, visualEndPoint);

                    // �h���b�O���̓h�b�g���\���ɌŒ�
                    if (!isDragging)
                    {
                        hitPoint.position = hitPos;
                        hitPointObj.SetActive(true);
                    }
                    else
                    {
                        hitPointObj.SetActive(false);
                    }

                    // UI�^�[�Q�b�g��ۑ�
                    currentTarget = targetObject;

                    // �h���b�O�n���h���[�̎擾�����݂�
                    if (canvasDragHandler == null && canvas != null)
                    {
                        canvasDragHandler = canvas.GetComponentInChildren<CanvasDragHandler>();
                    }

                    // �h���b�O�����ƒʏ��UI��������
                    if (isDragging)
                    {
                        // �h���b�O���̓h�b�g���\���ɂ���̂ŐF�̕ύX�͕s�v
                    }
                    else
                    {
                        // �ʏ�̃|�C���^Enter/Exit����
                        HandlePointerEnterExit(currentTarget, lastTarget);
                    }

                    return true; // UI�Ƃ̌�������
                }
            }
        }

        return false; // UI�Ƃ̌����Ȃ�
    }

    // �h�b�g�ʒu���擾���郁�\�b�h
    public Vector3 GetDotPosition()
    {
        // �h�b�g���\������Ă���ꍇ�͂��̈ʒu��Ԃ�
        if (hitPointObj != null && hitPointObj.activeSelf)
        {
            return hitPoint.position;
        }

        // �h�b�g���\������Ă��Ȃ��ꍇ�̓��[�U�[�̐�[�ʒu���v�Z���ĕԂ�
        return transform.position + transform.forward * maxDetectionDistance;
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

    // �c��̃R�[�h�iHandlePointerEnter, HandlePointerExit, HandlePointerDown, TriggerDown, TriggerUp �Ȃǁj�͂��̂܂܎g�p
    // ...

    // �ȉ��̊����R�[�h�͂��̂܂܎g�p
    private void HandlePointerEnter(GameObject go)
    {
        // ���ڍׂȃf�o�b�O����ǉ�
        Debug.Log("[LaserPointer] �|�C���^�[Enter: " + go.name);

        // PointerEnter�C�x���g��z�M
        cachedPointerData.pointerEnter = go;
        cachedPointerData.position = pointerData.position;

        // Selectable�R���|�[�l���g�ɂ��炩�����ă��C���C�g�_�g����
        Selectable selectable = go.GetComponent<Selectable>();
        if (selectable != null)
        {
            Debug.Log("[LaserPointer] Selectable���o: " + go.name + ", Interactable: " + selectable.interactable);
            selectable.OnPointerEnter(cachedPointerData);

            // �{�^���ɒ����I�ȃt�B�[�h�o�b�N��K�p
            ColorBlock colors = selectable.colors;
            Image image = go.GetComponent<Image>();
            if (image != null)
            {
                // �F�̕ύX�������I�ɓK�p
                Color targetColor = colors.highlightedColor;
                Debug.Log("[LaserPointer] �F��ύX: " + colors.normalColor + " -> " + targetColor);
                image.color = targetColor;
                image.CrossFadeColor(targetColor, colors.fadeDuration, true, true);
            }
        }

        // �C�x���g��z�M
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerEnterHandler);
    }

    private void HandlePointerExit(GameObject go)
    {
        // PointerExit�C�x���g��z�M
        cachedPointerData.pointerEnter = null;
        cachedPointerData.position = pointerData.position;

        // Selectable�R���|�[�l���g�ɑ΂��Ẵn�C���C�g����
        Selectable selectable = go.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.OnPointerExit(cachedPointerData);

            // �{�^���̒����I�ȃt�B�[�h�o�b�N�����ɖ߂�
            ColorBlock colors = selectable.colors;
            Image image = go.GetComponent<Image>();
            if (image != null)
            {
                image.CrossFadeColor(colors.normalColor, colors.fadeDuration, true, true);
            }
        }

        // �C�x���g��z�M
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerExitHandler);

        // �g���K�[��������Ă����ꍇ�͉���
        if (triggerPressed)
        {
            HandlePointerUp(go);
        }
    }

    private void HandlePointerDown(GameObject go)
    {
        // DragHandle���ǂ������m�F���A�h���b�O�J�n����
        if (canvasDragHandler != null && canvasDragHandler.IsDragHandle(go))
        {
            StartDragging();
            return;
        }

        cachedPointerData.pointerPress = go;
        cachedPointerData.pressPosition = pointerData.position;
        cachedPointerData.pointerPressRaycast = pointerData.pointerPressRaycast;

        // Selectable�R���|�[�l���g��Button�ɑ΂��Ẳ�������
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

        // �C�x���g��z�M
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerDownHandler);
    }

    private void HandlePointerUp(GameObject go)
    {
        // PointerUp�C�x���g��z�M
        cachedPointerData.position = pointerData.position;

        // Selectable�R���|�[�l���g��Button�̏���
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

        // �C�x���g��z�M
        ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerUpHandler);

        // �N���b�N�C�x���g��z�M
        if (cachedPointerData.pointerPress == go)
        {
            ExecuteEvents.Execute(go, cachedPointerData, ExecuteEvents.pointerClickHandler);
        }

        cachedPointerData.pointerPress = null;
    }

    private void StartDragging()
    {
        isDragging = true;

        // �h���b�O���̓h�b�g���\���ɂ���
        hitPointObj.SetActive(false);

        // �h���b�O�n���h���[�ɒʒm
        if (canvasDragHandler != null)
        {
            canvasDragHandler.OnStartDrag(this);
        }

        Debug.Log("�h���b�O�J�n");
    }

    private void EndDragging()
    {
        isDragging = false;

        // �h���b�O���I��������h�b�g���ĕ\������
        hitPointObj.SetActive(true);
        dotRenderer.material.color = triggerPressed ? dotPressedColor : dotColor;

        // �h���b�O�n���h���[�ɒʒm
        if (canvasDragHandler != null)
        {
            canvasDragHandler.OnEndDrag();
        }

        Debug.Log("�h���b�O�I��");
    }

    public void TriggerDown()
    {
        triggerPressed = true;

        // �f�o�b�O���
        Debug.Log("[LaserPointer] �g���K�[����: �^�[�Q�b�g = " + (currentTarget != null ? currentTarget.name : "�Ȃ�"));

        // �h���b�O���łȂ���΃h�b�g�̐F��ύX
        if (!isDragging && hitPointObj.activeSelf)
        {
            dotRenderer.material.color = dotPressedColor;
        }

        if (currentTarget != null)
        {
            // DragHandle�̃`�F�b�N��ǉ�
            bool isDragHandle = false;
            if (canvasDragHandler != null)
            {
                isDragHandle = canvasDragHandler.IsDragHandle(currentTarget);
                Debug.Log("[LaserPointer] �h���b�O�n���h�����o: " + isDragHandle);
            }

            // �h���b�O�n���h���Ȃ珈��
            if (isDragHandle)
            {
                StartDragging();
            }
            // �ʏ��UI�Ȃ珈��
            else
            {
                // �{�^���̌��o���O
                Selectable selectable = currentTarget.GetComponent<Selectable>();
                if (selectable != null)
                {
                    Debug.Log("[LaserPointer] �{�^������: " + currentTarget.name +
                             ", Interactable: " + selectable.interactable +
                             ", Navigation: " + selectable.navigation.mode);
                }

                // �|�C���^�_�E���̏���
                HandlePointerDown(currentTarget);

                // �{�^���̉��݌��ʁi�����I�ȁj
                RectTransform buttonRect = currentTarget.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.localPosition = new Vector3(
                        buttonRect.localPosition.x,
                        buttonRect.localPosition.y,
                        buttonRect.localPosition.z - 0.001f
                    );
                }
            }
        }
    }

    public void TriggerUp()
    {
        triggerPressed = false;

        // �h���b�O���Ȃ�h���b�O�I��
        if (isDragging)
        {
            EndDragging();
        }
        else
        {
            // �h�b�g�F�����ɖ߂�
            if (hitPointObj.activeSelf)
            {
                dotRenderer.material.color = dotColor;
            }

            if (currentTarget != null)
            {
                // �|�C���^�A�b�v�̏���
                HandlePointerUp(currentTarget);

                // �{�^�������̈ʒu�ɖ߂��i�����I�ȁj
                RectTransform buttonRect = currentTarget.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.localPosition = new Vector3(
                        buttonRect.localPosition.x,
                        buttonRect.localPosition.y,
                        buttonRect.localPosition.z + 0.001f
                    );
                }
            }
        }
    }
}