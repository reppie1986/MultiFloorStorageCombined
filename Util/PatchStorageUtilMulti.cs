// --------------------------------------------------------------------------------------
// File: PatchStorageUtilMulti.cs
// Purpose: Core utility for MultiFloorStorage patches requiring storage building access by position.
// - Provides type-safe lookup helpers (Get, GetWithTickCache) using map + position.
// - Adds per-tick caching for repeated lookups in GetWithTickCache.
// - Maintains a map-to-MFSMapComponent lookup table.
// - Global flag SkipAcceptsPatch allows toggling Accepts() patches.
// --------------------------------------------------------------------------------------

using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using MultiFloorStorage.Components; // Reference to the MFS map component
using ProjectRimFactory.Common; // Common infrastructure

namespace MultiFloorStorage.Util
{
    public static class PatchStorageUtilMulti
    {
        private static Dictionary<Tuple<Map, IntVec3, Type>, object> cache = new(); // Tick cache for lookups
        private static int lastTick = 0; // Cache reset marker
        private static Dictionary<Map, MFSMapComponent> mapComps = new(); // Map to component cache

        public static bool SkipAcceptsPatch = false; // Global flag for bypassing Accepts() patch

        // Retrieves and caches the MFS map component per map
        public static MFSMapComponent GetMFSMapComponent(Map map)
        {
            if (map == null) return null;

            if (!mapComps.TryGetValue(map, out var outval))
            {
                outval = map.GetComponent<MFSMapComponent>();
                mapComps.Add(map, outval);
            }

            return outval;
        }

        // Direct typed lookup of object at given position
        public static T Get<T>(Map map, IntVec3 pos) where T : class
        {
            return pos.IsValid ? pos.GetFirst<T>(map) : null;
        }

        // Caches typed position lookups for one tick to reduce redundant GetFirst calls
        public static T GetWithTickCache<T>(Map map, IntVec3 pos) where T : class
        {
            // Clear cache once per tick
            if (Find.TickManager.TicksGame != lastTick)
            {
                cache.Clear();
                lastTick = Find.TickManager.TicksGame;
            }

            // Compose key and look up or populate
            var key = new Tuple<Map, IntVec3, Type>(map, pos, typeof(T));
            if (!cache.TryGetValue(key, out object val))
            {
                val = Get<T>(map, pos);
                cache[key] = val;
            }

            return val as T;
        }
    }
}