using System;
using System.Collections.Generic;
using Unity.MARS;
using Unity.MARS.Data.Synthetic;
using Unity.MARS.MARSUtils;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEditor.MARS.Simulation.Rendering;
using UnityEditor.XRTools.Utils;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.MARS.Simulation
{
    /// <summary>
    /// MARS Simulation View displays a separate 3D scene by extending SceneView.
    /// </summary>
    //[EditorWindowTitle(useTypeNameAsIconName = false)]  // TODO update when this is not an Internal attribute
    [Serializable]
    [MovedFrom("Unity.MARS")]
    public class SimulationView : SceneView, ISimulationView
    {
        class SimulationViewData
        {
            public float size;
            public Vector3 pivot;
            public Quaternion rotation;
            public bool usesWorldScale;

            bool m_DrawGizmos;
            bool m_SceneLighting;
            SceneViewState m_SceneViewState;
            bool m_In2DMode;
            bool m_IsRotationLocked;
            bool m_AudioPlay;
            CameraSettings m_CameraSettings;
            bool m_Orthographic;

            public void CopySimulationViewData(SimulationView view)
            {
                m_DrawGizmos = view.drawGizmos;
                m_SceneLighting = view.sceneLighting;
                m_SceneViewState = view.sceneViewState;
                m_In2DMode = view.in2DMode;
                m_IsRotationLocked = view.isRotationLocked;
                m_AudioPlay = view.audioPlay;
                m_CameraSettings = view.cameraSettings;
                size = view.size;
                m_Orthographic = view.orthographic;
                pivot = view.pivot;
                rotation = view.rotation;
            }

            public void CopySimulationViewData(SimulationViewData data)
            {
                m_DrawGizmos = data.m_DrawGizmos;
                m_SceneLighting = data.m_SceneLighting;
                m_SceneViewState = data.m_SceneViewState;
                m_In2DMode = data.m_In2DMode;
                m_IsRotationLocked = data.m_IsRotationLocked;
                m_AudioPlay = data.m_AudioPlay;
                m_CameraSettings = data.m_CameraSettings;
                size = data.size;
                m_Orthographic = data.m_Orthographic;
                pivot = data.pivot;
                rotation = data.rotation;
                usesWorldScale = data.usesWorldScale;
            }

            public void SetSimulationViewFromData(SimulationView view, bool useViewLocation = true)
            {
                view.drawGizmos = m_DrawGizmos;
                view.sceneLighting = m_SceneLighting;
                view.sceneViewState = m_SceneViewState;
                view.in2DMode = m_In2DMode;
                view.isRotationLocked = m_IsRotationLocked;
                view.audioPlay = m_AudioPlay;
                view.cameraSettings = m_CameraSettings;
                view.orthographic = m_Orthographic;

                if (useViewLocation)
                {
                    view.size = size;
                    view.pivot = pivot;
                    view.rotation = rotation;
                }
            }
        }

        class Styles
        {
            public const int HelpBoxWidth = 242;
            public const int HelpBoxHeight = 50;
            public const int HelpBoxPadding = 12;
            public const int HelpBoxLabelWidth = 190;
            public const int InfoIconPadding = 6;
            public const int DismissButtonSize = 20;

            public const int HalfHelpBoxWidth = HelpBoxWidth / 2;
            public const int HalfHelpBoxHeight = HelpBoxHeight / 2;
            public const int HelpBoxVerticalOffset = HelpBoxHeight + HelpBoxPadding;
            public const int HalfDismissButtonSize = DismissButtonSize / 2;

            public readonly Texture2D InfoIcon;
            public readonly GUIStyle HelpBoxBackgroundStyle;
            public readonly GUIStyle HelpBoxLabelStyle;
            public readonly GUIStyle DismissButtonStyle;

            public Styles()
            {
                InfoIcon = EditorGUIUtility.FindTexture("console.infoicon.sml");

                HelpBoxBackgroundStyle = GUI.skin.button;

                HelpBoxLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    fontSize = 10,
                    padding = new RectOffset(0, 0, 0, 0)
                };

                DismissButtonStyle = GUIStyle.none;
                DismissButtonStyle.normal.textColor = Color.white;
            }
        }

        public const string SimulationViewWindowTitle = "Simulation View";
        public const string DeviceViewWindowTitle = "Device View";

        const string k_CustomMARSViewWindowTitle = "Custom MARS View";
        const string k_SceneBackgroundPrefsKey = "Scene/Background";
        const string k_DeviceControlsHelp = "Press 'Play' in the Simulation controls, then hold the right mouse button to look around, and WASD to move.";
        static readonly List<SimulationView> k_SimulationViews = new List<SimulationView>();

        static SimulationView s_ActiveSimulationView;
        static SceneView s_LastHoveredSimulationOrDeviceView;
        static Styles s_Styles;

        [SerializeField]
        ViewSceneType m_SceneType = ViewSceneType.None;

        [SerializeField]
        SimulationViewData m_DefaultViewData;

        [SerializeField]
        bool m_EnvironmentSceneActive;

        [SerializeField]
        bool m_DesaturateInactive;

        [SerializeField]
        bool m_UseXRay = true;

        bool m_FramedOnStart;
        Bounds m_MovementBounds;

        CameraFPSModeHandler m_FPSModeHandler;
        Vector2 m_MouseDelta;

        GUIContent m_SimulationViewTitleContent;
        GUIContent m_DeviceViewTitleContent;
        GUIContent m_CustomViewTitleContent;

        readonly Dictionary<ViewSceneType, SimulationViewData> m_DataForViewType = new Dictionary<ViewSceneType, SimulationViewData>();

        GUIContent CurrentTitleContent
        {
            get
            {
                switch (SceneType)
                {
                    case ViewSceneType.Simulation:
                        return m_SimulationViewTitleContent;
                    case ViewSceneType.Device:
                        return m_DeviceViewTitleContent;
                    default:
                        return m_CustomViewTitleContent;
                }
            }
        }

        bool UseMovementBounds => MarsUserPreferences.RestrictCameraToEnvironmentBounds && m_MovementBounds != default;

        public static Color EditorBackgroundColor => EditorMaterialUtils.PrefToColor(EditorPrefs.GetString(k_SceneBackgroundPrefsKey));

        /// <summary>
        /// Whether this view is in Sim or Device mode
        /// </summary>
        public ViewSceneType SceneType
        {
            get => m_SceneType;
            set => SetupSceneTypeData(value);
        }

        /// <summary>
        /// The primary scene view.  Not a Simulation View.
        /// </summary>
        public static SceneView NormalSceneView { get; private set; }

        /// <summary>
        /// List of all Simulation Views
        /// </summary>
        public static List<SimulationView> SimulationViews => k_SimulationViews;

        /// <summary>
        /// The last Simulation View the user focused
        /// </summary>
        public static SceneView ActiveSimulationView
        {
            get
            {
                if (!s_ActiveSimulationView && k_SimulationViews.Count > 0)
                    s_ActiveSimulationView = k_SimulationViews[0];

                return s_ActiveSimulationView;
            }
        }

        /// <summary>
        /// The Simulation or Device View that was most recently hovered by the mouse
        /// </summary>
        public static SceneView LastHoveredSimulationOrDeviceView
        {
            get
            {
                if (s_LastHoveredSimulationOrDeviceView == null)
                    return ActiveSimulationView;

                return s_LastHoveredSimulationOrDeviceView;
            }
            private set { s_LastHoveredSimulationOrDeviceView = value; }
        }

        public bool EnvironmentSceneActive
        {
            get { return m_EnvironmentSceneActive; }
            set
            {
                m_EnvironmentSceneActive = value;

                if (!CompositeRenderModule.TryGetCompositeRenderContext(this, out var context))
                    return;

                context.SetBackgroundSceneActive(value);
            }
        }

        public bool DesaturateInactive
        {
            get { return m_DesaturateInactive; }
            set
            {
                m_DesaturateInactive = value;

                if (!CompositeRenderModule.TryGetCompositeRenderContext(this, out var context))
                    return;

                context.DesaturateComposited = value;
            }
        }

        public bool UseXRay
        {
            get { return m_UseXRay; }
            set
            {
                m_UseXRay = value;
                if (!CompositeRenderModule.TryGetCompositeRenderContext(this, out var context))
                    return;

                context.UseXRay = value;
            }
        }

        // Delay creation of Styles till first access
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        static bool useFallbackRendering => CompositeRenderModuleOptions.instance.UseFallbackCompositeRendering;

        [MenuItem(MenuConstants.MenuPrefix + SimulationViewWindowTitle, priority = MenuConstants.SimulationViewPriority)]
        public static void InitWindowInSimulationView()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = SimulationViewWindowTitle, active = true});
            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.SceneType = ViewSceneType.Simulation;
            s_ActiveSimulationView = window;
            window.Show();
            window.ShowTab();
        }

        internal static void NewTabSimulationView(object userData)
        {
            if (!FindNormalSceneView())
            {
                NormalSceneView = MarsEditorUtils.CustomAddTabToHere(typeof(SceneView)) as SceneView;
                NormalSceneView.Show();
            }

            if (MarsEditorUtils.CustomAddTabToHere(userData) is SimulationView window)
            {
                window.SceneType = ViewSceneType.Simulation;
                s_ActiveSimulationView = window;
            }

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = SimulationViewWindowTitle, active = true});
        }

        [MenuItem(MenuConstants.MenuPrefix + DeviceViewWindowTitle, priority = MenuConstants.DeviceViewPriority)]
        public static void InitWindowInDeviceView()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = DeviceViewWindowTitle, active = true});
            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.SceneType = ViewSceneType.Device;
            window.Show();
            window.ShowTab();
        }

        internal static void NewTabDeviceView(object userData)
        {
            if (!FindNormalSceneView())
            {
                NormalSceneView = MarsEditorUtils.CustomAddTabToHere(typeof(SceneView)) as SceneView;
                NormalSceneView.Show();
            }

            if (MarsEditorUtils.CustomAddTabToHere(userData) is SimulationView window)
                window.SceneType = ViewSceneType.Device;

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = DeviceViewWindowTitle, active = true});
        }

        internal static void EnsureDeviceViewAvailable()
        {
            foreach (var simView in SimulationViews)
            {
                if (simView.SceneType == ViewSceneType.Device)
                    return;
            }

            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.SceneType = ViewSceneType.Device;
            window.Show();
            window.ShowTab();
        }

        protected override bool SupportsStageHandling() { return false; }

        /// <inheritdoc/>
        public override void AddItemsToMenu(GenericMenu menu)
        {
            this.MarsCustomMenuOptions(menu);
            base.AddItemsToMenu(menu);
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            // Suppress the error message about missing scene icon. It is not an exception, but in case any other
            // exceptions happen in base.OnEnable, we use try/catch to log them re-enable logging
            var logEnabled = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;
                base.OnEnable();
            }
            catch (Exception e)
            {
                Debug.LogFormat("Exception in SimulationView.OnEnable: {0}\n{1}", e.Message, e.StackTrace);
            }
            finally
            {
                Debug.unityLogger.logEnabled = logEnabled;
            }

            m_SimulationViewTitleContent = new GUIContent(SimulationViewWindowTitle, MarsUIResources.instance.SimulationViewIcon);
            m_DeviceViewTitleContent = new GUIContent(DeviceViewWindowTitle, MarsUIResources.instance.SimulationViewIcon);
            m_CustomViewTitleContent = new GUIContent(k_CustomMARSViewWindowTitle, MarsUIResources.instance.SimulationViewIcon);

            titleContent = CurrentTitleContent;
            autoRepaintOnSceneChange = true;

            m_DataForViewType.Clear();
            if (m_DefaultViewData == null)
            {
                m_DefaultViewData = new SimulationViewData();
                m_DefaultViewData.CopySimulationViewData(this);
            }

            k_SimulationViews.Add(this);

            // Name our scene view camera so it is easier to track
            camera.name = $"{SimulationViewWindowTitle} Camera {GetInstanceID()}";
            camera.gameObject.hideFlags = HideFlags.HideAndDontSave;

            m_FPSModeHandler = new CameraFPSModeHandler();

            var moduleLoaderCore = ModuleLoaderCore.instance;
            // Used for one time module subscribing and setup of values from environment manager
            if (moduleLoaderCore.ModulesAreLoaded)
            {
                EditorApplication.delayCall += OnModulesLoaded;
            }

            moduleLoaderCore.ModulesLoaded += OnModulesLoaded;

            if (SceneType == ViewSceneType.None)
                SceneType = ViewSceneType.Simulation;
        }

        void OnModulesLoaded()
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            var environmentManager = moduleLoaderCore.GetModule<MARSEnvironmentManager>();
            var compositeRenderViewModule = moduleLoaderCore.GetModule<CompositeRenderModule>();

            // These will be null in module tests
            if (environmentManager == null || compositeRenderViewModule == null)
                return;

            MARSEnvironmentManager.onEnvironmentSetup += OnEnvironmentSetup;
            OnEnvironmentSetup();
            SetupViewAsSimUser();
        }

        void OnEnvironmentSetup()
        {
            m_MovementBounds = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>().EnvironmentBounds;
            // Need to make sure camera is assigned to the sim scene if scene has changed.
            SetupViewAsSimUser();
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            moduleLoaderCore.ModulesLoaded -= OnModulesLoaded;

            MARSEnvironmentManager.onEnvironmentSetup -= OnEnvironmentSetup;

            if (CompositeRenderModule.GetActiveCompositeRenderModule(out var renderModule))
                renderModule.RemoveView(this);

            k_SimulationViews.Remove(this);
            m_FPSModeHandler.StopMoveInput(Vector2.zero);
            m_FPSModeHandler = null;

            CheckActiveSimulationView();

            base.OnDisable();
        }

        protected new virtual void OnDestroy()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = "Simulation View", active = false});
            base.OnDestroy();
        }

        void OnFocus()
        {
            if (SceneType != ViewSceneType.Simulation)
                return;

            s_ActiveSimulationView = this;
        }

        void Update()
        {
            if (maximized)
                return;

            // Check if normal scene view is closed and close simulation because otherwise Window -> Scene will focus
            // simulation view instead of reopening normal scene view
            if (NormalSceneView == null && !FindNormalSceneView())
                Close();
        }

        static bool FindNormalSceneView()
        {
            var allSceneViews = Resources.FindObjectsOfTypeAll(typeof (SceneView)) as SceneView[];
            if (allSceneViews == null)
                return false;

            foreach (var view in allSceneViews)
            {
                if (view is SimulationView)
                    continue;

                NormalSceneView = view;
                return true;
            }

            return false;
        }

        protected override void OnGUI()
        {
            if (!MARSEntitlements.instance.EntitlementsCheckGUI(position.width))
                return;

            // Custom Scene is the target for drag and drop and is needed for Scene Placement Module
            customScene = camera.scene;

            // Called before base.OnGUI to consume input
            this.DrawSimulationViewToolbar();
            var currentEvent = Event.current;
            var type = currentEvent.type;
            if (type == EventType.MouseDrag)
                m_MouseDelta = currentEvent.delta;

            base.OnGUI();
            this.DrawSimulationViewToolbar();

            // Setting in OnGUI prevents single frame flash of skybox
            // when interacting with the scene view state in the GUI.
            if (SceneType != ViewSceneType.None && !useFallbackRendering)
                sceneViewState.showSkybox = false;

            var toolbarHeightOffset = MarsEditorGUI.Styles.ToolbarHeight - 1;
            var rect = new Rect(0, toolbarHeightOffset, position.width,
                position.height - toolbarHeightOffset);

            var simulationSettings = SimulationSettings.instance;

            if (SceneType == ViewSceneType.Device)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
                var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();

                if (focusedWindow == this && environmentManager != null && environmentManager.IsMovementEnabled &&
                    // User has pressed "Play" on device mode to move.
                    querySimulationModule != null && querySimulationModule.simulatingTemporal)
                {
                    m_FPSModeHandler.MovementBounds = m_MovementBounds;
                    m_FPSModeHandler.UseMovementBounds = UseMovementBounds;
                    m_FPSModeHandler.HandleGUIInput(rect, currentEvent, type, m_MouseDelta);
                }
                else
                {
                    m_FPSModeHandler.StopMoveInput(currentEvent.mousePosition);
                }
            }

            if (mouseOverWindow != null && mouseOverWindow is SimulationView view)
                LastHoveredSimulationOrDeviceView = view;

            var helpMessageDisplayed = SimulationControlsGUI.DrawHelpArea(SceneType);

            if (!helpMessageDisplayed && SceneType == ViewSceneType.Device &&
                MarsHints.ShowDeviceViewControlsHint)
            {
                if (simulationSettings.EnvironmentMode == EnvironmentMode.Synthetic && !simulationSettings.UseSyntheticRecording)
                {
                    DrawDeviceViewControlsHint();
                }
            }

            UpdateCamera();
        }

        void DrawDeviceViewControlsHint()
        {
            var helpBoxPosition = new Vector2(
                position.width / 2f - Styles.HalfHelpBoxWidth,
                position.height - Styles.HelpBoxVerticalOffset);
            var helpBoxRect = new Rect(helpBoxPosition.x, helpBoxPosition.y, Styles.HelpBoxWidth, Styles.HelpBoxHeight);

            GUI.Box(helpBoxRect, string.Empty, styles.HelpBoxBackgroundStyle);

            var iconRect = new Rect(helpBoxPosition.x + Styles.InfoIconPadding + 2,
                helpBoxPosition.y + Styles.HalfHelpBoxHeight - styles.InfoIcon.height / 2f, styles.InfoIcon.width, styles.InfoIcon.height);
            GUI.DrawTexture(iconRect, styles.InfoIcon);

            var labelRect = new Rect(iconRect.x + iconRect.width + Styles.InfoIconPadding, helpBoxPosition.y,
                Styles.HelpBoxLabelWidth, Styles.HelpBoxHeight);
            GUI.Label(labelRect, k_DeviceControlsHelp, styles.HelpBoxLabelStyle);

            var dismissButtonRect = new Rect(helpBoxRect.x + Styles.HelpBoxWidth - Styles.DismissButtonSize,
                helpBoxRect.y + Styles.HalfHelpBoxHeight - Styles.HalfDismissButtonSize + 2, Styles.DismissButtonSize, Styles.DismissButtonSize);

            if (GUI.Button(dismissButtonRect, "✕", styles.DismissButtonStyle))
                MarsHints.ShowDeviceViewControlsHint = false;
        }

        internal static void AddElementToLastHoveredWindow(VisualElement element)
        {
            var view = LastHoveredSimulationOrDeviceView;
            if (view != null && element.parent != view.rootVisualElement)
            {
                view.rootVisualElement.Add(element);
                element.BringToFront();
            }
        }

        /// <summary>
        /// Cache the LookAt information for the Simulation scene type when it is not the active view type
        /// </summary>
        /// <param name="point">The position in world space to frame.</param>
        /// <param name="direction">The direction that the Scene view should view the target point from.</param>
        /// <param name="newSize">The amount of camera zoom. Sets <c>size</c>.</param>
        public void CacheLookAt(Vector3 point, Quaternion direction, float newSize)
        {
            if (SceneType == ViewSceneType.Simulation)
                return;

            if (m_DefaultViewData == null)
            {
                m_DefaultViewData = new SimulationViewData();
                m_DefaultViewData.CopySimulationViewData(this);
            }

            if (!m_DataForViewType.TryGetValue(ViewSceneType.Simulation, out var data))
            {
                data = new SimulationViewData();
                data.CopySimulationViewData(m_DefaultViewData);
            }

            data.pivot = point;
            data.rotation = direction;
            data.size = Mathf.Abs(newSize);
            m_DataForViewType[ViewSceneType.Simulation] = data;
        }

        /// <inheritdoc/>
        public void SetupViewAsSimUser(bool forceFrame = false)
        {
            switch (SceneType)
            {
                case ViewSceneType.Simulation:
                case ViewSceneType.Device:
                {
                    if (camera != null && CompositeRenderModule.TryGetCompositeRenderContext(this, out var context))
                    {
                        context.SetBackgroundColor(EditorBackgroundColor);
                        context.AssignCameraToSimulation();

                        // Only Frame Synthetic Simulation View
                        if (SceneType != ViewSceneType.Device && (!m_FramedOnStart || forceFrame))
                        {
                            MARSEnvironmentManager.instance.TryFrameSimViewOnEnvironment(this, forceFrame, true);
                            m_FramedOnStart = true;
                        }
                    }

                    break;
                }
                default:
                {
                    Debug.LogFormat("Scene type {0} not supported in Simulation View.", SceneType);
                    SceneType = ViewSceneType.Simulation;
                    break;
                }
            }
        }

        internal void UpdateCamera()
        {
            if (SceneType != ViewSceneType.Device)
                return;

            // Do not let Device View enter orthographic mode
            orthographic = false;
            in2DMode = false;
            isRotationLocked = true;

            var controllingCamera = MarsRuntimeUtils.GetActiveCamera();

            if (controllingCamera == null)
            {
                var envManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
                var devicePose = envManager == null ? Pose.identity : envManager.DeviceStartingPose;
                rotation = devicePose.rotation;
                size = 1f; // Size is locked when there is no controlling camera to prevent clip planes from changing
                pivot = devicePose.position + (devicePose.rotation * Vector3.forward) * cameraDistance;
                return;
            }

            var controllingCameraTransform = controllingCamera.transform;

            // need to set size before FOV and clip planes
            size = MARSSession.GetWorldScale();
            camera.fieldOfView = controllingCamera.fieldOfView;
            camera.nearClipPlane = controllingCamera.nearClipPlane;
            camera.farClipPlane = controllingCamera.farClipPlane;

            if (controllingCamera.usePhysicalProperties)
            {
                camera.usePhysicalProperties = true;
                camera.focalLength = controllingCamera.focalLength;
            }
            else
            {
                camera.usePhysicalProperties = false;
            }

            rotation = controllingCameraTransform.rotation;
            pivot = controllingCameraTransform.position + controllingCameraTransform.forward * cameraDistance;
        }

        void SetupSceneTypeData(ViewSceneType newType)
        {
            if (m_SceneType == newType)
                return;

            var oldType = m_SceneType;
            if (oldType != ViewSceneType.None)
            {
                if (!m_DataForViewType.TryGetValue(oldType, out var oldTypeData))
                    oldTypeData = new SimulationViewData();

                oldTypeData.CopySimulationViewData(this);
                m_DataForViewType[oldType] = oldTypeData;
            }

            if (m_DefaultViewData == null)
            {
                m_DefaultViewData = new SimulationViewData();
                m_DefaultViewData.CopySimulationViewData(this);
                m_DefaultViewData.usesWorldScale = false;
            }

            if (m_DataForViewType.TryGetValue(newType, out var newTypeData))
                newTypeData.SetSimulationViewFromData(this);
            else
                m_DefaultViewData.SetSimulationViewFromData(this, newType != ViewSceneType.Device);

            m_SceneType = newType;
            titleContent = CurrentTitleContent;

            switch (newType)
            {
                case ViewSceneType.None:
                    CheckActiveSimulationView();
                    break;
                case ViewSceneType.Simulation:
                    s_ActiveSimulationView = this;
                    break;
                case ViewSceneType.Device:
                    CheckActiveSimulationView();
                    drawGizmos = false;
                    in2DMode = false;
                    isRotationLocked = true;
                    sceneLighting = true;
                    orthographic = false;
                    break;
            }
        }

        void CheckActiveSimulationView()
        {
            if (s_ActiveSimulationView != this)
                return;

            for (var i = k_SimulationViews.Count - 1; i >= 0; i--)
            {
                if (k_SimulationViews[i].SceneType != ViewSceneType.Simulation)
                    continue;

                s_ActiveSimulationView = k_SimulationViews[i];
                return;
            }
        }

        internal void OffsetViewCachedData(Transform envTransform, Matrix4x4 inversePreviousOffset,
            Quaternion differenceRotation, float differenceScale)
        {
            foreach (var viewTypeData in m_DataForViewType)
            {
                var data = viewTypeData.Value;
                if (!data.usesWorldScale)
                    continue;

                var scaledSize = data.size * differenceScale;

                var previousLocalPivot = inversePreviousOffset.MultiplyPoint3x4(data.pivot);
                data.pivot = envTransform.TransformPoint(previousLocalPivot);
                data.rotation = differenceRotation * data.rotation;
                data.size = scaledSize;
            }
        }
    }
}
