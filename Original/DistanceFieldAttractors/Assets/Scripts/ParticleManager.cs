using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using Random = UnityEngine.Random;


public class ParticleManager : MonoBehaviour {
	
	ProfilerMarker updateStateMarker = new ProfilerMarker("updateParticleStateMarker");
	ProfilerMarker dataMarker = new ProfilerMarker("DataPrepare");
	ProfilerMarker renderMarker = new ProfilerMarker("Rendering");

	private int ColorID = Shader.PropertyToID("_Color");
	public float attraction;
	public float speedStretch;
	public float jitter;
	public Mesh particleMesh;
	public Material particleMaterial;
	public Color surfaceColor;
	public Color interiorColor;
	public Color exteriorColor;
	public float exteriorColorDist = 3f;
	public float interiorColorDist = 3f;
	public float colorStiffness;
	NativeArray<Orbiter> orbiters;

	private NativeArray<float4x4> matrices;
	private NativeArray<float4> colors;
	
	int finalBatchCount;

	const int instancesPerBatch = 1023;
	const int particleCount = 60000;

	private Matrix4x4[] matricesM = new Matrix4x4[instancesPerBatch];
	private Vector4[] colorsM = new Vector4[instancesPerBatch];
	private MaterialPropertyBlock matProps;
	private JobHandle updateHandle;
	
	void OnEnable () {
		finalBatchCount = 0;
		orbiters = new NativeArray<Orbiter>(particleCount,Allocator.Persistent);
		matrices = new NativeArray<float4x4>(particleCount,Allocator.Persistent);
		colors = new NativeArray<float4>(particleCount, Allocator.Persistent);
		matProps = new MaterialPropertyBlock();

		for (var i = 0; i < orbiters.Length; i++)
		{
			orbiters[i] = new Orbiter(Random.insideUnitSphere * 50f);
		}
		
		matProps.SetVectorArray("_Color",new Vector4[instancesPerBatch]);
	}

	void OnDisable()
	{
		orbiters.Dispose();
		matrices.Dispose();
		colors.Dispose();
	}
	
	[BurstCompile]
	struct OrbiterUpdateJob : IJobParallelFor
	{
		public NativeArray<Orbiter> orbiters;
		public float attraction, jitter;
		public float4 surfaceColor, exteriorColor, interiorColor;
		public float exteriorColorDist, interiorColorDist, colorStiffness;
		public float Dt;
		public float time;
		public uint frameCount;
		public DistanceFieldModel model;
		
		public void Execute(int index)
		{
			var orbiter = orbiters[index];
			var r = new Unity.Mathematics.Random();
			var seed = (frameCount * 2147483647) ^ (index + 1);
			r.InitState((uint)seed);
			var insideSphere = r.NextFloat3();
			var n = math.length(insideSphere);
			if (n > 1)
			{
				insideSphere /= n;
			}

			var dist = DistanceField.GetDistance(model, time,orbiter.position.x,orbiter.position.y,orbiter.position.z,out var normal);
			normal /= math.length(normal);
			orbiter.velocity -=  math.clamp(dist,-1f,1f) * attraction * normal;
			orbiter.velocity += insideSphere*jitter;
			orbiter.velocity *= .99f;
			orbiter.position += orbiter.velocity;
			var targetColor = dist>0f ? 
				math.lerp(surfaceColor,exteriorColor,dist/exteriorColorDist) : 
				math.lerp(surfaceColor,interiorColor,-dist / interiorColorDist);
			orbiter.color = math.lerp(orbiter.color,targetColor,Dt * colorStiffness);
			orbiters[index] = orbiter;
		}
	}
	
	void FixedUpdate () 
	{
		using (updateStateMarker.Auto())
		{
			var updateJob = new OrbiterUpdateJob
			{
				orbiters = orbiters, attraction = attraction, jitter = jitter, 
				surfaceColor = new float4(surfaceColor.r,surfaceColor.g,surfaceColor.b, surfaceColor.a),
				exteriorColor = new float4(exteriorColor.r,exteriorColor.g,exteriorColor.b, exteriorColor.a), 
				interiorColor = new float4(interiorColor.r,interiorColor.g,interiorColor.b, interiorColor.a),
				exteriorColorDist = exteriorColorDist, interiorColorDist = interiorColorDist,
				colorStiffness = colorStiffness,
				Dt = Time.deltaTime,
				model = DistanceField.instance.model,
				time = DistanceField.timeStatic,
				frameCount = (uint) Time.frameCount
			};
			
			updateHandle = updateJob.Schedule(orbiters.Length, 1,updateHandle); // Depend on previous iteration
		}
	}

	[BurstCompile]
	struct PrepareRenderData : IJobParallelFor
	{
		public NativeArray<Orbiter> orbiters;
		public NativeArray<float4x4> matrices;
		public NativeArray<float4> colors;
		public float speedStretch;
		public void Execute(int index)
		{
			var orbiter = orbiters[index];
			var scale = new float3(.1f,.01f,math.max(.1f,math.length(orbiter.velocity) * speedStretch));
			matrices[index] = float4x4.TRS(orbiter.position,quaternion.LookRotation(orbiter.velocity, new float3(0,1,0)),scale);
			colors[index] = orbiter.color;
		}
	}

	private void Update() {
		using (dataMarker.Auto())
		{
			var prepareData  = new PrepareRenderData
			{
				orbiters = orbiters,
				matrices = matrices,
				colors = colors,
				speedStretch = speedStretch
			};

			var handle = prepareData.Schedule(orbiters.Length, 1,updateHandle);
			handle.Complete();
		}
		
		unsafe
		{
			using (renderMarker.Auto())
			{
				for (var i = 0; i < particleCount; i += instancesPerBatch)
				{
					var count = Math.Min(instancesPerBatch, particleCount - i);
					var src = matrices.GetUnsafeReadOnlyPtr();
					fixed (void* dst = matricesM)
					{
						UnsafeUtility.MemCpy(dst,src,count * UnsafeUtility.SizeOf<Matrix4x4>());
					}
					
					src = colors.GetUnsafeReadOnlyPtr();
					fixed (void* dst = colorsM)
					{
						UnsafeUtility.MemCpy(dst,src,count * UnsafeUtility.SizeOf<Color>());
					}
					
					matProps.SetVectorArray(ColorID, colorsM);
					Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matricesM, count, matProps);
				}
			}
		}
	}
}
