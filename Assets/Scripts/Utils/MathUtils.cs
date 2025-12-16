using System.Collections.Generic;
using UnityEngine;

public static class MathUtils
{
    // === common functions ===
    public static bool IsNoise(float x)
    {
        return Mathf.Abs(x) < Mathf.Epsilon;
    }

     public static float Ceil(float value, int digits)
    {
        float m = Mathf.Pow(10f, digits);
        return Mathf.Ceil(value * m) / m;
    }

     public static float Floor(float value, int digits)
    {
        float m = Mathf.Pow(10f, digits);
        return Mathf.Floor(value * m) / m;
    }

    // === sigmoid functions ===
    public static float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }

    public static float[] Sigmoid(float[] arr)
    {
        float[] result = new float[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            result[i] = 1f / (1f + Mathf.Exp(-arr[i]));
        return result;
    }

    public static List<float> Sigmoid(List<float> arr)
    {
        List<float> result = new List<float>(arr.Count);
        for (int i = 0; i < arr.Count; i++)
            result.Add(1f / (1f + Mathf.Exp(-arr[i])));
        return result;
    }

    // softmax functions ===
    public static float[] Softmax(float[] arr)
    {
        float max = float.NegativeInfinity;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] > max)
                max = arr[i];

        // exp(x - max) 계산
        float sumExp = 0f;
        float[] expArr = new float[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
            float e = Mathf.Exp(arr[i] - max);
            expArr[i] = e;
            sumExp += e;
        }

        // softmax = exp / sum(exp)
        float[] result = new float[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            result[i] = expArr[i] / sumExp;

        return result;
    }
    public static List<float> Softmax(List<float> arr)
    {
        float max = float.NegativeInfinity;
        for (int i = 0; i < arr.Count; i++)
            if (arr[i] > max)
                max = arr[i];

        float sumExp = 0f;
        float[] expArr = new float[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            float e = Mathf.Exp(arr[i] - max);
            expArr[i] = e;
            sumExp += e;
        }

        List<float> result = new List<float>(arr.Count);
        for (int i = 0; i < arr.Count; i++)
            result.Add(expArr[i] / sumExp);

        return result;
    }
}
