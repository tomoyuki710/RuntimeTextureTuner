# 開発用プロジェクトについて

このリポジトリは、パッケージ `com.tn.runtimetexturetuner` と、その開発・動作確認に使う Unity プロジェクトを含んでいます。パッケージを使うだけであれば、この文書の手順は不要です。[README.md](README.md) を参照してください。

## 必要な環境

- Unity 6000.3.27f2

## 別途導入が必要な SDK

次の SDK は再頒布が許可されていないため、このリポジトリには含まれていません。各自で取得して `Packages/` フォルダに配置してください。これらがない状態でプロジェクトを開くと、コンパイルエラーになります。

| SDK | パッケージ名 | バージョン | 配置先 |
|---|---|---|---|
| PICO Unity Integration SDK | `com.unity.xr.picoxr` | 3.4.0 | `Packages/PICO-Unity-Integration-SDK-release_3.4.0/` |
| PICO Unity Live Preview Plugin | `com.unity.pico.livepreview` | 1.0.5 | `Packages/Unity Live Preview Plugin-1.0.5-20250211/` |

取得元:

- PICO Unity Integration SDK: <https://github.com/Pico-Developer/PICO-Unity-Integration-SDK>（`release_3.4.0`）
- PICO Unity Live Preview Plugin: [PICO Developer](https://developer.picoxr.com/resources/) のダウンロードページ

手順:

1. このリポジトリを clone します。
2. 上記の SDK をダウンロードして展開し、`package.json` がフォルダ直下に来るように表の配置先へ置きます。
3. Unity Hub からプロジェクトを開きます。

配置先のフォルダ名は `.gitignore` の除外パターン（`PICO-Unity-Integration-SDK-*`、`Unity Live Preview Plugin-*`）に合わせています。別の名前にする場合は、誤ってコミットしないように `.gitignore` を調整してください。

## 動作確認用のサンプル

`Assets/RuntimeTextureTunerPoC/` に、動作確認用のシーンとテストパターンがあります。メニューは `Tools > Runtime Texture Tuner > PoC` です。

| メニュー | 内容 |
|---|---|
| Generate Content | テストパターン、マテリアル、シーンを作り直す |
| Build Tuning APK | 調整機能つきの APK を `Builds/PoC/` に作る |
| Build APK Without Tuner (for verification) | 調整機能なしの APK を `Builds/PoCWithoutTuner/` に作る |

サンプルのシーンでは、左スティックで移動できます（XR Interaction Toolkit を使用）。パッケージ本体は XR Interaction Toolkit に依存しません。

## OpenXR Plugin について

`com.unity.xr.openxr` 1.15.0 は `Packages/manifest.json` に記載しているため、Unity が公式レジストリから自動で取得します。

## Unity CLI Loop について

開発には [Unity CLI Loop](https://github.com/hatayama/unity-cli-loop) を使っています。`.uloop/` と `.claude/skills/` は Unity CLI Loop が生成するファイルのため、リポジトリには含めていません。
