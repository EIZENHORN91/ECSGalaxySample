using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Transforms;

namespace Galaxy
{
    [BurstCompile]
    [UpdateInGroup(typeof(BuildSpatialDatabaseGroup))]
    public partial struct BuildSpatialDatabasesSystem : ISystem
    {
        private EntityQuery _spatialDatabasesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _spatialDatabasesQuery = SystemAPI.QueryBuilder().WithAll<SpatialDatabase, SpatialDatabaseCell, SpatialDatabaseElement>().Build();
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<GameIsSimulating>();
            state.RequireForUpdate<SpatialDatabaseSingleton>();
            state.RequireForUpdate(_spatialDatabasesQuery);
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public void OnUpdate(ref SystemState state)
        {
            Config config = SystemAPI.GetSingleton<Config>();
            SpatialDatabaseSingleton spatialDatabaseSingleton = SystemAPI.GetSingleton<SpatialDatabaseSingleton>();

            if (config.BuildSpatialDatabaseParallel)
            {
                BuildSpatialDatabaseParallelJob buildJob = new BuildSpatialDatabaseParallelJob
                {
                    CachedSpatialDatabase = new CachedSpatialDatabaseUnsafeParallel
                    {
                        SpatialDatabaseEntity = spatialDatabaseSingleton.TargetablesSpatialDatabase,
                        SpatialDatabaseLookup = SystemAPI.GetComponentLookup<SpatialDatabase>(false),
                        CellsBufferLookup = SystemAPI.GetBufferLookup<SpatialDatabaseCell>(false),
                        ElementsBufferLookup = SystemAPI.GetBufferLookup<SpatialDatabaseElement>(false),
                    },
                };
                state.Dependency = buildJob.ScheduleParallel(state.Dependency);

                // The following job is only necessary for determinism.
                // It sorts spatial database entries in every cell by Entity index, so that no matter in
                // what order they were added, the results will always be the same.
                int workersCountForSort = math.max(1, JobsUtility.JobWorkerCount - 1);
                SortSpatialDatabaseCellElementsParallelJob sortElementsJob =
                    new SortSpatialDatabaseCellElementsParallelJob
                    {
                        CellsIterationStride = workersCountForSort,
                        CachedSpatialDatabase = new CachedSpatialDatabaseUnsafeParallel
                        {
                            SpatialDatabaseEntity = spatialDatabaseSingleton.TargetablesSpatialDatabase,
                            SpatialDatabaseLookup = SystemAPI.GetComponentLookup<SpatialDatabase>(false),
                            CellsBufferLookup = SystemAPI.GetBufferLookup<SpatialDatabaseCell>(false),
                            ElementsBufferLookup = SystemAPI.GetBufferLookup<SpatialDatabaseElement>(false),
                        },
                    };
                state.Dependency = sortElementsJob.Schedule(workersCountForSort, 1, state.Dependency);
            }
            else
            {
                BuildSpatialDatabaseSingleJob buildJob = new BuildSpatialDatabaseSingleJob
                {
                    CachedSpatialDatabase = new CachedSpatialDatabase
                    {
                        SpatialDatabaseEntity = spatialDatabaseSingleton.TargetablesSpatialDatabase,
                        SpatialDatabaseLookup = SystemAPI.GetComponentLookup<SpatialDatabase>(false),
                        CellsBufferLookup = SystemAPI.GetBufferLookup<SpatialDatabaseCell>(false),
                        ElementsBufferLookup = SystemAPI.GetBufferLookup<SpatialDatabaseElement>(false),
                    },
                };
                state.Dependency = buildJob.Schedule(state.Dependency);
            }
        }

        [WithAll(typeof(Targetable))]
        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public partial struct BuildSpatialDatabaseSingleJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public CachedSpatialDatabase CachedSpatialDatabase;

            public void Execute(Entity entity, in LocalToWorld ltw, in Team team, in ActorType actorType)
            {
                SpatialDatabaseElement element = new SpatialDatabaseElement
                {
                    Entity = entity,
                    Position = ltw.Position,
                    Team = (byte)team.Index,
                    Type = actorType.Type,
                };
                SpatialDatabase.AddToDataBaseSingleThread(in CachedSpatialDatabase._SpatialDatabase,
                    ref CachedSpatialDatabase._SpatialDatabaseCells, ref CachedSpatialDatabase._SpatialDatabaseElements,
                    element);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                CachedSpatialDatabase.CacheData();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted)
            {
            }
        }

        [WithAll(typeof(Targetable))]
        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public partial struct BuildSpatialDatabaseParallelJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public CachedSpatialDatabaseUnsafeParallel CachedSpatialDatabase;

            public void Execute(Entity entity, in LocalToWorld ltw, in Team team, in ActorType actorType)
            {
                SpatialDatabaseElement element = new SpatialDatabaseElement
                {
                    Entity = entity,
                    Position = ltw.Position,
                    Team = (byte)team.Index,
                    Type = actorType.Type,
                };
                SpatialDatabase.AddToDataBaseParallel(in CachedSpatialDatabase._SpatialDatabase,
                    ref CachedSpatialDatabase._SpatialDatabaseCells,
                    ref CachedSpatialDatabase._SpatialDatabaseElements,
                    element);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                CachedSpatialDatabase.CacheData();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted)
            {
            }
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public unsafe struct SortSpatialDatabaseCellElementsParallelJob : IJobParallelFor
        {
            public int CellsIterationStride;
            public CachedSpatialDatabaseUnsafeParallel CachedSpatialDatabase;

            public void Execute(int index)
            {
                CachedSpatialDatabase.CacheData();
                UnsafeList<SpatialDatabaseCell> cells = CachedSpatialDatabase._SpatialDatabaseCells;
                UnsafeList<SpatialDatabaseElement> elements = CachedSpatialDatabase._SpatialDatabaseElements;

                for (int i = index; i < cells.Length; i += CellsIterationStride)
                {
                    SpatialDatabaseCell cell = cells[i];
                    int excessCount = cell.GetExcessElementsCount();
                    if (excessCount > 0)
                    {
                        // In cases of excess, we have to clear all elements of the cell. This is for the sake of
                        // determinism. Because cell storage capacity doesn't grow as we add elements, and because
                        // elements don't get added when we're over capacity, there is a possibility that the entities
                        // added to a cell will differ from one run to another. So for determinism, when we overflow 
                        // capacity, we will have one frame of empty spatial DB so that the next frame can be valid and
                        // deterministic.
                        
                        // We flip the sign of elements count to signify "invalid". The clear and resize system will use
                        // the abs value to know what the new capacity should be
                        cell.UncappedElementsCount = -cell.UncappedElementsCount;
                        cells[i] = cell;
                    }
                    else
                    {
                        int trueElementsCount = cell.GetValidElementsCount();
                        if (trueElementsCount > 0)
                        {
                            UnsafeList<SpatialDatabaseElement> elementsSubList =
                                new UnsafeList<SpatialDatabaseElement>(elements.Ptr + (long)cell.StartIndex, trueElementsCount);
                        
                            // Sort elements by ascending entity index, for determinism
                            elementsSubList.Sort();
                        }
                    }
                }
            }
        }
    }
}
