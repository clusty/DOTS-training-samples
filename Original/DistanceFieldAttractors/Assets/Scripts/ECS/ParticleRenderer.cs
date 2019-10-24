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
		        var localToWorlds = query.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
		        var colors = query.ToComponentDataArray<ColorData>(Allocator.TempJob);

		        for (var i = 0; i < localToWorlds.Length; i += instancesPerBatch)
		        {
			        var count = Math.Min(instancesPerBatch, localToWorlds.Length - i);
			        var srcM = (float4x4*)localToWorlds.GetUnsafeReadOnlyPtr();
			        fixed (Matrix4x4* dst = matricesM)
			        {
				        UnsafeUtility.MemCpy(dst,srcM+i,count * UnsafeUtility.SizeOf<Matrix4x4>());
			        }
					
			        var srcC = (float4*)colors.GetUnsafeReadOnlyPtr();
			        fixed (void* dst = colorsM)
			        {
				        UnsafeUtility.MemCpy(dst,srcC+i,count * UnsafeUtility.SizeOf<Color>());
			        }
					
			        matProps.SetVectorArray(ColorID, colorsM);
			        Graphics.DrawMeshInstanced(particleMesh, 0, particleMaterial, matricesM, count, matProps);
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