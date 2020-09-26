#if UNITY_EDITOR
using System.Diagnostics;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

#if !NET_4_6
using Unity.XRTools.Utils;
#endif

namespace Unity.MARS.Data.Tests
{
    [AddComponentMenu("")]
    abstract class TimedPerformanceTest : MonoBehaviour, IMonoBehaviourTest
    {
        protected float m_StartTime;
        protected float m_Duration = 1f;

        protected static int s_DataCount = 1000;
        protected int m_SampleIndex;

        protected readonly long[] m_ElapsedTickSamples = new long[s_DataCount];
        protected static Stopwatch s_Stopwatch = new Stopwatch();

        public bool IsTestFinished
        {
            get
            {
                if (Time.time - m_StartTime >= m_Duration)
                {
                    LogResults();
                    enabled = false;
                    return true;
                }

                return false;
            }
        }

        protected virtual void Awake()
        {
            m_StartTime = Time.time;
        }

        protected virtual void LogResults()
        {
            var sum = 0f;
            for (int i = 0; i < m_SampleIndex; i++)
            {
                sum += m_ElapsedTickSamples[i];
            }

            Debug.LogFormat("Total ticks elapsed: {0} , Total calls {1}", sum, m_SampleIndex * s_DataCount);
            Debug.LogFormat("Average time: {0} , for {1} calls per frame", sum / m_SampleIndex, s_DataCount);
        }

        // you have to re-implement Update following this pattern
        protected virtual void Update()
        {
            s_Stopwatch.Restart();
            for (var i = 0; i < s_DataCount; i++)
            {
                // do stuff
            }
            s_Stopwatch.Stop();

            m_ElapsedTickSamples[m_SampleIndex] = s_Stopwatch.ElapsedTicks;
            m_SampleIndex++;
        }

        protected virtual void OnDestroy() { }
    }
}
#endif
