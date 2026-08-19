using BenchmarkDotNet.Attributes;
using Delta.Maths;
using Matrix4x4 = Delta.Maths.float4x4;
using Quaternion = Delta.Maths.quaternion;
using Vector3 = Delta.Maths.float3;
using Vector4 = Delta.Maths.float4;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Delta.Engine.Benchmarks;

[SimpleJob(iterationCount: 30)]
[MeanColumn, StdErrorColumn, StdDevColumn, MedianColumn, MemoryDiagnoser]
public class MatrixBench
{
    private readonly Vector3[] _positions = new Vector3[R];
    private readonly Quaternion[] _rotations = new Quaternion[R];
    private readonly Vector3[] _scales = new Vector3[R];

    private readonly Vector3[] positions = new Vector3[R];
    private readonly Quaternion[] rotations = new Quaternion[R];
    private readonly Vector3[] scales = new Vector3[R];

    private static Random rnd = new(Magic);

    private const int Magic = 132;
    private const int R = 40000000;

    static MatrixBench()
    {
        ValidateMathLayout();
        ValidateMatrixSemantics();
    }

    private static void ValidateMathLayout()
    {
        if (Unsafe.SizeOf<Quaternion>() != 16 || Unsafe.SizeOf<Vector4>() != 16 ||
            Marshal.OffsetOf<Quaternion>(nameof(Quaternion.x)) != Marshal.OffsetOf<Vector4>(nameof(Vector4.x)) ||
            Marshal.OffsetOf<Quaternion>(nameof(Quaternion.y)) != Marshal.OffsetOf<Vector4>(nameof(Vector4.y)) ||
            Marshal.OffsetOf<Quaternion>(nameof(Quaternion.z)) != Marshal.OffsetOf<Vector4>(nameof(Vector4.z)) ||
            Marshal.OffsetOf<Quaternion>(nameof(Quaternion.w)) != Marshal.OffsetOf<Vector4>(nameof(Vector4.w)))
        {
            throw new InvalidOperationException("Delta.Maths quaternion/float4 layout is incompatible with the benchmark SIMD reinterpretation.");
        }
    }

    private static void ValidateMatrixSemantics()
    {
        var translation = new Vector3(3f, -2f, 5f);
        var rotation = Quaternion.CreateFromYawPitchRoll(0.31f, -0.47f, 0.73f);
        var scale = new Vector3(2f, 3f, 4f);
        var expected = Matrix4x4.CreateTRS(translation, rotation, scale);

        AssertMatrix(expected, ModelMatrix2(translation, rotation, scale), nameof(ModelMatrix2));
        AssertMatrix(expected, ModelMatrix3(translation, rotation, scale), nameof(ModelMatrix3));
        AssertMatrix(expected, ModelMatrixSqrt2NonVec(translation, rotation, scale), nameof(ModelMatrixSqrt2NonVec));
        AssertMatrix(expected, ModelMatrixSqrt2(translation, rotation, scale), nameof(ModelMatrixSqrt2));
        AssertMatrix(expected, ModelMatrixOldVectorizedSqrt2V2(translation, rotation, scale), nameof(ModelMatrixOldVectorizedSqrt2V2));
        AssertMatrix(expected, ModelMatrixOldNonVectorized(translation, rotation, scale), nameof(ModelMatrixOldNonVectorized));
        AssertMatrix(expected, ModelMatrixOld2(translation, rotation, scale), nameof(ModelMatrixOld2));
        AssertMatrix(expected, ModelMatrixOldFused(translation, rotation, scale), nameof(ModelMatrixOldFused));
        AssertMatrix(expected, ModelMatrix(translation, rotation, scale), nameof(ModelMatrix));
        AssertMatrix(expected, ModelMatrixCasted(translation, rotation, scale), nameof(ModelMatrixCasted));
    }

    private static void AssertMatrix(Matrix4x4 expected, Matrix4x4 actual, string name)
    {
        for (var column = 0; column < 4; column++)
        {
            for (var row = 0; row < 4; row++)
            {
                var difference = MathF.Abs(expected.GetElement(column, row) - actual.GetElement(column, row));
                if (difference > 1e-5f)
                    throw new InvalidOperationException($"{name} does not match Delta.Maths column-vector TRS at ({column}, {row}).");
            }
        }
    }

    public MatrixBench()
    {
        rnd = new(Magic);
        for (int i = 0; i < R; i++)
        {
            _positions[i] = new((rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100);
            _rotations[i] = Quaternion.CreateFromYawPitchRoll(rnd.NextSingle() * float.Pi, rnd.NextSingle() * float.Pi, rnd.NextSingle() * float.Pi);
            _scales[i] = new((rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100);
        }
    }

    [GlobalSetup]
    public void StaticSetup()
    {
        rnd = new(Magic);
        for (int i = 0; i < R; i++)
        {
            _positions[i] = new((rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100);
            _rotations[i] = Quaternion.CreateFromYawPitchRoll(rnd.NextSingle() * float.Pi, rnd.NextSingle() * float.Pi, rnd.NextSingle() * float.Pi);
            _scales[i] = new((rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100, (rnd.NextSingle() - 0.5f) * 100);
        }
    }

    [IterationSetup]
    public void Setup()
    {
        Array.Copy(_positions, positions, R);
        Array.Copy(_rotations, rotations, R);
        Array.Copy(_scales, scales, R);
    }


    //[Benchmark]
    public float MatrixOld()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrix(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }
    //[Benchmark]
    public float MatrixOldNew()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrixOld2(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }
    [Benchmark]
    public float MatrixOldNonVectorized()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrixOldNonVectorized(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }
    [Benchmark]
    public float MatrixSqrt2()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrixSqrt2(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }
    [Benchmark]
    public float MatrixSqrt2V2()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrixOldVectorizedSqrt2V2(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }
    [Benchmark]
    public float MatrixOldSlow()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = Matrix4x4.CreateTRS(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }

    //[Benchmark]
    public float MatrixOldFused()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrixOldFused(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }
    //[Benchmark]
    public float MatrixNew()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrixCasted(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }

    //[Benchmark]
    public float MatrixNew2()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrix2(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }
    //[Benchmark]
    public float MatrixNew3()
    {
        float v = 0;
        bool add = false;
        for (int i = 0; i < R; i++)
        {
            var m = ModelMatrix3(positions[i], rotations[i], scales[i]);
            v += (add = !add) ? m.M11 : -m.M11;
        }
        return v;
    }


    private static Matrix4x4 ModelMatrix2(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        float x2 = rotation.x + rotation.x;
        float y2 = rotation.y + rotation.y;
        float z2 = rotation.z + rotation.z;

        float wx2 = rotation.w * x2;
        float wy2 = rotation.w * y2;
        float wz2 = rotation.w * z2;
        float xx2 = rotation.x * x2;
        float xy2 = rotation.x * y2;
        float xz2 = rotation.x * z2;
        float yy2 = rotation.y * y2;
        float yz2 = rotation.y * z2;
        float zz2 = rotation.z * z2;
        //float oneMinuszz2 = 1.0f - zz2;
        //float halfMinusyy2 = 0.5f - yy2;
        //float halfMinuszz2 = 0.5f - zz2;
        //float halfMinusxx2 = 0.5f - xx2;
        var x = new Vector3(1.0f - yy2 - zz2, xy2 + wz2, xz2 - wy2);
        var y = new Vector3(xy2 - wz2, 1.0f - xx2 - zz2, yz2 + wx2);
        var z = new Vector3(xz2 + wy2, yz2 - wx2, 1.0f - xx2 - yy2);

        // Next, scale the basis vectors
        x *= scale.x; // Vector * float
        y *= scale.y; // Vector * float
        z *= scale.z; // Vector * float

        // Extract the position of the transform
        Vector3 t = translation;

        return new Matrix4x4(
            new Vector4(x.x, x.y, x.z, 0f),
            new Vector4(y.x, y.y, y.z, 0f),
            new Vector4(z.x, z.y, z.z, 0f),
            new Vector4(t.x, t.y, t.z, 1f));
    }

    private static Matrix4x4 ModelMatrix3(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        float x2 = rotation.x + rotation.x;
        float y2 = rotation.y + rotation.y;
        float z2 = rotation.z + rotation.z;

        float wx2 = rotation.w * x2;
        float wy2 = rotation.w * y2;
        float wz2 = rotation.w * z2;
        float xy2 = rotation.x * y2;
        float xz2 = rotation.x * z2;
        float yz2 = rotation.y * z2;
        float halfMinusxx2 = 0.5f - (rotation.x * x2);
        float halfMinusyy2 = 0.5f - (rotation.y * y2);
        float halfMinuszz2 = 0.5f - (rotation.z * z2);
        var x = new Vector3(halfMinusyy2 + halfMinuszz2, xy2 + wz2, xz2 - wy2) * scale.x;
        var y = new Vector3(xy2 - wz2, halfMinusxx2 + halfMinuszz2, yz2 + wx2) * scale.y;
        var z = new Vector3(xz2 + wy2, yz2 - wx2, halfMinusxx2 + halfMinusyy2) * scale.z;
        return new Matrix4x4(
            new Vector4(x.x, x.y, x.z, 0f),
            new Vector4(y.x, y.y, y.z, 0f),
            new Vector4(z.x, z.y, z.z, 0f),
            new Vector4(translation.x, translation.y, translation.z, 1f));
    }
    private static Matrix4x4 ModelMatrixSqrt2NonVec(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        const float sqrt2 = 1.4142135623730951f; // every element in matrix except of translation
                                                 // ends up being multiplied by 2, so we multiply whole vector by sqrt2
                                                 // to skip multiplication at the end

        var rot = Unsafe.As<Quaternion, Vector4>(ref rotation);
        rot.x *= sqrt2;
        rot.y *= sqrt2;
        rot.z *= sqrt2;
        rot.w *= sqrt2;

        float xx = rot.x * rot.x;
        float yy = rot.y * rot.y;
        float zz = rot.z * rot.z;

        float xy = rot.x * rot.y;
        float xz = rot.x * rot.z;
        float xw = rot.x * rot.w;
        float yz = rot.y * rot.z;
        float yw = rot.y * rot.w;
        float zw = rot.z * rot.w;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale.y * (xy - zw);
        modelMatrix.M13 = scale.z * (xz + yw);
        modelMatrix.M21 = scale.x * (xy + zw);
        modelMatrix.M23 = scale.z * (yz - xw);
        modelMatrix.M31 = scale.x * (xz - yw);
        modelMatrix.M32 = scale.y * (yz + xw);
        modelMatrix.M11 = scale.x * (1f - (yy + zz));
        modelMatrix.M22 = scale.y * (1f - (xx + zz));
        modelMatrix.M33 = scale.z * (1f - (xx + yy));
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixSqrt2(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        const float sqrt2 = 1.4142135623730951f; // every element in matrix except of translation
                                                 // ends up being multiplied by 2, so we multiply whole vector by sqrt2
                                                 // to skip multiplication at the end

        var rot = Unsafe.As<Quaternion, Vector4>(ref rotation) * sqrt2;

        float xx = rot.x * rot.x;
        float yy = rot.y * rot.y;
        float zz = rot.z * rot.z;

        float xy = rot.x * rot.y;
        float xz = rot.x * rot.z;
        float xw = rot.x * rot.w;
        float yz = rot.y * rot.z;
        float yw = rot.y * rot.w;
        float zw = rot.z * rot.w;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale.y * (xy - zw);
        modelMatrix.M13 = scale.z * (xz + yw);
        modelMatrix.M21 = scale.x * (xy + zw);
        modelMatrix.M23 = scale.z * (yz - xw);
        modelMatrix.M31 = scale.x * (xz - yw);
        modelMatrix.M32 = scale.y * (yz + xw);
        modelMatrix.M11 = scale.x * (1f - (yy + zz));
        modelMatrix.M22 = scale.y * (1f - (xx + zz));
        modelMatrix.M33 = scale.z * (1f - (xx + yy));
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }
    const float sqrt2 = 1.4142135623730951f;
    private static readonly Vector4 sqrt2Vec = new(sqrt2);

    [MethodImpl( MethodImplOptions.AggressiveInlining)]
    private static Matrix4x4 ModelMatrixOldVectorizedSqrt2V2(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        //const float sqrt2 = 1.4142135623730951f; // every element in matrix except of translation
                                                 // ends up being multiplied by 2, so we multiply whole vector by sqrt2
                                                 // to skip multiplication at the end

        var rot = Unsafe.As<Quaternion, Vector4>(ref rotation) * sqrt2Vec;
        float x = rot.x;
        float y = rot.y;
        float xx = x * x;
        float xy = x * rot.y;
        float xz = x * rot.z;
        float xw = x * rot.w;
        float yy = rot.y * rot.y;
        float zz = rot.z * rot.z;

        float yz = rot.y * rot.z;
        float yw = rot.y * rot.w;
        float zw = rot.z * rot.w;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale.y * (xy - zw);
        modelMatrix.M13 = scale.z * (xz + yw);
        modelMatrix.M21 = scale.x * (xy + zw);
        modelMatrix.M23 = scale.z * (yz - xw);
        modelMatrix.M31 = scale.x * (xz - yw);
        modelMatrix.M32 = scale.y * (yz + xw);
        modelMatrix.M11 = scale.x * (1f - (yy + zz));
        modelMatrix.M22 = scale.y * (1f - (xx + zz));
        modelMatrix.M33 = scale.z * (1f - (xx + yy));
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixOldNonVectorized(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        float xx = rotation.x * rotation.x;
        float xy = rotation.x * rotation.y;
        float xz = rotation.x * rotation.z;
        float xw = rotation.x * rotation.w;

        float yy = rotation.y * rotation.y;
        float yz = rotation.y * rotation.z;
        float yw = rotation.y * rotation.w;

        float zz = rotation.z * rotation.z;
        float zw = rotation.z * rotation.w;

        float scaleX2 = scale.x + scale.x;
        float scaleY2 = scale.y + scale.y;
        float scaleZ2 = scale.z + scale.z;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scaleY2 * (xy - zw);
        modelMatrix.M13 = scaleZ2 * (xz + yw);
        modelMatrix.M21 = scaleX2 * (xy + zw);
        modelMatrix.M23 = scaleZ2 * (yz - xw);
        modelMatrix.M31 = scaleX2 * (xz - yw);
        modelMatrix.M32 = scaleY2 * (yz + xw);
        modelMatrix.M11 = scaleX2 * (0.5f - (yy + zz));
        modelMatrix.M22 = scaleY2 * (0.5f - (xx + zz));
        modelMatrix.M33 = scaleZ2 * (0.5f - (xx + yy));
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixOld2(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.x * Unsafe.As<Quaternion, Vector4>(ref rotation);

        float yy = rotation.y * rotation.y;
        float yz = rotation.y * rotation.z;
        float yw = rotation.y * rotation.w;

        float zz = rotation.z * rotation.z;
        float zw = rotation.z * rotation.w;
        var scale2 = scale + scale;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.y * (x.y - zw);
        modelMatrix.M13 = scale2.z * (x.z + yw);
        modelMatrix.M21 = scale2.x * (x.y + zw);
        modelMatrix.M23 = scale2.z * (yz - x.w);
        modelMatrix.M31 = scale2.x * (x.z - yw);
        modelMatrix.M32 = scale2.y * (x.w + yz);
        modelMatrix.M11 = scale2.x * (0.5f - (yy + zz));
        modelMatrix.M22 = scale2.y * (0.5f - (x.x + zz));
        modelMatrix.M33 = scale2.z * (0.5f - (x.x + yy));
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixOldFused(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.x * Unsafe.As<Quaternion, Vector4>(ref rotation);

        float yy = rotation.y * rotation.y;
        float yz = rotation.y * rotation.z;
        float yw = rotation.y * rotation.w;

        float zz = rotation.z * rotation.z;
        float zw = rotation.z * rotation.w;
        var scale2 = scale + scale;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.y * (x.y - zw);
        modelMatrix.M13 = scale2.z * (x.z + yw);
        modelMatrix.M21 = scale2.x * (x.y + zw);
        modelMatrix.M23 = scale2.z * (yz - x.w);
        modelMatrix.M31 = scale2.x * (x.z - yw);
        modelMatrix.M32 = scale2.y * (x.w + yz);
        modelMatrix.M11 = MathF.FusedMultiplyAdd(-scale2.x, yy + zz, scale.x);
        modelMatrix.M22 = MathF.FusedMultiplyAdd(-scale2.y, x.x + zz, scale.y);
        modelMatrix.M33 = MathF.FusedMultiplyAdd(-scale2.z, x.x + yy, scale.z);
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.x * Unsafe.As<Quaternion, Vector4>(ref rotation);

        float yy = rotation.y * rotation.y;
        float yz = rotation.y * rotation.z;
        float yw = rotation.y * rotation.w;

        float zz = rotation.z * rotation.z;
        float zw = rotation.z * rotation.w;
        var scale2 = scale * 2;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.y * (x.y - zw);
        modelMatrix.M13 = scale2.z * (x.z + yw);
        modelMatrix.M21 = scale2.x * (x.y + zw);
        modelMatrix.M23 = scale2.z * (yz - x.w);
        modelMatrix.M31 = scale2.x * (x.z - yw);
        modelMatrix.M32 = scale2.y * (x.w + yz);
        modelMatrix.M11 = scale.x - (scale2.x * (yy + zz));
        modelMatrix.M22 = scale.y - (scale2.y * (x.x + zz));
        modelMatrix.M33 = scale.z - (scale2.z * (x.x + yy));
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixCasted(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.x * Unsafe.As<Quaternion, Vector4>(ref rotation);

        Span<float> yzwSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<Quaternion, float>(ref rotation), 4)[1..];
        var y = MemoryMarshal.Cast<float, Vector3>(yzwSpan)[0] * rotation.y;

        float zz = rotation.z * rotation.z;
        float zw = rotation.z * rotation.w;
        var scale2 = scale * 2;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.y * (x.y - zw);
        modelMatrix.M13 = scale2.z * (x.z + y.z);
        modelMatrix.M21 = scale2.x * (x.y + zw);
        modelMatrix.M23 = scale2.z * (y.y - x.w);
        modelMatrix.M31 = scale2.x * (x.z - y.z);
        modelMatrix.M32 = scale2.y * (x.w + y.y);
        modelMatrix.M11 = scale.x - (scale2.x * (y.x + zz));
        modelMatrix.M22 = scale.y - (scale2.y * (x.x + zz));
        modelMatrix.M33 = scale.z - (scale2.z * (x.x + y.x));
        modelMatrix.M14 = translation.x;
        modelMatrix.M24 = translation.y;
        modelMatrix.M34 = translation.z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }
}
