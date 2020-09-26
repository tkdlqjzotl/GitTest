using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MARS.Behaviors;
using Unity.MARS.Conditions;
using Unity.MARS.Data;
using Unity.MARS.MARSUtils;
using Unity.MARS.Providers;
using Unity.MARS.Query;
using Unity.MARS.Settings;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.MARS
{
    /// <summary>
    /// Represents the runtime behavior of MARS.  One of these must be in any scene with MARS content for it to function properly
    /// </summary>
    [AddComponentMenu("")]
    public class MARSSession : MonoBehaviour, IUsesFunctionalityInjection
    {
        class Subscriber : IUsesMarkerTracking
        {
            IProvidesMarkerTracking IFunctionalitySubscriber<IProvidesMarkerTracking>.provider { get; set; }
        }

        public const string NotEntitledMessage =
            "No MARS subscription found for the current user. Users must purchase a subscription from the online store.";
        public const string LicensingUrl = "https://www.unity.com/mars";
        public const string NotEntitledMessageWithUrl = NotEntitledMessage + " Please go to " + LicensingUrl;

        const string k_ObjectName = "MARS Session";
        const string k_CameraName = "Main Camera";
        const string k_CameraTag = "MainCamera";
        const string k_UserName = "MARS User";
        const string k_PlayModeErrorFormat = "{0} There are likely errors above due to late setup of functionality " +
            "islands.  You should ensure proper scene setup by by going to Create > MARS > Session";

        internal const string EntitlementsFailedMessage = "MARS features are disabled in Play mode. " + NotEntitledMessageWithUrl;
        internal const string TokenExpiredMessage =
            "MARS token issue: Please refresh your Unity Hub login by logging out and back in again";

        const float k_DefaultNearPlane = 0.03f;
        const float k_MaxNearPlane = 0.1f;

        static readonly Type[] k_CameraComponents = { typeof(Camera) };

#pragma warning disable 649
        /// <summary>
        /// Features that the scene requires
        /// </summary>
        [SerializeField]
        Capabilities m_Requirements;

        /// <summary>
        /// Optional functionality island for this scene
        /// </summary>
        [SerializeField]
        FunctionalityIsland m_Island;

        [SerializeField]
        MarsMarkerLibrary m_MarkerLibrary;
#pragma warning restore 649

        [HideInInspector]
        [SerializeField]
        MARSCamera m_CameraReference;

        [HideInInspector]
        [SerializeField]
        Transform m_UserReference;

        readonly Subscriber m_Subscriber = new Subscriber();

        internal MARSCamera cameraReference => m_CameraReference;

        public static MARSSession Instance { get; private set; }
        internal static bool TestMode { get; set; }

        public FunctionalityIsland island => m_Island;
        public Capabilities requirements => m_Requirements;

#if UNITY_EDITOR
        public MarsMarkerLibrary MarkerLibrary => m_MarkerLibrary;
#endif

        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<MARSSession> k_Sessions = new List<MARSSession>();

        void Awake()
        {
            k_Sessions.Clear();
            GameObjectUtils.GetComponentsInActiveScene(k_Sessions);
            var sessionCount = k_Sessions.Count;
            Assert.AreEqual(sessionCount, 1, "More than one MARSSession in the scene");
            if (sessionCount != 1)
                Debug.LogError("More than one MARSSession in the scene. MARS will not function properly");
            else
            {
                if(m_Requirements == null)
                    m_Requirements = new Capabilities();

                Instance = this;
                var moduleLoaderCore = ModuleLoaderCore.instance;
                if (Application.isPlaying)
                {
                    moduleLoaderCore.ReloadModules();
                    MarsRuntimeUtils.TryGetActiveCamera = TryGetSessionCamera;
                }

                moduleLoaderCore.OnBehaviorAwake();

                this.InjectFunctionalitySingle(m_Subscriber);
                if (m_Subscriber.HasProvider())
                {
                    m_Subscriber.SetActiveMarkerLibrary(m_MarkerLibrary);
                    m_Subscriber.StartTrackingMarkers();
                }
            }
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying && !EditorOnlyDelegates.IsEntitled(false))
                Debug.LogWarning(EntitlementsFailedMessage);
#endif
            ModuleLoaderCore.instance.OnBehaviorEnable();
        }

        void Start() { ModuleLoaderCore.instance.OnBehaviorStart(); }

        void Update() { ModuleLoaderCore.instance.OnBehaviorUpdate(); }

        void OnDisable() { ModuleLoaderCore.instance.OnBehaviorDisable(); }

        // We don't need to set the instance pointer to null here, as destroying the object will make the instance null inherently
        void OnDestroy()
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            moduleLoaderCore.OnBehaviorDestroy();

            if (m_Subscriber.HasProvider())
            {
                m_Subscriber.StopTrackingMarkers();
                m_Subscriber.SetActiveMarkerLibrary(null);
            }

            if (Application.isPlaying)
            {
                MarsRuntimeUtils.TryGetActiveCamera = null;
                moduleLoaderCore.UnloadModules();
            }
        }

        /// <summary>
        /// Ensures that a MARS Scene has all required components and configuration
        /// </summary>
        public static void EnsureRuntimeState(Transform overrideUserRef = null)
        {
            // Make sure we have a MARS Session
            if (Instance == null)
                Instance = GameObjectUtils.ExhaustiveComponentSearch<MARSSession>(null);

            if (MARSSceneModule.instance.BlockEnsureSession)
                return;

            // Early out if nothing needs to change, otherwise we add an unnecessary undo group
            if (SessionConfigured(Instance))
                return;

            using (var undoBlock = new UndoBlock("Ensure MARS Runtime State"))
            {
                if (Instance == null)
                {
                    var session = CreateSession(undoBlock);
                    var sessionTransform = session.transform;
                    sessionTransform.SetAsFirstSibling();

                    MatchSessionTransformWithCameraParent(sessionTransform);
                    DirtySimulatableSceneIfNeeded();
                }
                else
                {
                    undoBlock.RecordObject(Instance.gameObject);
                }

                if (!TestMode)
                    EnsureSessionConfigured(Instance, undoBlock, overrideUserRef);
            }
        }

        static MARSSession CreateSession(UndoBlock undoBlock)
        {
            LogRuntimeIssue("MARS Session not present in the scene - creating one now.");
            var sessionObject = new GameObject(k_ObjectName);
            sessionObject.SetActive(false);
            undoBlock.RegisterCreatedObject(sessionObject);
            var session = sessionObject.AddComponent<MARSSession>();

            // Check capabilities before we activate to ensure providers are set up properly
            session.CheckCapabilities();
            Instance = session;
            sessionObject.SetActive(true);
            return session;
        }

        static bool SessionConfigured(MARSSession session)
        {
            if (session == null)
                return false;

            var sessionObject = session.gameObject;
            var sessionTransform = session.transform;
            var sessionCamera = session.m_CameraReference;
            return sessionTransform.parent == null && sessionTransform.GetSiblingIndex() == 0 && sessionCamera != null &&
                sessionCamera.gameObject.activeInHierarchy && sessionCamera.transform.parent == sessionTransform &&
                session.m_UserReference != null && sessionObject.name == k_ObjectName && sessionObject.activeInHierarchy;
        }

        static void EnsureSessionConfigured(MARSSession session, UndoBlock undoBlock, Transform overrideUserRef = null)
        {
            var sessionObject = session.gameObject;
            var sessionTransform = session.transform;
            var changed = false;

            // The MARS Session must be a top level object
            if (sessionTransform.parent != null)
            {
                LogRuntimeIssue("MARS Session must be a top-level object.  Moving back to root.");
                undoBlock.SetTransformParent(sessionTransform, null);
                changed = true;
            }

            // The MARS Session should be at the top of the hierarchy
            if (sessionTransform.GetSiblingIndex() != 0)
            {
                Debug.Log("Moving the MARS Session back to be the top sibling of the hierarchy.");
                sessionTransform.SetAsFirstSibling();
                changed = true;
            }

            // Make sure we have a properly configured MARS Camera
            if (!TestMode && session.m_CameraReference == null)
            {
                var marsCameraRef = GameObjectUtils.ExhaustiveComponentSearch<MARSCamera>(sessionObject);

                // If we can't find a MARS camera, get the main camera and create one on that
                if (marsCameraRef == null)
                {
                    LogRuntimeIssue("MARSCamera not present in the scene.  Adding one to the main camera.");

                    var createdNewCamera = false;
                    var cameraRef = GameObjectUtils.ExhaustiveTaggedComponentSearch<Camera>(sessionObject, k_CameraTag);
                    if (cameraRef == null)
                    {
                        LogRuntimeIssue("Scene does not have a main camera.  Adding one to the scene.");
                        var newCamera = new GameObject(k_CameraName, k_CameraComponents) { tag = k_CameraTag };
                        cameraRef = newCamera.GetComponent<Camera>();
                        cameraRef.nearClipPlane = k_DefaultNearPlane * sessionTransform.localScale.x;
                        newCamera.transform.parent = sessionTransform;
                        undoBlock.RegisterCreatedObject(newCamera);
                        createdNewCamera = true;
                    }

                    marsCameraRef = undoBlock.AddComponent<MARSCamera>(cameraRef.gameObject);

                    if (!createdNewCamera)
                    {
                        var nearPlane = cameraRef.nearClipPlane;
                        var cameraParent = cameraRef.transform.parent;
                        if (cameraParent == null)
                            cameraParent = sessionTransform;

                        var worldScale = cameraParent.localScale.x;
                        var scaledMaxNearPlane = k_MaxNearPlane * worldScale;
                        if (nearPlane > scaledMaxNearPlane)
                        {
                            LogRuntimeIssue("Camera near clip plane is greater than the recommended distance. " +
                                $"Setting near plane from {nearPlane} to {scaledMaxNearPlane}.");

                            undoBlock.RecordObject(cameraRef);
                            cameraRef.nearClipPlane = scaledMaxNearPlane;
                        }
                    }
                }

                session.m_CameraReference = marsCameraRef;
                changed = true;
            }

            if (!TestMode && session.m_CameraReference.transform.GetComponentInParent<MARSSession>() == null)
            {
                LogRuntimeIssue("MARSCamera must have a MARSSession object in its list of parents.  Re-parenting.");
                MatchSessionTransformWithCameraParent(sessionTransform);
                undoBlock.SetTransformParent(session.m_CameraReference.transform, sessionTransform);
                changed = true;
            }

            // Look for the user object, if it's not in the scene, make a new one
            var userRef = session.m_UserReference;
            if (!TestMode && userRef == null)
            {
                userRef = session.m_CameraReference.transform.Find(k_UserName);
                if (userRef == null)
                {
                    if (overrideUserRef == null)
                        userRef = GameObjectUtils.Instantiate(MARSRuntimePrefabs.instance.UserPrefab).transform;
                    else
                        userRef = overrideUserRef;

                    userRef.name = k_UserName;
                    userRef.parent = session.m_CameraReference.transform;
                    userRef.localPosition = Vector3.zero;
                    userRef.localRotation = Quaternion.identity;
                    userRef.localScale = Vector3.one;
                    undoBlock.RegisterCreatedObject(userRef.gameObject);
                }

                session.m_UserReference = userRef;
                changed = true;
            }


            if (sessionObject.name != k_ObjectName)
            {
                LogRuntimeIssue(string.Format("The MARS Session object was not called {0}.  Renaming.", k_ObjectName));
                session.name = k_ObjectName;
                changed = true;
            }

            // One more bizarre scenario to catch - the MARS Session and Camera being *one* object.
            // We can't just delete the MARS Session since the user might have other scripts there, so we just throw up a warning telling them to fix things manually
            if (!TestMode && session.m_CameraReference.transform == sessionTransform)
            {
                Debug.LogError("The MARS Session should be a parent of the MARSCamera, *NOT* the same object!  " +
                    "Please correct this in your scene! If you have not customized this object, just delete your " +
                    "MARS Session object and go to Create > MARS > Session.");
            }

            if (sessionObject.activeInHierarchy == false)
            {
                LogRuntimeIssue("There is a MARS Session object in your scene that is *not* active.  Running your scene with an inactive MARS Session will cause problems.");
                sessionObject.SetActive(true);
                changed = true;
            }

            var cameraObject = session.cameraReference.gameObject;
            if (cameraObject.activeInHierarchy == false)
            {
                LogRuntimeIssue("There is a MARS Camera GameObject in your scene that is *not* active.  " +
                    "Re-enabling here, as running your scene with an inactive MARS Camera would cause problems.");

                cameraObject.SetActive(true);
                changed = true;
            }

            if (changed)
                DirtySimulatableSceneIfNeeded();
        }

        /// <summary>
        /// Ensures that the active scene has a MARS Session
        /// </summary>
        /// <returns>Thew newly created MARSSession, or null if a session previously exists</returns>
        public static MARSSession EnsureSessionInActiveScene()
        {
            var session = GameObjectUtils.GetComponentInActiveScene<MARSSession>();
            if (SessionConfigured(session))
                return null;

            using (var undoBlock = new UndoBlock("Ensure Session in Active Scene", TestMode))
            {
                if (!session)
                {
                    session = CreateSession(undoBlock);
                    var sessionTransform = session.transform;
                    sessionTransform.SetAsFirstSibling();

                    var camera = GameObjectUtils.ExhaustiveTaggedComponentSearch<Camera>(null, k_CameraTag);
                    GameObject cameraObj;
                    if (camera != null)
                    {
                        cameraObj = camera.gameObject;
                        undoBlock.SetTransformParent(cameraObj.transform, sessionTransform);
                    }
                    else
                    {
                        cameraObj = new GameObject(k_CameraName, k_CameraComponents) { tag = k_CameraTag };
                        cameraObj.transform.parent = sessionTransform;
                        undoBlock.RegisterCreatedObject(cameraObj);
                    }

                    session.m_CameraReference = undoBlock.AddComponent<MARSCamera>(cameraObj);

                    var userRef = GameObjectUtils.Instantiate(MARSRuntimePrefabs.instance.UserPrefab).transform;
                    userRef.name = k_UserName;
                    userRef.parent = session.m_CameraReference.transform;
                    userRef.localPosition = Vector3.zero;
                    userRef.localRotation = Quaternion.identity;
                    userRef.localScale = Vector3.one;
                    undoBlock.RegisterCreatedObject(userRef.gameObject);
                    session.m_UserReference = userRef;

                    DirtySimulatableSceneIfNeeded();
                    return session;
                }

                EnsureSessionConfigured(session, undoBlock);
            }

            return null;
        }

        static void MatchSessionTransformWithCameraParent(Transform sessionTransform)
        {
            var cameraRef = GameObjectUtils.ExhaustiveTaggedComponentSearch<Camera>(null, k_CameraTag);
            if (cameraRef)
            {
                var cameraParent = cameraRef.transform.parent;
                if (cameraParent)
                {
                    sessionTransform.position = cameraParent.position;
                    sessionTransform.rotation = cameraParent.rotation;
                    sessionTransform.localScale = cameraParent.localScale;
                }
            }
        }

        static void LogRuntimeIssue(object message)
        {
            if (!TestMode)
            {
                if (Application.isPlaying)
                    Debug.LogError(string.Format(k_PlayModeErrorFormat, message));
                else
                    Debug.LogWarning(message);
            }
        }

        static void DirtySimulatableSceneIfNeeded()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorOnlyDelegates.DirtySimulatableScene?.Invoke();
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (m_MarkerLibrary == null)
                return;

            foreach (var condition in FindObjectsOfType<MarkerCondition>())
            {
                condition.ValidateMarkerGuid(this);
            }
        }
#endif

        public bool CheckCapabilities()
        {
            if (m_Requirements == null)
                m_Requirements = new Capabilities();

            var gatheredTraitRequirements = new HashSet<TraitRequirement>();

            // Calculate scene requirements and complexity
            var modified = false;
            var faceRequirements = false;
            var scene = SceneManager.GetActiveScene();
            var rootGameObjects = scene.GetRootGameObjects();
            foreach (var rootGameObject in rootGameObjects)
            {
                var requiringComponents = rootGameObject.GetComponentsInChildren<IRequiresTraits>(true);

                var hostedConditionRequirements = rootGameObject.GetComponentsInChildren<IComponentHost<ICondition>>(true)
                    .SelectMany(host => host.HostedComponents).Cast<IRequiresTraits>();

                foreach (var requirer in requiringComponents.Union(hostedConditionRequirements))
                {
                    // this can happen if a requirer is destroyed
                    if (requirer == null)
                        continue;

                    foreach (var requirement in requirer.GetRequiredTraits())
                    {
                        // this can happen if a tag condition's trait name isn't filled in
                        var traitName = requirement.TraitName;
                        if (!string.IsNullOrEmpty(traitName))
                        {
                            gatheredTraitRequirements.Add(requirement);
                            faceRequirements |= traitName == TraitNames.Face;
                        }
                    }
                }
            }

            var traitRequirements = m_Requirements.TraitRequirements;
            if (!gatheredTraitRequirements.SetEquals(traitRequirements))
            {
                modified = true;
                traitRequirements.Clear();
                // this is where all requirements for the scene actually get added to the Capabilities
                traitRequirements.UnionWith(gatheredTraitRequirements);
            }

            if (!faceRequirements)
            {
                foreach (var rootGameObject in rootGameObjects)
                {
                    if (rootGameObject.GetComponentInChildren<IUsesFaceTracking>() != null ||
                        rootGameObject.GetComponentInChildren<IUsesFacialExpressions>() != null)
                    {
                        faceRequirements = true;
                        break;
                    }
                }
            }

            var requiredCameraFacing = faceRequirements ? CameraFacingDirection.User : CameraFacingDirection.World;
            if (m_Requirements.RequiredCameraFacing != requiredCameraFacing)
            {
                modified = true;
                m_Requirements.RequiredCameraFacing = requiredCameraFacing;
            }

#if UNITY_EDITOR
            if (modified && !Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(scene);
#endif

            return modified;
        }

        /// <summary>
        /// Get the scale of the session object, or return 1 if it doesn't exist
        /// Used only for authoring systems that need World Scale when no Camera Offset Provider exists
        /// </summary>
        /// <returns></returns>
        internal static float GetWorldScale()
        {
            if (Instance == null)
                return 1f;

            return Instance.transform.localScale.x;
        }

        internal Camera TryGetSessionCamera()
        {
            return m_CameraReference != null ? m_CameraReference.MarsCamera : null;
        }

        /// <summary>
        /// Search all loaded scenes for MARSSession objects and set the first one to the Instance property, if any exist
        /// </summary>
        public static void FindExistingInstance()
        {
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var session = GameObjectUtils.GetComponentInScene<MARSSession>(SceneManager.GetSceneAt(i));
                if (session != null)
                {
                    Instance = session;
                    break;
                }
            }
        }
    }
}
