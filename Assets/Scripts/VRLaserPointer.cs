using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class VRLaserPointer : MonoBehaviour
{
    [Header("���[�U�[�ݒ�")]
    public Color laserColor = Color.blue;
    public float laserWidth = 0.002f;
    public float laserMaxLength = 10f;
    public GameObject laserDot;
    public float dotScale = 0.01f;

    [Header("�R���g���[���[�Q��")]
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;
    public bool useLeftController = true;
    public bool useRightController = true;

    [Header("�C���^���N�V�����ݒ�")]
    public LayerMask interactionLayers;
    public InputActionReference triggerAction;
    public InputActionReference primaryButtonAction;

    // ���[�U�[�ƃh�b�g�̃����_���[
    private LineRenderer leftLaser;
    private LineRenderer rightLaser;
    private GameObject leftDot;
    private GameObject rightDot;

    // ���݂̃|�C���^�[�f�[�^
    private PointerEventData leftPointerEventData;
    private PointerEventData rightPointerEventData;
    private List<RaycastResult> leftRaycastResults = new List<RaycastResult>();
    private List<RaycastResult> rightRaycastResults = new List<RaycastResult>();

    // Start is called before the first frame update
    void Start()
    {
        // ���R���g���[���[���[�U�[������
        if (useLeftController && leftControllerAnchor != null)
        {
            leftLaser = CreateLaserBeam(leftControllerAnchor, "LeftLaser");
            leftDot = CreateLaserDot(leftControllerAnchor, "LeftDot");
            leftPointerEventData = new PointerEventData(EventSystem.current);
        }

        // �E�R���g���[���[���[�U�[������
        if (useRightController && rightControllerAnchor != null)
        {
            rightLaser = CreateLaserBeam(rightControllerAnchor, "RightLaser");
            rightDot = CreateLaserDot(rightControllerAnchor, "RightDot");
            rightPointerEventData = new PointerEventData(EventSystem.current);
        }

        // ���̓A�N�V������L����
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPerformed;
        }

        if (primaryButtonAction != null && primaryButtonAction.action != null)
        {
            primaryButtonAction.action.Enable();
            primaryButtonAction.action.performed += OnPrimaryButtonPerformed;
        }
    }

    // ���[�U�[�r�[�����쐬
    private LineRenderer CreateLaserBeam(Transform parent, string name)
    {
        GameObject laserObj = new GameObject(name);
        laserObj.transform.parent = parent;
        laserObj.transform.localPosition = Vector3.zero;
        laserObj.transform.localRotation = Quaternion.identity;

        LineRenderer lr = laserObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = laserWidth;
        lr.endWidth = laserWidth;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = laserColor;

        return lr;
    }

    // ���[�U�[�h�b�g���쐬
    private GameObject CreateLaserDot(Transform parent, string name)
    {
        if (laserDot != null)
        {
            GameObject dotObj = Instantiate(laserDot, parent);
            dotObj.name = name;
            dotObj.transform.localScale = new Vector3(dotScale, dotScale, dotScale);
            dotObj.SetActive(false);
            return dotObj;
        }
        else
        {
            GameObject dotObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dotObj.name = name;
            dotObj.transform.parent = parent;
            dotObj.transform.localScale = new Vector3(dotScale, dotScale, dotScale);

            Renderer renderer = dotObj.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = laserColor;

            Collider collider = dotObj.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            dotObj.SetActive(false);
            return dotObj;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (useLeftController && leftControllerAnchor != null)
        {
            UpdateLaser(leftLaser, leftDot, leftControllerAnchor, leftPointerEventData, leftRaycastResults);
        }

        if (useRightController && rightControllerAnchor != null)
        {
            UpdateLaser(rightLaser, rightDot, rightControllerAnchor, rightPointerEventData, rightRaycastResults);
        }
    }

    // ���[�U�[���X�V
    private void UpdateLaser(LineRenderer laser, GameObject dot, Transform controller, PointerEventData pointerEventData, List<RaycastResult> raycastResults)
    {
        // ���[�U�[�̊J�n�ʒu��ݒ�
        Vector3 startPos = controller.position;
        laser.SetPosition(0, startPos);

        // ���[�U�[�̕����i�R���g���[���[�̑O���j
        Vector3 direction = controller.forward;

        // �q�b�g�|�C���g�̏�����
        Vector3 hitPoint = startPos + direction * laserMaxLength;
        bool hitUI = false;

        // UI�Ƃ̏Փˌ��o
        raycastResults.Clear();
        pointerEventData.position = new Vector2(Screen.width / 2, Screen.height / 2); // ���̒l
        pointerEventData.position = Camera.main.WorldToScreenPoint(startPos + direction);
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        if (raycastResults.Count > 0)
        {
            // UI�v�f�Ƀq�b�g�����ꍇ
            hitPoint = raycastResults[0].worldPosition;
            hitUI = true;
        }
        else
        {
            // �����I�ȃI�u�W�F�N�g�Ƃ̏Փˌ��o
            Ray ray = new Ray(startPos, direction);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, laserMaxLength, interactionLayers))
            {
                hitPoint = hit.point;
                hitUI = false;
            }
        }

        // ���[�U�[�̏I���ʒu��ݒ�
        laser.SetPosition(1, hitPoint);

        // �h�b�g�̈ʒu���X�V
        if (dot != null)
        {
            dot.transform.position = hitPoint;
            dot.SetActive(true);
        }
    }

    // �g���K�[���͎��̏���
    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        // �R���g���[���[����̃��C�L���X�g���ʂɊ�Â��ăN���b�N�C�x���g�𔭐�������
        if (useLeftController && leftRaycastResults.Count > 0)
        {
            ExecuteUIClick(leftRaycastResults[0]);
        }

        if (useRightController && rightRaycastResults.Count > 0)
        {
            ExecuteUIClick(rightRaycastResults[0]);
        }
    }

    // �v���C�}���{�^�����͎��̏���
    private void OnPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        // �ǉ��̋@�\�������i��F�o�b�N�{�^���j
        Debug.Log("Primary Button Pressed");
    }

    // UI�v�f�̃N���b�N�����s
    private void ExecuteUIClick(RaycastResult raycastResult)
    {
        GameObject hitObj = raycastResult.gameObject;

        // �{�^���̏ꍇ
        Button button = hitObj.GetComponent<Button>();
        if (button != null && button.isActiveAndEnabled)
        {
            button.onClick.Invoke();
            return;
        }

        // ���̑���UI�v�f�i�K�v�ɉ����Ċg���j
        // ...
    }

    // �A�v���P�[�V�����I�����ɃN���[���A�b�v
    private void OnDestroy()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            triggerAction.action.performed -= OnTriggerPerformed;
        }

        if (primaryButtonAction != null && primaryButtonAction.action != null)
        {
            primaryButtonAction.action.performed -= OnPrimaryButtonPerformed;
        }
    }
}