using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.XRTools.ModuleLoader;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Providers
{
    /// <summary>
    /// Defines the API for a Face Tracking Provider
    /// This functionality provider is responsible for face tracking
    /// </summary>
    [MovedFrom("Unity.MARS")]
    public interface IProvidesFaceTracking : IFunctionalityProvider
    {
        /// <summary>
        /// Get the total number of faces that can be tracked by this provider simultaneously
        /// </summary>
        /// <returns>The maximum possible number of simultaneously tracked faces, -1 if there is no theoretical limit</returns>
        int GetMaxFaceCount();

        /// <summary>
        /// Called when a face become tracked for the first time
        /// </summary>
        event Action<IMRFace> FaceAdded;

        /// <summary>
        /// Called when a tracked face has updated data
        /// </summary>
        event Action<IMRFace> FaceUpdated;

        /// <summary>
        /// Called when a tracked face is removed (lost)
        /// </summary>
        event Action<IMRFace> FaceRemoved;

        /// <summary>
        /// Get the currently tracked faces
        /// </summary>
        /// <param name="faces">A list of MRFace objects to which the currently tracked faces will be added</param>
        void GetFaces(List<IMRFace> faces);
    }
}
