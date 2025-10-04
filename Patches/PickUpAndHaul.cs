using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace MultiFloorStorage.Patches
{
    /// <summary>
    /// Adds runtime compatibility with Pick Up And Haul by injecting IHoldMultipleThings into storage buildings.
    /// </summary>
    public static class PickUpAndHaulCompatibilityMulti
    {
        static PickUpAndHaulCompatibilityMulti()
        {
            // Check if PUAH is active
            if (!LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "Mehni.PickUpAndHaul"))
                return;

            var assemblyName = new AssemblyName("MFS_PUAH_Compatibility_Assembly");
            var ab = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule(assemblyName.Name);

            // Get the interface from PUAH
            var holdInterface = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypesSafe())
                .FirstOrDefault(t => t.FullName == "IHoldMultipleThings.IHoldMultipleThings");

            if (holdInterface == null)
            {
                Log.Error("[MFS] PUAH loaded but IHoldMultipleThings interface not found. Hauling compatibility may fail.");
                return;
            }

            var baseType = typeof(Buildings.Building_MassStorageUnitMulti);

            foreach (var storageType in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t != null && baseType.IsAssignableFrom(t) && !t.IsAbstract))
            {
                // Dynamically create subclass with the interface added
                var tb = mb.DefineType(
                    "MFS_PUAH_" + storageType.Name,
                    TypeAttributes.Public | TypeAttributes.Class,
                    storageType,
                    new[] { holdInterface }
                );
                tb.DefineDefaultConstructor(MethodAttributes.Public);
                var newType = tb.CreateType();

                // Replace ThingDef's thingClass with the new type
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.thingClass == storageType)
                        def.thingClass = newType;
                }
            }
        }

        // Safe type fetch to avoid exceptions during mod reflection
        private static Type[] GetTypesSafe(this Assembly asm)
        {
            try { return asm.GetTypes(); }
            catch { return Array.Empty<Type>(); }
        }
    }
}