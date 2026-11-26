using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// 現在の設定値と診断情報を表示する、読み取り専用の World Space パネル。
    /// 入力は受け付けないため、EventSystem や Canvas を必要としない。
    /// </summary>
    internal sealed class PocHud
    {
        private const float DistanceFromHead = 1.5f;
        private const float HeightOffset = -0.1f;
        private const float PanelWidth = 1.1f;
        private const float PanelHeight = 0.8f;
        private const float TextMargin = 0.03f;
        private const int FontSize = 64;
        private const float CharacterSize = 0.0042f;

        private readonly GameObject _root;
        private readonly TextMesh _textMesh;

        private PocHud(GameObject root, TextMesh textMesh)
        {
            _root = root;
            _textMesh = textMesh;
        }

        public bool IsVisible => _root.activeSelf;

        public static PocHud Create()
        {
            var root = new GameObject(nameof(PocHud));
            CreateBackground(root.transform);
            TextMesh textMesh = CreateText(root.transform);
            return new PocHud(root, textMesh);
        }

        public void SetText(string text)
        {
            _textMesh.text = text;
        }

        /// <summary>
        /// 頭の正面（水平方向）にパネルを置いて表示する。以後は頭に追従しない。
        /// </summary>
        public void ShowInFrontOf(Transform head)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 position = head.position + forward * DistanceFromHead + Vector3.up * HeightOffset;
            _root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private static TextMesh CreateText(Transform parent)
        {
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            float left = -PanelWidth * 0.5f + TextMargin;
            float top = PanelHeight * 0.5f - TextMargin;
            textObject.transform.localPosition = new Vector3(left, top, 0f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.font = font;
            textMesh.fontSize = FontSize;
            textMesh.characterSize = CharacterSize;
            textMesh.anchor = TextAnchor.UpperLeft;
            textMesh.alignment = TextAlignment.Left;
            textMesh.color = Color.white;
            textObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return textMesh;
        }

        private static void CreateBackground(Transform parent)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                // 背景がなくても文字は表示できる。
                return;
            }

            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Background";
            Object.Destroy(background.GetComponent<Collider>());
            background.transform.SetParent(parent, false);
            background.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            background.transform.localScale = new Vector3(PanelWidth, PanelHeight, 1f);

            var material = new Material(shader);
            material.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.03f, 1f));
            background.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
