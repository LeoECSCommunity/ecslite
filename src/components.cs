// ----------------------------------------------------------------------------
// The MIT License
// Lightweight ECS framework https://github.com/Leopotam/ecslite
// Copyright (c) 2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsLite {
    interface IEcsPool {
        void Resize (int capacity);
        bool Has (int entity);
        void Del (int entity);
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsPool<T> : IEcsPool where T : struct {
        readonly EcsWorld _world;
        readonly int _id;
        PoolItem[] _items;

        internal EcsPool (EcsWorld world, int id, int capacity) {
            _world = world;
            _id = id;
            _items = new PoolItem[capacity];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal int GetId () {
            return _id;
        }

        void IEcsPool.Resize (int capacity) {
            Array.Resize (ref _items, capacity);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Add (int entity) {
#if DEBUG
            if (!_world.IsEntityAlive (entity)) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            ref var itemData = ref _items[entity];
#if DEBUG
            if (_world.GetEntityGen (entity) < 0) { throw new Exception ("Cant add component to destroyed entity."); }
            if (itemData.Attached) { throw new Exception ("Already attached."); }
#endif
            itemData.Attached = true;
            _world.OnEntityChange (entity, _id, true);
            _world.Entities[entity].ComponentsCount++;
            return ref itemData.Data;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public ref T Get (int entity) {
#if DEBUG
            if (!_world.IsEntityAlive (entity)) { throw new Exception ("Cant touch destroyed entity."); }
#endif
#if DEBUG
            if (_world.GetEntityGen (entity) < 0) { throw new Exception ("Cant get component from destroyed entity."); }
            if (!_items[entity].Attached) { throw new Exception ("Not attached."); }
#endif
            return ref _items[entity].Data;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public bool Has (int entity) {
#if DEBUG
            if (!_world.IsEntityAlive (entity)) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            return _items[entity].Attached;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void Del (int entity) {
#if DEBUG
            if (!_world.IsEntityAlive (entity)) { throw new Exception ("Cant touch destroyed entity."); }
#endif
            ref var itemData = ref _items[entity];
            if (itemData.Attached) {
                _world.OnEntityChange (entity, _id, false);
                itemData.Attached = false;
                itemData.Data = default;
                ref var entityData = ref _world.Entities[entity];
                entityData.ComponentsCount--;
                if (entityData.ComponentsCount == 0) {
                    _world.DelEntity (entity);
                }
            }
        }

        struct PoolItem {
            public bool Attached;
            public T Data;
        }
    }
}