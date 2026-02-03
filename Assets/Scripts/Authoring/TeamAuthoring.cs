using Unity.Entities;
using UnityEngine;
using Galaxy;

public class TeamAuthoring : MonoBehaviour
{
    class Baker : Baker<TeamAuthoring>
    {
        public override void Bake(TeamAuthoring authoring)
        {
            Entity entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Team());
            AddComponent(entity, new ApplyTeam());
        }
    }
}
