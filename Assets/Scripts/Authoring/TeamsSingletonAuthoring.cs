using Unity.Entities;
using UnityEngine;
using Galaxy;

public class TeamsSingletonAuthoring : MonoBehaviour
{
    class Baker : Baker<TeamsSingletonAuthoring>
    {
        public override void Bake(TeamsSingletonAuthoring managerAuthoring)
        {
            Entity entity = GetEntity(managerAuthoring, TransformUsageFlags.None);
            AddBuffer<TeamManagerReference>(entity);
        }
    }
}
