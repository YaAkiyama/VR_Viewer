using UnityEngine;

public class SimplePanoramaControls : MonoBehaviour
{
    private Panorama360Controller panoramaController;
    private MapMarkerManager markerManager;

    // �L�[�{�[�h���͂��g�p����ꍇ�̃t���O
    [SerializeField] private bool useKeyboardControls = true;

    // �{�^�����͂��g�p����ꍇ�̃{�^����
    [SerializeField] private string nextButton = "Fire1"; // �f�t�H���g�͍��N���b�N��R���g���[���[�̃g���K�[
    [SerializeField] private string prevButton = "Fire2"; // �f�t�H���g�͉E�N���b�N��ʂ̃{�^��

    // ���ԊԊu�ɂ�鎩���؂�ւ�
    [SerializeField] private float autoChangeInterval = 0f; // 0=�����A����=�b�Ԋu
    private float timer = 0f;

    void Start()
    {
        panoramaController = GetComponent<Panorama360Controller>();

        // MapMarkerManager�̎Q�Ƃ��擾
        markerManager = FindFirstObjectByType<MapMarkerManager>();
        if (markerManager == null)
        {
            Debug.LogError("MapMarkerManager��������܂���BSimplePanoramaControls���g�p����ɂ�MapMarkerManager���K�v�ł��B");
            enabled = false; // �G���[������ꍇ�̓R���|�[�l���g�𖳌���
            return;
        }
    }

    void Update()
    {
        // �L�[�{�[�h����
        if (useKeyboardControls)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                markerManager.SelectNextMarker();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                markerManager.SelectPreviousMarker();
            }
        }

        // �{�^�����́iVR�R���g���[���[�p�j
        if (Input.GetButtonDown(nextButton))
        {
            markerManager.SelectNextMarker();
        }

        if (Input.GetButtonDown(prevButton))
        {
            markerManager.SelectPreviousMarker();
        }

        // �����؂�ւ�
        if (autoChangeInterval > 0)
        {
            timer += Time.deltaTime;
            if (timer >= autoChangeInterval)
            {
                timer = 0f;
                markerManager.SelectNextMarker();
            }
        }
    }
}