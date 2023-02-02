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
}