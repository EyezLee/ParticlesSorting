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


    ComputeBuffer particleBuffer;
    int initializeKernel, updateKernel;
    int particleGridPairKernel, debugKernel;
    ComputeBuffer indirectArgsBuffer;
    ComputeBuffer particleGridPairBuffer;

    int particleDispatchGroupX { get { return Mathf.CeilToInt(particleNum / 8.0f); } }
    Material material;

    BitonicSort bitonicSort;

    // initialize
    private void Start()
    {
        initializeKernel = cs.FindKernel("Initialize");
        particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));
        particleGridPairBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(Vector2)));
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


        // debug dispatch
        debugKernel = cs.FindKernel("Debug");
        cs.SetInt("_GridNumX", gridNumX);
        cs.SetInt("_GridNumY", gridNumY);
        cs.SetBuffer(debugKernel, "_ParticleBuffer", particleBuffer);
        cs.SetBuffer(debugKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.Dispatch(debugKernel, particleDispatchGroupX, 1, 1);

        // update dispatch
        updateKernel = cs.FindKernel("Update");
        cs.SetFloat("_MouseInWorldX", target.transform.position.x);
        cs.SetFloat("_MouseInWorldY", target.transform.position.y);
        cs.SetBuffer(updateKernel, "_ParticleBuffer", particleBuffer);
        //cs.Dispatch(updateKernel, particleDispatchGroupX, 1, 1);


        // draw instances
        Graphics.DrawMeshInstancedIndirect(prefab, 0, material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), indirectArgsBuffer);
    }


    private void OnDisable()
    {
        if(particleBuffer!=null)particleBuffer.Release();
        if(indirectArgsBuffer!=null)indirectArgsBuffer.Release();
    }

    private void OnEnable()
    {
        if (particleBuffer != null) particleBuffer.Release();
        if (indirectArgsBuffer != null) indirectArgsBuffer.Release();
    }

    private void OnDestroy()
    {
        if (particleBuffer != null) particleBuffer.Release();
        if (indirectArgsBuffer != null) indirectArgsBuffer.Release();

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

