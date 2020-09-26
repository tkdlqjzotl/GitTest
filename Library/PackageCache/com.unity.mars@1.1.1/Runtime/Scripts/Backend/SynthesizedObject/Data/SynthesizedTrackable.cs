using Unity.MARS.Simulation;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Data.Synthetic
{
    /// <summary>
    /// Base representation of a single data type in a Synthesized MARS data
    /// Most often you'll want to inherit from SynthesizedDaa<T> instead
    /// </summary>
    [MovedFrom("Unity.MARS.Data")]
    public abstract class SynthesizedTrackable : MonoBehaviour, ISimulatable
    {
        /// <summary>
        /// Identifies the type of data being synthesized
        /// </summary>
        public abstract string TraitName { get; }

        /// <summary>
        /// Called when the SynthesizedTrackable is used for the first time by a SynthesizedObject
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when the SynthesizedTrackable data is cleared by a SynthesizedObject
        /// </summary>
        public virtual void Terminate() { }

        /// <summary>
        /// Called when the SynthesizedObject is appending this data to an existing object
        /// </summary>
        /// <param name="dataID">The entity that will hold the synthesized data</param>
        public abstract void AddSynthData(int dataID);

        /// <summary>
        /// Called when the SynthesizedObject around this data is being updated
        /// </summary>
        public abstract void UpdateSynthData();

        /// <summary>
        /// Called when the SynthesizedObject around this data is being removed
        /// </summary>
        /// <param name="dataID">The entity that holds this data</param>
        public abstract void RemoveSynthData(int dataID);
    }

    /// <summary>
    /// Representation for a single typed property in a piece of Synthesized MARS data
    /// </summary>
    /// <typeparam name="T">The type of data being represented by this trait</typeparam>
    public abstract class SynthesizedTrackable<T> : SynthesizedTrackable, IProvidesTraits<bool>,
        IUsesMARSTrackableData<T> where T : IMRTrackable
    {
        readonly TraitDefinition[] k_ProvidedTraits = new TraitDefinition[1];

        /// <summary>
        /// Calculates and retrieves the most up-to-date piece of data representing this trait
        /// </summary>
        /// <returns></returns>
        public abstract T GetData();

        public sealed override void AddSynthData(int dataID)
        {
            Initialize();
            this.AddData(dataID, GetData());
            this.AddOrUpdateTrait(dataID, TraitName, true);
        }

        public sealed override void UpdateSynthData()
        {
            this.AddOrUpdateData(GetData());
        }

        public sealed override void RemoveSynthData(int dataID)
        {
            this.RemoveData(GetData());
            this.RemoveTrait(dataID, TraitName);
            Terminate();
        }

        public virtual TraitDefinition[] GetProvidedTraits()
        {
            if (k_ProvidedTraits[0] == default(TraitDefinition))
                k_ProvidedTraits[0] = new TraitDefinition(TraitName, typeof(T));

            return k_ProvidedTraits;
        }
    }
}
