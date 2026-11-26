using System;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// パネル上の部品（ラベル、ボタン、スライダーの溝やつまみ）。四角形の板と文字で構成する。
    /// Canvas、EventSystem、Collider を使わず、当たり判定はパネルのローカル座標で行う。
    /// </summary>
    internal sealed class PanelWidget
    {
        private const int FontSize = 64;
        private const float TextPadding = 0.008f;
        private const float HoverBrightness = 0.18f;

        private static Mesh _quadMesh;

        private readonly GameObject _root;
        private readonly Material _material;
        private readonly TextMesh _textMesh;
        private Color _color;
        private bool _isHovered;

        /// <summary>
        /// 部品を作る。rect はパネル中心を原点とするローカル座標（メートル）。
        /// 背景色の不透明度が 0 の場合は板を作らず、文字だけにする。
        /// </summary>
        public PanelWidget(Transform parent, string name, Rect rect, Color color, float characterSize, TextAnchor anchor, int layer)
        {
            Rect = rect;
            _color = color;
            _root = new GameObject(name);
            _root.transform.SetParent(parent, false);

            // 手前の層ほどわずかに利用者側へ出す。パネルは利用者から見て +Z 方向を向いている。
            _root.transform.localPosition = new Vector3(rect.center.x, rect.center.y, -0.001f * layer);

            if (color.a > 0f)
            {
                _material = TunerMaterials.CreateUnlit(color);
                CreateQuad(rect.size, layer);
            }

            _textMesh = CreateText(rect.size, characterSize, anchor, layer);
        }

        /// <summary>
        /// 押されたときの処理。設定すると当たり判定の対象になる。
        /// </summary>
        public Action OnPress { get; set; }

        /// <summary>
        /// 押したまま動かしている間の処理。引数は部品の左端を 0、右端を 1 とした横位置。
        /// </summary>
        public Action<float> OnDrag { get; set; }

        public Rect Rect { get; private set; }

        public bool IsInteractive => OnPress != null || OnDrag != null;

        public bool IsVisible => _root.activeSelf;

        public void SetVisible(bool isVisible)
        {
            _root.SetActive(isVisible);
        }

        public void SetText(string text)
        {
            _textMesh.text = text;
        }

        public void SetTextColor(Color color)
        {
            _textMesh.color = color;
        }

        public void SetColor(Color color)
        {
            _color = color;
            ApplyColor();
        }

        public void SetHovered(bool isHovered)
        {
            _isHovered = isHovered;
            ApplyColor();
        }

        /// <summary>
        /// 部品の中心の横位置を動かす（スライダーのつまみ用）。
        /// </summary>
        public void SetCenterX(float centerX)
        {
            Rect = new Rect(centerX - Rect.width * 0.5f, Rect.y, Rect.width, Rect.height);
            Vector3 position = _root.transform.localPosition;
            _root.transform.localPosition = new Vector3(centerX, position.y, position.z);
        }

        public bool Contains(Vector2 point)
        {
            return IsVisible && Rect.Contains(point);
        }

        public void Destroy()
        {
            if (_material != null)
            {
                UnityEngine.Object.Destroy(_material);
            }
        }

        private void ApplyColor()
        {
            if (_material == null)
            {
                return;
            }

            Color shown = _isHovered ? Color.Lerp(_color, Color.white, HoverBrightness) : _color;
            _material.color = shown;
        }

        private void CreateQuad(Vector2 size, int layer)
        {
            var quad = new GameObject("Quad");
            quad.transform.SetParent(_root.transform, false);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            quad.AddComponent<MeshFilter>().sharedMesh = GetQuadMesh();
            MeshRenderer quadRenderer = quad.AddComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = _material;
            quadRenderer.sortingOrder = layer;
            TunerMaterials.DisableLighting(quadRenderer);
        }

        private TextMesh CreateText(Vector2 size, float characterSize, TextAnchor anchor, int layer)
        {
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(_root.transform, false);
            textObject.transform.localPosition = GetTextPosition(size, anchor);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.font = font;
            textMesh.fontSize = FontSize;
            textMesh.characterSize = characterSize;
            textMesh.anchor = anchor;
            textMesh.alignment = anchor == TextAnchor.MiddleCenter ? TextAlignment.Center : TextAlignment.Left;
            textMesh.richText = false;
            textMesh.color = Color.white;

            MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
            textRenderer.sharedMaterial = font.material;
            textRenderer.sortingOrder = layer + 10;
            TunerMaterials.DisableLighting(textRenderer);
            return textMesh;
        }

        private static Vector3 GetTextPosition(Vector2 size, TextAnchor anchor)
        {
            float left = -size.x * 0.5f + TextPadding;
            float top = size.y * 0.5f - TextPadding;
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return new Vector3(left, top, -0.0005f);
                case TextAnchor.MiddleLeft:
                    return new Vector3(left, 0f, -0.0005f);
                default:
                    return new Vector3(0f, 0f, -0.0005f);
            }
        }

        private static Mesh GetQuadMesh()
        {
            if (_quadMesh != null)
            {
                return _quadMesh;
            }

            _quadMesh = new Mesh { name = "RuntimeTextureTunerQuad" };
            _quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            };
            _quadMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            _quadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            _quadMesh.RecalculateBounds();
            return _quadMesh;
        }
    }
}
