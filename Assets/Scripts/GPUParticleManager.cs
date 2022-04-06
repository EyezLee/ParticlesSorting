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
    [SerializeField] int boundaryXMin;
    [SerializeField] int boundaryXMax;
    [SerializeField] int boundaryYMin;
    [SerializeField] int boundaryYMax;

    [SerializeField] int gridNumX;
    [SerializeField] int gridNumY;

    [SerializeField] int particleNum = 8;
    [SerializeField] Mesh prefab;
    [SerializeField] Shader shader;
    [SerializeField] GameObject target;
    [SerializeField] int targetIndex = 0;
    [SerializeField] [Range(0, 1)] float range = 0.5f;


    ComputeBuffer particleBuffer, particleRearrangedBuffer;
    int initializeKernel, updateKernel;
    int particleGridPairKernel, debugKernel;
    ComputeBuffer indirectArgsBuffer;
    ComputeBuffer particleGridPairBuffer;
    ComputeBuffer gridTableBuffer;

    int particleDispatchGroupX { get { return Mathf.CeilToInt(particleNum / 8.0f); } }
    Material material;

    BitonicSort bitonicSort;

    // initialize
    private void Start()
    {
        initializeKernel = cs.FindKernel("Initialize");
        particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));
        particleRearrangedBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));
        particleGridPairBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(Vector2)));
        gridTableBuffer = new ComputeBuffer(gridNumX * gridNumY, Marshal.SizeOf(typeof(Vector2)));
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

        bitonicSort = GetComponent<BitonicSort>();
    }

    // update
    private void Update()
    {
        // make <particle, gridID> pair
        particleGridPairKernel = cs.FindKernel("MakeParticleGridPair");
        cs.SetInt("_BoundaryXMin", boundaryXMin);
        cs.SetInt("_BoundaryXMax", boundaryXMax);
        cs.SetInt("_BoundaryYMin", boundaryYMin);
        cs.SetInt("_BoundaryYMax", boundaryYMax);
        cs.SetInt("_GridNumX", gridNumX);
        cs.SetInt("_GridNumY", gridNumY);
        cs.SetBuffer(particleGridPairKernel, "_ParticleBuffer", particleBuffer);
        cs.SetBuffer(particleGridPairKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.Dispatch(particleGridPairKernel, particleDispatchGroupX, 1, 1);

        // sort 
        bitonicSort.Sort(particleGridPairBuffer);

        // clean grid look up table
        int resetGridTableKernel = cs.FindKernel("ResetGridLookUpTable");
        cs.SetBuffer(resetGridTableKernel, "_GridTable", gridTableBuffer);
        cs.Dispatch(resetGridTableKernel, Mathf.CeilToInt(gridNumX / 8.0f), Mathf.CeilToInt(gridNumY / 8.0f), 1);
        // build grid look up table
        int makeGridTableKernel = cs.FindKernel("MakeGridLookUpTable");
        cs.SetFloat("_ParticleNum", particleNum);
        cs.SetBuffer(makeGridTableKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.SetBuffer(makeGridTableKernel, "_GridTable", gridTableBuffer);
        cs.Dispatch(makeGridTableKernel, particleDispatchGroupX, 1, 1);

        // rearrange particles
        int rearrangeParticleKernel = cs.FindKernel("RearrangeParticle");
        cs.SetBuffer(rearrangeParticleKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.SetBuffer(rearrangeParticleKernel, "_ReadParticleBuffer", particleBuffer);
        cs.SetBuffer(rearrangeParticleKernel, "_ParticleBuffer", particleRearrangedBuffer);
        cs.Dispatch(rearrangeParticleKernel, particleDispatchGroupX, 1, 1);

        // swap buffer
        (particleBuffer, particleRearrangedBuffer) = (particleRearrangedBuffer, particleBuffer);

        // debug dispatch
        debugKernel = cs.FindKernel("Debug");
        cs.SetInt("_GridNumX", gridNumX);
        cs.SetInt("_GridNumY", gridNumY);
        cs.SetBuffer(debugKernel, "_ParticleBuffer", particleBuffer);
        cs.SetBuffer(debugKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.Dispatch(debugKernel, particleDispatchGroupX, 1, 1);

        // update dispatch
        updateKernel = cs.FindKernel("Update");
        cs.SetInt("_BoundaryXMin", boundaryXMin);
        cs.SetInt("_BoundaryXMax", boundaryXMax);
        cs.SetInt("_BoundaryYMin", boundaryYMin);
        cs.SetInt("_BoundaryYMax", boundaryYMax);
        cs.SetInt("_GridNumX", gridNumX);
        cs.SetInt("_GridNumY", gridNumY);
        cs.SetInt("_TargetIndex", targetIndex);
        cs.SetInt("_ParticleNum", particleNum);
        cs.SetBuffer(updateKernel, "_GridTable", gridTableBuffer);
        cs.SetBuffer(updateKernel, "_ParticleBuffer", particleBuffer);
        cs.Dispatch(updateKernel, particleDispatchGroupX, 1, 1);


        // draw instances
        Graphics.DrawMeshInstancedIndirect(prefab, 0, material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), indirectArgsBuffer);
    }


    private void OnDisable()
    {
        particleBuffer?.Release();
        indirectArgsBuffer?.Release();
        particleGridPairBuffer?.Release();
        particleRearrangedBuffer?.Release();
        gridTableBuffer?.Release();
    }

    private void OnEnable()
    {
        particleBuffer?.Release();
        indirectArgsBuffer?.Release();
        particleGridPairBuffer?.Release();
        particleRearrangedBuffer?.Release();
        gridTableBuffer?.Release();

    }

    private void OnDestroy()
    {
        particleBuffer?.Release();
        indirectArgsBuffer?.Release();
        particleGridPairBuffer?.Release();
        particleRearrangedBuffer?.Release();
        gridTableBuffer?.Release();

    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < gridNumX; i++)
        {
            for(int j = 0; j < gridNumY; j++)
            {
                float width = (boundaryXMax - boundaryXMin) / (float)gridNumX;
                float height = (boundaryYMax - boundaryYMin) / (float)gridNumY;
                Gizmos.DrawWireCube(new Vector3(i * width + width * 0.5f, j * height + height * 0.5f, 0), new Vector3(width, height, 0));
            }
        }
    }
}

