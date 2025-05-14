using System.IO; // Path.ChangeExtension�̂���
using UnityEngine;
using System.Collections.Generic;

public class Panorama360Controller : MonoBehaviour
{
    [SerializeField] private Material panoramaMaterial;
    [SerializeField] private float changeInterval = 0f; // 0�̏ꍇ�͎����ύX�Ȃ��A���̒l�ŕb���w��

    // �p�m���}�e�N�X�`���Ǘ��i�}�[�J�[�ƘA�g���邽�ߕύX�j
    private Dictionary<string, Texture2D> panoramaTextureDict = new Dictionary<string, Texture2D>();
    private string currentTexturePath = "";
    private float timer = 0f;

    // �}�[�J�[�}�l�[�W���[�ւ̎Q��
    private MapMarkerManager markerManager;

    void Start()
    {
        // �}�[�J�[�}�l�[�W���[�ւ̎Q�Ƃ��擾
        markerManager = FindFirstObjectByType<MapMarkerManager>();

        if (markerManager == null)
        {
            Debug.LogError("MapMarkerManager��������܂���B���MapMarkerManager���Z�b�g�A�b�v���Ă��������B");
            return;
        }

        // �J�����̃N���A�t���O��Skybox�ɐݒ�
        Camera.main.clearFlags = CameraClearFlags.Skybox;

        // �X�J�C�{�b�N�X�Ƀ}�e���A����ݒ�
        RenderSettings.skybox = panoramaMaterial;
        // �f�o�b�O: Image/360�f�B���N�g���̓��e�����X�g�\��
        Object[] allTextures = Resources.LoadAll("Image/360", typeof(Texture2D));
        Debug.Log($"Resources/Image/360���̉摜��: {allTextures.Length}");
        foreach (var tex in allTextures)
        {
            Debug.Log($"���������摜: {tex.name}");
        }

        // �e�X�g: �摜�𒼐ږ��O�Ń��[�h
        Texture2D testTexture = Resources.Load<Texture2D>("Image/360/R0010042");
        if (testTexture != null)
        {
            Debug.Log("�e�X�g�摜�̃��[�h�ɐ������܂���: R0010042");
        }
        else
        {
            Debug.LogError("�e�X�g�摜�̃��[�h�Ɏ��s���܂���: R0010042");
        }
    }

    // �N���X���̂ǂ����AStart()���\�b�h�̊O�ɒǉ�
#if UNITY_EDITOR
    [ContextMenu("�e�X�g: �S�p�m���}�����[�h")]
    private void EditorTestLoadAll()
    {
        TestLoadAllPanoramas();
    }
#endif

    void Update()
    {
        // �����؂�ւ����ݒ肳��Ă���ꍇ
        if (changeInterval > 0 && panoramaTextureDict.Count > 1 && markerManager != null)
        {
            timer += Time.deltaTime;
            if (timer >= changeInterval)
            {
                timer = 0f;
                // ���̃}�[�J�[�ֈړ��i�}�[�J�[�}�l�[�W���[�o�R�Ŏ����j
                markerManager.SelectNextMarker();
            }
        }
    }

    // ����̃p�X�̃p�m���}�摜�����[�h���郁�\�b�h
    public void LoadPanoramaByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        // �p�X����Resources��������菜������
        if (path.StartsWith("Assets/Resources/"))
        {
            path = path.Substring("Assets/Resources/".Length);
        }

        // �g���q���폜�i���̍s��ǉ��j
        path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

        // �p�X�����݂̂��̂Ɠ����Ȃ牽�����Ȃ�
        if (path == currentTexturePath && panoramaTextureDict.ContainsKey(path)) return;

        // ���Ƀ��[�h�ς݂̏ꍇ�̓L���b�V������擾
        if (panoramaTextureDict.ContainsKey(path))
        {
            SetPanoramaTexture(panoramaTextureDict[path]);
            currentTexturePath = path;
            return;
        }

        // �V�������[�h����ꍇ
        Texture2D texture = Resources.Load<Texture2D>(path);

        // ������Ȃ��ꍇ�A�g���q���������ɕς��Ď���
        if (texture == null && path.ToLower() != path)
        {
            string lowercasePath = Path.ChangeExtension(path, Path.GetExtension(path).ToLower());
            texture = Resources.Load<Texture2D>(lowercasePath);

            if (texture != null)
            {
                Debug.LogWarning($"�啶�����������قȂ�p�X�ŉ摜��������܂���: {lowercasePath}");
                path = lowercasePath;
            }
        }

        if (texture != null)
        {
            // �L���b�V���ɒǉ�
            panoramaTextureDict[path] = texture;

            // �e�N�X�`����ݒ�
            SetPanoramaTexture(texture);
            currentTexturePath = path;

            Debug.Log($"�p�m���}�摜�����[�h: {path}");
        }
        else
        {
            Debug.LogError($"�p�m���}�摜��������܂���: {path}");
            Debug.LogWarning($"�����p�X: {path}");
            Debug.LogWarning("Resources�t�H���_����̑��΃p�X���g�p���Ă��������i��: 'Image/360/example.jpg'�j");

            // ���\�[�X�f�B���N�g�����̗��p�\�ȃt�@�C�������O�ɏo��
            Object[] allTextures = Resources.LoadAll("Image/360", typeof(Texture2D));
            if (allTextures.Length > 0)
            {
                Debug.LogWarning("���p�\�ȃp�m���}�摜�ꗗ:");
                foreach (var tex in allTextures)
                {
                    Debug.LogWarning($" - Image/360/{tex.name}");
                }
            }
        }
    }

    // �e�N�X�`���𒼐ڐݒ肷��v���C�x�[�g���\�b�h
    private void SetPanoramaTexture(Texture2D texture)
    {
        if (texture == null) return;

        // �}�e���A���Ƀe�N�X�`����ݒ�
        panoramaMaterial.SetTexture("_MainTex", texture);

        // �V�F�[�_�[�v���p�e�B�̐ݒ�
        panoramaMaterial.SetFloat("_Mapping", 1); // 1 = ���ʃ}�b�s���O
        panoramaMaterial.SetFloat("_ImageType", 0); // 0 = 360�x�i�����j
        panoramaMaterial.SetFloat("_Layout", 0); // 0 = �ʏ�
    }

    // ���ݕ\�����̃p�m���}�p�X���擾
    public string GetCurrentPanoramaPath()
    {
        return currentTexturePath;
    }

    // �p�m���}�L���b�V�����N���A�i����������p�j
    public void ClearPanoramaCache()
    {
        panoramaTextureDict.Clear();
        currentTexturePath = "";

        // �p�m���}�e�N�X�`����null�ɐݒ�
        panoramaMaterial.SetTexture("_MainTex", null);
    }
    // Panorama360Controller�N���X�Ƀe�X�g���\�b�h��ǉ�
    public void TestLoadAllPanoramas()
    {
        Debug.Log("�S�p�m���}�摜�̃��[�h�e�X�g�J�n");

        // Resources�t�H���_��Image/360�f�B���N�g�����̂��ׂẴe�N�X�`�������[�h
        Object[] textures = Resources.LoadAll("Image/360", typeof(Texture2D));

        if (textures.Length == 0)
        {
            Debug.LogError("Image/360�f�B���N�g���Ƀe�N�X�`����������܂���");
            return;
        }

        Debug.Log($"�e�N�X�`���� {textures.Length} ������܂���:");

        // �ŏ��̃e�N�X�`����\��
        if (textures.Length > 0)
        {
            Texture2D firstTexture = (Texture2D)textures[0];
            panoramaMaterial.SetTexture("_MainTex", firstTexture);
            panoramaMaterial.SetFloat("_Mapping", 1); // 1 = ���ʃ}�b�s���O
            panoramaMaterial.SetFloat("_ImageType", 0); // 0 = 360�x�i�����j
            panoramaMaterial.SetFloat("_Layout", 0); // 0 = �ʏ�

            Debug.Log($"�e�X�g: �ŏ��̃e�N�X�`����\��: {firstTexture.name}");
        }

        // �S�e�N�X�`���̏����o��
        foreach (Texture2D tex in textures)
        {
            Debug.Log($"�e�N�X�`��: {tex.name}, �T�C�Y: {tex.width}x{tex.height}");
        }
    }
}