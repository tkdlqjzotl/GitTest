using Unity.MARS.Simulation;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Data.Synthetic
{
    /// <summary>
    /// Base representation of a single property in a Synthesized MARS Object
    /// Most often you'll want to inherit from SynthesizedTrait<T> instead
    /// </summary>
    [MovedFrom("Unity.MARS.Data")]
    public abstract class SynthesizedTrait : MonoBehaviour, ISimulatable
    {
        /// <summary>
        /// Identifies the trait being applied to the Synthesized Object
        /// </summary>
        public abstract string TraitName { get; }

        /// <summary>
        /// Should this trait update when its transform does?
        /// </summary>
        public abstract bool UpdateWithTransform { get; }

        /// <summary>
        /// Called when the SynthesizedObject around this trait is being added
        /// </summary>
        /// <param name="dataID">The entity that will hold all of the synthesized data</param>
        public abstract void AddTrait(int dataID);

        /// <summary>
        /// Called when the SynthesizedObject around this trait is being updated
        /// </summary>
        /// <param name="dataID">The entity that will hold all of the synthesized data</param>
        public abstract void UpdateTrait(int dataID);

        /// <summary>
        /// Called when the SynthesizedObject around this trait is being removed
        /// </summary>
        /// <param name="entityID">The entity that holds this trait</param>
        public abstract void RemoveTrait(int dataID);
    }

    /// <summary>
    /// Representation for a single typed property in a Synthesized MARS Object
    /// </summary>
    /// <typeparam name="T">The type of data being represented by this trait</typeparam>
    public abstract class SynthesizedTrait<T> : SynthesizedTrait, IProvidesTraits<T>
    {
        protected static readonly TraitDefinition[] k_ProvidedTraits = new TraitDefinition[1];

        public TraitDefinition[] GetProvidedTraits()
        {
            if (k_ProvidedTraits[0] == default(TraitDefinition))
                k_ProvidedTraits[0] = new TraitDefinition(TraitName, typeof(T));

            return k_ProvidedTraits;
        }

        /// <summary>
        /// Calculates and retrieves the most up-to-date piece of data representing this trait
        /// </summary>
        /// <returns></returns>
        public abstract T GetTraitData();

        public sealed override void AddTrait(int dataID)
        {
            // Get the trait data, insert it in this entity
            this.AddOrUpdateTrait(dataID, TraitName, GetTraitData());
        }

        public sealed override void UpdateTrait(int dataID)
        {
            // Get the trait data, insert it in this entity
            this.AddOrUpdateTrait(dataID, TraitName, GetTraitData());
        }

        public sealed override void RemoveTrait(int dataID)
        {
            this.RemoveTrait(dataID, TraitName);
        }
    }
}
