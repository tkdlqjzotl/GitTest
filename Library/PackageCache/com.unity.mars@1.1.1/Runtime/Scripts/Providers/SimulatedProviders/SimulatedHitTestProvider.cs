using System;
using Unity.MARS.Data;
using Unity.MARS.MARSUtils;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityObject = UnityEngine.Object;

namespace Unity.MARS.Providers.Synthetic
{
#if UNITY_EDITOR
    [ProviderSelectionOptions(ProviderPriorities.SimulatedProviderPriority)]
    [MovedFrom("Unity.MARS.Providers")]
    public class SimulatedHitTestProvider : IProvidesMRHitTesting
    {
        Camera m_Camera;
        bool m_Enabled;

        void IFunctionalityProvider.LoadProvider()
        {
            m_Camera = MarsRuntimeUtils.GetActiveCamera(true);

            m_Enabled = true;
        }

        void IFunctionalityProvider.ConnectSubscriber(object obj)
        {
            this.TryConnectSubscriber<IProvidesMRHitTesting>(obj);
        }

        void IFunctionalityProvider.UnloadProvider() { }

        public bool ScreenHitTest(Vector2 screenPosition, out MRHitTestResult result, MRHitTestResultTypes types = MRHitTestResultTypes.Any)
        {
            if (m_Camera == null)
            {
                result = default;
                return false;
            }

            var ray = m_Camera.ScreenPointToRay(screenPosition);
            return Raycast(ray, out result);
        }

        public bool WorldHitTest(Ray ray, out MRHitTestResult result, MRHitTestResultTypes types = MRHitTestResultTypes.Any) { return Raycast(ray, out result); }

        bool Raycast(Ray ray, out MRHitTestResult result)
        {
            if (!m_Enabled)
            {
                result = default;
                return false;
            }

            if (Physics.Raycast(ray, out var hit))
            {
                result = new MRHitTestResult();
                result.pose = new Pose(hit.point, Quaternion.LookRotation(hit.normal));
                return true;
            }

            result = default;
            return false;
        }

        public void StopHitTesting() { m_Enabled = false; }
        public void StartHitTesting() { m_Enabled = true; }
    }
#endif
}
