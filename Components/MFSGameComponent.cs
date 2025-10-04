// --------------------------------------------------------------------------------------
// File: MFSGameComponent.cs
// Purpose: GameComponent for MultiFloorStorage; manages assembler queues globally.
// Main Class: MFSGameComponent
// Description:
//   - Holds a global list of assembler queues (IAssemblerQueue) for custom recipe/automation logic.
//   - Provides registration/deregistration methods for queues.
//   - Used for global coordination of assembly/building tasks in the MultiFloorStorage system.
// --------------------------------------------------------------------------------------

using ProjectRimFactory.Common.HarmonyPatches;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MultiFloorStorage.Components
{
    /// <summary>
    /// Global GameComponent for MultiFloorStorage. Manages assembler queues across all maps/games.
    /// </summary>
    public class MFSGameComponent : GameComponent
    {
        // Master list of registered assembler queues (for automated crafting/processing)
        public List<IAssemblerQueue> AssemblerQueue = new List<IAssemblerQueue>();

        public MFSGameComponent(Game game) { }

        public void RegisterAssemblerQueue(IAssemblerQueue queue)
        {
            if (!AssemblerQueue.Contains(queue))
                AssemblerQueue.Add(queue);
        }

        public void DeRegisterAssemblerQueue(IAssemblerQueue queue)
        {
            AssemblerQueue.Remove(queue);
        }
    }
}
