using System;
using Unity.MARS.Query;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Data.Synthetic
{
    [RequireComponent(typeof(SynthesizedPose))]
    [RequireComponent(typeof(SynthesizedBounds2D))]
    [RequireComponent(typeof(SynthesizedMarkerId))]
    [MovedFrom("Unity.MARS.Data")]
    public class SynthesizedMarker : SynthesizedTrackable<MRMarker>
    {
        static readonly TraitDefinition[] k_ProvidedTraits = { TraitDefinitions.Marker, TraitDefinitions.TrackingState };

        MRMarker m_Marker;
        SynthesizedPose m_PoseSource;
        SynthesizedBounds2D m_ExtentsSource;
        SynthesizedMarkerId m_IdSource;

        internal int dataID { get; set; }

        public override string TraitName => TraitNames.Marker;

        public override void Initialize()
        {
            base.Initialize();
            if (MarsTrackableId.InvalidId == m_Marker.id)
                m_Marker.id = MarsTrackableId.Create();

            m_IdSource = GetComponent<SynthesizedMarkerId>();
            m_PoseSource = GetComponent<SynthesizedPose>();
            m_ExtentsSource = GetComponent<SynthesizedBounds2D>();

            if (!Guid.TryParse(m_IdSource.GetTraitData(), out var guid))
            {
                Debug.LogWarning($"The Synthesized Marker guid on '{name}' is missing or improperly formed.  " +
                                 "Image Marker Proxies may not match in simulation.");
                guid = Guid.Empty;
            }

            m_Marker.markerId = guid;
            m_Marker.pose = m_PoseSource.GetTraitData();
            m_Marker.extents = m_ExtentsSource.GetTraitData();
            // for now, simulated markers simply always report tracking.
            // this should be dependent on camera position when doing non-instant simulation
            m_Marker.trackingState = MARSTrackingState.Tracking;

            // We cannot guarantee that the synthetic marker has been set so use the renderer scale as initial size
            m_IdSource.UpdateMarkerSizeWithLocalScale();
        }

        public override MRMarker GetData()
        {
            m_Marker.pose = m_PoseSource.GetTraitData();
            m_Marker.extents = m_ExtentsSource.GetTraitData();
            return m_Marker;
        }
    }
}
