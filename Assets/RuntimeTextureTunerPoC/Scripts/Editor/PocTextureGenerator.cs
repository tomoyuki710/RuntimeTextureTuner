using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// PoC 用のテストパターンを生成する。すべて手続き的に描くため、外部の素材を使わない。
    /// </summary>
    internal static class PocTextureGenerator
    {
        public const string TextureFolder = "Assets/RuntimeTextureTunerPoC/Textures";
        public const string CheckerName = "Poc_Checker";
        public const string CheckerNoMipName = "Poc_CheckerNoMip";
        public const string LinesName = "Poc_Lines";
        public const string GridName = "Poc_Grid";
        public const string TextName = "Poc_Text";
        public const string NoiseName = "Poc_Noise";
        public const string NoiseNormalName = "Poc_NoiseNormal";
        public const string MipColorsName = "Poc_MipColors";

        private const int Size = 1024;
        private const float NormalStrength = 6f;

        private static readonly Color32 Black = new Color32(0, 0, 0, 255);
        private static readonly Color32 White = new Color32(255, 255, 255, 255);

        private static readonly (string Text, int Scale)[] TextLines =
        {
            ("RUNTIME TEXTURE TUNER", 8),
            ("MIP BIAS ANISO FILTER", 8),
            ("ABCDEFGHIJKLMNOPQRSTUVWXYZ", 6),
            ("0123456789 THE QUICK BROWN FOX", 5),
            ("JUMPS OVER THE LAZY DOG 0123456789", 4),
            ("PACK MY BOX WITH FIVE DOZEN LIQUOR JUGS", 3),
            ("THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG 0123456789", 2),
            ("PACK MY BOX WITH FIVE DOZEN LIQUOR JUGS 0123456789", 2),
            ("THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG 0123456789 PACK MY BOX WITH FIVE DOZEN LIQUOR JUGS", 1),
            ("ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 ABCDEFGHIJKLMNOPQRSTUVWXYZ", 1),
        };

        // Mip レベルごとの色。実機でどの Mip が使われているかを色で見分ける。
        private static readonly Color32[] MipColors =
        {
            new Color32(230, 40, 40, 255),
            new Color32(40, 200, 40, 255),
            new Color32(40, 80, 240, 255),
            new Color32(240, 220, 40, 255),
            new Color32(230, 40, 230, 255),
            new Color32(40, 220, 220, 255),
            new Color32(255, 255, 255, 255),
            new Color32(255, 140, 20, 255),
            new Color32(128, 128, 128, 255),
        };

        public static void GenerateAll()
        {
            Directory.CreateDirectory(TextureFolder);

            WritePng(CheckerName, PaintChecker);
            WritePng(CheckerNoMipName, PaintChecker);
            WritePng(LinesName, PaintLines);
            WritePng(GridName, PaintGrid);

            bool[] textMask = BuildTextMask();
            WritePng(TextName, (x, y) => PaintText(textMask, x, y));

            float[] heights = BuildHeights();
            WritePng(NoiseName, (x, y) => PaintNoise(heights, x, y));
            WritePng(NoiseNormalName, (x, y) => PaintNormal(heights, x, y));

            AssetDatabase.Refresh();

            ConfigureImporter(CheckerName, true, false);
            ConfigureImporter(CheckerNoMipName, false, false);
            ConfigureImporter(LinesName, true, false);
            ConfigureImporter(GridName, true, false);
            ConfigureImporter(TextName, true, false);
            ConfigureImporter(NoiseName, true, false);
            ConfigureImporter(NoiseNormalName, true, true);

            CreateMipColorTexture();
            AssetDatabase.SaveAssets();
        }

        public static Texture2D Load(string textureName)
        {
            string pngPath = $"{TextureFolder}/{textureName}.png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (texture != null)
            {
                return texture;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureName}.asset");
        }

        private static void WritePng(string textureName, Func<int, int, Color32> painter)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = painter(x, y);
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            File.WriteAllBytes($"{TextureFolder}/{textureName}.png", png);
        }

        private static void ConfigureImporter(string textureName, bool hasMipmaps, bool isNormalMap)
        {
            string path = $"{TextureFolder}/{textureName}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !isNormalMap;
            importer.mipmapEnabled = hasMipmaps;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.isReadable = false;

            // mipMapBias は未指定（-100）のままにし、実行時の実効値を実機で記録する。
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = Size;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }

        private static Color32 PaintChecker(int x, int y)
        {
            const int CellSize = 4;
            bool isWhite = ((x / CellSize) + (y / CellSize)) % 2 == 0;
            return isWhite ? White : Black;
        }

        private static Color32 PaintLines(int x, int y)
        {
            const int HalfPeriod = 4;
            bool isWhite = ((x + y) / HalfPeriod) % 2 == 0;
            return isWhite ? White : Black;
        }

        private static Color32 PaintGrid(int x, int y)
        {
            const int Spacing = 16;
            const int MajorSpacing = 128;
            bool isMajorLine = x % MajorSpacing < 2 || y % MajorSpacing < 2;
            if (isMajorLine)
            {
                return new Color32(255, 200, 40, 255);
            }

            bool isLine = x % Spacing == 0 || y % Spacing == 0;
            return isLine ? White : new Color32(30, 40, 60, 255);
        }

        private static bool[] BuildTextMask()
        {
            const int BlockHeight = Size / 2;
            const int Margin = 8;
            var mask = new bool[Size * Size];
            for (int block = 0; block < 2; block++)
            {
                int top = block * BlockHeight + Margin;
                foreach ((string text, int scale) in TextLines)
                {
                    DrawText(mask, text, Margin, top, scale);
                    top += (PocBitmapFont.GlyphHeight + 2) * scale + 6;
                }
            }

            return mask;
        }

        private static void DrawText(bool[] mask, string text, int left, int top, int scale)
        {
            for (int index = 0; index < text.Length; index++)
            {
                int glyphLeft = left + index * PocBitmapFont.Advance * scale;
                DrawGlyph(mask, text[index], glyphLeft, top, scale);
            }
        }

        private static void DrawGlyph(bool[] mask, char character, int left, int top, int scale)
        {
            int width = PocBitmapFont.GlyphWidth * scale;
            int height = PocBitmapFont.GlyphHeight * scale;
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int x = left + dx;
                    int row = top + dy;
                    bool isInside = x < Size && row < Size;
                    if (isInside && PocBitmapFont.IsSet(character, dx / scale, dy / scale))
                    {
                        // マスクは上端を0行目として持つ。
                        mask[row * Size + x] = true;
                    }
                }
            }
        }

        private static Color32 PaintText(bool[] mask, int x, int y)
        {
            // テクスチャは下端が y = 0 なので上下を反転する。
            int row = Size - 1 - y;
            bool isInk = mask[row * Size + x];
            bool isInvertedBlock = row >= Size / 2;
            return isInk != isInvertedBlock ? Black : White;
        }

        private static float[] BuildHeights()
        {
            var heights = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    heights[y * Size + x] = SampleFractalNoise(x, y);
                }
            }

            return heights;
        }

        private static float SampleFractalNoise(int x, int y)
        {
            const int Octaves = 6;
            float frequency = 4f / Size;
            float amplitude = 0.5f;
            float sum = 0f;
            for (int octave = 0; octave < Octaves; octave++)
            {
                sum += Mathf.PerlinNoise(x * frequency + octave * 17.3f, y * frequency + octave * 5.1f) * amplitude;
                frequency *= 2f;
                amplitude *= 0.5f;
            }

            return Mathf.Clamp01(sum);
        }

        private static Color32 PaintNoise(float[] heights, int x, int y)
        {
            float height = heights[y * Size + x];
            Color dark = new Color(0.22f, 0.20f, 0.18f);
            Color bright = new Color(0.78f, 0.74f, 0.66f);
            float contrast = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.75f, height));
            return Color.Lerp(dark, bright, contrast);
        }

        private static Color32 PaintNormal(float[] heights, int x, int y)
        {
            float left = heights[y * Size + ((x - 1 + Size) % Size)];
            float right = heights[y * Size + ((x + 1) % Size)];
            float down = heights[((y - 1 + Size) % Size) * Size + x];
            float up = heights[((y + 1) % Size) * Size + x];
            var normal = new Vector3((left - right) * NormalStrength, (down - up) * NormalStrength, 1f / Size * 16f);
            normal.Normalize();
            return new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
        }

        private static void CreateMipColorTexture()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            texture.name = MipColorsName;
            for (int mip = 0; mip < texture.mipmapCount; mip++)
            {
                texture.SetPixels32(BuildMipPixels(mip), mip);
            }

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 1;

            // Mip を再計算せず、読み取り不可にして確定する。
            texture.Apply(false, true);

            string path = $"{TextureFolder}/{MipColorsName}.asset";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(texture, path);
                return;
            }

            // 既存アセットの GUID を保つため、中身だけを差し替える。
            EditorUtility.CopySerialized(texture, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Color32[] BuildMipPixels(int mip)
        {
            const int CellsPerSide = 8;
            int mipSize = Mathf.Max(1, Size >> mip);
            Color32 baseColor = MipColors[Mathf.Min(mip, MipColors.Length - 1)];
            Color32 shadedColor = Color.Lerp(baseColor, Color.black, 0.3f);
            int cellSize = Mathf.Max(1, mipSize / CellsPerSide);
            var pixels = new Color32[mipSize * mipSize];
            for (int y = 0; y < mipSize; y++)
            {
                for (int x = 0; x < mipSize; x++)
                {
                    bool isShaded = mipSize >= CellsPerSide && ((x / cellSize) + (y / cellSize)) % 2 == 1;
                    pixels[y * mipSize + x] = isShaded ? shadedColor : baseColor;
                }
            }

            return pixels;
        }
    }
}
