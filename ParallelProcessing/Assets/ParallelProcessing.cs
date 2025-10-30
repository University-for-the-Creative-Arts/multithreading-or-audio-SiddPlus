using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ParallelProcessing : MonoBehaviour
{
    [Header("Image Settings")]
    public string imagePath = "Assets/Images/test.png";  // Update with your image path

    void Start()
    {
        ProcessImage();
    }

    void ProcessImage()
    {
        // --- STEP 1: Load the image from disk ---
        if (!System.IO.File.Exists(imagePath))
        {
            UnityEngine.Debug.LogError($"Image not found at path: {imagePath}");
            return;
        }

        byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        Color32[] pixels = texture.GetPixels32();
        int pixelCount = pixels.Length;

        // --- STEP 2: Prepare NativeArrays ---
        NativeArray<Color32> pixelArray = new NativeArray<Color32>(pixels, Allocator.TempJob);

        // Partial sums (one per batch)
        int batchSize = 1024;
        int batchCount = Mathf.CeilToInt(pixelCount / (float)batchSize);
        NativeArray<long> batchSums = new NativeArray<long>(batchCount, Allocator.TempJob);

        // --- STEP 3: Define and schedule the job ---
        RedChannelSumJob sumJob = new RedChannelSumJob
        {
            pixels = pixelArray,
            batchSums = batchSums,
            batchSize = batchSize
        };

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        JobHandle handle = sumJob.Schedule(batchCount, 1);
        handle.Complete();

        stopwatch.Stop();

        // --- STEP 4: Aggregate batch results ---
        long totalRed = 0;
        for (int i = 0; i < batchSums.Length; i++)
        {
            totalRed += batchSums[i];
        }

        // --- STEP 5: Log results ---
        UnityEngine.Debug.Log($"✅ Total Red Channel Sum: {totalRed}");
        UnityEngine.Debug.Log($"⏱ Execution Time: {stopwatch.ElapsedMilliseconds} ms");
        UnityEngine.Debug.Log($"Pixels Processed: {pixelCount}");

        // --- STEP 6: Cleanup ---
        pixelArray.Dispose();
        batchSums.Dispose();
    }

    // --- JOB STRUCT ---
    [BurstCompile]
    struct RedChannelSumJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Color32> pixels;
        [WriteOnly] public NativeArray<long> batchSums;
        [ReadOnly] public int batchSize;

        public void Execute(int batchIndex)
        {
            int start = batchIndex * batchSize;
            int end = math.min(start + batchSize, pixels.Length);
            long localSum = 0;

            for (int i = start; i < end; i++)
            {
                localSum += pixels[i].r;
            }

            batchSums[batchIndex] = localSum;
        }
    }
}
