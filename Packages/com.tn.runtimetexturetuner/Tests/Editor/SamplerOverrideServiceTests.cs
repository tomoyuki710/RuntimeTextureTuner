using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RuntimeTextureTuner
{
    public sealed class SamplerOverrideServiceTests
    {
        private static readonly TextureSamplerSettings InitialSettings =
            new TextureSamplerSettings(0.25f, 2, FilterMode.Bilinear);

        private static readonly TextureSamplerSettings AdjustedSettings =
            new TextureSamplerSettings(-0.5f, 8, FilterMode.Trilinear);

        private readonly List<Texture2D> _createdTextures = new List<Texture2D>();

        [TearDown]
        public void TearDown()
        {
            foreach (Texture2D texture in _createdTextures)
            {
                Object.DestroyImmediate(texture);
            }

            _createdTextures.Clear();
        }

        [Test]
        public void TryRegister_RecordsCurrentValuesAsOriginal()
        {
            Texture2D texture = CreateTexture();
            var service = new SamplerOverrideService();

            bool isRegistered = service.TryRegister(texture);

            Assert.IsTrue(isRegistered);
            Assert.IsTrue(service.TryGetOriginal(texture, out TextureSamplerSettings original));
            Assert.AreEqual(InitialSettings, original);
            Assert.AreEqual(1, service.Count);
        }

        [Test]
        public void TryRegister_SameTextureTwice_KeepsFirstOriginal()
        {
            Texture2D texture = CreateTexture();
            var service = new SamplerOverrideService();
            service.TryRegister(texture);
            texture.mipMapBias = 1.5f;

            bool isRegisteredAgain = service.TryRegister(texture);

            Assert.IsFalse(isRegisteredAgain);
            service.TryGetOriginal(texture, out TextureSamplerSettings original);
            Assert.AreEqual(InitialSettings, original);
            Assert.AreEqual(1, service.Count);
        }

        [Test]
        public void TryRegister_Null_ReturnsFalse()
        {
            var service = new SamplerOverrideService();

            Assert.IsFalse(service.TryRegister(null));
            Assert.AreEqual(0, service.Count);
        }

        [Test]
        public void TrySetAdjusted_InAdjustedMode_WritesToTexture()
        {
            Texture2D texture = CreateTexture();
            var service = new SamplerOverrideService();

            bool isSet = service.TrySetAdjusted(texture, AdjustedSettings);

            Assert.IsTrue(isSet);
            Assert.AreEqual(AdjustedSettings, TextureSamplerSettings.Capture(texture));
            service.TryGetOriginal(texture, out TextureSamplerSettings original);
            Assert.AreEqual(InitialSettings, original);
        }

        [Test]
        public void TrySetAdjusted_InOriginalMode_KeepsTextureAndStoresCandidate()
        {
            Texture2D texture = CreateTexture();
            var service = new SamplerOverrideService();
            service.TryRegister(texture);
            service.SetCompareMode(CompareMode.Original);

            service.TrySetAdjusted(texture, AdjustedSettings);

            Assert.AreEqual(InitialSettings, TextureSamplerSettings.Capture(texture));
            service.TryGetAdjusted(texture, out TextureSamplerSettings adjusted);
            Assert.AreEqual(AdjustedSettings, adjusted);
        }

        [Test]
        public void SetCompareMode_SwitchesBetweenOriginalAndAdjusted()
        {
            Texture2D texture = CreateTexture();
            var service = new SamplerOverrideService();
            service.TrySetAdjusted(texture, AdjustedSettings);

            service.SetCompareMode(CompareMode.Original);
            Assert.AreEqual(InitialSettings, TextureSamplerSettings.Capture(texture));

            service.SetCompareMode(CompareMode.Adjusted);
            Assert.AreEqual(AdjustedSettings, TextureSamplerSettings.Capture(texture));
        }

        [Test]
        public void TryReset_RestoresOriginalAndDiscardsCandidate()
        {
            Texture2D texture = CreateTexture();
            var service = new SamplerOverrideService();
            service.TrySetAdjusted(texture, AdjustedSettings);

            bool isReset = service.TryReset(texture);

            Assert.IsTrue(isReset);
            Assert.AreEqual(InitialSettings, TextureSamplerSettings.Capture(texture));
            service.TryGetAdjusted(texture, out TextureSamplerSettings adjusted);
            Assert.AreEqual(InitialSettings, adjusted);
        }

        [Test]
        public void ResetAll_RestoresEveryTexture()
        {
            Texture2D first = CreateTexture();
            Texture2D second = CreateTexture();
            var service = new SamplerOverrideService();
            service.TrySetAdjusted(first, AdjustedSettings);
            service.TrySetAdjusted(second, AdjustedSettings);

            service.ResetAll();

            Assert.AreEqual(InitialSettings, TextureSamplerSettings.Capture(first));
            Assert.AreEqual(InitialSettings, TextureSamplerSettings.Capture(second));
        }

        [Test]
        public void TrySetAdjusted_InvalidValues_AreRejected()
        {
            Texture2D texture = CreateTexture();
            var service = new SamplerOverrideService();

            Assert.IsFalse(service.TrySetAdjusted(texture, new TextureSamplerSettings(float.NaN, 1, FilterMode.Bilinear)));
            Assert.IsFalse(service.TrySetAdjusted(texture, new TextureSamplerSettings(0f, 17, FilterMode.Bilinear)));
            Assert.IsFalse(service.TrySetAdjusted(texture, new TextureSamplerSettings(0f, -1, FilterMode.Bilinear)));
            Assert.IsFalse(service.TrySetAdjusted(texture, new TextureSamplerSettings(0f, 1, (FilterMode)99)));
            Assert.AreEqual(InitialSettings, TextureSamplerSettings.Capture(texture));
        }

        [Test]
        public void ResetAll_SkipsDestroyedTextures()
        {
            Texture2D kept = CreateTexture();
            Texture2D destroyed = CreateTexture();
            var service = new SamplerOverrideService();
            service.TrySetAdjusted(kept, AdjustedSettings);
            service.TrySetAdjusted(destroyed, AdjustedSettings);
            Object.DestroyImmediate(destroyed);

            Assert.DoesNotThrow(() => service.ResetAll());

            Assert.AreEqual(1, service.Count);
            Assert.AreEqual(InitialSettings, TextureSamplerSettings.Capture(kept));
        }

        private Texture2D CreateTexture()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, true);
            InitialSettings.ApplyTo(texture);
            _createdTextures.Add(texture);
            return texture;
        }
    }
}
