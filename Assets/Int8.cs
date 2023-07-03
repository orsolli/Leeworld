using UnityEngine;

public static class Int8
{
    public static string ToUInt8(float f)
    {
        string res = "";
        int i = (int)f;
        while (f >= (int)f && res.Length < 3)
        {
            if (f < 1 / 512) break;
            i = (int)f;
            res = $"{res}{i}";
            f -= i;
            f *= 8;
        }
        if (res.Length == 0)
            return "0";
        return res;
    }

    public static Vector3 FromUInt8Vector(string vector)
    {
        string[] vectorParts = vector.Split('_');
        string x = vectorParts[0];
        string y = vectorParts[1];
        string z = vectorParts[2];
        return new Vector3(FromUInt8(x), FromUInt8(y), FromUInt8(z));
    }

    public static float FromUInt8(string num)
    {
        int max_loop = 64;
        int i = 1;
        float f = 0;
        while (num.Length > 0)
        {
            int n = int.Parse(num.Substring(0, 1));
            f += (float)n / i;
            i = i * 8;
            num = num.Substring(1);
            if (max_loop-- < 0) break;
        }
        return f;
    }
}