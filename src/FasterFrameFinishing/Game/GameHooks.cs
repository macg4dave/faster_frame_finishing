using System;
using System.Runtime.CompilerServices;
using Assets.Scripts.Objects;

namespace FasterFrameFinishing.Game
{
    /// <summary>
    /// Thin wrappers around the game's prefab lifecycle. Each member that touches game types is
    /// NoInlining so that, if a game update removes or renames something, the resulting
    /// MissingMemberException / TypeLoadException is thrown at the caller's call site (where it is
    /// caught and logged) rather than while JIT-compiling the plugin's own methods.
    /// </summary>
    internal static class GameHooks
    {
        /// <summary>Verified: <c>public static event Action Prefab.OnPrefabsLoaded</c>, raised once at the end of Prefab.LoadAll().</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SubscribePrefabsLoaded(Action handler) => Prefab.OnPrefabsLoaded += handler;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UnsubscribePrefabsLoaded(Action handler) => Prefab.OnPrefabsLoaded -= handler;

        /// <summary>
        /// True if Prefab.LoadAll() has already registered structure prefabs (e.g. the plugin was
        /// loaded late). Re-applying is harmless because the patcher is idempotent.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool StructurePrefabsAvailable() =>
            Prefab.PrefabsGameObject != null && Structure.AllStructurePrefabs.Count > 0;
    }
}
