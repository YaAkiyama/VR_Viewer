using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class Panorama360Controller : MonoBehaviour
{
    [SerializeField] private Material panoramaMaterial;
    [SerializeField] private float changeInterval = 0f; // 0�̏ꍇ�͎����ύX�Ȃ��A���̒l�ŕb���w��

    private Texture2D[] panoramaTextures;
    private int currentTextureIndex = 0;
    private float timer = 0f;

    void Start()
    {
        // Resources/Image/360�t�H���_����摜��ǂݍ���
        LoadPanoramaTextures();

        // �ŏ��̉摜��ݒ�
        if (panoramaTextures != null && panoramaTextures.Length > 0)
        {
            SetPanoramaTexture(0);
        }

        // �J�����̃N���A�t���O��Skybox�ɐݒ�
        Camera.main.clearFlags = CameraClearFlags.Skybox;

        // �X�J�C�{�b�N�X�Ƀ}�e���A����ݒ�
        RenderSettings.skybox = panoramaMaterial;
    }

    void Update()
    {
        // �����؂�ւ����ݒ肳��Ă���ꍇ
        if (changeInterval > 0 && panoramaTextures.Length > 1)
        {
            timer += Time.deltaTime;
            if (timer >= changeInterval)
            {
                timer = 0f;
                NextPanorama();
            }
        }

        // �����ɃR���g���[���[�̓��͂Ȃǂŉ摜��؂�ւ��鏈����ǉ��ł��܂�
    }

    private void LoadPanoramaTextures()
    {
        // Resources/Image/360�t�H���_���̂��ׂẲ摜��ǂݍ���
        Object[] loadedTextures = Resources.LoadAll("Image/360", typeof(Texture2D));
        panoramaTextures = new Texture2D[loadedTextures.Length];

        for (int i = 0; i < loadedTextures.Length; i++)
        {
            panoramaTextures[i] = (Texture2D)loadedTextures[i];
        }

        Debug.Log($"�ǂݍ��܂ꂽ�p�m���}�摜: {panoramaTextures.Length}��");
    }

    // ����̃C���f�b�N�X�̃p�m���}��\��
    public void SetPanoramaTexture(int index)
    {
        if (panoramaTextures == null || panoramaTextures.Length == 0) return;

        // �C���f�b�N�X�͈̔͂��m�F
        if (index >= 0 && index < panoramaTextures.Length)
        {
            currentTextureIndex = index;

            // �}�e���A���Ƀe�N�X�`����ݒ�
            panoramaMaterial.SetTexture("_MainTex", panoramaTextures[currentTextureIndex]);

            // �V�F�[�_�[�ɉ����đ��̃v���p�e�B���ݒ�
            // �p�m���}�摜�̃}�b�s���O��ݒ�i���������̉摜�̏ꍇ�j
            panoramaMaterial.SetFloat("_Mapping", 1); // 1 = ���ʃ}�b�s���O
            panoramaMaterial.SetFloat("_ImageType", 0); // 0 = 360�x�i�����j
            panoramaMaterial.SetFloat("_Layout", 0); // 0 = �ʏ�A1 = �~���[��

            Debug.Log($"�p�m���}�摜��؂�ւ�: {panoramaTextures[currentTextureIndex].name}");
        }
    }

    // ���̃p�m���}�ɐ؂�ւ�
    public void NextPanorama()
    {
        if (panoramaTextures == null || panoramaTextures.Length <= 1) return;

        int nextIndex = (currentTextureIndex + 1) % panoramaTextures.Length;
        SetPanoramaTexture(nextIndex);
    }

    // �O�̃p�m���}�ɐ؂�ւ�
    public void PreviousPanorama()
    {
        if (panoramaTextures == null || panoramaTextures.Length <= 1) return;

        int prevIndex = (currentTextureIndex - 1 + panoramaTextures.Length) % panoramaTextures.Length;
        SetPanoramaTexture(prevIndex);
    }
}