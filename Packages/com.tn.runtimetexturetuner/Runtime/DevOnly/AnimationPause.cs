using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace RuntimeTextureTuner
{
    /// <summary>
    /// シーン内で動いているアニメーションを止め、再開時に止める前の速度へ戻す。
    /// 時間（Time.timeScale）は変えないため、移動や回転などの操作は止まらない。
    /// 対象は止めた時点で動いている Animator、PlayableDirector（タイムライン）、ParticleSystem、Animation。
    /// </summary>
    public sealed class AnimationPause
    {
        private readonly List<PausedAnimator> _animators = new List<PausedAnimator>();
        private readonly List<PausedPlayable> _playables = new List<PausedPlayable>();
        private readonly List<ParticleSystem> _particleSystems = new List<ParticleSystem>();
        private readonly List<PausedAnimationState> _animationStates = new List<PausedAnimationState>();

        /// <summary>
        /// このクラスでアニメーションを止めているかどうか。
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 止めた対象の数（Animator、タイムライン、パーティクル、Animation の合計）。
        /// </summary>
        public int PausedCount { get; private set; }

        /// <summary>
        /// シーン内で動いているアニメーションを止める。既に止めている場合は何もしない。
        /// </summary>
        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            PauseAnimators();
            PauseDirectors();
            PauseParticleSystems();
            PauseAnimations();
            IsPaused = true;
            PausedCount = _animators.Count + CountDirectors() + _particleSystems.Count + CountAnimations();
        }

        /// <summary>
        /// 止めたアニメーションを再開する。止めている間にコンテンツ側が速度を変えた対象や、破棄された対象はそのままにする。
        /// </summary>
        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            ResumeAnimators();
            ResumePlayables();
            ResumeParticleSystems();
            ResumeAnimations();
            IsPaused = false;
            PausedCount = 0;
        }

        public void Toggle()
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private void PauseAnimators()
        {
            Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Animator animator in animators)
            {
                if (!animator.isActiveAndEnabled || animator.speed == 0f)
                {
                    continue;
                }

                _animators.Add(new PausedAnimator(animator, animator.speed));
                animator.speed = 0f;
            }
        }

        private void PauseDirectors()
        {
            // タイムラインは Animator Controller とは別の PlayableGraph で Animator を動かすため、
            // Animator.speed では止まらない。グラフの再生速度を 0 にして、その場の姿勢のまま止める。
            PlayableDirector[] directors = Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (PlayableDirector director in directors)
            {
                bool isPlaying = director.isActiveAndEnabled && director.state == PlayState.Playing;
                if (!isPlaying || !director.playableGraph.IsValid())
                {
                    continue;
                }

                PlayableGraph graph = director.playableGraph;
                int rootCount = graph.GetRootPlayableCount();
                for (int i = 0; i < rootCount; i++)
                {
                    Playable root = graph.GetRootPlayable(i);
                    double speed = root.GetSpeed();
                    if (speed == 0d)
                    {
                        continue;
                    }

                    _playables.Add(new PausedPlayable(director, root, speed));
                    root.SetSpeed(0d);
                }
            }
        }

        private void PauseParticleSystems()
        {
            ParticleSystem[] particleSystems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                // isPlaying は一時停止中には false になる。子は個別に集めるので、子をまとめて止めない。
                if (particleSystem.isPlaying)
                {
                    _particleSystems.Add(particleSystem);
                    particleSystem.Pause(false);
                }
            }
        }

        private void PauseAnimations()
        {
            Animation[] animations = Object.FindObjectsByType<Animation>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Animation animation in animations)
            {
                if (!animation.isActiveAndEnabled || !animation.isPlaying)
                {
                    continue;
                }

                foreach (AnimationState state in animation)
                {
                    if (state.enabled && state.speed != 0f)
                    {
                        _animationStates.Add(new PausedAnimationState(animation, state, state.speed));
                        state.speed = 0f;
                    }
                }
            }
        }

        private void ResumeAnimators()
        {
            foreach (PausedAnimator paused in _animators)
            {
                bool isUnchanged = paused.Animator != null && paused.Animator.speed == 0f;
                if (isUnchanged)
                {
                    paused.Animator.speed = paused.Speed;
                }
            }

            _animators.Clear();
        }

        private void ResumePlayables()
        {
            foreach (PausedPlayable paused in _playables)
            {
                bool isAlive = paused.Director != null && paused.Root.IsValid();
                if (isAlive && paused.Root.GetSpeed() == 0d)
                {
                    paused.Root.SetSpeed(paused.Speed);
                }
            }

            _playables.Clear();
        }

        private void ResumeParticleSystems()
        {
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem != null && particleSystem.isPaused)
                {
                    particleSystem.Play(false);
                }
            }

            _particleSystems.Clear();
        }

        private void ResumeAnimations()
        {
            foreach (PausedAnimationState paused in _animationStates)
            {
                bool isAlive = paused.Animation != null && paused.State != null;
                if (isAlive && paused.State.speed == 0f)
                {
                    paused.State.speed = paused.Speed;
                }
            }

            _animationStates.Clear();
        }

        private int CountDirectors()
        {
            var directors = new HashSet<PlayableDirector>();
            foreach (PausedPlayable paused in _playables)
            {
                directors.Add(paused.Director);
            }

            return directors.Count;
        }

        private int CountAnimations()
        {
            var animations = new HashSet<Animation>();
            foreach (PausedAnimationState paused in _animationStates)
            {
                animations.Add(paused.Animation);
            }

            return animations.Count;
        }

        private readonly struct PausedAnimator
        {
            public PausedAnimator(Animator animator, float speed)
            {
                Animator = animator;
                Speed = speed;
            }

            public Animator Animator { get; }

            public float Speed { get; }
        }

        private readonly struct PausedPlayable
        {
            public PausedPlayable(PlayableDirector director, Playable root, double speed)
            {
                Director = director;
                Root = root;
                Speed = speed;
            }

            public PlayableDirector Director { get; }

            public Playable Root { get; }

            public double Speed { get; }
        }

        private readonly struct PausedAnimationState
        {
            public PausedAnimationState(Animation animation, AnimationState state, float speed)
            {
                Animation = animation;
                State = state;
                Speed = speed;
            }

            public Animation Animation { get; }

            public AnimationState State { get; }

            public float Speed { get; }
        }
    }
}
