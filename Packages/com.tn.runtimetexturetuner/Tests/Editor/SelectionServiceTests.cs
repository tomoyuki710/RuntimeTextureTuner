using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RuntimeTextureTuner
{
    public sealed class SelectionServiceTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject created in _createdObjects)
            {
                Object.DestroyImmediate(created);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void TryIntersect_RayHitsBox_ReturnsNearAndFarDistances()
        {
            var bounds = new Bounds(new Vector3(0f, 0f, 5f), Vector3.one * 2f);
            var ray = new Ray(Vector3.zero, Vector3.forward);

            bool isHit = SelectionService.TryIntersect(ray, bounds, out float nearDistance, out float farDistance);

            Assert.IsTrue(isHit);
            Assert.AreEqual(4f, nearDistance, 0.0001f);
            Assert.AreEqual(6f, farDistance, 0.0001f);
        }

        [Test]
        public void TryIntersect_RayMissesOrPointsAway_ReturnsFalse()
        {
            var bounds = new Bounds(new Vector3(0f, 0f, 5f), Vector3.one * 2f);

            Assert.IsFalse(SelectionService.TryIntersect(new Ray(new Vector3(3f, 0f, 0f), Vector3.forward), bounds, out _, out _));
            Assert.IsFalse(SelectionService.TryIntersect(new Ray(Vector3.zero, Vector3.back), bounds, out _, out _));
        }

        [Test]
        public void TryIntersect_OriginInside_ReturnsNegativeNear()
        {
            var bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
            var ray = new Ray(Vector3.zero, Vector3.right);

            bool isHit = SelectionService.TryIntersect(ray, bounds, out float nearDistance, out float farDistance);

            Assert.IsTrue(isHit);
            Assert.Less(nearDistance, 0f);
            Assert.AreEqual(5f, farDistance, 0.0001f);
        }

        [Test]
        public void FindCandidates_ReturnsHitsNearestFirst()
        {
            Renderer far = CreateCube(new Vector3(0f, 0f, 10f), 1f);
            Renderer near = CreateCube(new Vector3(0f, 0f, 3f), 1f);
            Renderer aside = CreateCube(new Vector3(5f, 0f, 3f), 1f);
            var results = new List<SelectionCandidate>();

            SelectionService.FindCandidates(new Ray(Vector3.zero, Vector3.forward), new[] { far, aside, near }, 100f, results);

            Assert.AreEqual(2, results.Count);
            Assert.AreSame(near, results[0].Renderer);
            Assert.AreSame(far, results[1].Renderer);
            Assert.AreEqual(2.5f, results[0].Distance, 0.0001f);
            Assert.IsFalse(results[0].IsOriginInside);
        }

        [Test]
        public void FindCandidates_WorksWithoutColliders()
        {
            Renderer target = CreateCube(new Vector3(0f, 0f, 3f), 1f);
            var results = new List<SelectionCandidate>();

            SelectionService.FindCandidates(new Ray(Vector3.zero, Vector3.forward), new[] { target }, 100f, results);

            Assert.IsNull(target.GetComponent<Collider>());
            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void FindCandidates_OriginInsideBounds_UsesExitDistance()
        {
            Renderer room = CreateCube(Vector3.zero, 20f);
            Renderer prop = CreateCube(new Vector3(0f, 0f, 3f), 1f);
            var results = new List<SelectionCandidate>();

            SelectionService.FindCandidates(new Ray(Vector3.zero, Vector3.forward), new[] { room, prop }, 100f, results);

            Assert.AreEqual(2, results.Count);
            Assert.AreSame(prop, results[0].Renderer);
            Assert.AreSame(room, results[1].Renderer);
            Assert.IsTrue(results[1].IsOriginInside);
            Assert.AreEqual(10f, results[1].Distance, 0.0001f);
        }

        [Test]
        public void FindCandidates_BeyondMaxDistance_IsExcluded()
        {
            Renderer far = CreateCube(new Vector3(0f, 0f, 50f), 1f);
            var results = new List<SelectionCandidate>();

            SelectionService.FindCandidates(new Ray(Vector3.zero, Vector3.forward), new[] { far }, 10f, results);

            Assert.AreEqual(0, results.Count);
        }

        private Renderer CreateCube(Vector3 position, float size)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * size;
            _createdObjects.Add(cube);
            return cube.GetComponent<Renderer>();
        }
    }
}
