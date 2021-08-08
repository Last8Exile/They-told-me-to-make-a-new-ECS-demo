using Unity.Mathematics;

public static class MathExtensions
{
    public static float Angle(float2 from, float2 to)
    {
        float num = math.sqrt(math.lengthsq(from) * math.lengthsq(to));
        if (num < 1E-15f)
            return 0f;

        float num2 = math.clamp(math.dot(from, to) / num, -1f, 1f);
        return math.acos(num2) * 57.29578f;
    }

    public static float SignedAngle(float2 from, float2 to)
    {
        float num = Angle(from, to);
        float num2 = math.sign(from.x * to.y - from.y * to.x);
        return num * num2;
    }

    public static float2 Direction2D(this quaternion quaternion)
    {
        return math.rotate(quaternion, new float3(1, 0, 0)).xy;
    }

    public static quaternion Rotate2D(this quaternion quaternion, float angle)
    {
        return math.mul(quaternion, quaternion.AxisAngle(new float3(0, 0, 1), angle));
    }

    public static float2 Rotate2D(this float2 vector, quaternion quaternion)
    {
        return math.rotate(quaternion, new float3(vector, 0)).xy;
    }

    public static float Clamp01(float value)
    {
        return math.clamp(value, 0, 1);
    }

    public static int Wrap(int value, int wrap)
    {
        return value % wrap;
    }
}
