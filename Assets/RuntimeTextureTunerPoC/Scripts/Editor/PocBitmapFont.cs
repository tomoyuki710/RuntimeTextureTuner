using System;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// テストパターン用の 5x7 ドットフォント（英大文字と数字）。フォントアセットに依存しない。
    /// </summary>
    internal static class PocBitmapFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;

        /// <summary>
        /// 1文字ぶんの送り幅（文字間の余白を含む）。
        /// </summary>
        public const int Advance = 6;

        // 1文字につき7行。各行は16進2桁で、bit4 が左端。
        private static readonly string[] LetterGlyphs =
        {
            "0E11111F111111", "1E11111E11111E", "0E11101010110E", "1E11111111111E", "1F10101E10101F",
            "1F10101E101010", "0E11101711110F", "1111111F111111", "0E04040404040E", "0702020202120C",
            "11121418141211", "1010101010101F", "111B1515111111", "11191513111111", "0E11111111110E",
            "1E11111E101010", "0E11111115120D", "1E11111E141211", "0F10100E01011E", "1F040404040404",
            "1111111111110E", "11111111110A04", "11111115151B11", "11110A040A1111", "11110A04040404",
            "1F01020408101F",
        };

        private static readonly string[] DigitGlyphs =
        {
            "0E11131519110E", "040C040404040E", "0E11010204081F", "1F02040201110E", "02060A121F0202",
            "1F101E0101110E", "0608101E11110E", "1F010204080808", "0E11110E11110E", "0E11110F01020C",
        };

        /// <summary>
        /// 指定した文字の、指定した位置のドットが点灯しているかどうか。未対応の文字は空白として扱う。
        /// </summary>
        public static bool IsSet(char character, int column, int row)
        {
            string glyph = FindGlyph(character);
            if (glyph == null)
            {
                return false;
            }

            int rowBits = Convert.ToInt32(glyph.Substring(row * 2, 2), 16);
            int mask = 1 << (GlyphWidth - 1 - column);
            return (rowBits & mask) != 0;
        }

        private static string FindGlyph(char character)
        {
            if (character >= 'A' && character <= 'Z')
            {
                return LetterGlyphs[character - 'A'];
            }

            if (character >= '0' && character <= '9')
            {
                return DigitGlyphs[character - '0'];
            }

            return null;
        }
    }
}
