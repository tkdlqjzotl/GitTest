using System;
using Unity.MARS.Recording;
using Unity.MARS.Simulation;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Timeline;
using UnityEngine.Video;

namespace Unity.MARS.Data.Recorded
{
    /// <summary>
    /// A Playable Asset for a Video Clip that plays in MARS simulation
    /// </summary>
    [Serializable]
    [MovedFrom("Unity.MARS.Recording")]
    public class MarsVideoPlayableAsset : PlayableAsset
    {
        const double k_DefaultPreparationTime = 0.5d;

        [SerializeField, NotKeyable]
        [Tooltip("The Video Clip to play")]
        VideoClip m_VideoClip;

        [SerializeField, NotKeyable]
        [Tooltip("Sets the amount of time, in seconds, to wait at the start of the Timeline Clip before playing the video. " +
            "This should be enough time for the Video Player to prepare for playback.")]
        double m_PreparationTime = k_DefaultPreparationTime;

        [SerializeField, NotKeyable]
        float m_FocalLength = SimulationVideoContextSettings.DefaultFocalLength;

        /// <summary>
        /// The Video Clip to play
        /// </summary>
        public VideoClip VideoClip
        {
            get => m_VideoClip;
            set => m_VideoClip = value;
        }

        /// <summary>
        /// The amount of time, in seconds, to wait at the start of the Timeline Clip before playing the video.
        /// This should be enough time for the Video Player to prepare for playback.
        /// </summary>
        public double PreparationTime
        {
            get => m_PreparationTime;
            set => m_PreparationTime = value;
        }

        public float FocalLength => m_FocalLength;

        /// <summary>
        /// Inject a Mars Video Playable Behaviour into the given graph
        /// </summary>
        /// <param name="graph">The graph to inject playables into</param>
        /// <param name="owner">The game object which initiated the build. This must have a RecordedSessionDirector component.</param>
        /// <returns>The playable injected into the graph</returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<MarsVideoPlayableBehaviour>.Create(graph);
            var sessionDirector = owner.GetComponent<RecordedSessionDirector>();
            playable.GetBehaviour().Setup(sessionDirector, this);
            return playable;
        }
    }
}
