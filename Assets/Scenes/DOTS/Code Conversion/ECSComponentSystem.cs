using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using static Unity.Mathematics.math;
using System;

public class ECSComponentSystem : ComponentSystem
{
    protected override void OnStartRunning()
    {
        NativeArray<Entity> ents = EntityManager.GetAllEntities(Allocator.Temp);
        for (int i = 0; i < ents.Length; i++)
        {
            Entity e = ents[i];

            try
            {
                RenderMesh rm = EntityManager.GetSharedComponentData<RenderMesh>(e);
                ColorComponent cc = EntityManager.GetComponentData<ColorComponent>(e);

                rm.material.SetColor("_BaseColor", new UnityEngine.Color(cc.color.x, cc.color.y, cc.color.z, 1f));
                EntityManager.SetSharedComponentData<RenderMesh>(ents[i], rm);
            }
            catch (Exception) { }
        }
    }

    

    protected override void OnUpdate()
    {
        
    }
}