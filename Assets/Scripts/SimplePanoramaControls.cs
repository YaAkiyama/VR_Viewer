using UnityEngine;

public class SimplePanoramaControls : MonoBehaviour
{
    private Panorama360Controller panoramaController;

    // �L�[�{�[�h���͂��g�p����ꍇ�̃t���O
    [SerializeField] private bool useKeyboardControls = true;

    // �{�^�����͂��g�p����ꍇ�̃{�^����
    [SerializeField] private string nextButton = "Fire1"; // �f�t�H���g�͍��N���b�N��R���g���[���̃g���K�[
    [SerializeField] private string prevButton = "Fire2"; // �f�t�H���g�͉E�N���b�N��ʂ̃{�^��

    // ���ԊԊu�ɂ�鎩���؂�ւ�
    [SerializeField] private float autoChangeInterval = 0f; // 0=�����A����=�b�Ԋu
    private float timer = 0f;

    void Start()
    {
        panoramaController = GetComponent<Panorama360Controller>();
    }

    void Update()
    {
        // �L�[�{�[�h����
        if (useKeyboardControls)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                panoramaController.NextPanorama();
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                panoramaController.PreviousPanorama();
            }
        }

        // �{�^�����́iVR�R���g���[���[�p�j
        if (Input.GetButtonDown(nextButton))
        {
            panoramaController.NextPanorama();
        }

        if (Input.GetButtonDown(prevButton))
        {
            panoramaController.PreviousPanorama();
        }

        // �����؂�ւ�
        if (autoChangeInterval > 0)
        {
            timer += Time.deltaTime;
            if (timer >= autoChangeInterval)
            {
                timer = 0f;
                panoramaController.NextPanorama();
            }
        }
    }
}