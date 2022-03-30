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

    [SerializeField] int cellNumX;
    [SerializeField] int cellNumY;

    [SerializeField] int particleNum = 8;
    [SerializeField] Mesh prefab;
    [SerializeField] Shader shader;


    ComputeBuffer particleBuffer;
    int initializeKernel, updateKernel;
    ComputeBuffer indirectArgsBuffer;

    int particleDispatchGroupX { get { return Mathf.CeilToInt(particleNum / 8.0f); } }
    Material material;

    // initialize
    private void Start()
    {
        initializeKernel = cs.FindKernel("Initialize");
        particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));
        cs.SetInt("_BoundaryXMin", boundaryXMin);
        cs.SetInt("_BoundaryXMax", boundaryXMax);
        cs.SetInt("_BoundaryYMin", boundaryYMin);
        cs.SetInt("_BoundaryYMax", boundaryYMax);
        cs.SetInt("_CellNumX", cellNumX);
        cs.SetInt("_CellNumY", cellNumY);
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
        // update dispatch

        // change color

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
        for (int i = 0; i < cellNumX; i++)
        {
            for(int j = 0; j < cellNumY; j++)
            {
                float width = (boundaryXMax - boundaryXMin) / cellNumX;
                float height = (boundaryYMax - boundaryYMin) / cellNumY;
                Gizmos.DrawWireCube(new Vector3(i * width + width / 2f, j * height + height / 2f, 0), new Vector3(width, height, 0));
            }
        }

    }
}

