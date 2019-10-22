using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public struct OrbiterData : IComponentData
{
    public float3 position;
    public float3 velocity;
    public float4 color;

    public OrbiterData(Vector3 pos)
    {
        position = pos;
        velocity = Vector3.zero;
        color = float4.zero;
    }
}

// ReSharper disable once InconsistentNaming
public class DistanceFieldSystem_IJobForEach : JobComponentSystem
{
    private EntityQuery _orbitersQuery;
    protected override void OnCreate()
    {
        _orbitersQuery = GetEntityQuery(ComponentType.ReadWrite<OrbiterData>(), ComponentType.ReadWrite<LocalToWorld>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new OrbiterUpdateJob
        {
            Dt = Time.deltaTime * 0.1f,
            model = DistanceFieldModel.Metaballs,
            time = Time.time * 0.1f,
            frameCount = (uint)Time.frameCount,
            orbiterType = GetArchetypeChunkComponentType<OrbiterData>(),
            localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(),
            jitter = .1f,
            attraction = .5f,
            surfaceColor = new float4(1, 0, 0, 1),
            exteriorColor = new float4(1, 1, 0, 1),
            interiorColor = new float4(1, 0, 1, 1),
            exteriorColorDist = .1f,
            interiorColorDist = .2f,
            colorStiffness = 1,
        };

        return job.Schedule(_orbitersQuery, inputDeps);
    }

    [BurstCompile]
    struct OrbiterUpdateJob : IJobChunk
    {
        public ArchetypeChunkComponentType<OrbiterData> orbiterType;
        public ArchetypeChunkComponentType<LocalToWorld> localToWorldType;
        public float attraction, jitter;
        public float4 surfaceColor, exteriorColor, interiorColor;
        public float exteriorColorDist, interiorColorDist, colorStiffness;
        public float Dt;
        public float time;
        public uint frameCount;
        public DistanceFieldModel model;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            NativeArray<LocalToWorld> localToWorlds = chunk.GetNativeArray(localToWorldType);
            NativeArray<OrbiterData> orbiters = chunk.GetNativeArray(orbiterType);

            for (int index = 0; index < chunk.Count; index++)
            {
                var orbiter = orbiters[index];
                var r = new Unity.Mathematics.Random();
                var seed = (frameCount * 2147483647) ^ (index + 1);
                r.InitState((uint)seed);
                var f3 = r.NextFloat3();
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
                orbiter.color = math.lerp(orbiter.color, targetColor, Dt * colorStiffness);
                orbiters[index] = orbiter;

                var localToWorld = localToWorlds[index];
                localToWorld.Value = float4x4.Translate(orbiter.position);
                localToWorlds[index] = localToWorld;
            }
        }
    }
}


