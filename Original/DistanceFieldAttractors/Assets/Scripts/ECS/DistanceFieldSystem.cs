using ECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

public struct OrbiterData : IComponentData
{
    public float3 position;
    public float3 velocity;


    public OrbiterData(Vector3 pos)
    {
        position = pos;
        velocity = Vector3.zero;
    }
}

public struct ColorData : IComponentData
{
    public float4 Value;
}



// ReSharper disable once InconsistentNaming
public class DistanceFieldSystem_IJobForEach : JobComponentSystem
{
    ProfilerMarker simmulate = new ProfilerMarker("Simmulate Paticles");
    private EntityQuery _orbitersQuery;
    private EntityQuery _settingsQuery;
    protected override void OnCreate()
    {
        _orbitersQuery = GetEntityQuery(ComponentType.ReadWrite<OrbiterData>(),ComponentType.ReadWrite<ColorData>(), 
            ComponentType.ReadWrite<LocalToWorld>());
        _settingsQuery = GetEntityQuery(ComponentType.ReadOnly<OrbiterSimmulationParams>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        using (simmulate.Auto())
        {
            var settings = _settingsQuery.GetSingleton<OrbiterSimmulationParams>();
            var job = new OrbiterUpdateJob
            {
                Dt = Time.deltaTime * 0.1f,
                model = settings.model,
                time = Time.time * 0.1f,
                frameCount = (uint) Time.frameCount,
                orbiterType = GetArchetypeChunkComponentType<OrbiterData>(),
                colorType = GetArchetypeChunkComponentType<ColorData>(),
                localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(),
                jitter = settings.jitter,
                attraction = settings.attraction,
                surfaceColor = settings.surfaceColor,
                exteriorColor = settings.exteriorColor,
                interiorColor = settings.interiorColor,
                exteriorColorDist = settings.exteriorColorDist,
                interiorColorDist = settings.interiorColorDist,
                colorStiffness = settings.colorStiffness,
                speedStretch = settings.speedStretch
            };

            return job.Schedule(_orbitersQuery, inputDeps);
        }
    }

   [BurstCompile]
    struct OrbiterUpdateJob : IJobChunk
    {
        public ArchetypeChunkComponentType<OrbiterData> orbiterType;
        public ArchetypeChunkComponentType<ColorData> colorType;
        public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
        public float attraction, jitter;
        public float4 surfaceColor, exteriorColor, interiorColor;
        public float exteriorColorDist, interiorColorDist, colorStiffness;
        public float speedStretch;
        public float Dt;
        public float time;
        public uint frameCount;
        public DistanceFieldModel model;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var localToWorlds = chunk.GetNativeArray(localToWorldType);
            var orbiters = chunk.GetNativeArray(orbiterType);
            var colors = chunk.GetNativeArray(colorType);

            for (int index = 0; index < chunk.Count; index++)
            {
                var orbiter = orbiters[index];
                var r = new Unity.Mathematics.Random();
                var seed = (frameCount * 2147483647) ^ (index + 1);
                r.InitState((uint)seed);
                var f3 = r.NextFloat3(-1,1);
                var insideSphere = new float3(f3.x, f3.y, f3.z);
                var n = math.length(insideSphere);
                if (n > 1)
                {
                    insideSphere /= n;
                }

                var dist = DistanceField.GetDistance(model, time, orbiter.position.x, orbiter.position.y, orbiter.position.z, out var normal);
                orbiter.velocity -= math.clamp(dist, -1f, 1f) * attraction * math.normalize(normal);
                orbiter.velocity += insideSphere * jitter;
                orbiter.velocity *= .99f;
                orbiter.position += orbiter.velocity;
                var targetColor = dist > 0f ?
                    math.lerp(surfaceColor, exteriorColor, dist / exteriorColorDist) :
                    math.lerp(surfaceColor, interiorColor, -dist / interiorColorDist);
                var c = math.lerp(colors[index].Value, targetColor, Dt * colorStiffness);
                colors[index] = new ColorData {Value = c};
                orbiters[index] = orbiter;

                var localToWorld = localToWorlds[index];

                var scale = new float3(.1f, .01f, math.max(.1f, math.length(orbiter.velocity) * speedStretch));

                localToWorld.Value =  float4x4.TRS(orbiter.position,quaternion.LookRotation(orbiter.velocity, new float3(0,1,0)),scale);//float4x4.Translate(orbiter.position);
                localToWorlds[index] = localToWorld;
            }
        }
    }
}


