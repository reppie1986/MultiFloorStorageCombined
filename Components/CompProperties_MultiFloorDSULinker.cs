// --------------------------------------------------------------------------------------
// File: CompProperties_MultiFloorDSULinker.cs
// Purpose: XML-exposed CompProperties for MultiFloor DSU Linker component
// Main Class: CompProperties_MultiFloorDSULinker
// Description:
//   - Allows <ThingDef> XML to specify which DSU defNames this Comp will consider valid link targets.
//   - Used to restrict/whitelist DSUs that can be selected via player UI link menu.
//   - All allowed defNames appear in the float menu for link selection.
//   - Example XML included at bottom of file.
// --------------------------------------------------------------------------------------

using System.Collections.Generic;
using Verse;

namespace MultiFloorStorage.Components
{
    /// <summary>
    /// CompProperties for MultiFloor DSU Linker, exposing allowed DSU defNames for linking (set via XML).
    /// </summary>
    public class CompProperties_MultiFloorDSULinker : CompProperties
    {
        /// <summary>
        /// Only buildings with these defNames will appear as linkable targets in the UI.
        /// </summary>
        public List<string> allowedDSUdefNames = new List<string>();

        public CompProperties_MultiFloorDSULinker()
        {
            compClass = typeof(Comp_MultiFloorDSULinker);
        }
    }
}

// ---
// Sample XML to use this CompProperties in a <ThingDef>:
/*
<comps>
  <li Class="MultiFloorStorage.CompProperties_MultiFloorDSULinker">
    <allowedDSUdefNames>
      <li>PRF_DigitalStorageUnit_I</li>
      <li>PRF_ColdStorageUnit_I</li>
    </allowedDSUdefNames>
  </li>
</comps>
*/
