using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// PoC 用のマテリアルとシーンを生成する。Collider のない背景を再現するため、Collider は付けない。
    /// </summary>
    internal static class PocSceneBuilder
    {
        public const string ScenePath = "Assets/RuntimeTextureTunerPoC/Scenes/PocScene.unity";

        private const string MaterialFolder = "Assets/RuntimeTextureTunerPoC/Materials";
        private const string SceneFolder = "Assets/RuntimeTextureTunerPoC/Scenes";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        private const float FloorLength = 40f;
        private const float FloorCenterZ = 18f;
        private const float StripWidth = 4f;
        private const float WallHeight = 3f;
        private const float PanelDistance = 3f;
        private const float PanelSize = 0.7f;
        private const float PanelHeight = 1.5f;
        private const float MarkerInterval = 5f;
        private const float MoveSpeed = 2f;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        public static void Build()
        {
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(SceneFolder);
            AssetDatabase.Refresh();

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene openedPocScene = SceneManager.GetSceneByPath(ScenePath);
            bool isPocSceneOpen = openedPocScene.IsValid() && openedPocScene.isLoaded;

            // PoC シーンが開かれていればその中身を作り直す。開かれていなければ、
            // 開いているシーンには触れず、追加で開いた新規シーンに構築して保存する。
            Scene scene = isPocSceneOpen
                ? openedPocScene
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                ClearScene(scene);
                ConfigureLighting();
                CreateLight();
                CreateRig();
                CreateTargets();
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    Debug.LogError($"[RuntimeTextureTuner] Failed to save the PoC scene: {ScenePath}");
                }
            }
            finally
            {
                if (!isPocSceneOpen)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void ClearScene(Scene scene)
        {
            // PoC シーンはすべて生成物なので、作り直す前に中身を空にする。
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static void ConfigureLighting()
        {
            // ライトのベイクに依存しないよう、環境光は単色にする。
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.None;
        }

        private static void CreateRig()
        {
            var rig = new GameObject("PocRig");
            var cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(rig.transform, false);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraOffset.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();

            TrackedPoseDriver driver = cameraObject.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(
                new InputAction("Position", InputActionType.Value, "<XRHMD>/centerEyePosition", null, null, "Vector3"));
            driver.rotationInput = new InputActionProperty(
                new InputAction("Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", null, null, "Quaternion"));
            driver.ignoreTrackingState = true;

            XROrigin origin = rig.AddComponent<XROrigin>();
            origin.Origin = rig;
            origin.CameraFloorOffsetObject = cameraOffset;
            origin.Camera = camera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            CreateLocomotion(rig.transform, origin);
        }

        /// <summary>
        /// XR Interaction Toolkit の移動機能を追加する。左スティックで、頭の向きを基準に水平移動する。
        /// 床に Collider がないため、重力は無効にして落下しないようにする。
        /// </summary>
        private static void CreateLocomotion(Transform rig, XROrigin origin)
        {
            var locomotionObject = new GameObject("Locomotion");
            locomotionObject.transform.SetParent(rig, false);

            // LocomotionMediator は XRBodyTransformer を必要とし、自動で追加される。
            LocomotionMediator mediator = locomotionObject.AddComponent<LocomotionMediator>();
            locomotionObject.GetComponent<XRBodyTransformer>().xrOrigin = origin;

            var gravityObject = new GameObject("Gravity");
            gravityObject.transform.SetParent(locomotionObject.transform, false);
            GravityProvider gravity = gravityObject.AddComponent<GravityProvider>();
            gravity.mediator = mediator;
            gravity.useGravity = false;

            var moveObject = new GameObject("Move");
            moveObject.transform.SetParent(locomotionObject.transform, false);
            ContinuousMoveProvider move = moveObject.AddComponent<ContinuousMoveProvider>();
            move.mediator = mediator;
            move.moveSpeed = MoveSpeed;
            move.enableStrafe = true;
            move.enableFly = false;

            // 重力は GravityProvider 側で無効にしているが、移動側の旧設定も合わせて無効にしておく。
#pragma warning disable CS0618
            move.useGravity = false;
#pragma warning restore CS0618

            var moveInput = new XRInputValueReader<Vector2>("Left Hand Move", XRInputValueReader.InputSourceMode.InputAction);
            moveInput.inputAction.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");
            move.leftHandMoveInput = moveInput;
        }

        private static void CreateTargets()
        {
            Transform targets = new GameObject(PocBootstrap.TargetRootName).transform;
            Transform labels = new GameObject("PocLabels").transform;

            Texture2D checker = PocTextureGenerator.Load(PocTextureGenerator.CheckerName);
            Texture2D checkerNoMip = PocTextureGenerator.Load(PocTextureGenerator.CheckerNoMipName);
            Texture2D lines = PocTextureGenerator.Load(PocTextureGenerator.LinesName);
            Texture2D grid = PocTextureGenerator.Load(PocTextureGenerator.GridName);
            Texture2D text = PocTextureGenerator.Load(PocTextureGenerator.TextName);
            Texture2D noise = PocTextureGenerator.Load(PocTextureGenerator.NoiseName);
            Texture2D noiseNormal = PocTextureGenerator.Load(PocTextureGenerator.NoiseNormalName);
            Texture2D mipColors = PocTextureGenerator.Load(PocTextureGenerator.MipColorsName);

            // 床: 左半分は高周波チェッカー、右半分は Mip レベルごとに色が変わる診断テクスチャ。1m で1回繰り返す。
            var stripTiling = new Vector2(StripWidth, FloorLength);
            CreateFloorStrip(targets, "Floor_Left_Checker", -StripWidth * 0.5f, CreateMaterial("Poc_FloorChecker", LitShaderName, checker, stripTiling));
            CreateFloorStrip(targets, "Floor_Right_MipColors", StripWidth * 0.5f, CreateMaterial("Poc_FloorMipColors", LitShaderName, mipColors, stripTiling));

            // 側面の壁: 斜めから見ることになるため、異方性フィルタリングの確認に使う。
            var wallTiling = new Vector2(FloorLength, WallHeight);
            CreateSideWall(targets, "Wall_Left_Lines", -StripWidth, -90f, CreateMaterial("Poc_WallLines", LitShaderName, lines, wallTiling));
            CreateSideWall(targets, PocPropertyBlockSample.SourceName, StripWidth, 90f, CreateMaterial("Poc_WallGrid", LitShaderName, grid, wallTiling));

            Material farWallMaterial = CreateMaterial("Poc_FarWallText", LitShaderName, text, new Vector2(2f, 1f));
            float farWallZ = FloorCenterZ + FloorLength * 0.5f;
            CreateQuad(targets, "Wall_Far_Text", new Vector3(0f, 2f, farWallZ), 0f, new Vector2(StripWidth * 2f, 4f), farWallMaterial);

            CreatePanels(targets, labels, checker, checkerNoMip, lines, text, noise, noiseNormal);
            CreateDistanceMarkers(labels);
        }

        private static void CreatePanels(
            Transform targets,
            Transform labels,
            Texture2D checker,
            Texture2D checkerNoMip,
            Texture2D lines,
            Texture2D text,
            Texture2D noise,
            Texture2D noiseNormal)
        {
            Material textMaterial = CreateMaterial("Poc_PanelText", LitShaderName, text, Vector2.one);
            Material checkerMaterial = CreateMaterial("Poc_PanelChecker", LitShaderName, checker, Vector2.one);
            Material noiseMaterial = CreateMaterial("Poc_PanelNoiseNormal", LitShaderName, noise, Vector2.one);
            noiseMaterial.SetTexture(BumpMapId, noiseNormal);
            noiseMaterial.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(noiseMaterial);

            // 床と同じチェッカーテクスチャを別のマテリアル（Unlit）でも使い、共有テクスチャの挙動を見る。
            Material unlitMaterial = CreateMaterial("Poc_PanelCheckerUnlit", UnlitShaderName, checker, Vector2.one);
            Material noMipMaterial = CreateMaterial("Poc_PanelCheckerNoMip", LitShaderName, checkerNoMip, Vector2.one);
            Material propertyBlockMaterial = CreateMaterial("Poc_PanelLines", LitShaderName, lines, Vector2.one);

            CreatePanel(targets, labels, "Panel_Text_Lit", "TEXT (Lit)", -1.5f, textMaterial);
            CreatePanel(targets, labels, "Panel_Checker_Lit", "CHECKER (Lit)", -2.3f, checkerMaterial);
            CreatePanel(targets, labels, "Panel_NoiseNormal_Lit", "NOISE + NORMAL (Lit)", -3.1f, noiseMaterial);
            CreatePanel(targets, labels, "Panel_Checker_Unlit", "CHECKER (Unlit, shared)", 1.5f, unlitMaterial);
            CreatePanel(targets, labels, "Panel_Checker_NoMip", "CHECKER (no mips)", 2.3f, noMipMaterial);
            CreatePanel(targets, labels, PocPropertyBlockSample.PanelName, "GRID via PropertyBlock", 3.1f, propertyBlockMaterial);
        }

        private static void CreatePanel(Transform targets, Transform labels, string objectName, string label, float x, Material material)
        {
            var position = new Vector3(x, PanelHeight, PanelDistance);
            CreateQuad(targets, objectName, position, 0f, new Vector2(PanelSize, PanelSize), material);
            var labelPosition = new Vector3(x, PanelHeight + PanelSize * 0.5f + 0.03f, PanelDistance);
            CreateLabel(labels, label, labelPosition, 0.0045f, TextAnchor.LowerCenter);
        }

        private static void CreateDistanceMarkers(Transform labels)
        {
            Material markerMaterial = CreateMaterial("Poc_Marker", UnlitShaderName, null, Vector2.one);
            markerMaterial.SetColor(BaseColorId, new Color(1f, 0.1f, 0.1f));
            EditorUtility.SetDirty(markerMaterial);

            for (float distance = MarkerInterval; distance < FloorLength - 2f; distance += MarkerInterval)
            {
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Marker_{distance:0}m";
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                marker.transform.SetParent(labels, false);
                marker.transform.localPosition = new Vector3(0f, 0.005f, distance);
                marker.transform.localScale = new Vector3(StripWidth * 2f, 0.01f, 0.05f);
                marker.GetComponent<MeshRenderer>().sharedMaterial = markerMaterial;

                CreateLabel(labels, $"{distance:0} m", new Vector3(0f, 0.05f, distance), 0.02f, TextAnchor.LowerCenter);
            }
        }

        private static void CreateFloorStrip(Transform parent, string objectName, float x, Material material)
        {
            // Plane は 10m 四方で上を向いている。
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Plane);
            strip.name = objectName;
            Object.DestroyImmediate(strip.GetComponent<Collider>());
            strip.transform.SetParent(parent, false);
            strip.transform.localPosition = new Vector3(x, 0f, FloorCenterZ);
            strip.transform.localScale = new Vector3(StripWidth / 10f, 1f, FloorLength / 10f);
            strip.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateSideWall(Transform parent, string objectName, float x, float yaw, Material material)
        {
            var position = new Vector3(x, WallHeight * 0.5f, FloorCenterZ);
            CreateQuad(parent, objectName, position, yaw, new Vector2(FloorLength, WallHeight), material);
        }

        private static void CreateQuad(Transform parent, string objectName, Vector3 position, float yaw, Vector2 size, Material material)
        {
            // Quad は -Z 側が表。yaw 0 で原点側（カメラ側）を向く。
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateLabel(Transform parent, string text, Vector3 position, float characterSize, TextAnchor anchor)
        {
            var labelObject = new GameObject($"Label_{text}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = position;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.font = font;
            textMesh.text = text;
            textMesh.fontSize = 64;
            textMesh.characterSize = characterSize;
            textMesh.anchor = anchor;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.yellow;
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        private static Material CreateMaterial(string materialName, string shaderName, Texture2D baseMap, Vector2 tiling)
        {
            string path = $"{MaterialFolder}/{materialName}.mat";
            Shader shader = Shader.Find(shaderName);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture(BaseMapId, baseMap);
            material.SetTextureScale(BaseMapId, tiling);
            if (material.HasProperty(SmoothnessId))
            {
                // 鏡面反射の映り込みでテクスチャの比較が難しくならないようにする。
                material.SetFloat(SmoothnessId, 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
