using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using UnityObject = UnityEngine.Object;

namespace Unity.MARS.MARSUtils
{
    [MovedFrom("Unity.MARS")]
    public static class MarsRuntimeUtils
    {
        static Scene s_CachedScene;
        static MARSSession s_CachedSession;

        internal static Func<Camera> TryGetActiveCamera { private get; set; }

        /// <summary>
        /// Returns the first MarsSession found in the active scene
        /// </summary>
        /// <returns>The first MarsSession found in the active scene, or null if none exists</returns>
        public static MARSSession GetMarsSessionInActiveScene()
        {
            return GetMarsSessionInScene(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Returns the first MarsSession found in the given scene
        /// </summary>
        /// <param name="scene">The scene to search</param>
        /// <returns>The first MarsSession found in the given scene, or null if none exists</returns>
        public static MARSSession GetMarsSessionInScene(Scene scene)
        {
            if (s_CachedScene == scene && s_CachedSession != null)
                return s_CachedSession;

            if (s_CachedScene != scene || s_CachedSession == null)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var session = root.GetComponentInChildren<MARSSession>();
                    if (session != null)
                    {
                        s_CachedScene = scene;
                        s_CachedSession = session;
                        return session;
                    }
                }
            }

            s_CachedScene = default;
            s_CachedSession = null;
            return null;
        }

        /// <summary>
        /// If simulating in edit mode, returns the simulated camera else
        /// returns the camera associated to the MarsSession through the MarsCamera camera reference.
        /// </summary>
        /// <param name="findFallbackCamera">If the active session camera is null,
        /// then it will try and return the main camera and if that is null will find a Camera object.</param>
        public static Camera GetActiveCamera(bool findFallbackCamera = false)
        {
            if (TryGetActiveCamera != null)
            {
                var activeMarsCamera = TryGetActiveCamera();
                if (activeMarsCamera != null)
                    return activeMarsCamera;
            }

            return findFallbackCamera ? GetBestCameraFallback() : null;
        }

        /// <summary>
        /// Returns the camera associated to the MarsSession through the MarsCamera camera reference.
        /// </summary>
        /// <param name="findFallbackCamera">If the active session camera is null,
        /// then it will try and return the main camera and if that is null will find a Camera object.</param>
        public static Camera GetSessionAssociatedCamera(bool findFallbackCamera = false)
        {
            var session = MARSSession.Instance;
            if (session == null)
                session = GetMarsSessionInActiveScene();

            if (session != null)
                return session.TryGetSessionCamera();

            return findFallbackCamera ? GetBestCameraFallback() : null;
        }

        static Camera GetBestCameraFallback()
        {
            var main = Camera.main;
            return main != null ? main : UnityObject.FindObjectOfType<Camera>();
        }
    }
}
