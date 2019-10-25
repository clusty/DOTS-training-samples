using ECS;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class OrbiterSpawnerKillerSystem: ComponentSystem
{
    private EntityCommandBuffer CommandBuffer;
    private BeginSimulationEntityCommandBufferSystem _barrier;
    private EntityQuery _settingsQuery;
    private EntityQuery _particleQuery;
    protected override void OnCreate()
    {
        _settingsQuery = GetEntityQuery(ComponentType.ReadOnly<OrbiterSimmulationParams>());
        _particleQuery = GetEntityQuery(ComponentType.ReadOnly<OrbiterData>());
        _barrier = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        RequireSingletonForUpdate<OrbiterSimmulationParams>();
    }

    private static int spawnCount = 1;
    protected override void OnUpdate()
    {
        var settings = _settingsQuery.GetSingleton<OrbiterSimmulationParams>();
        var currentParticleCount = _particleQuery.CalculateEntityCount();

        CommandBuffer = _barrier.CreateCommandBuffer();
        while (currentParticleCount < settings.particleCount)
        {
            currentParticleCount++;
            var e = CommandBuffer.CreateEntity();
            

            var localToWorld = new LocalToWorld();
            localToWorld.Value = float4x4.identity;
            CommandBuffer.AddComponent(e, localToWorld);

            var r = new Unity.Mathematics.Random();
            var seed = (Time.frameCount * 2147483647) ^ (currentParticleCount + 1);
            r.InitState((uint)seed);
            var insideSphere = r.NextFloat3(-1,1);
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

        if (currentParticleCount > settings.particleCount)
        {
            var entityArray = _particleQuery.ToEntityArray(Allocator.TempJob);
            var toKill = currentParticleCount - settings.particleCount;
            for (int i = 0; i < toKill; ++i)
            {
                CommandBuffer.DestroyEntity(entityArray[i]);
            }

            entityArray.Dispose();
        }
    }
}
