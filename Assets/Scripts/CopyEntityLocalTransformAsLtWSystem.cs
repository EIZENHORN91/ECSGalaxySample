using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Galaxy
{
    [UpdateInGroup(typeof(TransformSystemGroup), OrderLast = true)]
    partial struct CopyEntityLocalTransformAsLtWSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameIsSimulating>();
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new CopyEntityLocalTransformAsLtWJob
            {
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public partial struct CopyEntityLocalTransformAsLtWJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            
            void Execute(ref LocalToWorld ltw, in CopyEntityLocalTransformAsLtW copyEntityTransform)
            {
                if (LocalTransformLookup.TryGetComponent(copyEntityTransform.TargetEntity,
                        out LocalTransform targetLocalTransform))
                {
                    ltw.Value = targetLocalTransform.ToMatrix();
                }
            }
        }
    }
}
