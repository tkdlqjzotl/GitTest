#if UNITY_EDITOR
using NUnit.Framework;
using Unity.MARS.Data.Tests;
using Unity.MARS.Simulation;
using UnityEngine;

namespace Unity.MARS.Tests
{
    /// <summary>
    /// Tests that ProxyGroup sets all member proxies to have the same QueryMatchID, after it registers the query.
    /// This keeps things semantically consistent, as all members are part of the same query instance, so have the same ID.
    /// It's also important to prevent errors with things that rely on the value of 'Proxy.queryID',
    /// </summary>
    [AddComponentMenu("")]
    class GroupMatchIdsAreConsistentTest : MarsRuntimeTest
    {
        bool m_Finished;
        public override bool IsTestFinished => m_Finished;

        protected override void OnMarsUpdate()
        {
            if (MarsTime.FrameCount != 2)
                return;

            var groupObject = InstantiateReferenceObject(QueryTestObjectSettings.instance.SimpleProxyGroup);
            var group = groupObject.GetComponent<ProxyGroup>();
            var proxies = groupObject.GetComponentsInChildren<Proxy>();

            Assert.Greater(proxies.Length, 0);
            foreach (var memberProxy in proxies)
            {
                Assert.AreEqual(group.queryID, memberProxy.queryID);
            }

            Destroy(groupObject);
            m_Finished = true;
            enabled = false;
        }
    }
}
#endif
