# VR_Viewer - MetaQuest3用VRポインター機能

## 概要
このリポジトリにはMetaQuest3向けのVRビューワーアプリに関するコードが含まれています。特にコントローラーによるレーザーポインター機能を提供し、MapCanvasとThumbnailCanvasの両方で直感的なインタラクションを可能にします。

## 主な機能
- MetaQuest3のホーム画面のようなレーザーポインター操作
- コントローラーの位置からのレーザー表示
- UI要素（ボタン、スクロールなど）とのインタラクション
- ThumbnailCanvasでのスクロール対応
- 視野角・パネル表示の制御

## セットアップ方法

### 1. コントローラーにレーザーポインターをアタッチ
Camera Rigの子オブジェクトであるLeftControllerAnchorとRightControllerAnchorに、LaserPointerプレハブをアタッチします：

1. Assets/Resources/Prefabs/LaserPointer.prefabをシーンのLeftControllerAnchorとRightControllerAnchorの子オブジェクトとしてインスタンス化
2. 各LaserPointerのInspectorで以下を設定:
   - 対象のCanvasを設定 (MapCanvas, ThumbnailCanvas)
   - UIレイキャスターを設定
   - パネル可視性コントローラーを設定

### 2. ThumbnailCanvasのインタラクション設定
ThumbnailCanvasをレーザーポインターの対象に追加するには、再生モード時に以下のいずれかの方法を使います：

1. VRLaserPointerのInspectorから「ThumbnailCanvasを検索して追加」ボタンをクリック
2. 「追加するCanvas」にThumbnailCanvasをドラッグ＆ドロップし、「Canvasを追加」ボタンをクリック

## コンポーネント説明

### VRLaserPointer.cs
レーザーポインターの主要な機能を提供します。レーザーの描画、UIとの交差判定、ポインターイベントの処理、ドラッグ操作などを制御します。

#### 主なプロパティ
- `maxRayDistance`: レーザーの最大検出距離
- `maxVisualDistance`: 表示されるレーザーの長さ
- `rayWidth`: レーザーの幅
- `rayColor`: レーザーの色
- `dotScale`: ポインタードットのサイズ
- `dotColor`: 通常時のドットの色
- `dotPressedColor`: 押下時のドットの色
- `triggerAction`: トリガーボタンの入力アクション
- `aButtonAction`: Aボタンの入力アクション
- `targetCanvasList`: 対象のCanvas一覧

### PanelVisibilityController.cs
パネルの表示/非表示を制御します。視野角に基づいてパネルの透明度を調整したり、Aボタンでパネルの表示/非表示を切り替えることができます。

### 使用方法
1. アプリを起動すると、コントローラーからレーザーが表示されます
2. レーザーをUIに向けると、ポインタードットが表示されます
3. トリガーボタンを押すと、ポインタードットがある位置でクリックイベントが発生します
4. ThumbnailCanvas上でトリガーを押しながらドラッグすると、スクロールできます
5. Aボタンを押すと、パネルの表示/非表示を切り替えることができます

## 注意事項
- Unity 2022.3以降を推奨
- Meta XR SDK 60.0以降が必要
- Input Systemのセットアップが必要

## 更新履歴
- 2025-05-13: ThumbnailCanvasに対してもインタラクションをサポート
- 2025-05-10: パネル可視性コントローラーを改良
- 2025-04-28: 初期バージョン作成
