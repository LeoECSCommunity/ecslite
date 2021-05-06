// ----------------------------------------------------------------------------
// The MIT License
// Lightweight ECS framework https://github.com/Leopotam/ecslite
// Copyright (c) 2021 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsLite {
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsFilter {
        readonly EcsWorld _world;
        readonly Mask _mask;
        int[] _entities;
        int _entitiesCount;
        internal readonly Dictionary<int, int> EntitiesMap;
        int _lockCount;
        DelayedOp[] _delayedOps;
        int _delayedOpsCount;

        internal EcsFilter (EcsWorld world, Mask mask, int capacity = 512) {
            _world = world;
            _mask = mask;
            _entities = new int[capacity];
            EntitiesMap = new Dictionary<int, int> (capacity);
            _entitiesCount = 0;
            _delayedOps = new DelayedOp[512];
            _delayedOpsCount = 0;
            _lockCount = 0;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public EcsWorld GetWorld () {
            return _world;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public int GetEntitiesCount () {
            return _entitiesCount;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator () {
            _lockCount++;
            return new Enumerator (this);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal Mask GetMask () {
            return _mask;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void AddEntity (int entity) {
            if (AddDelayedOp (true, entity)) { return; }
            if (_entitiesCount == _entities.Length) {
                Array.Resize (ref _entities, _entitiesCount << 1);
            }
            EntitiesMap[entity] = _entitiesCount;
            _entities[_entitiesCount++] = entity;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        internal void RemoveEntity (int entity) {
            if (AddDelayedOp (false, entity)) { return; }
            var idx = EntitiesMap[entity];
            EntitiesMap.Remove (entity);
            _entitiesCount--;
            if (idx < _entitiesCount) {
                _entities[idx] = _entities[_entitiesCount];
                EntitiesMap[_entities[idx]] = idx;
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        bool AddDelayedOp (bool added, int entity) {
            if (_lockCount <= 0) { return false; }
            if (_delayedOpsCount == _delayedOps.Length) {
                Array.Resize (ref _delayedOps, _delayedOpsCount << 1);
            }
            ref var op = ref _delayedOps[_delayedOpsCount++];
            op.Added = added;
            op.Entity = entity;
            return true;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        void Unlock () {
#if DEBUG
            if (_lockCount <= 0) {
                throw new Exception ($"Invalid lock-unlock balance for \"{GetType ().Name}\".");
            }
#endif
            _lockCount--;
            if (_lockCount == 0 && _delayedOpsCount > 0) {
                for (int i = 0, iMax = _delayedOpsCount; i < iMax; i++) {
                    ref var op = ref _delayedOps[i];
                    if (op.Added) {
                        AddEntity (op.Entity);
                    } else {
                        RemoveEntity (op.Entity);
                    }
                }
                _delayedOpsCount = 0;
            }
        }

        public struct Enumerator : IDisposable {
            readonly EcsFilter _filter;
            readonly int[] _entities;
            readonly int _count;
            int _idx;

            public Enumerator (EcsFilter filter) {
                _filter = filter;
                _entities = filter._entities;
                _count = _filter._entitiesCount;
                _idx = -1;
            }

            public int Current {
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                get => _entities[_idx];
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public bool MoveNext () {
                return ++_idx < _count;
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public void Dispose () {
                _filter.Unlock ();
            }
        }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
        public sealed class Mask {
            EcsWorld _world;
            internal int[] Include;
            internal int[] Exclude;
            internal int IncludeCount;
            internal int ExcludeCount;
            internal int Hash;

            static readonly object SyncObj = new object ();
            static Mask[] _pool = new Mask[32];
            static int _poolCount;
#if DEBUG
            bool _built;
#endif

            Mask () {
                Include = new int[8];
                Exclude = new int[2];
                Reset ();
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            void Reset () {
                _world = null;
                IncludeCount = 0;
                ExcludeCount = 0;
                Hash = 0;
#if DEBUG
                _built = false;
#endif
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public Mask Inc<T> () where T : struct {
                var poolId = _world.GetPool<T> ().GetId ();
#if DEBUG
                if (_built) { throw new Exception ("Cant change built mask."); }
                if (Array.IndexOf (Include, poolId, 0, IncludeCount) != -1) { throw new Exception ($"{typeof (T).Name} already in constraints list."); }
                if (Array.IndexOf (Exclude, poolId, 0, ExcludeCount) != -1) { throw new Exception ($"{typeof (T).Name} already in constraints list."); }
#endif
                if (IncludeCount == Include.Length) { Array.Resize (ref Include, IncludeCount << 1); }
                Include[IncludeCount++] = poolId;
                return this;
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public Mask Exc<T> () where T : struct {
                var poolId = _world.GetPool<T> ().GetId ();
#if DEBUG
                if (_built) { throw new Exception ("Cant change built mask."); }
                if (Array.IndexOf (Include, poolId, 0, IncludeCount) != -1) { throw new Exception ($"{typeof (T).Name} already in constraints list."); }
                if (Array.IndexOf (Exclude, poolId, 0, ExcludeCount) != -1) { throw new Exception ($"{typeof (T).Name} already in constraints list."); }
#endif
                if (ExcludeCount == Exclude.Length) { Array.Resize (ref Exclude, ExcludeCount << 1); }
                Exclude[ExcludeCount++] = poolId;
                return this;
            }

            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            public EcsFilter End (int capacity = 512) {
#if DEBUG
                if (_built) { throw new Exception ("Cant change built mask."); }
                _built = true;
#endif
                Array.Sort (Include, 0, IncludeCount);
                Array.Sort (Exclude, 0, ExcludeCount);
                // calculate hash.
                Hash = IncludeCount + ExcludeCount;
                for (int i = 0, iMax = IncludeCount; i < iMax; i++) {
                    Hash = unchecked (Hash * 314159 + Include[i]);
                }
                for (int i = 0, iMax = ExcludeCount; i < iMax; i++) {
                    Hash = unchecked (Hash * 314159 - Exclude[i]);
                }
                var (filter, isNew) = _world.GetFilterInternal (this, capacity);
                if (!isNew) { Recycle (); }
                return filter;
            }

            void Recycle () {
                Reset ();
                lock (SyncObj) {
                    if (_poolCount == _pool.Length) {
                        Array.Resize (ref _pool, _poolCount << 1);
                    }
                    _pool[_poolCount++] = this;
                }
            }

            internal static Mask New (EcsWorld world) {
                lock (SyncObj) {
                    var mask = _poolCount > 0 ? _pool[--_poolCount] : new Mask ();
                    mask._world = world;
                    return mask;
                }
            }
        }

        struct DelayedOp {
            public bool Added;
            public int Entity;
        }
    }
}