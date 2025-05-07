using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class Panorama360Controller : MonoBehaviour
{
    [SerializeField] private Material panoramaMaterial;
    [SerializeField] private float changeInterval = 0f; // 0の場合は自動変更なし、正の値で秒数指定

    private Texture2D[] panoramaTextures;
    private int currentTextureIndex = 0;
    private float timer = 0f;

    void Start()
    {
        // Resources/Image/360フォルダから画像を読み込む
        LoadPanoramaTextures();

        // 最初の画像を設定
        if (panoramaTextures != null && panoramaTextures.Length > 0)
        {
            SetPanoramaTexture(0);
        }

        // カメラのクリアフラグをSkyboxに設定
        Camera.main.clearFlags = CameraClearFlags.Skybox;

        // スカイボックスにマテリアルを設定
        RenderSettings.skybox = panoramaMaterial;
    }

    void Update()
    {
        // 自動切り替えが設定されている場合
        if (changeInterval > 0 && panoramaTextures.Length > 1)
        {
            timer += Time.deltaTime;
            if (timer >= changeInterval)
            {
                timer = 0f;
                NextPanorama();
            }
        }

        // ここにコントローラーの入力などで画像を切り替える処理を追加できます
    }

    private void LoadPanoramaTextures()
    {
        // Resources/Image/360フォルダ内のすべての画像を読み込む
        Object[] loadedTextures = Resources.LoadAll("Image/360", typeof(Texture2D));
        panoramaTextures = new Texture2D[loadedTextures.Length];

        for (int i = 0; i < loadedTextures.Length; i++)
        {
            panoramaTextures[i] = (Texture2D)loadedTextures[i];
        }

        Debug.Log($"読み込まれたパノラマ画像: {panoramaTextures.Length}枚");
    }

    // 特定のインデックスのパノラマを表示
    public void SetPanoramaTexture(int index)
    {
        if (panoramaTextures == null || panoramaTextures.Length == 0) return;

        // インデックスの範囲を確認
        if (index >= 0 && index < panoramaTextures.Length)
        {
            currentTextureIndex = index;

            // マテリアルにテクスチャを設定
            panoramaMaterial.SetTexture("_MainTex", panoramaTextures[currentTextureIndex]);

            // シェーダーに応じて他のプロパティも設定
            // パノラマ画像のマッピングを設定（水平方向の画像の場合）
            panoramaMaterial.SetFloat("_Mapping", 1); // 1 = 球面マッピング
            panoramaMaterial.SetFloat("_ImageType", 0); // 0 = 360度（水平）
            panoramaMaterial.SetFloat("_Layout", 0); // 0 = 通常、1 = ミラー等

            Debug.Log($"パノラマ画像を切り替え: {panoramaTextures[currentTextureIndex].name}");
        }
    }

    // 次のパノラマに切り替え
    public void NextPanorama()
    {
        if (panoramaTextures == null || panoramaTextures.Length <= 1) return;

        int nextIndex = (currentTextureIndex + 1) % panoramaTextures.Length;
        SetPanoramaTexture(nextIndex);
    }

    // 前のパノラマに切り替え
    public void PreviousPanorama()
    {
        if (panoramaTextures == null || panoramaTextures.Length <= 1) return;

        int prevIndex = (currentTextureIndex - 1 + panoramaTextures.Length) % panoramaTextures.Length;
        SetPanoramaTexture(prevIndex);
    }
}