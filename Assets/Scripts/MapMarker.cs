using UnityEngine;

[System.Serializable]
public class MapMarker
{
    public int pointNumber;        // �|�C���g�ԍ�
    public Vector2 position;       // x, y���W
    public string panoramaPath;    // �g�p����p�m���}JPG�̃p�X�iResources���j
    public string thumbnailPath;   // �g�p����T���l�C��JPG�̃p�X�iResources���j

    // �ʒu�����ŏ���������R���X�g���N�^
    public MapMarker(int number, Vector2 pos)
    {
        pointNumber = number;
        position = pos;
        panoramaPath = "";
        thumbnailPath = "";
    }

    // �S�f�[�^�ŏ���������R���X�g���N�^
    public MapMarker(int number, Vector2 pos, string panorama, string thumbnail)
    {
        pointNumber = number;
        position = pos;
        panoramaPath = panorama;
        thumbnailPath = thumbnail;
    }
}