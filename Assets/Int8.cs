using UnityEngine;

public static class Int8
{
    public static string To8Adic(float f)
    {
        if (f < 0 || f > 1) throw new System.Exception("Value must be between 0 and 1");
        string res = "";
        f = f*8;
        int i;
        while (f >= (int)f && res.Length < 3)
        {
            if (f < 1 / 512) break;
            i = (int)f;
            res = $"{i}{res}";
            f -= i;
            f *= 8;
        }
        if (res.Length == 0)
            return "0";
        return res;
    }

    public static float From8Adic(string num)
    {
        int max_loop = 64;
        int i = 8;
        float f = 0;
        while (num.Length > 0)
        {
            int n = int.Parse(num.Substring(num.Length - 1, 1));
            f += (float)n / i;
            i = i * 8;
            num = num.Substring(0, num.Length - 1);
            if (max_loop-- < 0) break;
        }
        return f;
    }

    public static Vector3 From8AdicVector(string vector)
    {
        string[] vectorParts = vector.Split('_');
        string x = vectorParts[0];
        string y = vectorParts[1];
        string z = vectorParts[2];
        return new Vector3(From8Adic(x), From8Adic(y), From8Adic(z));
    }
}