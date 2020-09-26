using System;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Query
{
    [Flags]
    [MovedFrom("Unity.MARS")]
    public enum QueryState
    {
        Unknown = 1,
        Unavailable = 2,
        Querying = 4,
        Tracking = 8,
        Acquiring = Tracking | 16,
        Resuming = Querying | 32
    }
}
