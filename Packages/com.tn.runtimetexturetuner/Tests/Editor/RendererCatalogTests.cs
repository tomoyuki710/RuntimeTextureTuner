using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RuntimeTextureTuner
{
    public sealed class RendererCatalogTests
    {
        private const string TexturePropertyName = "_MainTex";

        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _createdObjects)
            {
                Object.DestroyImmediate(created);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Rebuild_ListsTexturesOfEachRenderer()
        {
            Texture2D texture = CreateTexture();
            Material material = CreateMaterial(texture);
            Renderer targetRenderer = CreateRenderer(material);
            var catalog = new RendererCatalog();

            catalog.Rebuild(new[] { targetRenderer });

            IReadOnlyList<TextureTarget> targets = catalog.GetTargets(targetRenderer);
            Assert.AreEqual(1, catalog.Renderers.Count);
            Assert.AreEqual(1, targets.Count);
            Assert.AreSame(texture, targets[0].Texture);
            Assert.AreEqual(TexturePropertyName, targets[0].Usages[0].PropertyName);
            Assert.AreEqual(0, targets[0].Usages[0].MaterialIndex);
            Assert.IsTrue(targets[0].IsAdjustable);
        }

        [Test]
        public void Rebuild_SharedTexture_BecomesSingleTarget()
        {
            Texture2D texture = CreateTexture();
            Renderer first = CreateRenderer(CreateMaterial(texture));
            Renderer second = CreateRenderer(CreateMaterial(texture));
            var catalog = new RendererCatalog();

            catalog.Rebuild(new[] { first, second });

            Assert.AreEqual(1, catalog.Targets.Count);
            Assert.IsTrue(catalog.TryGetTarget(texture, out TextureTarget target));
            Assert.AreEqual(2, target.RendererCount);
            Assert.AreEqual(2, target.MaterialCount);
            Assert.AreSame(target, catalog.GetTargets(first)[0]);
            Assert.AreSame(target, catalog.GetTargets(second)[0]);
        }

        [Test]
        public void Rebuild_SharedMaterial_CountsOneMaterial()
        {
            Material material = CreateMaterial(CreateTexture());
            Renderer first = CreateRenderer(material);
            Renderer second = CreateRenderer(material);
            var catalog = new RendererCatalog();

            catalog.Rebuild(new[] { first, second });

            TextureTarget target = catalog.Targets[0];
            Assert.AreEqual(2, target.RendererCount);
            Assert.AreEqual(1, target.MaterialCount);
        }

        [Test]
        public void Rebuild_DoesNotInstantiateMaterials()
        {
            Material material = CreateMaterial(CreateTexture());
            Renderer targetRenderer = CreateRenderer(material);
            var catalog = new RendererCatalog();

            catalog.Rebuild(new[] { targetRenderer });

            Assert.AreSame(material, targetRenderer.sharedMaterial);
        }

        [Test]
        public void Rebuild_NonTexture2D_IsListedAsNotAdjustable()
        {
            var renderTexture = new RenderTexture(8, 8, 0);
            _createdObjects.Add(renderTexture);
            Renderer targetRenderer = CreateRenderer(CreateMaterial(renderTexture));
            var catalog = new RendererCatalog();

            catalog.Rebuild(new[] { targetRenderer });

            Assert.IsFalse(catalog.GetTargets(targetRenderer)[0].IsAdjustable);
        }

        [Test]
        public void Rebuild_PropertyBlock_IsFlagged()
        {
            Renderer targetRenderer = CreateRenderer(CreateMaterial(CreateTexture()));
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetFloat("_Dummy", 1f);
            targetRenderer.SetPropertyBlock(propertyBlock);
            var catalog = new RendererCatalog();

            catalog.Rebuild(new[] { targetRenderer });

            Assert.IsTrue(catalog.GetTargets(targetRenderer)[0].HasPropertyBlockUsage);
        }

        [Test]
        public void GetTargets_UnknownRenderer_ReturnsEmpty()
        {
            Renderer unknown = CreateRenderer(CreateMaterial(CreateTexture()));
            var catalog = new RendererCatalog();

            Assert.AreEqual(0, catalog.GetTargets(unknown).Count);
            Assert.AreEqual(0, catalog.GetTargets(null).Count);
        }

        private Texture2D CreateTexture()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, true);
            _createdObjects.Add(texture);
            return texture;
        }

        private Material CreateMaterial(Texture texture)
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.SetTexture(TexturePropertyName, texture);
            _createdObjects.Add(material);
            return material;
        }

        private Renderer CreateRenderer(Material material)
        {
            var rendererObject = new GameObject("CatalogTestRenderer");
            _createdObjects.Add(rendererObject);
            MeshRenderer meshRenderer = rendererObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            return meshRenderer;
        }
    }
}
