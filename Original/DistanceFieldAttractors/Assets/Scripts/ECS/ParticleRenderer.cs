using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
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
        private Matrix4x4[] matricesM ;
        private Vector4[] colorsM ;
        private MaterialPropertyBlock matProps;
        private int ColorID = Shader.PropertyToID("_Color");
        EntityQuery query;

        protected override void OnCreate()
        {
            matricesM = new Matrix4x4[instancesPerBatch];
            colorsM = new Vector4[instancesPerBatch];
            matProps = new MaterialPropertyBlock();
            query  = World.Active.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<LocalToWorld>(),
	            ComponentType.ReadOnly<ColorData>());
        }

        protected override unsafe void  OnUpdate()
        {
	        using (renderMarker.Auto())
	        {
		        var chunkArray = query.CreateArchetypeChunkArray(Allocator.TempJob);
		        var localToWorldType = GetArchetypeChunkComponentType<LocalToWorld>();
		        var colorType = GetArchetypeChunkComponentType<ColorData>();

		        int dstIdx = 0;
		        for (int i = 0; i < chunkArray.Length; ++i)
		        {
			        var chunk = chunkArray[i];
			        var localToWorlds = chunk.GetNativeArray(localToWorldType);
			        var colors = chunk.GetNativeArray(colorType);

			        int srcIdx = 0;
			        int j = 0;
			        do
			        {
				        while (srcIdx < localToWorlds.Length && dstIdx < instancesPerBatch)
				        {
					        //   localToWorlds.Length
					        //   instancesPerBatch - idx
					        int count = Mathf.Min(localToWorlds.Length - srcIdx, instancesPerBatch - dstIdx);

					        void* src = (Matrix4x4*) localToWorlds.GetUnsafeReadOnlyPtr() + srcIdx;
					        fixed (Matrix4x4* dst = matricesM)
					        {
						        UnsafeUtility.MemCpy(dst + dstIdx, src, count * UnsafeUtility.SizeOf<Matrix4x4>());
					        }

					        src = (float4*) colors.GetUnsafeReadOnlyPtr() + srcIdx;
					        fixed (Vector4* dst = colorsM)
					        {
						        UnsafeUtility.MemCpy(dst + dstIdx, src, count * UnsafeUtility.SizeOf<Vector4>());
					        }

					        dstIdx += count;
					        srcIdx += count;
				        }

				        if (i == chunkArray.Length - 1 || dstIdx == instancesPerBatch)
				        {
					        matProps.SetVectorArray(ColorID, colorsM);
					        Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matricesM, dstIdx, matProps);
					        dstIdx = 0;
				        }

			        } while (srcIdx < localToWorlds.Length);
		        }

		        chunkArray.Dispose();
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