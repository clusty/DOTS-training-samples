using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
struct Spawner : ISharedComponentData, IEquatable<Spawner>
{
    //public Mesh mesh;
    public Material material;
    public int particleCount;
    
    public bool Equals(Spawner other) // unused, bogus implementation
    {
        return particleCount > other.particleCount;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

public class OrbiterSpawnerSystem: ComponentSystem
{
    private EntityCommandBuffer CommandBuffer;
    private BeginSimulationEntityCommandBufferSystem _barrier;
    private EntityQuery spawnerEntityQuery;
    protected override void OnCreate()
    {
        _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        
        spawnerEntityQuery = Entities.WithAll<Spawner>().ToEntityQuery();
    }

    private static int spawnCount = 1;
    protected override void OnUpdate()
    {
        
        var chunks = spawnerEntityQuery.CreateArchetypeChunkArray(Allocator.TempJob);

        if (chunks.Length == 0)
            return;
        var chunkSpawnerType = GetArchetypeChunkSharedComponentType<Spawner>();
        var spawner = chunks[0].GetSharedComponentData(chunkSpawnerType, EntityManager);

        CommandBuffer = _barrier.CreateCommandBuffer();
        while (spawner.particleCount > 0)
        {
            spawner.particleCount--;
            var e = CommandBuffer.CreateEntity();

            /*var renderMesh = new RenderMesh();
            renderMesh.mesh = spawner.mesh;
            renderMesh.material = spawner.material;
            CommandBuffer.AddSharedComponent(e, renderMesh);*/

            var localToWorld = new LocalToWorld();
            localToWorld.Value = float4x4.identity;
            CommandBuffer.AddComponent(e, localToWorld);

            var r = new Unity.Mathematics.Random();
            var seed = (Time.frameCount * 2147483647) ^ (spawner.particleCount + 1);
            r.InitState((uint)seed);
            var insideSphere = r.NextFloat3();
            var n = math.length(insideSphere);
            if (n > 1)
            {
                insideSphere /= n;
            }

            var orbiterData = new OrbiterData(insideSphere * 50.0f);
            var colorData = new ColorData();
            CommandBuffer.AddComponent(e, orbiterData);
            CommandBuffer.AddComponent(e,colorData);
        }

        var entities = spawnerEntityQuery.ToEntityArray(Allocator.TempJob);
        CommandBuffer.DestroyEntity(entities[0]);
        entities.Dispose();

        chunks.Dispose();
    }
}
