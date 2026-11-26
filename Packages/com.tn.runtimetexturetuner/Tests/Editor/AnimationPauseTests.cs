using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RuntimeTextureTuner
{
    public sealed class AnimationPauseTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in _createdObjects)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Pause_ThenResume_RestoresAnimatorSpeed()
        {
            Animator animator = CreateAnimator(1.5f);
            var pause = new AnimationPause();

            pause.Pause();
            Assert.That(animator.speed, Is.EqualTo(0f));
            Assert.That(pause.IsPaused, Is.True);

            pause.Resume();
            Assert.That(animator.speed, Is.EqualTo(1.5f));
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void Toggle_SwitchesBetweenPausedAndResumed()
        {
            Animator animator = CreateAnimator(1f);
            var pause = new AnimationPause();

            pause.Toggle();
            Assert.That(animator.speed, Is.EqualTo(0f));

            pause.Toggle();
            Assert.That(animator.speed, Is.EqualTo(1f));
        }

        [Test]
        public void Pause_SkipsAnimatorAlreadyStoppedByContent()
        {
            Animator animator = CreateAnimator(0f);
            var pause = new AnimationPause();

            pause.Pause();
            pause.Resume();

            Assert.That(animator.speed, Is.EqualTo(0f));
        }

        [Test]
        public void Pause_SkipsInactiveAnimator()
        {
            Animator animator = CreateAnimator(1f);
            animator.gameObject.SetActive(false);
            var pause = new AnimationPause();

            pause.Pause();

            Assert.That(animator.speed, Is.EqualTo(1f));
        }

        [Test]
        public void Resume_DoesNotOverrideSpeedChangedByContent()
        {
            Animator animator = CreateAnimator(1f);
            var pause = new AnimationPause();
            pause.Pause();
            animator.speed = 2f;

            pause.Resume();

            Assert.That(animator.speed, Is.EqualTo(2f));
        }

        [Test]
        public void Resume_IgnoresDestroyedAnimator()
        {
            Animator animator = CreateAnimator(1f);
            var pause = new AnimationPause();
            pause.Pause();
            Object.DestroyImmediate(animator.gameObject);

            Assert.DoesNotThrow(pause.Resume);
            Assert.That(pause.IsPaused, Is.False);
        }

        private Animator CreateAnimator(float speed)
        {
            var gameObject = new GameObject("AnimationPauseTest");
            _createdObjects.Add(gameObject);
            Animator animator = gameObject.AddComponent<Animator>();
            animator.speed = speed;
            return animator;
        }
    }
}
