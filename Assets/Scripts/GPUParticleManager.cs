using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

struct Particle
{
    Vector3 position;
    Vector3 color;
}

public class GPUParticleManager : MonoBehaviour
{
    [SerializeField] ComputeShader cs;
    [SerializeField] int particleNum = 8;
    [SerializeField] Mesh prefab;
    [SerializeField] Shader shader;
    [SerializeField] int targetIndex = 0;
    [SerializeField] [Range(0, 10)] float neighborRange = 1f;

    [SerializeField] GridConfig gridConfig;


    ComputeBuffer particleBuffer;
    int initializeKernel;
    ComputeBuffer indirectArgsBuffer;

    GridSortHelper<Particle> gridSortHelper;

    int particleDispatchGroupX { get { return Mathf.CeilToInt(particleNum / 512.0f); } }
    Material material;

    // initialize
    private void Start()
    {
        gridSortHelper = new GridSortHelper<Particle>(particleNum, gridConfig);

        initializeKernel = cs.FindKernel("Initialize");
        particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));
        cs.SetFloat("_BoundaryXMin", gridConfig.boundaryXMin);
        cs.SetFloat("_BoundaryXMax", gridConfig.boundaryXMax);
        cs.SetFloat("_BoundaryYMin", gridConfig.boundaryYMin);
        cs.SetFloat("_BoundaryYMax", gridConfig.boundaryYMax);
        cs.SetFloat("_BoundaryZMin", gridConfig.boundaryZMin);
        cs.SetFloat("_BoundaryZMax", gridConfig.boundaryZMax);
        cs.SetInt("_GridNumX", gridConfig.gridNumX);
        cs.SetInt("_GridNumY", gridConfig.gridNumY);
        cs.SetInt("_GridNumZ", gridConfig.gridNumZ);
        cs.SetBuffer(initializeKernel, "_ParticleBuffer", particleBuffer);
        cs.Dispatch(initializeKernel, particleDispatchGroupX, 1, 1);

        // indirect draw arg buffer
        indirectArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        indirectArgsBuffer.SetData(new uint[5]
        {
            prefab.GetIndexCount(0), (uint)particleNum, 0, 0, 0
        });
        material = new Material(shader);
        material.SetBuffer("_ParticleBuffer", particleBuffer);

    }

    // update
    private void Update()
    {
        gridSortHelper.Sort(ref particleBuffer, gridConfig);
        gridSortHelper.GridSortDebug(particleBuffer);
        gridSortHelper.NeighborRangeDebug(particleBuffer, targetIndex, neighborRange);

        // draw instances
        Graphics.DrawMeshInstancedIndirect(prefab, 0, material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), indirectArgsBuffer);
    }

    
    private void OnDisable()
    {
        particleBuffer?.Release();
        indirectArgsBuffer?.Release();
        gridSortHelper?.DisposeGridBuffers();
    }

    private void OnEnable()
    {
        particleBuffer?.Release();
        indirectArgsBuffer?.Release();
        gridSortHelper?.DisposeGridBuffers();

    }

    private void OnDestroy()
    {
        particleBuffer?.Release();
        indirectArgsBuffer?.Release();
        gridSortHelper?.DisposeGridBuffers();

    }

    private void OnDrawGizmos()
    {
        gridSortHelper?.DebugGrid();
    }

}

