using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class GridSortHelper<T>
{
    ComputeShader cs = Resources.Load<ComputeShader>("GridSort");

    int boundaryXMin;
    int boundaryXMax;
    int boundaryYMin;
    int boundaryYMax;
    int boundaryZMin;
    int boundaryZMax;
    int gridNumX;
    int gridNumY;
    int gridNumZ;


    ComputeBuffer particleRearrangedBuffer;
    ComputeBuffer particleGridPairBuffer;
    ComputeBuffer gridTableBuffer;

    int particleNum;
    int particleDispatchGroupX { get { return Mathf.CeilToInt(particleNum / BitonicSort.BITONIC_BLOCK_SIZE); } }

    public GridSortHelper(int _particleNum, int _boundaryXMin, int _boundaryXMax, int _boundaryYMin, int _boundaryYMax, int _boundaryZMin, int _boundaryZMax, int _gridNumX, int _gridNumY, int _gridNumZ)
    {
        particleNum = _particleNum;
        boundaryXMin = _boundaryXMin;
        boundaryXMax = _boundaryXMax;
        boundaryYMin = _boundaryYMin;
        boundaryYMax = _boundaryYMax;
        boundaryZMin = _boundaryZMin;
        boundaryZMax = _boundaryZMax;
        gridNumX = _gridNumX;
        gridNumY = _gridNumY;
        gridNumZ = _gridNumZ;
        particleGridPairBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Vector2()));
        particleRearrangedBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Particle()));
        gridTableBuffer = new ComputeBuffer(gridNumX * gridNumY * gridNumZ, Marshal.SizeOf(typeof(Vector2)));

    }

    void CheckConfigUpdates()
    {

    }

    void CheckBufferUpdate(ComputeBuffer buffer)
    {
        
    }

    public void Sort(ref ComputeBuffer particleBuff)
    {
        particleNum = particleNum == particleBuff.count ? particleNum : particleBuff.count;
        // make <particle, gridID> pair
        int particleGridPairKernel = cs.FindKernel("MakeParticleGridPair");
        //if (particleGridPairBuffer.count != particleNum)
        //{
        //    particleGridPairBuffer?.Release();
        //    particleGridPairBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Vector2()));
        //}
        SetGridConfig();
        cs.SetBuffer(particleGridPairKernel, "_ParticleBuffer", particleBuff);
        cs.SetBuffer(particleGridPairKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.Dispatch(particleGridPairKernel, particleDispatchGroupX, 1, 1);

        // sort 
        ComputeBuffer tempBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(new Vector2()));
        BitonicSort.Sort(particleGridPairBuffer, tempBuffer);
        tempBuffer?.Release();

        // clean grid look up table
        int resetGridTableKernel = cs.FindKernel("ResetGridLookUpTable");
        cs.SetBuffer(resetGridTableKernel, "_GridTable", gridTableBuffer);
        cs.Dispatch(resetGridTableKernel, particleDispatchGroupX, 1, 1);
        //build grid look up table
        int makeGridTableKernel = cs.FindKernel("MakeGridLookUpTable");
        cs.SetFloat("_ParticleNum", particleBuff.count);
        cs.SetBuffer(makeGridTableKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.SetBuffer(makeGridTableKernel, "_GridTable", gridTableBuffer);
        cs.Dispatch(makeGridTableKernel, particleDispatchGroupX, 1, 1);

        //rearrange particles
        int rearrangeParticleKernel = cs.FindKernel("RearrangeParticle");
        //if (particleRearrangedBuffer.count != particleNum)
        //{
        //    particleRearrangedBuffer?.Release();
        //    particleRearrangedBuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(T)));
        //}
        cs.SetBuffer(rearrangeParticleKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.SetBuffer(rearrangeParticleKernel, "_ReadParticleBuffer", particleBuff);
        cs.SetBuffer(rearrangeParticleKernel, "_ParticleBuffer", particleRearrangedBuffer);
        cs.Dispatch(rearrangeParticleKernel, particleDispatchGroupX, 1, 1);

        // swap buffer
        (particleBuff, particleRearrangedBuffer) = (particleRearrangedBuffer, particleBuff);
    }

    public void GridSortDebug(ComputeBuffer particleBuff)
    {
        // debug dispatch
        int debugKernel = cs.FindKernel("Debug");
        cs.SetInt("_GridNumX", gridNumX);
        cs.SetInt("_GridNumY", gridNumY);
        cs.SetInt("_GridNumZ", gridNumZ);
        cs.SetBuffer(debugKernel, "_ParticleBuffer", particleBuff);
        cs.SetBuffer(debugKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.Dispatch(debugKernel, particleDispatchGroupX, 1, 1);
    }

    public void NeighborRangeDebug(ComputeBuffer particleBuff, int index, float neighborRange)
    {
        int neighborDebug = cs.FindKernel("NeighborDebug");
        SetGridConfig();
        cs.SetInt("_ParticleNum", particleBuff.count);
        cs.SetInt("_TargetIndex", index);
        cs.SetFloat("_Range", neighborRange);
        cs.SetBuffer(neighborDebug, "_GridTable", gridTableBuffer);
        cs.SetBuffer(neighborDebug, "_ParticleBuffer", particleBuff);
        cs.Dispatch(neighborDebug, particleDispatchGroupX, 1, 1);
    }

    void SetGridConfig()
    {
        cs.SetInt("_BoundaryXMin", boundaryXMin);
        cs.SetInt("_BoundaryXMax", boundaryXMax);
        cs.SetInt("_BoundaryYMin", boundaryYMin);
        cs.SetInt("_BoundaryYMax", boundaryYMax);
        cs.SetInt("_BoundaryZMin", boundaryZMin);
        cs.SetInt("_BoundaryZMax", boundaryZMax);
        cs.SetInt("_GridNumX", gridNumX);
        cs.SetInt("_GridNumY", gridNumY);
        cs.SetInt("_GridNumZ", gridNumZ);
    }

    public void DisposeGridBuffers()
    {
        particleGridPairBuffer?.Release();
        particleRearrangedBuffer?.Release();
        gridTableBuffer?.Release();
    }

    public void DebugGrid()
    {
        for (int i = 0; i < gridNumX; i++)
        {
            for (int j = 0; j < gridNumY; j++)
            {
                for (int k = 0; k < gridNumZ; k++)
                {
                    float width = (boundaryXMax - boundaryXMin) / (float)gridNumX;
                    float height = (boundaryYMax - boundaryYMin) / (float)gridNumY;
                    float depth = (boundaryZMax - boundaryZMin) / (float)gridNumZ;
                    Gizmos.DrawWireCube(new Vector3(i * width + width * 0.5f, j * height + height * 0.5f, k * depth + depth * 0.5f), 
                        new Vector3(width, height, depth));
                }
            }
        }
    }

}
