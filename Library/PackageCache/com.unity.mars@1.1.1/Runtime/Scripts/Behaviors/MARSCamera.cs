using Unity.MARS.Data;
using Unity.MARS.Providers;
using Unity.MARS.Simulation;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(Camera))]
    [MovedFrom("Unity.MARS.Behaviors")]
    public class MARSCamera : MonoBehaviour, IUsesCameraPose, IUsesCameraProjectionMatrix, IUsesFunctionalityInjection,
        ISimulatable
    {
        class CameraIntrinsicsSubscriber : IUsesCameraIntrinsics
        {
            IProvidesCameraIntrinsics IFunctionalitySubscriber<IProvidesCameraIntrinsics>.provider { get; set; }
        }

#pragma warning disable 649
        [SerializeField]
        GameObject m_TrackingWarning;
#pragma warning restore 649

        Camera m_Camera;
#if UNITY_EDITOR
        bool m_CameraSetup;
#endif

        readonly CameraIntrinsicsSubscriber m_IntrinsicsSubscriber = new CameraIntrinsicsSubscriber();

        IProvidesCameraPose IFunctionalitySubscriber<IProvidesCameraPose>.provider { get; set; }
        IProvidesCameraProjectionMatrix IFunctionalitySubscriber<IProvidesCameraProjectionMatrix>.provider { get; set; }
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }

        internal Camera MarsCamera
        {
            get
            {
#if UNITY_EDITOR
                if (!m_CameraSetup)
                {
                    m_Camera = GetComponent<Camera>();
                    m_CameraSetup = true;
                }
#endif
                return m_Camera;
            }
        }

        void OnEnable()
        {
            this.SubscribePoseUpdated(OnPoseUpdated);
            m_Camera = GetComponent<Camera>();

            // Composite Camera Rendering handles setting clear flags in editor
            if (!Application.isEditor)
            {
                m_Camera.clearFlags = CameraClearFlags.SolidColor;
                m_Camera.backgroundColor = Color.black;
            }

            if (m_TrackingWarning)
                this.SubscribeTrackingTypeChanged(OnTrackingStateChanged);

            var projectionMatrix = this.GetProjectionMatrix();
            if (projectionMatrix.HasValue)
                m_Camera.projectionMatrix = projectionMatrix.Value;

            transform.SetLocalPose(this.GetPose());

            this.InjectFunctionalitySingle(m_IntrinsicsSubscriber);
            if (m_IntrinsicsSubscriber.HasProvider())
            {
                var fov = m_IntrinsicsSubscriber.GetFieldOfView();
                if (fov.HasValue)
                    SetFOV(fov.Value);

                m_IntrinsicsSubscriber.SubscribeFieldOfViewUpdated(SetFOV);
            }
        }

        void OnDisable()
        {
            this.UnsubscribePoseUpdated(OnPoseUpdated);
            this.UnsubscribeTrackingTypeChanged(OnTrackingStateChanged);
            if (m_IntrinsicsSubscriber.HasProvider())
                m_IntrinsicsSubscriber.UnsubscribeFieldOfViewUpdated(SetFOV);
        }

        void OnTrackingStateChanged(MRCameraTrackingState state)
        {
            switch (state)
            {
                case MRCameraTrackingState.Normal:
                    m_TrackingWarning.SetActive(false);
                    break;
                default:
                    m_TrackingWarning.SetActive(true);
                    break;
            }
        }

        void OnPoseUpdated(Pose pose)
        {
            var projectionMatrix = this.GetProjectionMatrix();
            if (projectionMatrix.HasValue)
                m_Camera.projectionMatrix = projectionMatrix.Value;

            transform.SetLocalPose(pose);
        }

        void SetFOV(float fov) { m_Camera.fieldOfView = fov; }
    }
}
