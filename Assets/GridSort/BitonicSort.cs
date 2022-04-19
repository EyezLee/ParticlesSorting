using UnityEngine;
using System.Runtime.InteropServices;

public static class BitonicSort
{
    // ref 
    // The number of elements to sort is limited to an even power of 2
    // At minimum 8,192 elements - BITONIC_BLOCK_SIZE * TRANSPOSE_BLOCK_SIZE
    // At maximum 262,144 elements - BITONIC_BLOCK_SIZE * BITONIC_BLOCK_SIZE
    private static uint botonicBlockSize = 512;
    public static uint BITONIC_BLOCK_SIZE { get { return botonicBlockSize; } set { botonicBlockSize = value; } }
    private static uint transposeBlockSize = 16;
    public static uint TRANSPOSE_BLOCK_SIZE { get { return transposeBlockSize; } set { transposeBlockSize = value; } }

    static ComputeShader bitonicCS = Resources.Load<ComputeShader>("BitonicSortCS");

    public static void Sort(ComputeBuffer inBuffer, ComputeBuffer tempBuffer)
    {
        if(tempBuffer == null || tempBuffer.count != inBuffer.count)
        {
            tempBuffer?.Release();
            tempBuffer = new ComputeBuffer(inBuffer.count, Marshal.SizeOf(typeof(Vector2)));
        }

        int KERNEL_ID_BITONICSORT = bitonicCS.FindKernel("BitonicSort");
        int KERNEL_ID_TRANSPOSE = bitonicCS.FindKernel("MatrixTranspose");

        uint NUM_ELEMENTS = (uint)inBuffer.count;
        uint MATRIX_WIDTH = BITONIC_BLOCK_SIZE;
        uint MATRIX_HEIGHT = (uint)NUM_ELEMENTS / BITONIC_BLOCK_SIZE;

        for (uint level = 2; level <= BITONIC_BLOCK_SIZE; level <<= 1)
        {
            SetGPUSortConstants(bitonicCS, level, level, MATRIX_HEIGHT, MATRIX_WIDTH);

            // Sort the row data
            bitonicCS.SetBuffer(KERNEL_ID_BITONICSORT, "Data", inBuffer);
            bitonicCS.Dispatch(KERNEL_ID_BITONICSORT, Mathf.CeilToInt(NUM_ELEMENTS / (float)BITONIC_BLOCK_SIZE), 1, 1);
        }

        // Then sort the rows and columns for the levels > than the block size
        // Transpose. Sort the Columns. Transpose. Sort the Rows.
        for (uint level = (BITONIC_BLOCK_SIZE << 1); level <= NUM_ELEMENTS; level <<= 1)
        {
            // Transpose the data from buffer 1 into buffer 2
            SetGPUSortConstants(bitonicCS, level / BITONIC_BLOCK_SIZE, (level & ~NUM_ELEMENTS) / BITONIC_BLOCK_SIZE, MATRIX_WIDTH, MATRIX_HEIGHT);
            bitonicCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Input", inBuffer);
            bitonicCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Data", tempBuffer);
            bitonicCS.Dispatch(KERNEL_ID_TRANSPOSE, (int)(MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE), (int)(MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the transposed column data
            bitonicCS.SetBuffer(KERNEL_ID_BITONICSORT, "Data", tempBuffer);
            bitonicCS.Dispatch(KERNEL_ID_BITONICSORT, (int)(NUM_ELEMENTS / BITONIC_BLOCK_SIZE), 1, 1);

            // Transpose the data from buffer 2 back into buffer 1
            SetGPUSortConstants(bitonicCS, BITONIC_BLOCK_SIZE, level, MATRIX_HEIGHT, MATRIX_WIDTH);
            bitonicCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Input", tempBuffer);
            bitonicCS.SetBuffer(KERNEL_ID_TRANSPOSE, "Data", inBuffer);
            bitonicCS.Dispatch(KERNEL_ID_TRANSPOSE, (int)(MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE), (int)(MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE), 1);

            // Sort the row data
            bitonicCS.SetBuffer(KERNEL_ID_BITONICSORT, "Data", inBuffer);
            bitonicCS.Dispatch(KERNEL_ID_BITONICSORT, (int)(NUM_ELEMENTS / BITONIC_BLOCK_SIZE), 1, 1);
        }
    }

    static void SetGPUSortConstants(ComputeShader cs, uint level, uint levelMask, uint width, uint height)
    {
        cs.SetInt("_Level", (int)level);
        cs.SetInt("_LevelMask", (int)levelMask);
        cs.SetInt("_Width", (int)width);
        cs.SetInt("_Height", (int)height);
    }
}
