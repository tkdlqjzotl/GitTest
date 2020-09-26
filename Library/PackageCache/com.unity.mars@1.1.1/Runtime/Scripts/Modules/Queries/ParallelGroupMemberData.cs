using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.MARS.Query;
using UnityEngine;

namespace Unity.MARS
{
    /// <summary>
    /// Stores all data for each set member in a number of parallel lists
    /// </summary>
    class ParallelGroupMemberData : ParallelSparseArrays
    {
        /// <summary>Maps a query match to all indices it is at</summary>
        public readonly Dictionary<QueryMatchID, List<int>> MatchIdToIndex;

        /// <summary>The unique identifier for each Proxy</summary>
        public QueryMatchID[] QueryMatchIds;
        /// <summary>The data use rule for each Proxy</summary>
        public Exclusivity[] Exclusivities;
        /// <summary>Whether each proxy is required for its Group match</summary>
        public bool[] Required;
        /// <summary>All defined conditions for this Proxy</summary>
        public ProxyConditions[] Conditions;
        /// <summary>All defined trait requirements for this Proxy</summary>
        public ProxyTraitRequirements[] TraitRequirements;
        /// <summary>Collections of references to the trait values the Conditions for each Proxy needs</summary>
        public CachedTraitCollection[] CachedTraits;
        /// <summary>Ratings for all individual Conditions on each Proxy</summary>
        public ConditionRatingsData[] ConditionRatings;
        /// <summary>The set of all data IDs that passed all Conditions on each Proxy</summary>
        public HashSet<int>[] ConditionMatchSets;
        /// <summary>Maps each data ID that passed all Conditions to the rating they receive for this proxy</summary>
        public Dictionary<int, float>[] ReducedConditionRatings;
        /// <summary>The data id assigned to each proxy. ReservedDataIds.Invalid if a proxy is not matched</summary>
        public int[] BestMatchDataIds;
        /// <summary>The data passed to users when a query matches, in OnMatchAcquire</summary>
        public QueryResult[] QueryResults;
        /// <summary>References to each member Proxy's object</summary>
        public IMRObject[] ObjectReferences;
        /// <summary>For each member proxy, an array of relation indices it is a member of</summary>
        public RelationMembership[][] RelationMemberships;

        public void ClearCycleIndices()
        {
            FilteredAcquiringIndices.Clear();
            PotentialMatchAcquiringIndices.Clear();
            DefiniteMatchAcquireIndices.Clear();
        }

        public bool Remove(QueryMatchID id)
        {
            List<int> indices;
            if (!MatchIdToIndex.TryGetValue(id, out indices))
                return false;

            MatchIdToIndex.Remove(id);
            foreach (var index in indices)
            {
                QueryMatchIds[index] = QueryMatchID.NullQuery;
                // the query result can still be in use (by the onLoss event), so we don't pool/recycle it
                QueryResults[index] = null;
                ObjectReferences[index] = null;

                Pools.TraitCaches.Recycle(CachedTraits[index]);
                CachedTraits[index] = null;
                Pools.ConditionRatings.Recycle(ConditionRatings[index]);
                ConditionRatings[index] = null;

                // these collection members are re-usable, but we null them out so it's clear what data is invalid
                Pools.DataIdHashSets.Recycle(ConditionMatchSets[index]);
                ConditionMatchSets[index] = null;
                Pools.Ratings.Recycle(ReducedConditionRatings[index]);
                ReducedConditionRatings[index] = null;
                TraitRequirements[index] = null;

                Exclusivities[index] = default;
                BestMatchDataIds[index] = (int) ReservedDataIDs.Invalid;
                Conditions[index] = null;
                Required[index] = false;

                FreeIndex(index);
                UpdatingIndices.Remove(index);
                AcquiringIndices.Remove(index);
                m_Count--;
            }

            return true;
        }

        public void Clear()
        {
            m_Count = 0;
            MatchIdToIndex.Clear();

            ValidIndices.Clear();
            FreedIndices.Clear();
            FilteredAcquiringIndices.Clear();
            AcquiringIndices.Clear();
            DefiniteMatchAcquireIndices.Clear();
            UpdatingIndices.Clear();

            Array.Clear(QueryMatchIds, 0, QueryMatchIds.Length);
            Array.Clear(Exclusivities, 0, Exclusivities.Length);
            Array.Clear(Required, 0, Required.Length);
            Array.Clear(Conditions, 0, Conditions.Length);
            Array.Clear(TraitRequirements, 0, TraitRequirements.Length);
            Array.Clear(CachedTraits, 0, CachedTraits.Length);
            Array.Clear(ConditionRatings, 0, ConditionRatings.Length);
            Array.Clear(ConditionMatchSets, 0, ConditionMatchSets.Length);
            Array.Clear(ReducedConditionRatings, 0, ReducedConditionRatings.Length);
            Array.Clear(BestMatchDataIds, 0, BestMatchDataIds.Length);
            Array.Clear(QueryResults, 0, QueryResults.Length);
            Array.Clear(ObjectReferences, 0, ObjectReferences.Length);
            Array.Clear(RelationMemberships, 0, RelationMemberships.Length);
        }

        public ParallelGroupMemberData(int initialCapacity) : base(initialCapacity)
        {
            MatchIdToIndex = new Dictionary<QueryMatchID, List<int>>(initialCapacity);

            QueryMatchIds = new QueryMatchID[initialCapacity];
            Required = new bool[initialCapacity];
            Exclusivities = new Exclusivity[initialCapacity];
            Conditions = new ProxyConditions[initialCapacity];
            TraitRequirements = new ProxyTraitRequirements[initialCapacity];
            CachedTraits = new CachedTraitCollection[initialCapacity];
            ConditionRatings = new ConditionRatingsData[initialCapacity];
            ReducedConditionRatings = new Dictionary<int, float>[initialCapacity];
            ConditionMatchSets = new HashSet<int>[initialCapacity];
            
            BestMatchDataIds = new int[initialCapacity];
            
            QueryResults = new QueryResult[initialCapacity];
            ObjectReferences = new IMRObject[initialCapacity];
            RelationMemberships = new RelationMembership[initialCapacity][];
        }

        public void Register(QueryMatchID id, Dictionary<IMRObject, SetChildArgs> childArgs)
        {
            MatchIdToIndex.Add(id, new List<int>());
            foreach (var kvp in childArgs)
            {
                Register(id, kvp.Key, kvp.Value);
            }
        }

        public int Register(QueryMatchID id, IMRObject obj, SetChildArgs args)
        {
            var index = Add(id,
                obj,
                args.tryBestMatchArgs.exclusivity,
                (int) ReservedDataIDs.Invalid,
                args.required,
                new CachedTraitCollection(args.tryBestMatchArgs.conditions),
                args.tryBestMatchArgs.conditions,
                args.TraitRequirements,
                Pools.ConditionRatings.Get().Initialize(args.tryBestMatchArgs.conditions),
                Pools.DataIdHashSets.Get(),
                Pools.Ratings.Get(),
                new QueryResult {queryMatchId = id});

            AcquiringIndices.Add(index);

            List<int> tempIndices;
            if (MatchIdToIndex.TryGetValue(id, out tempIndices))
            {
                tempIndices.Add(index);
            }
            else
            {
                tempIndices = new List<int>{index};
                MatchIdToIndex[id] = tempIndices;
            }

            return index;
        }

        int Add(QueryMatchID matchId,
                IMRObject objectRef,
                Exclusivity exclusivity,
                int bestMatchDataId,
                bool isRequired,
                CachedTraitCollection traitCache,
                ProxyConditions condition,
                ProxyTraitRequirements requirements,
                ConditionRatingsData rating,
                HashSet<int> matchSet,
                Dictionary<int, float> flatRatings,
                QueryResult result)
        {
            var index = GetInsertionIndex();

            QueryMatchIds[index] = matchId;
            ObjectReferences[index] = objectRef;
            Exclusivities[index] = exclusivity;
            Required[index] = isRequired;
            BestMatchDataIds[index] = bestMatchDataId;
            CachedTraits[index] = traitCache;
            Conditions[index] = condition;
            TraitRequirements[index] = requirements;
            RelationMemberships[index] = null;
            ConditionRatings[index] = rating;
            ReducedConditionRatings[index] = flatRatings;
            ConditionMatchSets[index] = matchSet;
            QueryResults[index] = result;

            m_Count++;
            ValidIndices.Add(index);
            return index;
        }

        /// <summary>
        /// Try to find the indices (into the set member data) of both members of a relation
        /// </summary>
        /// <param name="queryMatchId">ID of the query match</param>
        /// <param name="context2">Object reference to the first member</param>
        /// <param name="context1">Object reference to the second member</param>
        /// <param name="index1">Index of the first member</param>
        /// <param name="index2">Index of the second member</param>
        /// <returns>True if both member's indices are found, false otherwise.</returns>
        public bool TryGetRelationMemberIndices(QueryMatchID queryMatchId, IMRObject context1, IMRObject context2,
            out int index1, out int index2)
        {
            List<int> indices;
            if (!MatchIdToIndex.TryGetValue(queryMatchId, out indices))
            {
                index1 = -1;
                index2 = -1;
                return false;
            }

            int tempIndex1 = -1, tempIndex2 = -1;
            foreach (var storageIndex in indices)
            {
                var storedContext = ObjectReferences[storageIndex];
                if (storedContext == context1)
                    tempIndex1 = storageIndex;
                else if (storedContext == context2)
                    tempIndex2 = storageIndex;
            }

            if (tempIndex1 > -1 && tempIndex2 > -1)
            {
                index1 = tempIndex1;
                index2 = tempIndex2;
                return true;
            }


            index1 = -1;
            index2 = -1;
            return false;
        }

        internal override void Resize(int newSize)
        {
            var oldCapacity = m_Capacity;
            m_Capacity = newSize;
            Array.Resize(ref QueryMatchIds, newSize);
            Array.Resize(ref Exclusivities, newSize);
            Array.Resize(ref Required, newSize);
            Array.Resize(ref Conditions, newSize);
            Array.Resize(ref TraitRequirements, newSize);
            Array.Resize(ref CachedTraits, newSize);
            Array.Resize(ref ConditionRatings, newSize);
            Array.Resize(ref ConditionMatchSets, newSize);
            Array.Resize(ref ReducedConditionRatings, newSize);
            
            Array.Resize(ref BestMatchDataIds, newSize);
            for (int i = oldCapacity; i < newSize; i++)
            {
                BestMatchDataIds[i] = (int) ReservedDataIDs.Invalid;
            }

            Array.Resize(ref QueryResults, newSize);
            Array.Resize(ref ObjectReferences, newSize);
            Array.Resize(ref RelationMemberships, newSize);
            OnResize?.Invoke();
        }
    }
}
