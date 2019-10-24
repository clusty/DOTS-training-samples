using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

namespace ECS
{

	[DisableAutoCreation]
	class RenderSystem : ComponentSystem
	{
		ProfilerMarker renderMarker = new ProfilerMarker("Prepare/Render");

		public Mesh particleMesh;
		public Material particleMaterial;
		const int instancesPerBatch = 1023;
		private Matrix4x4[][] matricesM;
		private Vector4[][] colorsM;
		private MaterialPropertyBlock matProps;
		private int ColorID = Shader.PropertyToID("_Color");
		EntityQuery query;
		GCHandle[] handlesC;
		GCHandle[] handlesM;
		private JobHandle[] jobs;

		protected override void OnCreate()
		{
			RequireSingletonForUpdate<OrbiterSimmulationParams>();
			//matricesM = new Matrix4x4[instancesPerBatch];
			//colorsM = new Vector4[instancesPerBatch];
			matProps = new MaterialPropertyBlock();
			query = World.Active.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<LocalToWorld>(),
				ComponentType.ReadOnly<ColorData>());
			var pcount = World.Active.EntityManager
				.CreateEntityQuery(ComponentType.ReadOnly<OrbiterSimmulationParams>())
				.GetSingleton<OrbiterSimmulationParams>().particleCount;
			var batchCount = pcount / instancesPerBatch + 1;
			matricesM = new Matrix4x4[batchCount][];
			colorsM = new Vector4[batchCount][];
			for (var i = 0; i < batchCount; ++i)
			{
				matricesM[i] = new Matrix4x4[instancesPerBatch];
				colorsM[i] = new Vector4[instancesPerBatch];
			}
			handlesC = new GCHandle[batchCount];
			handlesM = new GCHandle[batchCount];
			jobs = new JobHandle[batchCount];
		}

		unsafe struct MemCpyJob : IJobParallelFor
		{
			public NativeArray<LocalToWorld> srcMatrix;
			public NativeArray<IntPtr> dstMatrix;

			public NativeArray<ColorData> srcColor;
			public NativeArray<IntPtr> dstColor;
			public int batchSize;
			public void Execute(int index)
			{
				batchSize = index == srcColor.Length / batchSize ? srcColor.Length % batchSize : batchSize;

				var srcM = (float4x4*) srcMatrix.GetUnsafeReadOnlyPtr() + index * batchSize;
				var dstM = (Matrix4x4*) dstMatrix[index];
				{
					UnsafeUtility.MemCpy(dstM, srcM, batchSize * UnsafeUtility.SizeOf<Matrix4x4>());
				}

				var srcC = (float4*) srcColor.GetUnsafeReadOnlyPtr() + index * batchSize;
				var dstC = (float4*) dstColor[index];
				{
					UnsafeUtility.MemCpy(dstC, srcC, batchSize * UnsafeUtility.SizeOf<float4>());
				}

			}
		}



		protected override void OnUpdate()
		{
			using (renderMarker.Auto())
			{
				
				var localToWorlds = query.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
				var colors = query.ToComponentDataArray<ColorData>(Allocator.TempJob);

				var batchcount = localToWorlds.Length / instancesPerBatch;

				/*using (*/
				var dstMatrix = new NativeArray<IntPtr>(batchcount, Allocator.TempJob); //)
				/*using (*/
				var dstColor = new NativeArray<IntPtr>(batchcount, Allocator.TempJob); //)
				// {
				for (var i = 0; i < batchcount; i++)
				{

						handlesM[i] = GCHandle.Alloc(matricesM[i],GCHandleType.Pinned);
						handlesC[i] = GCHandle.Alloc(colorsM[i],GCHandleType.Pinned);
						dstMatrix[i] = handlesM[i].AddrOfPinnedObject();
						dstColor[i] = handlesC[i].AddrOfPinnedObject();
				}

				var job = new MemCpyJob
				{
					srcColor = colors,
					dstColor = dstColor,
					srcMatrix = localToWorlds,
					dstMatrix = dstMatrix,
					batchSize = instancesPerBatch

				};

				job.Schedule(batchcount,1).Complete();
				dstColor.Dispose();
				dstMatrix.Dispose();

				for (int i = 0; i < batchcount; ++i)
				{
					handlesC[i].Free();
					handlesM[i].Free();
				}

				var remainder = localToWorlds.Length % instancesPerBatch;
				for (int i = 0; i < batchcount - 1; ++i)
				{
					matProps.SetVectorArray(ColorID, colorsM[i]);
					Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matricesM[i], instancesPerBatch,
						matProps);
				}

				if (remainder > 0)
				{
					matProps.SetVectorArray(ColorID, colorsM[batchcount - 1]);
					Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matricesM[batchcount - 1], remainder,
						matProps);
				}

				localToWorlds.Dispose();
				colors.Dispose();
			}
		}
	}

	public class ParticleRenderer : MonoBehaviour
    {
	    public Mesh particleMesh;
	    public Material particleMaterial;
        private RenderSystem render;
        private void Start()
        {
	        render = World.Active.GetOrCreateSystem<RenderSystem>(); //new RenderSystem {particleMesh = particleMesh, particleMaterial = particleMaterial};
	        render.particleMaterial = particleMaterial;
	        render.particleMesh = particleMesh;
        }

        private void LateUpdate()
        {
	        render.Update();
        }
    }
}