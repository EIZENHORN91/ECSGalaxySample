using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(TransformSystemGroup))]
partial struct CustomLODSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (Camera.main != null)
        {
            Transform cameraTransform = Camera.main.transform;

            state.Dependency = new CustomLODSetupJob
            {
                MaterialMeshInfoLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>()
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new CustomLODJob
            {
                CameraPosition = cameraTransform.position,
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
    [WithAll(typeof(InitializeLOD))]
    public unsafe partial struct CustomLODSetupJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<MaterialMeshInfo> MaterialMeshInfoLookup;
        
        public void Execute(EnabledRefRW<InitializeLOD> initializeLOD, ref DynamicBuffer<CustomLOD> customLods)
        {
            UnsafeList<CustomLOD> customLodsUnsafe =
                new UnsafeList<CustomLOD>((CustomLOD*)customLods.GetUnsafePtr(), customLods.Length);
            for (int i = 0; i < customLodsUnsafe.Length; i++)
            {
                CustomLOD elem = customLodsUnsafe[i];
                if (MaterialMeshInfoLookup.TryGetComponent(elem.LODEntity, out MaterialMeshInfo meshInfo))
                {
                    elem.MeshIndex = meshInfo.Mesh;
                    customLodsUnsafe[i] = elem;
                }
            }

            initializeLOD.ValueRW = false;
        }
    }

    [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
    public unsafe partial struct CustomLODJob : IJobEntity
    {
        public float3 CameraPosition;
        
        public void Execute(in LocalToWorld ltw, in DynamicBuffer<CustomLOD> customLods, ref MaterialMeshInfo materialMeshInfo)
        {
            float distSq = math.distancesq(ltw.Position, CameraPosition);

            UnsafeList<CustomLOD> customLodsUnsafe =
                new UnsafeList<CustomLOD>((CustomLOD*)customLods.GetUnsafeReadOnlyPtr(), customLods.Length);
            for (int i = 0; i < customLodsUnsafe.Length; i++)
            {
                CustomLOD elem = customLodsUnsafe[i];
                if (distSq > elem.DistanceSq || i == customLodsUnsafe.Length - 1)
                {
                    materialMeshInfo.Mesh = elem.MeshIndex;
                    break;
                }
            }
        }
    }
}
