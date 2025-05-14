using UnityEngine;

[System.Serializable]
public class MapMarker
{
    public int pointNumber;        // ポイント番号
    public Vector2 position;       // x, y座標
    public string panoramaPath;    // 使用するパノラマJPGのパス（Resources内）
    public string thumbnailPath;   // 使用するサムネイルJPGのパス（Resources内）

    // 位置だけで初期化するコンストラクタ
    public MapMarker(int number, Vector2 pos)
    {
        pointNumber = number;
        position = pos;
        panoramaPath = "";
        thumbnailPath = "";
    }

    // 全データで初期化するコンストラクタ
    public MapMarker(int number, Vector2 pos, string panorama, string thumbnail)
    {
        pointNumber = number;
        position = pos;
        panoramaPath = panorama;
        thumbnailPath = thumbnail;
    }
}