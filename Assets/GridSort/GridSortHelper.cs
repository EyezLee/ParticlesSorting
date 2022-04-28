using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public enum NeighborMode
{
    NEAREST_RADIUS,
    THREEBYTHREE_BLOCK
}

[System.Serializable]
public struct GridConfig
{
    public float boundaryXMin;
    public float boundaryXMax;
    public float boundaryYMin;
    public float boundaryYMax;
    public float boundaryZMin;
    public float boundaryZMax;
    public int gridNumX;
    public int gridNumY;
    public int gridNumZ;
    public int gridNum { get { return gridNumX * gridNumY * gridNumZ; } }

    public void UpdateGrid(GridConfig gridConfiguration)
    {
        if (boundaryXMax != gridConfiguration.boundaryXMax) boundaryXMax = gridConfiguration.boundaryXMax;
        if (boundaryXMin != gridConfiguration.boundaryXMin) boundaryXMin = gridConfiguration.boundaryXMin;
        if (boundaryYMax != gridConfiguration.boundaryYMax) boundaryYMax = gridConfiguration.boundaryYMax;
        if (boundaryYMin != gridConfiguration.boundaryYMin) boundaryYMin = gridConfiguration.boundaryYMin;
        if (boundaryZMax != gridConfiguration.boundaryZMax) boundaryZMax = gridConfiguration.boundaryZMax;
        if (boundaryZMin != gridConfiguration.boundaryZMin) boundaryZMin = gridConfiguration.boundaryZMin;
        if (gridNumX != gridConfiguration.gridNumX) gridNumX = gridConfiguration.gridNumX;
        if (gridNumY != gridConfiguration.gridNumY) gridNumY = gridConfiguration.gridNumY;
        if (gridNumZ != gridConfiguration.gridNumZ) gridNumZ = gridConfiguration.gridNumZ;
    }
}

public class GridSortHelper<T>
{
    ComputeShader cs = Resources.Load<ComputeShader>("GridSort");


    ComputeBuffer particleRearrangedBuffer;
    ComputeBuffer particleGridPairBuffer;
    ComputeBuffer gridTableBuffer;
    GridConfig gridConfig;

    PARTICLE_NUM particleNum;
    int particleDispatchGroupX { get { return Mathf.CeilToInt((int)particleNum / BitonicSort.BITONIC_BLOCK_SIZE); } }

    public GridSortHelper(PARTICLE_NUM particleNumber, GridConfig gridConfiguration)
    {
        particleNum = particleNumber;
        gridConfig = gridConfiguration;
        particleGridPairBuffer = new ComputeBuffer((int)particleNum, Marshal.SizeOf(typeof(Vector2)));
        particleRearrangedBuffer = new ComputeBuffer((int)particleNum, Marshal.SizeOf(typeof(T)));
        gridTableBuffer = new ComputeBuffer(gridConfig.gridNum, Marshal.SizeOf(typeof(Vector2)));
    }

    void CheckConfigUpdates(PARTICLE_NUM _particleNum, GridConfig gridConfiguration)
    {
        if (particleNum != _particleNum) particleNum = _particleNum;
        gridConfig.UpdateGrid(gridConfiguration);
        if (particleGridPairBuffer.count != (int)particleNum)
        {
            particleGridPairBuffer?.Release();
            particleGridPairBuffer = new ComputeBuffer((int)particleNum, Marshal.SizeOf(new Vector2()));
        }
        if (particleRearrangedBuffer.count != (int)particleNum)
        {
            particleRearrangedBuffer?.Release();
            particleRearrangedBuffer = new ComputeBuffer((int)particleNum, Marshal.SizeOf(new Vector2()));
        }
        if(gridTableBuffer.count != gridConfig.gridNum)
        {
            gridTableBuffer?.Release();
            gridTableBuffer = new ComputeBuffer(gridConfig.gridNum, Marshal.SizeOf(typeof(Vector2)));
        }
    }

    public void Sort(ref ComputeBuffer particleBuff, GridConfig gridConfiguration)
    {
        // check for updates 
        CheckConfigUpdates((PARTICLE_NUM)particleBuff.count, gridConfiguration);

        // make <particle, gridID> pair
        int particleGridPairKernel = cs.FindKernel("MakeParticleGridPair");
        SetGridConfig();
        cs.SetBuffer(particleGridPairKernel, "_ParticleBuffer", particleBuff);
        cs.SetBuffer(particleGridPairKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.Dispatch(particleGridPairKernel, particleDispatchGroupX, 1, 1);

        // sort 
        ComputeBuffer tempBuffer = new ComputeBuffer((int)particleNum, Marshal.SizeOf(new Vector2()));
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
        cs.SetInt("_GridNumX", gridConfig.gridNumX);
        cs.SetInt("_GridNumY", gridConfig.gridNumY);
        cs.SetInt("_GridNumZ", gridConfig.gridNumZ);
        cs.SetBuffer(debugKernel, "_ParticleBuffer", particleBuff);
        cs.SetBuffer(debugKernel, "_ParticleGridPair", particleGridPairBuffer);
        cs.Dispatch(debugKernel, particleDispatchGroupX, 1, 1);
    }

    public void NeighborRangeDebug(ComputeBuffer particleBuff, int index, float neighborRange, NeighborMode mode)
    {
        int neighborDebug = cs.FindKernel("NeighborDebug");
        SetGridConfig();
        cs.SetInt("_ParticleNum", particleBuff.count);
        cs.SetInt("_TargetIndex", index);
        cs.SetInt("_NeighborMode", (int)mode);
        cs.SetFloat("_Range", neighborRange);
        cs.SetBuffer(neighborDebug, "_GridTable", gridTableBuffer);
        cs.SetBuffer(neighborDebug, "_ParticleBuffer", particleBuff);
        cs.Dispatch(neighborDebug, particleDispatchGroupX, 1, 1);
    }

    void SetGridConfig()
    {
        cs.SetFloat("_BoundaryXMin", gridConfig.boundaryXMin);
        cs.SetFloat("_BoundaryXMax", gridConfig.boundaryXMax);
        cs.SetFloat("_BoundaryYMin", gridConfig.boundaryYMin);
        cs.SetFloat("_BoundaryYMax", gridConfig.boundaryYMax);
        cs.SetFloat("_BoundaryZMin", gridConfig.boundaryZMin);
        cs.SetFloat("_BoundaryZMax", gridConfig.boundaryZMax);
        cs.SetInt("_GridNumX", gridConfig.gridNumX);
        cs.SetInt("_GridNumY", gridConfig.gridNumY);
        cs.SetInt("_GridNumZ", gridConfig.gridNumZ);
    }

    public void DisposeGridBuffers()
    {
        particleGridPairBuffer?.Release();
        particleRearrangedBuffer?.Release();
        gridTableBuffer?.Release();
    }

    public void DebugGrid()
    {
        for (int i = 0; i < gridConfig.gridNumX; i++)
        {
            for (int j = 0; j < gridConfig.gridNumY; j++)
            {
                for (int k = 0; k < gridConfig.gridNumZ; k++)
                {
                    float width = (gridConfig.boundaryXMax - gridConfig.boundaryXMin) / (float)gridConfig.gridNumX;
                    float height = (gridConfig.boundaryYMax - gridConfig.boundaryYMin) / (float)gridConfig.gridNumY;
                    float depth = (gridConfig.boundaryZMax - gridConfig.boundaryZMin) / (float)gridConfig.gridNumZ;
                    Vector3 center = new Vector3(gridConfig.boundaryXMin + i * width + width * 0.5f,
                                                 gridConfig.boundaryYMin + j * height + height * 0.5f,
                                                 gridConfig.boundaryZMin + k * depth + depth * 0.5f);
                    Gizmos.DrawWireCube(center, new Vector3(width, height, depth));
                }
            }
        }
    }

}
