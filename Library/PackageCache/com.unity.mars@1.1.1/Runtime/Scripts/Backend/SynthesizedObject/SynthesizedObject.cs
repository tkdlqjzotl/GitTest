using System.Collections.Generic;
using Unity.MARS.Attributes;
using Unity.MARS.Query;
using Unity.MARS.Simulation;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils.GUI;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Data.Synthetic
{
    /// <summary>
    /// Class that automatically inserts and updates data in MARS' reality database
    /// Intrinsically linked to the Real World Object it is parented to
    /// </summary>
    [ExcludeInMARSEditor]
    [MovedFrom("Unity.MARS.Data")]
    public class SynthesizedObject : MonoBehaviour, IMatchAcquireHandler, IMatchUpdateHandler, IMatchLossHandler,
        IUsesMARSData<SynthesizedObject>, IUsesFunctionalityInjection
    {
        readonly List<SynthesizedTrackable> m_Data = new List<SynthesizedTrackable>();
        readonly List<SynthesizedTrait> m_Traits = new List<SynthesizedTrait>();
        Proxy m_ParentClient;

        [SerializeField]
        [EnumDisplay(ReservedDataIDs.Unset, ReservedDataIDs.ImmediateEnvironment, ReservedDataIDs.LocalUser)]
        int m_OverrideDataID = (int)ReservedDataIDs.Unset;

        int m_DataID = -1;

        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }

        void OnEnable()
        {
            MarsTime.MarsUpdate += OnMarsUpdate;

            // Get all the traits and data to associate with this object
            GetComponents(m_Data);
            GetComponents(m_Traits);


            this.EnsureFunctionalityInjected();
            foreach (var currentTrait in m_Traits)
            {
                this.InjectFunctionalitySingle(currentTrait);
            }

            // If an acquire call is not going to make us active, call the insert method right away
            if (transform.parent != null)
                m_ParentClient = transform.parent.GetComponentInParent<Proxy>();

            if (m_ParentClient == null || (m_ParentClient.queryState == QueryState.Tracking))
            {
                InitializeData();
            }

            // Otherwise we wait
        }

        void OnMarsUpdate()
        {
            if (m_DataID == -1)
                return;

            if (transform.hasChanged)
            {
                // All trackable data has poses attached, so we update those with transforms
                foreach (var currentData in m_Data)
                {
                    currentData.UpdateSynthData();
                }

                // Traits conditionally change
                foreach (var currentTrait in m_Traits)
                {
                    if (currentTrait.UpdateWithTransform)
                        currentTrait.UpdateTrait(m_DataID);
                }

                transform.hasChanged = false;
            }
        }

        void OnDisable()
        {
            MarsTime.MarsUpdate -= OnMarsUpdate;
            RemoveAllData();
        }

        public void OnMatchAcquire(QueryResult queryResult)
        {
            if (isActiveAndEnabled)
                InitializeData();
        }

        public void OnMatchLoss(QueryResult queryResult)
        {
            if (isActiveAndEnabled)
                RemoveAllData();
        }

        public void OnMatchUpdate(QueryResult queryResult)
        {
            if (isActiveAndEnabled)
            {
                UpdateAllData();
                transform.hasChanged = false;
            }
        }

        /// <summary>
        /// Called if the database that the synthesized object feeds into shuts down.
        /// In that case, we just reset our data ID so we don't try to interact with the database anymore.
        /// </summary>
        public void OnDataBaseLost()
        {
            m_DataID = -1;
        }

        void InitializeData()
        {
            if (m_DataID != -1)
            {
                return;
            }

            if (m_OverrideDataID == (int)ReservedDataIDs.Unset)
            {
                m_DataID = this.AddData(this);
            }
            else
            {
                m_DataID = m_OverrideDataID;
            }

            foreach (var currentData in m_Data)
            {
                currentData.AddSynthData(m_DataID);
            }

            foreach (var currentTrait in m_Traits)
            {
                currentTrait.AddTrait(m_DataID);
            }
        }

        void UpdateAllData()
        {
            foreach (var currentData in m_Data)
            {
                currentData.UpdateSynthData();
            }

            foreach (var currentTrait in m_Traits)
            {
                currentTrait.UpdateTrait(m_DataID);
            }
        }

        void RemoveAllData()
        {
            if (m_DataID == -1)
            {
                return;
            }

            foreach (var currentTrait in m_Traits)
            {
                currentTrait.RemoveTrait(m_DataID);
            }

            foreach (var currentData in m_Data)
            {
                currentData.RemoveSynthData(m_DataID);
            }

            this.RemoveData(m_DataID);
            m_DataID = -1;
        }
    }
}
