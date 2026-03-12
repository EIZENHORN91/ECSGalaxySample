using Unity.Burst;
using Unity.Entities;

namespace Galaxy
{
    [BurstCompile]
    [UpdateAfter(typeof(DeathSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct FinishInitializeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameIsSimulating>();
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        public void OnUpdate(ref SystemState state)
        {
            FinishInitializeJob job = new FinishInitializeJob();
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile(FloatPrecision.High, FloatMode.Deterministic)]
        [WithAll(typeof(Initialize))]
        public partial struct FinishInitializeJob : IJobEntity
        {
            private void Execute(EnabledRefRW<Initialize> initialized)
            {
                initialized.ValueRW = false;
            }
        }
    }

}
