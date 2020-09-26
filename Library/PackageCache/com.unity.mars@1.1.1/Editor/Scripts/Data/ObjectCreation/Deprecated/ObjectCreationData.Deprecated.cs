﻿using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS
{
    [MovedFrom("Unity.MARS")]
    partial class ObjectCreationData
    {
        [Obsolete("CreateGameObject() has been deprecated. Use CreateGameObject(out GameObject createdObj, Transform parentTransform) instead.", false)]
        public virtual bool CreateGameObject() { return false; }
    }
}
