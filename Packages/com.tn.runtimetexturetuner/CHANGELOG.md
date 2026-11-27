# Changelog

## [0.2.0] - 2026-11-27

### 互換性のない変更

- `TextureUsage.HasPropertyBlock` を `TextureUsage.IsFromPropertyBlock` に変更。意味も「Renderer に MaterialPropertyBlock があるか」から「このテクスチャが MaterialPropertyBlock で設定されているか」に変わった
- `TextureTarget.HasPropertyBlockUsage` を `TextureTarget.IsSetByPropertyBlock` に変更（意味の変更は上と同じ）

### 追加

- 調整パネルに、シーン内のアニメーション（Animator、タイムライン、パーティクル、Animation）を止める/再開する `Pause anims` ボタンを追加。時間（`Time.timeScale`）は変えないため、移動や回転は止まらない。パネルを閉じると再開する
- `MaterialPropertyBlock` でテクスチャが差し替えられている Renderer で、差し替え後のテクスチャを一覧の先頭に出して調整できるようにした
- パッケージ内に英語版の README（`README.en.md`）を追加

### 変更

- 調整パネルが、プレイヤーの移動（カメラの親の移動）に追従するようにした。タイムラインなどでプレイヤーが移動してもパネルが置き去りにならない
- レイで対象を選ぶたびに Renderer の一覧を作り直すようにした。パネルを開いた後に表示された物も選べる
- 調整機能の毎フレームの処理を `Update` から `LateUpdate`（実行順 10000）に移動。`RuntimeTuner.OnTuningModeChanged` も `LateUpdate` の中で呼ばれる
- 調整パネルの上端の帯の表示を短くした
- パッケージの依存に Animation、Director、Particle System のモジュールを追加

### 修正

- 調整パネルで、長いアセットのパスがパネルの枠からはみ出す問題を修正

## [0.1.0] - 2026-11-26

- テクスチャのサンプラー設定（Mip Map Bias、Aniso Level、Filter Mode）を実行時に上書きし、元の値との切り替えと復元を行う機能
- Renderer からマテリアルとテクスチャを列挙する機能と、レイと Bounds の交差による対象の選択
- HMD 内の調整パネル（Bias、Aniso、Filter、Original / Adjusted の切り替え、Keep、Reset、パネルの移動）
- 調整用ビルドの作成と、ビルド用のシーンコピーへのテクスチャ対応表の追加
- 調整結果の JSON への保存と、Editor での確認、`TextureImporter` への反映と復元
