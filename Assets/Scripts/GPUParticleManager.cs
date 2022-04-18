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

    [SerializeField] int boundaryXMin;
    [SerializeField] int boundaryXMax;
    [SerializeField] int boundaryYMin;
    [SerializeField] int boundaryYMax;
    [SerializeField] int gridNumX;
    [SerializeField] int gridNumY;


    ComputeBuffer particleBuffer;
    int initializeKernel;
    ComputeBuffer indirectArgsBuffer;

    GridSortHelper<Particle> gridSortHelper;

    int particleDispatchGroupX { get { return Mathf.CeilToInt(particleNum / 512.0f); } }
    Material material;

    // initialize
    private void Start()
    {
        gridSortHelper = new GridSortHelper<Particle>(particleNum, boundaryXMin, boundaryXMax, boundaryYMin, boundaryYMax, gridNumX, gridNumY);

        initializeKernel = cs.FindKernel("Initialize");
        particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));
        cs.SetInt("_BoundaryXMin", boundaryXMin);
        cs.SetInt("_BoundaryXMax", boundaryXMax);
        cs.SetInt("_BoundaryYMin", boundaryYMin);
        cs.SetInt("_BoundaryYMax", boundaryYMax);
        cs.SetInt("_GridNumX", gridNumX);
        cs.SetInt("_GridNumY", gridNumY);
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
        gridSortHelper.Sort(ref particleBuffer);
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

