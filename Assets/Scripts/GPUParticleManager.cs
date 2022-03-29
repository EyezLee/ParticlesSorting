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


    ComputeBuffer particleBuffer;
    int initializeKernel, updateKernel;

    int particleDispatchGroupX { get { return Mathf.CeilToInt(particleNum / 8.0f); } }

    // initialize
    private void Start()
    {
        initializeKernel = cs.FindKernel("Initialize");
        particleBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));

    }

    // update
    private void Update()
    {
        // update dispatch

        // change color

        // draw instances
    }


    private void OnDisable()
    {
        if(particleBuffer!=null)particleBuffer.Release();
    }

    private void OnEnable()
    {
        if (particleBuffer != null) particleBuffer.Release();
    }

    private void OnDestroy()
    {
        if (particleBuffer != null) particleBuffer.Release();

    }
}

