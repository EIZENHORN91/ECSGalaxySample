using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Galaxy
{
    [BurstCompile]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(VFXSystem))]
    public partial struct ShipVFXThrustersSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameIsSimulating>();
            state.RequireForUpdate<VFXThrustersSingleton>();
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public void OnUpdate(ref SystemState state)
        {
            VFXThrustersSingleton vfxThrustersSingleton = SystemAPI.GetSingletonRW<VFXThrustersSingleton>().ValueRW;

            ShipSetVFXDataJob shipSetVFXDataJob = new ShipSetVFXDataJob
            {
                ThrustersData = vfxThrustersSingleton.Manager.Datas,
            };
            state.Dependency = shipSetVFXDataJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public partial struct ShipSetVFXDataJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<VFXThrusterData> ThrustersData;

            private void Execute(in LocalTransform transform, in Ship ship)
            {
                if (ship.ThrusterVFXIndex >= 0)
                {
                    ref ShipData shipData = ref ship.ShipData.Value;

                    VFXThrusterData thrusterData = ThrustersData[ship.ThrusterVFXIndex];
                    thrusterData.Position =
                        transform.Position + math.mul(transform.Rotation, shipData.ThrusterLocalPosition);
                    thrusterData.Direction = math.mul(transform.Rotation, -math.forward());
                    ThrustersData[ship.ThrusterVFXIndex] = thrusterData;
                }
            }
        }
    }
}