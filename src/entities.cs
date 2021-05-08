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

    public struct EcsPackedEntityWithWorld {
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
            if (!world.IsAlive () || !world.IsEntityAliveInternal (packed.Id) || world.GetEntityGen (packed.Id) != packed.Gen) {
                entity = -1;
                return false;
            }
            entity = packed.Id;
            return true;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool Equals (this in EcsPackedEntity a, in EcsPackedEntity b) {
            return a.Id == b.Id && a.Gen == b.Gen;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static EcsPackedEntityWithWorld PackEntityWithWorld (this EcsWorld world, int entity) {
            EcsPackedEntityWithWorld packedEntity;
            packedEntity.World = world;
            packedEntity.Id = entity;
            packedEntity.Gen = world.GetEntityGen (entity);
            return packedEntity;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool Unpack (this in EcsPackedEntityWithWorld packedEntity, out EcsWorld world, out int entity) {
            if (!packedEntity.World.IsAlive () || !packedEntity.World.IsEntityAliveInternal (packedEntity.Id) || packedEntity.World.GetEntityGen (packedEntity.Id) != packedEntity.Gen) {
                world = null;
                entity = -1;
                return false;
            }
            world = packedEntity.World;
            entity = packedEntity.Id;
            return true;
        }
        
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static bool Equals (this in EcsPackedEntityWithWorld a, in EcsPackedEntityWithWorld b) {
            return a.Id == b.Id && a.Gen == b.Gen && a.World == b.World;
        }
    }
}