#if UNITY_EDITOR
using NUnit.Framework;
using Unity.MARS.Query;
using Unity.MARS.Tests;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.MARS.Data.Tests
{
    [AddComponentMenu("")]
    class StandaloneReplicatorUnMatchingTest : RuntimeQueryTest, IProvidesTraits<Vector2>, IProvidesTraits<Pose>, IUsesQueryResults
    {
        const int k_MaxDataId = 20;

        readonly Vector2 k_FakePlaneBounds = new Vector2(0.5f, 2.5f);

        public GameObject TestPrefab;

        GameObject m_ProxyContent;
        GameObject m_ProxyObject;
        Proxy m_Proxy;

        GameObject m_ReplicatorObject;
        Replicator m_Replicator;

        public override bool IsTestFinished
        {
            get
            {
                if (Time.frameCount - m_StartFrame >= m_FrameCount)
                {
                    enabled = false;
                    return true;
                }

                return false;
            }
        }

        public void Start()
        {
            m_FrameCount = 15;
            TestPrefab = QueryTestObjectSettings.instance.SimpleReplicatedProxy;
            m_QueryBackend = ModuleLoaderCore.instance.GetModule<MARSQueryBackend>();
        }

        protected override void OnMarsUpdate() { }

        protected void Update()
        {
            var frameCount = Time.frameCount - m_StartFrame;
            switch (frameCount)
            {
                case 2:
                    m_ProxyObject = InstantiateReferenceObject(TestPrefab);
                    m_Proxy = m_ProxyObject.GetComponentInChildren<Proxy>();
                    m_ProxyContent = m_Proxy.gameObject.transform.GetChild(0).gameObject;
                    break;
                case 4:
                    Assert.False(m_ProxyContent.activeInHierarchy);
                    break;
                case 5:
                    for (var i = 0; i < k_MaxDataId; i++)
                    {
                        // this data is what the proxy in the prefab needs to match
                        this.AddOrUpdateTrait(i, TraitNames.Bounds2D, k_FakePlaneBounds);
                        this.AddOrUpdateTrait(i, TraitNames.Pose, new Pose());
                    }
                    break;
                case 6:
                    Assert.True(ForceUpdateQueries());
                    break;
                case 7:
                    Assert.True(m_ProxyContent.activeInHierarchy);
                    break;
                case 8:
                    // unmatch this group, and seek a match again.
                    Assert.True(m_Proxy.Unmatch());
                    break;
                case 9:
                    // the child content should now be inactive because the loss handlers have been called
                    Assert.False(m_ProxyContent.activeInHierarchy);
                    // because we're seeking a new match, we shouldn't add this query to the un-set standalone indices
                    Assert.AreEqual(0, m_QueryBackend.UnsetStandaloneMatchIndices.Count);
                    break;
                case 10:
                    Assert.True(ForceUpdateQueries());
                    break;
                case 11:
                    // because we didn't specify to not seek a new match, updating queries again should result in re-activation.
                    Assert.True(m_ProxyContent.activeInHierarchy);
                    break;
                case 12:
                    // unmatch this group again, but don't seek a new match this time.
                    m_Proxy.Unmatch(false);
                    break;;
                case 14:
                    // because we specified to not seek a new match, that spawn instance should be destroyed
                    Assert.True(m_Proxy == null);
                    // not seeking also means we should add this query to the un-set standalone indices
                    Assert.AreEqual(1, m_QueryBackend.UnsetStandaloneMatchIndices.Count);
                    break;
            }
        }
    }
}
#endif
