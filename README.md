# VR_Viewer

MetaQuest3のコントローラーを使用したVRポインターシステムを実装したビューワーアプリケーション。

## 機能

- VRコントローラーからのレーザーポインタービジュアル表示
- インタラクティブなパネル操作システム
- エディタ拡張によるパネル管理

## セットアップ手順

1. Camera Rigオブジェクトに `PanelVisibilityController` コンポーネントをアタッチします
2. `LeftControllerAnchor` と `RightControllerAnchor` を設定します
3. レーザーポインタープレハブ（`Assets/Resources/Prefabs/LaserPointer.prefab`）を割り当てます
4. インタラクティブなパネルを追加します

## 実装内容

### VRLaserPointer.cs

コントローラーからのレーザービジュアルを制御するコンポーネント。以下の特徴があります：

- LineRendererを使用した先細りレーザービーム
- レーザーが当たった位置にドットを表示
- カスタマイズ可能な色と大きさ

### PanelVisibilityController.cs 

パネルの管理と表示・非表示を制御するメインコンポーネント。以下の特徴があります：

- 複数パネルの管理
- コントローラーポインターとの連携
- パネルの表示・非表示切り替え機能

### PanelVisibilityControllerEditor.cs

Unityエディタ上での設定を容易にするエディタ拡張。以下の特徴があります：

- 直感的なインターフェース
- パネル追加機能
- 一括非表示機能

## 注意事項

- このシステムはUnity 2021.3以降で開発されています
- VRコントローラーには対応するインプットシステムが必要です

## 作者

- YaAkiyama
