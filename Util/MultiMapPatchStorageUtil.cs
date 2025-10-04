// MultiMapStorageUtil.cs
// Multi-floor safe cache + MFSMapComponent access, no PRF dependencies

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using MultiFloorStorage.Components;
using ProjectRimFactory.Common;

namespace MultiFloorStorage.Util
{
    public static class MultiMapStorageUtil
    {
        private static readonly Dictionary<ValueTuple<int, IntVec3, Type>, object> cache = new();
        private static int lastTick = -1;

        private static readonly Dictionary<int, MFSMapComponent> mapComps = new();

        public static bool SkippAcceptsPatch = false;

        /// <summary>
        /// Get MFSMapComponent for a map (registers and caches if missing).
        /// </summary>
        public static MFSMapComponent GetMFSMapComponent(Map map)
        {
            if (map == null)
                return null;

            int id = map.uniqueID;
            if (!mapComps.TryGetValue(id, out var comp))
            {
                comp = map.GetComponent<MFSMapComponent>();
                if (comp != null)
                {
                    mapComps[id] = comp;
                    Log.Message($"[MFS] Cached MFSMapComponent for mapID: {id}");
                }
                else
                {
                    Log.Warning($"[MFS] MFSMapComponent missing on mapID: {id}");
                }
            }
            return comp;
        }

        /// <summary>
        /// Safely get a Thing of type T at a position on a given map.
        /// </summary>
        public static T Get<T>(Map map, IntVec3 pos) where T : class
        {
            return pos.IsValid ? pos.GetFirst<T>(map) : null;
        }

        /// <summary>
        /// Cached Thing lookup with per-tick clearing.
        /// </summary>
        public static T GetWithTickCache<T>(Map map, IntVec3 pos) where T : class
        {
            if (Find.TickManager.TicksGame != lastTick)
            {
                cache.Clear();
                lastTick = Find.TickManager.TicksGame;
                Log.Message("[MFS] Cleared per-tick cache.");
            }

            var key = new ValueTuple<int, IntVec3, Type>(map.uniqueID, pos, typeof(T));
            if (!cache.TryGetValue(key, out var val))
            {
                val = Get<T>(map, pos);
                cache[key] = val;
                Log.Message($"[MFS] Cached {typeof(T).Name} at {pos} on mapID {map.uniqueID}");
            }

            return (T)val;
        }
    }

    // Interface used for hiding items (used by DSUs and hiding systems)
}