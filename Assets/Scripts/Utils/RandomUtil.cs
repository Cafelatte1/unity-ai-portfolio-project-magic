using System;
using System.Collections.Generic;
using Unity.Mathematics;

// Comment
// rng를 ref(call by reference)로 넘기면, struct 타입인 rng가 deep copy본으로 넘어가지 않고 포인터가 넘어감
// 이 상태에서 랜덤 값을 생성하게 되면 내부 상태값이 바뀌고 그래야 난수 배열의 state가 바뀜
// Ex. ref를 안쓰면 Method1(rng) -> rng(state=100).Random(), Method2(rng) -> rng(state=100).Random() 같이 state가 안 바뀌고
// ref를 써서 넘기면 Method1(ref rng) -> rng(state=100).Random(), Method2(ref rng) -> rng(state=101).Random() 같이 state가 바뀜
public static class RandomUtils
{
    // === random number generation functions ===
    public static float RandomNormal(float mean, float stdDev)
    {
        float u1 = 1f - UnityEngine.Random.value; // (0,1)
        float u2 = 1f - UnityEngine.Random.value;
        float randStdNormal = math.sqrt(-2f * math.log(u1)) * math.sin(2f * math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
    public static float RandomNormal(float mean, float stdDev, ref Unity.Mathematics.Random rng)
    {
        float u1 = 1f - rng.NextFloat();
        float u2 = 1f - rng.NextFloat();
        float randStdNormal = math.sqrt(-2f * math.log(u1)) * math.sin(2f * math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
    public static float3 RandomNormal3(float mean = 0f, float stdDev = 1f)
    {
        return RandomNormal3(new float3(mean), new float3(stdDev));
    }
    public static float3 RandomNormal3(float3 mean, float3 stdDev)
    {
        float x = RandomNormal(mean.x, stdDev.x);
        float y = RandomNormal(mean.y, stdDev.y);
        float z = RandomNormal(mean.z, stdDev.z);
        return new float3(x, y, z);
    }
    
    /// === sampling function ===
    private static int WeightedIndex(float[] weights, System.Random rng)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        float r = (float)(rng.NextDouble() * total);
        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (r <= cumulative)
                return i;
        }
        return weights.Length - 1;
    }
    private static int WeightedIndex(float[] weights, ref Unity.Mathematics.Random rng)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        float r = rng.NextFloat() * total;
        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (r <= cumulative)
                return i;
        }
        return weights.Length - 1;
    }
    public static T[] SampleArray<T>(T[] array, int n, bool replacement = false, float[] weights = null)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("입력 배열이 비어 있습니다.");
        if (weights != null && weights.Length != array.Length)
            throw new ArgumentException("weights 길이가 array 길이와 다릅니다.");
        if (!replacement && n > array.Length)
            throw new ArgumentException("비복원 추출에서 n이 배열 길이를 초과했습니다.");

        System.Random rng = new System.Random();
        List<T> result = new List<T>(n);

        if (weights == null)
        {
            // ===== Uniform 기존 로직 =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                    result.Add(array[rng.Next(array.Length)]);
            }
            else
            {
                List<T> pool = new List<T>(array);
                for (int i = pool.Count - 1; i > 0; i--)
                    (pool[i], pool[rng.Next(i + 1)]) = (pool[rng.Next(i + 1)], pool[i]);

                result.AddRange(pool.GetRange(0, n));
            }
        }
        else
        {
            // ===== Weighted Sampling =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                {
                    int idx = WeightedIndex(weights, rng);
                    result.Add(array[idx]);
                }
            }
            else
            {
                List<T> pool = new List<T>(array);
                List<float> w = new List<float>(weights);

                for (int k = 0; k < n; k++)
                {
                    int idx = WeightedIndex(w.ToArray(), rng);
                    result.Add(pool[idx]);
                    pool.RemoveAt(idx);
                    w.RemoveAt(idx);
                }
            }
        }

        return result.ToArray();
    }
    public static T[] SampleArray<T>(T[] array, int n, ref Unity.Mathematics.Random rng, bool replacement = false, float[] weights = null)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("입력 배열이 비어 있습니다.");
        if (weights != null && weights.Length != array.Length)
            throw new ArgumentException("weights 길이가 array 길이와 다릅니다.");
        if (!replacement && n > array.Length)
            throw new ArgumentException("비복원 추출에서 n이 배열 길이를 초과했습니다.");

        List<T> result = new List<T>(n);

        if (weights == null)
        {
            // ===== Uniform 기존 로직 =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                    result.Add(array[rng.NextInt(array.Length)]);
            }
            else
            {
                List<T> pool = new List<T>(array);
                for (int i = pool.Count - 1; i > 0; i--)
                    (pool[i], pool[rng.NextInt(i + 1)]) = (pool[rng.NextInt(i + 1)], pool[i]);

                result.AddRange(pool.GetRange(0, n));
            }
        }
        else
        {
            // ===== Weighted Sampling =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                {
                    int idx = WeightedIndex(weights, ref rng);
                    result.Add(array[idx]);
                }
            }
            else
            {
                List<T> pool = new List<T>(array);
                List<float> w = new List<float>(weights);

                for (int k = 0; k < n; k++)
                {
                    int idx = WeightedIndex(w.ToArray(), ref rng);
                    result.Add(pool[idx]);
                    pool.RemoveAt(idx);
                    w.RemoveAt(idx);
                }
            }
        }

        return result.ToArray();
    }
    public static T[] SampleList<T>(List<T> list, int n, bool replacement = false, float[] weights = null)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("입력 리스트가 비어 있습니다.");
        if (weights != null && weights.Length != list.Count)
            throw new ArgumentException("weights 길이가 list 길이와 다릅니다.");
        if (!replacement && n > list.Count)
            throw new ArgumentException("비복원 추출에서 n이 리스트 길이를 초과했습니다.");

        System.Random rng = new System.Random();
        List<T> result = new List<T>(n);

        if (weights == null)
        {
            // ===== Uniform 기존 로직 =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                    result.Add(list[rng.Next(list.Count)]);
            }
            else
            {
                List<T> pool = new List<T>(list);
                for (int i = pool.Count - 1; i > 0; i--)
                    (pool[i], pool[rng.Next(i + 1)]) = (pool[rng.Next(i + 1)], pool[i]);

                result.AddRange(pool.GetRange(0, n));
            }
        }
        else
        {
            // ===== Weighted Sampling =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                {
                    int idx = WeightedIndex(weights, rng);
                    result.Add(list[idx]);
                }
            }
            else
            {
                List<T> pool = new List<T>(list);
                List<float> w = new List<float>(weights);

                for (int k = 0; k < n; k++)
                {
                    int idx = WeightedIndex(w.ToArray(), rng);
                    result.Add(pool[idx]);
                    pool.RemoveAt(idx);
                    w.RemoveAt(idx);
                }
            }
        }

        return result.ToArray();
    }
    public static T[] SampleList<T>(List<T> list, int n, ref Unity.Mathematics.Random rng, bool replacement = false, float[] weights = null)
    {
        if (list == null || list.Count == 0)
            throw new ArgumentException("입력 리스트가 비어 있습니다.");
        if (weights != null && weights.Length != list.Count)
            throw new ArgumentException("weights 길이가 list 길이와 다릅니다.");
        if (!replacement && n > list.Count)
            throw new ArgumentException("비복원 추출에서 n이 리스트 길이를 초과했습니다.");

        List<T> result = new List<T>(n);

        if (weights == null)
        {
            // ===== Uniform 기존 로직 =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                    result.Add(list[rng.NextInt(list.Count)]);
            }
            else
            {
                List<T> pool = new List<T>(list);
                for (int i = pool.Count - 1; i > 0; i--)
                    (pool[i], pool[rng.NextInt(i + 1)]) = (pool[rng.NextInt(i + 1)], pool[i]);

                result.AddRange(pool.GetRange(0, n));
            }
        }
        else
        {
            // ===== Weighted Sampling =====
            if (replacement)
            {
                for (int i = 0; i < n; i++)
                {
                    int idx = WeightedIndex(weights, ref rng);
                    result.Add(list[idx]);
                }
            }
            else
            {
                List<T> pool = new List<T>(list);
                List<float> w = new List<float>(weights);

                for (int k = 0; k < n; k++)
                {
                    int idx = WeightedIndex(w.ToArray(), ref rng);
                    result.Add(pool[idx]);
                    pool.RemoveAt(idx);
                    w.RemoveAt(idx);
                }
            }
        }

        return result.ToArray();
    }

    // === shuffling functions ===
    public static void ShuffleArray<T>(T[] array)
    {
        System.Random rng = new System.Random();
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
    public static void ShuffleArray<T>(T[] array, ref Unity.Mathematics.Random rng)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rng.NextInt(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
    public static void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    public static void ShuffleList<T>(List<T> list, ref Unity.Mathematics.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

}
