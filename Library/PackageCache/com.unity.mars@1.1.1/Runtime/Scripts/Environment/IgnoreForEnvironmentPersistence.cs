using UnityEngine;

namespace Unity.MARS
{
    /// <summary>
    /// Changes to this game object will be ignored when checking for modifications to the simulation environment.
    /// This can be used for animated or otherwise changing
    /// objects in a simulation environment, eg. a moving fan or a dog walking around the environment.
    /// </summary>
    [ExecuteInEditMode]
    class IgnoreForEnvironmentPersistence : MonoBehaviour
    {
        HideFlags m_InitialFlags;

        void Awake()
        {
            m_InitialFlags = gameObject.hideFlags;
        }

        internal void SetDontSaveFlags()
        {
            gameObject.hideFlags = HideFlags.DontSave;
        }

        internal void RestoreHideFlags()
        {
            gameObject.hideFlags = m_InitialFlags;
        }
    }
}
