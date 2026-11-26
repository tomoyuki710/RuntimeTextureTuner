using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// セッションの保存と読み込み、プロジェクトとの照合、TextureImporter への反映と復元のテスト。
    /// 一時フォルダーに作ったテクスチャアセットを使い、終了時にフォルダーごと消す。
    /// </summary>
    public sealed class SessionApplyTests
    {
        private const string TempFolder = "Assets/RuntimeTextureTunerTestsTemp";

        private static readonly TextureSamplerSettings AdjustedSettings = new TextureSamplerSettings(-0.5f, 8, FilterMode.Trilinear);

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempFolder);
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void Json_RoundTrip_KeepsValuesAndWritesLocalFileIdAsText()
        {
            Texture2D texture = CreateTextureAsset("RoundTrip.png");
            TuningSessionData session = CreateSession(texture, AdjustedSettings);

            string json = SessionSerializer.ToJson(session);
            bool isLoaded = SessionSerializer.TryFromJson(json, out TuningSessionData loaded, out string error);

            Assert.IsTrue(isLoaded, error);
            StringAssert.Contains("\"localFileId\": \"", json);
            Assert.AreEqual(1, loaded.Entries.Count);
            TuningSessionEntry entry = loaded.Entries[0];
            Assert.IsTrue(entry.TryGetKey(out TextureKey key));
            Assert.AreEqual(session.Entries[0].Guid, key.Guid);
            Assert.AreEqual(SamplerSettingsData.From(AdjustedSettings), entry.Adjusted);
            CollectionAssert.AreEquivalent(new[] { "bias", "aniso", "filter" }, entry.ChangedFields);
            Assert.IsTrue(entry.Kept);
        }

        [Test]
        public void Json_BrokenOrUnsupported_IsRejected()
        {
            Assert.IsFalse(SessionSerializer.TryFromJson("{ not json", out _, out string brokenError));
            Assert.IsNotEmpty(brokenError);
            Assert.IsFalse(SessionSerializer.TryFromJson(string.Empty, out _, out _));
            Assert.IsFalse(SessionSerializer.TryFromJson("{\"schemaVersion\": 99, \"entries\": []}", out _, out string versionError));
            StringAssert.Contains("99", versionError);
        }

        [Test]
        public void Evaluate_FreshEntry_IsReady()
        {
            Texture2D texture = CreateTextureAsset("Ready.png");
            TuningSessionData session = CreateSession(texture, AdjustedSettings);

            SessionRow row = SessionImportService.Evaluate(session.Entries[0]);

            Assert.AreEqual(SessionRowStatus.Ready, row.Status);
            Assert.IsTrue(row.IsSelected);
            Assert.AreEqual(AssetDatabase.GetAssetPath(texture), row.AssetPath);
        }

        [Test]
        public void Apply_WritesOnlyTheThreeFields_AndLeavesSourceUntouched()
        {
            Texture2D texture = CreateTextureAsset("Apply.png");
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = GetImporter(path);
            int maxSizeBefore = importer.maxTextureSize;
            bool mipmapsBefore = importer.mipmapEnabled;
            bool isSrgbBefore = importer.sRGBTexture;
            bool isReadableBefore = importer.isReadable;
            TextureImporterCompression compressionBefore = importer.textureCompression;
            string sourceBefore = TextureManifestBuilder.ComputeFileFingerprint(path);
            TuningSessionData session = CreateSession(texture, AdjustedSettings);

            ApplyTransaction transaction = Apply(session, out List<string> messages);

            Assert.IsNotNull(transaction, string.Join("\n", messages));
            importer = GetImporter(path);
            Assert.AreEqual(-0.5f, importer.mipMapBias, 0.0001f);
            Assert.AreEqual(8, importer.anisoLevel);
            Assert.AreEqual(FilterMode.Trilinear, importer.filterMode);
            Assert.AreEqual(maxSizeBefore, importer.maxTextureSize);
            Assert.AreEqual(mipmapsBefore, importer.mipmapEnabled);
            Assert.AreEqual(isSrgbBefore, importer.sRGBTexture);
            Assert.AreEqual(isReadableBefore, importer.isReadable);
            Assert.AreEqual(compressionBefore, importer.textureCompression);
            Assert.AreEqual(sourceBefore, TextureManifestBuilder.ComputeFileFingerprint(path));
        }

        [Test]
        public void Apply_OnlyChangedFieldsAreWritten()
        {
            Texture2D texture = CreateTextureAsset("BiasOnly.png");
            string path = AssetDatabase.GetAssetPath(texture);
            TextureSamplerSettings original = TextureSamplerSettings.Capture(texture);
            var biasOnly = new TextureSamplerSettings(-0.7f, original.AnisoLevel, original.FilterMode);
            TuningSessionData session = CreateSession(texture, biasOnly);
            int anisoBefore = GetImporter(path).anisoLevel;
            FilterMode filterBefore = GetImporter(path).filterMode;

            ApplyTransaction transaction = Apply(session, out _);

            CollectionAssert.AreEqual(new[] { "bias" }, session.Entries[0].ChangedFields);
            CollectionAssert.AreEqual(new[] { "bias" }, transaction.Items[0].AppliedFields);
            Assert.AreEqual(-0.7f, GetImporter(path).mipMapBias, 0.0001f);
            Assert.AreEqual(anisoBefore, GetImporter(path).anisoLevel);
            Assert.AreEqual(filterBefore, GetImporter(path).filterMode);
        }

        [Test]
        public void Apply_AlreadyApplied_BecomesUnchanged()
        {
            Texture2D texture = CreateTextureAsset("Twice.png");
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            Apply(session, out _);

            SessionRow row = SessionImportService.Evaluate(session.Entries[0]);
            ApplyTransaction second = Apply(session, out _);

            Assert.AreEqual(SessionRowStatus.Unchanged, row.Status);
            Assert.IsNull(second);
        }

        [Test]
        public void Restore_ReturnsToValuesBeforeApply()
        {
            Texture2D texture = CreateTextureAsset("Restore.png");
            string path = AssetDatabase.GetAssetPath(texture);
            SamplerSettingsData before = TextureManifestBuilder.ReadImporterSettings(GetImporter(path));
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            ApplyTransaction transaction = Apply(session, out _);
            var messages = new List<string>();

            int restoredCount = TextureImporterApplier.Restore(transaction, null, messages);

            Assert.AreEqual(1, restoredCount, string.Join("\n", messages));
            Assert.AreEqual(before, TextureManifestBuilder.ReadImporterSettings(GetImporter(path)));
            Assert.AreEqual(before, transaction.Items[0].Before);
        }

        [Test]
        public void Restore_ValueChangedAfterApply_IsSkipped()
        {
            Texture2D texture = CreateTextureAsset("RestoreConflict.png");
            string path = AssetDatabase.GetAssetPath(texture);
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            ApplyTransaction transaction = Apply(session, out _);
            TextureImporter importer = GetImporter(path);
            importer.mipMapBias = 1.25f;
            importer.SaveAndReimport();
            var messages = new List<string>();

            int restoredCount = TextureImporterApplier.Restore(transaction, null, messages);

            Assert.AreEqual(0, restoredCount);
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual(1.25f, GetImporter(path).mipMapBias, 0.0001f);
            Assert.AreEqual(8, GetImporter(path).anisoLevel);
        }

        [Test]
        public void Evaluate_SourceChanged_IsStale_AndNotAppliedByDefault()
        {
            Texture2D texture = CreateTextureAsset("Stale.png");
            string path = AssetDatabase.GetAssetPath(texture);
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            WritePng(path, Color.red);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            SessionRow row = SessionImportService.Evaluate(session.Entries[0]);
            ApplyTransaction transaction = Apply(session, out List<string> messages);

            Assert.AreEqual(SessionRowStatus.Stale, row.Status);
            Assert.IsFalse(row.IsSelected);
            Assert.IsNull(transaction);
            Assert.AreNotEqual(8, GetImporter(path).anisoLevel, string.Join("\n", messages));
        }

        [Test]
        public void Evaluate_ImporterChangedAfterBuild_IsConflict()
        {
            Texture2D texture = CreateTextureAsset("Conflict.png");
            string path = AssetDatabase.GetAssetPath(texture);
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            TextureImporter importer = GetImporter(path);
            importer.anisoLevel = 3;
            importer.SaveAndReimport();

            SessionRow row = SessionImportService.Evaluate(session.Entries[0]);

            Assert.AreEqual(SessionRowStatus.Conflict, row.Status);
            Assert.IsFalse(row.IsSelected);
            Assert.IsTrue(row.CanApply(false, true));
            Assert.IsFalse(row.CanApply(false, false));
        }

        [Test]
        public void Evaluate_UnknownGuid_IsMissing()
        {
            Texture2D texture = CreateTextureAsset("Missing.png");
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            string json = SessionSerializer.ToJson(session).Replace(session.Entries[0].Guid, "0123456789abcdef0123456789abcdef");
            SessionSerializer.TryFromJson(json, out TuningSessionData edited, out _);

            SessionRow row = SessionImportService.Evaluate(edited.Entries[0]);

            Assert.AreEqual(SessionRowStatus.Missing, row.Status);
        }

        [Test]
        public void Evaluate_DeletedAsset_IsMissing()
        {
            Texture2D texture = CreateTextureAsset("Deleted.png");
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(texture));

            SessionRow row = SessionImportService.Evaluate(session.Entries[0]);

            Assert.AreEqual(SessionRowStatus.Missing, row.Status);
        }

        [Test]
        public void Evaluate_MovedAsset_IsFoundByGuid()
        {
            Texture2D texture = CreateTextureAsset("Moved.png");
            TuningSessionData session = CreateSession(texture, AdjustedSettings);
            Directory.CreateDirectory($"{TempFolder}/Sub");
            AssetDatabase.Refresh();
            string newPath = $"{TempFolder}/Sub/Renamed.png";
            string moveError = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(texture), newPath);

            SessionRow row = SessionImportService.Evaluate(session.Entries[0]);

            Assert.IsEmpty(moveError);
            Assert.AreEqual(SessionRowStatus.Ready, row.Status);
            Assert.AreEqual(newPath, row.AssetPath);
        }

        [Test]
        public void Apply_SameNameInAnotherFolder_IsNotTouched()
        {
            Directory.CreateDirectory($"{TempFolder}/A");
            Directory.CreateDirectory($"{TempFolder}/B");
            AssetDatabase.Refresh();
            Texture2D target = CreateTextureAsset("A/Same.png");
            Texture2D other = CreateTextureAsset("B/Same.png");
            string otherPath = AssetDatabase.GetAssetPath(other);
            SamplerSettingsData otherBefore = TextureManifestBuilder.ReadImporterSettings(GetImporter(otherPath));
            TuningSessionData session = CreateSession(target, AdjustedSettings);

            Apply(session, out _);

            Assert.AreEqual(8, GetImporter(AssetDatabase.GetAssetPath(target)).anisoLevel);
            Assert.AreEqual(otherBefore, TextureManifestBuilder.ReadImporterSettings(GetImporter(otherPath)));
        }

        [Test]
        public void CreateEntry_TextureWithoutImporter_IsNotExportable()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, true);
            AssetDatabase.CreateAsset(texture, $"{TempFolder}/NoImporter.asset");

            TextureManifestEntry entry = TextureManifestBuilder.CreateEntry(texture);

            Assert.AreEqual(TextureExportSupport.NoTextureImporter, entry.Support);
        }

        [Test]
        public void CreateEntry_RuntimeTexture_IsNotAnAsset()
        {
            var texture = new Texture2D(8, 8);

            TextureManifestEntry entry = TextureManifestBuilder.CreateEntry(texture);
            Object.DestroyImmediate(texture);

            Assert.AreEqual(TextureExportSupport.NotAnAsset, entry.Support);
        }

        private static ApplyTransaction Apply(TuningSessionData session, out List<string> messages)
        {
            messages = new List<string>();
            List<SessionRow> rows = SessionImportService.BuildRows(session);
            return TextureImporterApplier.Apply(rows, session.BuildId, false, false, null, messages);
        }

        private static TuningSessionData CreateSession(Texture2D texture, TextureSamplerSettings adjusted)
        {
            TextureManifestEntry manifestEntry = TextureManifestBuilder.CreateEntry(texture);
            Assert.AreEqual(TextureExportSupport.Exportable, manifestEntry.Support);

            var session = new TuningSessionData("test-build", Application.unityVersion, "test-device", "test-api", "test-quality", "2026-01-01T00:00:00Z");
            session.AddEntry(new TuningSessionEntry(manifestEntry, TextureSamplerSettings.Capture(texture), adjusted, true));
            return session;
        }

        private static Texture2D CreateTextureAsset(string relativePath)
        {
            string path = $"{TempFolder}/{relativePath}";
            WritePng(path, Color.gray);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(texture, path);
            return texture;
        }

        private static void WritePng(string path, Color color)
        {
            var source = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            source.SetPixels(pixels);
            File.WriteAllBytes(path, source.EncodeToPNG());
            Object.DestroyImmediate(source);
        }

        private static TextureImporter GetImporter(string path)
        {
            return (TextureImporter)AssetImporter.GetAtPath(path);
        }
    }
}
