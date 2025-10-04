// --------------------------------------------------------------------------------------
// File: IStorageWithPositionMulti.cs
// Purpose: Simple interface for any storage building that needs to provide its map position
// Main Interface: IStorageWithPositionMulti
// Description:
//   - Provides a single method GetPosition() for multi-floor storage units.
//   - Used for cross-floor linking logic, global lookups, and registration.
//   - Allows components and managers to treat all such storages uniformly.
// --------------------------------------------------------------------------------------

using Verse;

namespace MultiFloorStorage.Components
{
    /// <summary>
    /// Interface for any multi-floor storage unit to provide its map position.
    /// Used for cross-map/floor linking and registry.
    /// </summary>
    public interface IStorageWithPositionMulti
    {
        IntVec3 GetPosition();
    }
}
