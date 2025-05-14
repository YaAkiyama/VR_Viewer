using System.IO; // Path.ChangeExtensionのため
using UnityEngine;
using System.Collections.Generic;

public class Panorama360Controller : MonoBehaviour
{
    [SerializeField] private Material panoramaMaterial;
    [SerializeField] private float changeInterval = 0f; // 0の場合は自動変更なし、正の値で秒数指定

    // パノラマテクスチャ管理（マーカーと連携するため変更）
    private Dictionary<string, Texture2D> panoramaTextureDict = new Dictionary<string, Texture2D>();
    private string currentTexturePath = "";
    private float timer = 0f;

    // マーカーマネージャーへの参照
    private MapMarkerManager markerManager;

    void Start()
    {
        // マーカーマネージャーへの参照を取得
        markerManager = FindFirstObjectByType<MapMarkerManager>();

        if (markerManager == null)
        {
            Debug.LogError("MapMarkerManagerが見つかりません。先にMapMarkerManagerをセットアップしてください。");
            return;
        }

        // カメラのクリアフラグをSkyboxに設定
        Camera.main.clearFlags = CameraClearFlags.Skybox;

        // スカイボックスにマテリアルを設定
        RenderSettings.skybox = panoramaMaterial;
        // デバッグ: Image/360ディレクトリの内容をリスト表示
        Object[] allTextures = Resources.LoadAll("Image/360", typeof(Texture2D));
        Debug.Log($"Resources/Image/360内の画像数: {allTextures.Length}");
        foreach (var tex in allTextures)
        {
            Debug.Log($"見つかった画像: {tex.name}");
        }

        // テスト: 画像を直接名前でロード
        Texture2D testTexture = Resources.Load<Texture2D>("Image/360/R0010042");
        if (testTexture != null)
        {
            Debug.Log("テスト画像のロードに成功しました: R0010042");
        }
        else
        {
            Debug.LogError("テスト画像のロードに失敗しました: R0010042");
        }
    }

    // クラス内のどこか、Start()メソッドの外に追加
#if UNITY_EDITOR
    [ContextMenu("テスト: 全パノラマをロード")]
    private void EditorTestLoadAll()
    {
        TestLoadAllPanoramas();
    }
#endif

    void Update()
    {
        // 自動切り替えが設定されている場合
        if (changeInterval > 0 && panoramaTextureDict.Count > 1 && markerManager != null)
        {
            timer += Time.deltaTime;
            if (timer >= changeInterval)
            {
                timer = 0f;
                // 次のマーカーへ移動（マーカーマネージャー経由で実装）
                markerManager.SelectNextMarker();
            }
        }
    }

    // 特定のパスのパノラマ画像をロードするメソッド
    public void LoadPanoramaByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        // パスからResources部分を取り除く処理
        if (path.StartsWith("Assets/Resources/"))
        {
            path = path.Substring("Assets/Resources/".Length);
        }

        // 拡張子を削除（この行を追加）
        path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));

        // パスが現在のものと同じなら何もしない
        if (path == currentTexturePath && panoramaTextureDict.ContainsKey(path)) return;

        // 既にロード済みの場合はキャッシュから取得
        if (panoramaTextureDict.ContainsKey(path))
        {
            SetPanoramaTexture(panoramaTextureDict[path]);
            currentTexturePath = path;
            return;
        }

        // 新しくロードする場合
        Texture2D texture = Resources.Load<Texture2D>(path);

        // 見つからない場合、拡張子を小文字に変えて試す
        if (texture == null && path.ToLower() != path)
        {
            string lowercasePath = Path.ChangeExtension(path, Path.GetExtension(path).ToLower());
            texture = Resources.Load<Texture2D>(lowercasePath);

            if (texture != null)
            {
                Debug.LogWarning($"大文字小文字が異なるパスで画像が見つかりました: {lowercasePath}");
                path = lowercasePath;
            }
        }

        if (texture != null)
        {
            // キャッシュに追加
            panoramaTextureDict[path] = texture;

            // テクスチャを設定
            SetPanoramaTexture(texture);
            currentTexturePath = path;

            Debug.Log($"パノラマ画像をロード: {path}");
        }
        else
        {
            Debug.LogError($"パノラマ画像が見つかりません: {path}");
            Debug.LogWarning($"検索パス: {path}");
            Debug.LogWarning("Resourcesフォルダからの相対パスを使用してください（例: 'Image/360/example.jpg'）");

            // リソースディレクトリ内の利用可能なファイルをログに出力
            Object[] allTextures = Resources.LoadAll("Image/360", typeof(Texture2D));
            if (allTextures.Length > 0)
            {
                Debug.LogWarning("利用可能なパノラマ画像一覧:");
                foreach (var tex in allTextures)
                {
                    Debug.LogWarning($" - Image/360/{tex.name}");
                }
            }
        }
    }

    // テクスチャを直接設定するプライベートメソッド
    private void SetPanoramaTexture(Texture2D texture)
    {
        if (texture == null) return;

        // マテリアルにテクスチャを設定
        panoramaMaterial.SetTexture("_MainTex", texture);

        // シェーダープロパティの設定
        panoramaMaterial.SetFloat("_Mapping", 1); // 1 = 球面マッピング
        panoramaMaterial.SetFloat("_ImageType", 0); // 0 = 360度（水平）
        panoramaMaterial.SetFloat("_Layout", 0); // 0 = 通常
    }

    // 現在表示中のパノラマパスを取得
    public string GetCurrentPanoramaPath()
    {
        return currentTexturePath;
    }

    // パノラマキャッシュをクリア（メモリ解放用）
    public void ClearPanoramaCache()
    {
        panoramaTextureDict.Clear();
        currentTexturePath = "";

        // パノラマテクスチャをnullに設定
        panoramaMaterial.SetTexture("_MainTex", null);
    }
    // Panorama360Controllerクラスにテストメソッドを追加
    public void TestLoadAllPanoramas()
    {
        Debug.Log("全パノラマ画像のロードテスト開始");

        // ResourcesフォルダのImage/360ディレクトリ内のすべてのテクスチャをロード
        Object[] textures = Resources.LoadAll("Image/360", typeof(Texture2D));

        if (textures.Length == 0)
        {
            Debug.LogError("Image/360ディレクトリにテクスチャが見つかりません");
            return;
        }

        Debug.Log($"テクスチャが {textures.Length} 個見つかりました:");

        // 最初のテクスチャを表示
        if (textures.Length > 0)
        {
            Texture2D firstTexture = (Texture2D)textures[0];
            panoramaMaterial.SetTexture("_MainTex", firstTexture);
            panoramaMaterial.SetFloat("_Mapping", 1); // 1 = 球面マッピング
            panoramaMaterial.SetFloat("_ImageType", 0); // 0 = 360度（水平）
            panoramaMaterial.SetFloat("_Layout", 0); // 0 = 通常

            Debug.Log($"テスト: 最初のテクスチャを表示: {firstTexture.name}");
        }

        // 全テクスチャの情報を出力
        foreach (Texture2D tex in textures)
        {
            Debug.Log($"テクスチャ: {tex.name}, サイズ: {tex.width}x{tex.height}");
        }
    }
}