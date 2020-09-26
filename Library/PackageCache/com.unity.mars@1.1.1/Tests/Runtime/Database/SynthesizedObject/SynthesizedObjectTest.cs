#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.MARS.Simulation;
using Unity.MARS.Tests;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.MARS.Data.Tests
{
    /// <summary>
    /// Base class used for each Synthesized Object test case.
    /// Responsible for clearing out the backend and database to a base state and creating any extra providers
    /// </summary>
    [AddComponentMenu("")]
    abstract class SynthesizedObjectTest : MarsRuntimeTest
    {
        const int k_FrameCount = 60;

        protected SynthesizedObjectTestSettings m_References;

        public override bool IsTestFinished
        {
            get
            {
                if (MarsTime.FrameCount - m_StartFrame >= k_FrameCount)
                {
                    enabled = false;
                    return true;
                }

                return false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_References = SynthesizedObjectTestSettings.instance;
        }
    }
}
#endif
