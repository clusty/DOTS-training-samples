using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using Random = UnityEngine.Random;


public class ParticleManager : MonoBehaviour {
	
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

	Matrix4x4[][] matrices;
	Vector4[][] colors;
	MaterialPropertyBlock[] matProps;
	int finalBatchCount;

	const int instancesPerBatch = 1023;

	void OnEnable () {
		finalBatchCount = 0;
		orbiters = new NativeArray<Orbiter>(40000,Allocator.Persistent);
		matrices = new Matrix4x4[orbiters.Length/instancesPerBatch+1][];
		matrices[0] = new Matrix4x4[instancesPerBatch];
		colors = new Vector4[matrices.Length][];
		colors[0] = new Vector4[instancesPerBatch];

		int batch = 0;
		for (int i=0;i<orbiters.Length;i++) {
			orbiters[i]=new Orbiter(Random.insideUnitSphere*50f);
			finalBatchCount++;
			if (finalBatchCount==instancesPerBatch) {
				batch++;
				finalBatchCount = 0;
				matrices[batch]=new Matrix4x4[instancesPerBatch];
				colors[batch] = new Vector4[instancesPerBatch];
			}
		}
		matProps = new MaterialPropertyBlock[colors.Length];
		for (int i = 0; i <= batch; i++) {
			matProps[i] = new MaterialPropertyBlock();
			matProps[i].SetVectorArray("_Color",new Vector4[instancesPerBatch]);
		}
	}

	void OnDisable()
	{
		orbiters.Dispose();
	}
	
	[BurstCompile]
	struct OrbiterUpdateJob : IJobParallelFor
	{
		public NativeArray<Orbiter> orbiters;
		public float attraction, jitter;
		public Color surfaceColor, exteriorColor, interiorColor;
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
			var f3 = r.NextFloat3();
			var insideSphere = new Vector3(f3.x, f3.y, f3.z);
			var n = insideSphere.magnitude;
			if (n > 1)
			{
				insideSphere /= n;
			}

			var dist = DistanceField.GetDistance(model, time,orbiter.position.x,orbiter.position.y,orbiter.position.z,out var normal);
			orbiter.velocity -= Mathf.Clamp(dist,-1f,1f) * attraction * normal.normalized;
			orbiter.velocity += insideSphere*jitter;
			orbiter.velocity *= .99f;
			orbiter.position += orbiter.velocity;
			Color targetColor;
			if (dist>0f) {
				targetColor = Color.Lerp(surfaceColor,exteriorColor,dist/exteriorColorDist);
			} else {
				targetColor = Color.Lerp(surfaceColor,interiorColor,-dist / interiorColorDist);
			}
			orbiter.color = Color.Lerp(orbiter.color,targetColor,Dt * colorStiffness);
			orbiters[index] = orbiter;
		}
	}
	
	void FixedUpdate () 
	{
		var updateJob  = new OrbiterUpdateJob
		{
			orbiters = orbiters, attraction = attraction, jitter = jitter, surfaceColor = surfaceColor,
			exteriorColor = exteriorColor, interiorColor = interiorColor,
			exteriorColorDist = exteriorColorDist, interiorColorDist = interiorColorDist,
			colorStiffness = colorStiffness,
			Dt = Time.deltaTime,
			model = DistanceField.instance.model,
			time = DistanceField.timeStatic,
			frameCount = (uint)Time.frameCount
		};

		var handle = updateJob.Schedule(orbiters.Length, 1);
		handle.Complete();
	}

	private void Update() {
		using (dataMarker.Auto())
		for (int i=0;i<orbiters.Length;i++) {
			Orbiter orbiter = orbiters[i];
			Vector3 scale = new Vector3(.1f,.01f,Mathf.Max(.1f,orbiter.velocity.magnitude * speedStretch));
			Matrix4x4 matrix = Matrix4x4.TRS(orbiter.position,Quaternion.LookRotation(orbiter.velocity),scale);
			matrices[i / instancesPerBatch][i % instancesPerBatch] = matrix;
			colors[i / instancesPerBatch][i % instancesPerBatch] = orbiter.color;
			//Unity.Mathematics.math.RigidTransform().
		}

		using (renderMarker.Auto())
		for (int i=0;i<matrices.Length;i++) {
			int count = instancesPerBatch;
			if (i==matrices.Length-1) {
				count = finalBatchCount;
			}
			matProps[i].SetVectorArray(ColorID,colors[i]);
			Graphics.DrawMeshInstanced(particleMesh,0,particleMaterial,matrices[i],count,matProps[i]);
		}
	}
}
