using System;
using Unity.MARS.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.MARS
{
    [Serializable]
    public abstract partial class ObjectCreationData : ScriptableObject
    {
        public enum CreateInContext
        {
            Scene,
            SyntheticSimulation
        }

        [FormerlySerializedAs("m_ButtonName")]
        [SerializeField]
        protected string m_ObjectName = "Name not set";

        [SerializeField]
        protected DarkLightIconPair m_Icon;

        [SerializeField]
        protected string m_Tooltip;

        [SerializeField]
        CreateInContext m_CreateInContext = CreateInContext.Scene;

        public string ObjectName => m_ObjectName;

        GUIContent m_ObjectGUIContent;

        public CreateInContext CreateInContextSelection => m_CreateInContext;

        public GUIContent ObjectGUIContent
        {
            get
            {
                if(m_ObjectGUIContent == null)
                    UpdateObjectGUIContent();
                return m_ObjectGUIContent;
            }

            private set => m_ObjectGUIContent = value;
        }

        internal void UpdateObjectGUIContent()
        {
            if (m_ObjectGUIContent == null)
            {
                m_ObjectGUIContent = new GUIContent(m_ObjectName, m_Icon.Icon, m_Tooltip);
            }
            else
            {
                m_ObjectGUIContent.image = m_Icon.Icon;
                m_ObjectGUIContent.text = m_ObjectName;
                m_ObjectGUIContent.tooltip = m_Tooltip;
            }
        }

        public abstract bool CreateGameObject(out GameObject createdObj, Transform parentTransform);

        protected GameObject GenerateInitialGameObject(string objName, Transform parent)
        {
            var go = new GameObject(GameObjectUtility.GetUniqueNameForSibling(parent, objName));
            MarsWorldScaleModule.ScaleChildren(go.transform);

            GameObjectUtility.SetParentAndAlign(go, parent != null ? parent.gameObject : null);

            foreach (var colorComponent in go.GetComponentsInChildren<IHasEditorColor>())
            {
                colorComponent.SetNewColor(true, true);
            }

            return go;
        }
    }
}
