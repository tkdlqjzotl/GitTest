using System.Collections.Generic;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Unity.MARS.Settings
{
    [MovedFrom("Unity.MARS")]
    public class MARSCore : ScriptableSettings<MARSCore>, IModuleSceneCallbacks
    {
        public const string AssetMarsRootFolder = "Assets/MARS";
        public const string UserSettingsFolder = AssetMarsRootFolder + "/UserSettings";
        public const string SettingsFolder = AssetMarsRootFolder + "/Settings"; // must match: PackageSource/com.unity.mars/Editor/Bootstrap/RegenerationWatcher.cs
        public const string PackageName = "com.unity.mars";
        public const string ProjectSettingsRootPath = "MARS";

#pragma warning disable 649
        [SerializeField]
        [Tooltip("Sets the default length of time a condition should be active before canceling due to lack of data. " +
            "-1 waits forever.")]
        float m_DefaultEntityTimeout = -1.0f;
#pragma warning restore 649

        public float defaultEntityTimeout { get { return m_DefaultEntityTimeout; } }

        public bool paused { get; set; }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<MonoBehaviour> k_Behaviors = new List<MonoBehaviour>();

        void IModule.LoadModule()
        {
#if UNITY_EDITOR
            // Make sure we have a MARS Session because we skip OnSceneOpened during builds
            if (MARSSession.Instance == null)
                MARSSession.FindExistingInstance();
#endif
        }

        void IModule.UnloadModule()
        {
            paused = false;
        }

#if UNITY_EDITOR
        void IModuleSceneCallbacks.OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode) { }
        void IModuleSceneCallbacks.OnSceneOpening(string path, OpenSceneMode mode) { }

        void IModuleSceneCallbacks.OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            CheckMARSBehaviors(scene);
        }
#endif

        void IModuleSceneCallbacks.OnSceneUnloaded(Scene scene) { }

        void IModuleSceneCallbacks.OnActiveSceneChanged(Scene oldScene, Scene newScene) { }

        void IModuleSceneCallbacks.OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckMARSBehaviors(scene);
        }

        static void CheckMARSBehaviors(Scene scene)
        {
            var hasMarsBehaviors = false;

            k_Behaviors.Clear();
            GameObjectUtils.GetComponentsInScene(scene, k_Behaviors, true);
            foreach (var behavior in k_Behaviors)
            {
                if (behavior == null)
                    continue;

                if ((behavior.gameObject.hideFlags & HideFlags.DontSave) != 0)
                    continue;

                if (behavior is MARSEntity || behavior is IFunctionalitySubscriber)
                {
                    hasMarsBehaviors = true;
                    break;
                }
            }

            // TODO: shut down MARS entirely if there are no MARS behaviors--the issue is starting it back up when adding them
            if (hasMarsBehaviors)
                MARSSession.EnsureRuntimeState();
        }
    }
}
