# Changelog

## [Unreleased]

- パッケージ内に英語版の README（`README.en.md`）を追加
- 調整パネルで、長いアセットのパスがパネルの枠からはみ出す問題を修正

## [0.1.0] - 2026-11-26

- テクスチャのサンプラー設定（Mip Map Bias、Aniso Level、Filter Mode）を実行時に上書きし、元の値との切り替えと復元を行う機能
- Renderer からマテリアルとテクスチャを列挙する機能と、レイと Bounds の交差による対象の選択
- HMD 内の調整パネル（Bias、Aniso、Filter、Original / Adjusted の切り替え、Keep、Reset、パネルの移動）
- 調整用ビルドの作成と、ビルド用のシーンコピーへのテクスチャ対応表の追加
- 調整結果の JSON への保存と、Editor での確認、`TextureImporter` への反映と復元
