# Changelog

## [Unreleased]

- 調整パネルに、シーン内のアニメーション（Animator、タイムライン、パーティクル、Animation）を止める/再開するボタンを追加。アニメーションで動いている物のテクスチャを調整するため。パネルを閉じると再開する
- パッケージの依存に Animation、Director、Particle System のモジュールを追加
- 調整パネルが、プレイヤーの移動（カメラの親の移動）に追従するように変更。タイムラインなどでプレイヤーが移動してもパネルが置き去りにならない
- レイで対象を選ぶたびに Renderer の一覧を作り直すように変更。パネルを開いた後に表示された物も選べる
- `MaterialPropertyBlock` でテクスチャが差し替えられている Renderer で、差し替え後のテクスチャを一覧に出して調整できるように変更
- 調整パネルの処理を `Update` から `LateUpdate`（実行順 10000）に移動
- パッケージ内に英語版の README（`README.en.md`）を追加
- 調整パネルで、長いアセットのパスがパネルの枠からはみ出す問題を修正

## [0.1.0] - 2026-11-26

- テクスチャのサンプラー設定（Mip Map Bias、Aniso Level、Filter Mode）を実行時に上書きし、元の値との切り替えと復元を行う機能
- Renderer からマテリアルとテクスチャを列挙する機能と、レイと Bounds の交差による対象の選択
- HMD 内の調整パネル（Bias、Aniso、Filter、Original / Adjusted の切り替え、Keep、Reset、パネルの移動）
- 調整用ビルドの作成と、ビルド用のシーンコピーへのテクスチャ対応表の追加
- 調整結果の JSON への保存と、Editor での確認、`TextureImporter` への反映と復元
