using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Simulation
{
    /// <summary>
    /// Constant values used for simulations in Mars
    /// </summary>
    [MovedFrom("Unity.MARS")]
    public static class SimulationConstants
    {
        /// <summary>
        /// The layer index value used for GameObjects in the simulated environment.
        /// This is used when setting `gameobject.layer` to that of the simulated environment.
        /// </summary>
        public static int SimulatedEnvironmentLayerIndex => 3;

        /// <summary>
        /// The bit mask for the layer used for GameObjects in the simulated environment.
        /// This is used when needing to a layer culling or include mask to the simulated environment.
        /// </summary>
        public static int SimulatedEnvironmentLayerMask => 1 << SimulatedEnvironmentLayerIndex;
    }
}
