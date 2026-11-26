# Realtime Texture Tuner

## インストール

Unity の Package Manager で「Install package from git URL...」を選び、次の URL を入力します。

```
https://github.com/tomoyuki710/RealtimeTextureTuner.git?path=/Packages/com.tn.realtimetexturetuner
```

## PICO SDK との関係

このパッケージは PICO Unity Integration SDK を依存パッケージとして宣言していません（PICO SDK は Unity のレジストリで配布されていないためです）。

PICO SDK（`com.unity.xr.picoxr`）がプロジェクトに導入されている場合だけ、スクリプト定義シンボル `REALTIMETEXTURETUNER_PICO` が有効になります。PICO SDK に依存するコードは次のように囲みます。

```csharp
#if REALTIMETEXTURETUNER_PICO
// PICO SDK の API を使う処理
#endif
```
