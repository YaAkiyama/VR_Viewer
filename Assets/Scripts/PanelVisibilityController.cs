using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PanelVisibilityController : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private List<GameObject> controlledPanels = new List<GameObject>();

    [Header("����p�ݒ�")]
    [SerializeField] private float minViewAngleX = -70f;
    [SerializeField] private float maxViewAngleX = 70f;
    [SerializeField] private float fadeSpeed = 5f;

    private Dictionary<GameObject, CanvasGroup> panelCanvasGroups = new Dictionary<GameObject, CanvasGroup>();
    private bool isInViewRange = true;
    private Coroutine fadeCoroutine;

    void Start()
    {
        // �J�����̎Q�Ƃ��擾
        if (cameraTransform == null)
        {
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null && xrOrigin.Camera != null)
            {
                cameraTransform = xrOrigin.Camera.transform;
            }
            else
            {
                cameraTransform = Camera.main.transform;
            }
        }

        // �e�p�l����CanvasGroup��ݒ�
        foreach (var panel in controlledPanels)
        {
            if (panel == null) continue;

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            panelCanvasGroups[panel] = canvasGroup;
            canvasGroup.alpha = 1f;
        }
    }

    void Update()
    {
        if (cameraTransform == null) return;

        // �J�����̑O�����x�N�g������p�x���v�Z
        float cameraXRotation = cameraTransform.eulerAngles.x;

        // 0-360�x�͈͂���-180����180�x�͈͂ɕϊ�
        if (cameraXRotation > 180f)
            cameraXRotation -= 360f;

        // ����͈͓����ǂ������`�F�b�N
        bool newIsInViewRange = (cameraXRotation >= minViewAngleX && cameraXRotation <= maxViewAngleX);

        // ��Ԃ��ς�����ꍇ�̂݃t�F�[�h�������J�n
        if (newIsInViewRange != isInViewRange)
        {
            isInViewRange = newIsInViewRange;

            // �����̃t�F�[�h�R���[�`��������Β�~
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            // �V�����t�F�[�h�R���[�`�����J�n
            fadeCoroutine = StartCoroutine(FadePanels(isInViewRange));
        }
    }

    // �p�l�����t�F�[�h�C��/�A�E�g������R���[�`��
    private IEnumerator FadePanels(bool fadeIn)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        Dictionary<CanvasGroup, float> startAlphas = new Dictionary<CanvasGroup, float>();

        // �J�n�A���t�@�l���L�^
        foreach (var canvasGroup in panelCanvasGroups.Values)
        {
            if (canvasGroup != null)
            {
                startAlphas[canvasGroup] = canvasGroup.alpha;
            }
        }

        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * fadeSpeed;
            float t = Mathf.Clamp01(time);

            foreach (var canvasGroup in panelCanvasGroups.Values)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlphas[canvasGroup], targetAlpha, t);
                }
            }

            yield return null;
        }

        // �ŏI�l���m���ɐݒ肵�A�C���^���N�V�����𒲐�
        foreach (var canvasGroup in panelCanvasGroups.Values)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
                canvasGroup.interactable = fadeIn;
                canvasGroup.blocksRaycasts = fadeIn;
            }
        }
    }

    // �p�l����ǉ�
    public void AddPanel(GameObject panel)
    {
        if (panel == null || controlledPanels.Contains(panel)) return;

        controlledPanels.Add(panel);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        panelCanvasGroups[panel] = canvasGroup;
        canvasGroup.alpha = isInViewRange ? 1f : 0f;
        canvasGroup.interactable = isInViewRange;
        canvasGroup.blocksRaycasts = isInViewRange;
    }

    // �p�l�����폜
    public void RemovePanel(GameObject panel)
    {
        if (panel == null || !controlledPanels.Contains(panel)) return;

        controlledPanels.Remove(panel);

        if (panelCanvasGroups.ContainsKey(panel))
        {
            panelCanvasGroups.Remove(panel);
        }
    }

    // ����p�͈͂�ݒ�
    public void SetViewAngleRange(float min, float max)
    {
        minViewAngleX = min;
        maxViewAngleX = max;
    }
}