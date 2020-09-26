using System;
using System.Collections;
using NUnit.Framework;
using Unity.MARS.Providers;
using Unity.MARS.Providers.Google;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.MARS.Tests
{
    class GoogleCloudDataStorageModuleTests
    {
        IProvidesCloudDataStorage m_Module;

        const string k_Key1 = "001";
        const string k_Key2 = "002";
        const string k_Key3 = "003";
        const string k_Data1 = "abcdef";
        const string k_Data2 = "{\"$type\" \"Unity.MARS.Companion.Core.ResourceList, Unity.MARS.Companion.Core\",\"m_Resources\" {\"$type\" \"System.Collections.Generic.List`1[System.Object], mscorlib\", \"$elements\" [\"class\", \"null\", \"!@#$%^&*()\\\\_+-=,.<>/?|:;\", \"private\", \"void\"]},\"m_BundleResources\" {\"$type\" \"System.Collections.Generic.List`1[System.Object], mscorlib\",\"$elements\" []}}"; // Test including nonsense special characters and words
        static readonly byte[] k_Data3 = {54, 68, 69, 73, 20, 69, 73, 20, 61, 20, 74, 65, 73, 74}; // UTF8 encoded "This is a test"

        bool m_SaveDataInCloudDone;
        bool m_SaveDataInCloudAndLoadItBackDone;
        bool m_SaveByteDataInCloudAndLoadItBackDone;

        [OneTimeSetUp]
        public void Setup()
        {
            m_Module = ModuleLoaderCore.instance.GetModule<GoogleCloudDataStorageModule>();
        }

        [SetUp]
        public void BeforeEach()
        {
            m_Module =  ModuleLoaderCore.instance.GetModule<GoogleCloudDataStorageModule>();
        }

        [UnityTest]
        public IEnumerator SaveDataInCloud()
        {
            m_SaveDataInCloudDone = false;

            if (m_Module.IsConnected() == false)
            {
                Assert.Ignore("Could not connect your project to the Unity Cloud");
            }

            m_Module.CloudSaveAsync(k_Key1, k_Data1, SaveDataInCloud_WasSavedProperly);

            while (!m_SaveDataInCloudDone)
            {
                yield return null;
            }
        }

        void SaveDataInCloud_WasSavedProperly(bool saveSuccess, long responseCode, string response)
        {
            m_SaveDataInCloudDone = true;
            if (m_Module.IsConnected())
            {
                Assert.True(saveSuccess);
                Assert.AreEqual(200L, responseCode);
            }
        }

        [UnityTest]
        public IEnumerator SaveDataInCloudAndLoadItBack()
        {
            m_SaveDataInCloudAndLoadItBackDone = false;

            if (m_Module.IsConnected() == false)
            {
                Assert.Ignore("Could not connect your project to the Unity Cloud");
            }

            m_Module.CloudSaveAsync(k_Key2, k_Data2, SaveDataInCloudAndLoadItBack_SaveDone);

            while (!m_SaveDataInCloudAndLoadItBackDone)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SaveByteDataInCloudAndLoadItBack()
        {
            m_SaveByteDataInCloudAndLoadItBackDone = false;

            if (m_Module.IsConnected() == false)
            {
                Assert.Ignore("Could not connect your project to the Unity Cloud");
            }

            m_Module.CloudSaveAsync(k_Key3, k_Data3, SaveByteDataInCloudAndLoadItBack_SaveDone);

            while (!m_SaveByteDataInCloudAndLoadItBackDone)
            {
                yield return null;
            }
        }
        void SaveDataInCloudAndLoadItBack_SaveDone(bool saveSuccess, long responseCode, string response)
        {
            if (!saveSuccess)
            {
                m_SaveDataInCloudAndLoadItBackDone = true;
                if (m_Module.IsConnected())
                {
                    Assert.True(false);
                }
            }
            else
            {
                m_Module.CloudLoadAsync(k_Key2, SaveDataInCloudAndLoadItBack_LoadDone);
            }
        }

        void SaveByteDataInCloudAndLoadItBack_SaveDone(bool saveSuccess, long responseCode, string response)
        {
            if (!saveSuccess)
            {
                m_SaveByteDataInCloudAndLoadItBackDone = true;
                if (m_Module.IsConnected())
                {
                    Assert.True(false);
                }
            }
            else
            {
                m_Module.CloudLoadAsync(k_Key3, SaveByteDataInCloudAndLoadItBack_LoadDone);
            }
        }

        [Test]
        public void SetProjectId()
        {
            var previousId = m_Module.GetProjectIdentifier();
            var testId = "test";
            m_Module.SetProjectIdentifier(testId);
            Assert.AreEqual(testId, m_Module.GetProjectIdentifier());
            m_Module.SetProjectIdentifier(previousId);
        }

        [Test]
        public void SetAPIKey()
        {
            var previousKey = m_Module.GetAPIKey();
            const string testKey = "test";
            m_Module.SetAPIKey(testKey);
            Assert.AreEqual(testKey, m_Module.GetAPIKey());
            m_Module.SetAPIKey(previousKey);
        }

        void SaveDataInCloudAndLoadItBack_LoadDone(bool loadSuccess, long responseCode, string dataLoaded)
        {
            m_SaveDataInCloudAndLoadItBackDone = true;

            if (loadSuccess)
            {
                Assert.AreEqual(k_Data2, dataLoaded);
                Assert.AreEqual(200L, responseCode);
            }
            else
            {
                if (m_Module.IsConnected())
                {
                    Assert.True(false);
                }
            }
        }

        void SaveByteDataInCloudAndLoadItBack_LoadDone(bool loadSuccess, long responseCode, byte[] dataLoaded)
        {
            m_SaveByteDataInCloudAndLoadItBackDone = true;

            if (loadSuccess)
            {
                Assert.AreEqual(k_Data3, dataLoaded);
                Assert.AreEqual(200L, responseCode);
            }
            else
            {
                if (m_Module.IsConnected())
                {
                    Assert.True(false);
                }
            }
        }
    }
}
