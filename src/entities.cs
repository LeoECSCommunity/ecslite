// ----------------------------------------------------------------------------
// The MIT License
// Lightweight ECS framework https://github.com/Leopotam/ecslite
// Copyright (c) 2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsLite {
    public struct EcsPackedEntity {
        internal int Id;
        internal int Gen;
    }

    public struct EcsPackedWithWorldEntity {
        internal int Id;
        internal int Gen;
        internal EcsWorld World;
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public static class EcsEntityExtensions {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static EcsPackedEntity PackEntity (this EcsWorld world, int entity) {
            EcsPackedEntity packed;
            packed.Id = entity;
            packed.Gen = world.GetEntityGen (entity);
            return packed;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool Unpack (this in EcsPackedEntity packed, EcsWorld world, out int entity) {
            if (!world.IsAlive () || !world.IsEntityAlive (packed.Id) || world.GetEntityGen (packed.Id) != packed.Gen) {
                entity = -1;
                return false;
            }
            entity = packed.Id;
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static EcsPackedWithWorldEntity PackEntityWithWorld (this EcsWorld world, int entity) {
            EcsPackedWithWorldEntity packed;
            packed.World = world;
            packed.Id = entity;
            packed.Gen = world.GetEntityGen (entity);
            return packed;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool Unpack (this in EcsPackedWithWorldEntity packed, out EcsWorld world, out int entity) {
            if (!packed.World.IsAlive () || !packed.World.IsEntityAlive (packed.Id) || packed.World.GetEntityGen (packed.Id) != packed.Gen) {
                world = null;
                entity = -1;
                return false;
            }
            world = packed.World;
            entity = packed.Id;
            return true;
        }
    }
}