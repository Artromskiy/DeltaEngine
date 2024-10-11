using BenchmarkDotNet.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeltaBench;

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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            var m = Matrix4x4.CreateFromQuaternion(rotations[i]) *
                    Matrix4x4.CreateScale(scales[i]) *
                    Matrix4x4.CreateTranslation(positions[i]);
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
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
            v += (add = !add) ? m[0, 0] : -m[0, 0];
        }
        return v;
    }


    private static Matrix4x4 ModelMatrix2(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        float x2 = rotation.X + rotation.X;
        float y2 = rotation.Y + rotation.Y;
        float z2 = rotation.Z + rotation.Z;

        float wx2 = rotation.W * x2;
        float wy2 = rotation.W * y2;
        float wz2 = rotation.W * z2;
        float xx2 = rotation.X * x2;
        float xy2 = rotation.X * y2;
        float xz2 = rotation.X * z2;
        float yy2 = rotation.Y * y2;
        float yz2 = rotation.Y * z2;
        float zz2 = rotation.Z * z2;
        //float oneMinuszz2 = 1.0f - zz2;
        //float halfMinusyy2 = 0.5f - yy2;
        //float halfMinuszz2 = 0.5f - zz2;
        //float halfMinusxx2 = 0.5f - xx2;
        var x = new Vector3(1.0f - yy2 - zz2, xy2 + wz2, xz2 - wy2);
        var y = new Vector3(xy2 - wz2, 1.0f - xx2 - zz2, yz2 + wx2);
        var z = new Vector3(xz2 + wy2, yz2 - wx2, 1.0f - xx2 - yy2);

        // Next, scale the basis vectors
        x *= scale.X; // Vector * float
        y *= scale.Y; // Vector * float
        z *= scale.Z; // Vector * float

        // Extract the position of the transform
        Vector3 t = translation;

        // Create matrix
        return new Matrix4x4(
            x.X, x.Y, x.Z, 0, // X basis (& Scale)
            y.X, y.Y, y.Z, 0, // Y basis (& scale)
            z.X, z.Y, z.Z, 0, // Z basis (& scale)
            t.X, t.Y, t.Z, 1  // Position
        );
    }

    private static Matrix4x4 ModelMatrix3(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        float x2 = rotation.X + rotation.X;
        float y2 = rotation.Y + rotation.Y;
        float z2 = rotation.Z + rotation.Z;

        float wx2 = rotation.W * x2;
        float wy2 = rotation.W * y2;
        float wz2 = rotation.W * z2;
        float xy2 = rotation.X * y2;
        float xz2 = rotation.X * z2;
        float yz2 = rotation.Y * z2;
        float halfMinusxx2 = 0.5f - (rotation.X * x2);
        float halfMinusyy2 = 0.5f - (rotation.Y * y2);
        float halfMinuszz2 = 0.5f - (rotation.Z * z2);
        Vector3 t = translation;
        return new Matrix4x4(
        (halfMinusyy2 + halfMinuszz2) * scale.X, (xy2 + wz2) * scale.X, (xz2 - wy2) * scale.X, 0,
        (xy2 - wz2) * scale.Y, (halfMinusxx2 + halfMinuszz2) * scale.Y, (yz2 + wx2) * scale.Y, 0,
        (xz2 + wy2) * scale.Z, (yz2 - wx2) * scale.Z, (halfMinusxx2 + halfMinusyy2) * scale.Z, 0,
            t.X, t.Y, t.Z, 1);
    }
    private static Matrix4x4 ModelMatrixSqrt2NonVec(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        const float sqrt2 = 1.4142135623730951f; // every element in matrix except of translation
                                                 // ends up being multiplied by 2, so we multiply whole vector by sqrt2
                                                 // to skip multiplication at the end

        var rot = Unsafe.As<Quaternion, Vector4>(ref rotation);
        rot.X *= sqrt2;
        rot.Y *= sqrt2;
        rot.Z *= sqrt2;
        rot.W *= sqrt2;

        float xx = rot.X * rot.X;
        float yy = rot.Y * rot.Y;
        float zz = rot.Z * rot.Z;

        float xy = rot.X * rot.Y;
        float xz = rot.X * rot.Z;
        float xw = rot.X * rot.W;
        float yz = rot.Y * rot.Z;
        float yw = rot.Y * rot.W;
        float zw = rot.Z * rot.W;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale.Y * (xy + zw);
        modelMatrix.M13 = scale.Z * (xz - yw);
        modelMatrix.M21 = scale.X * (xy - zw);
        modelMatrix.M23 = scale.Z * (yz + xw);
        modelMatrix.M31 = scale.X * (xz + yw);
        modelMatrix.M32 = scale.Y * (yz - xw);
        modelMatrix.M11 = scale.X * (1f - (yy + zz));
        modelMatrix.M22 = scale.Y * (1f - (xx + zz));
        modelMatrix.M33 = scale.Z * (1f - (xx + yy));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixSqrt2(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        const float sqrt2 = 1.4142135623730951f; // every element in matrix except of translation
                                                 // ends up being multiplied by 2, so we multiply whole vector by sqrt2
                                                 // to skip multiplication at the end

        var rot = Unsafe.As<Quaternion, Vector4>(ref rotation) * sqrt2;

        float xx = rot.X * rot.X;
        float yy = rot.Y * rot.Y;
        float zz = rot.Z * rot.Z;

        float xy = rot.X * rot.Y;
        float xz = rot.X * rot.Z;
        float xw = rot.X * rot.W;
        float yz = rot.Y * rot.Z;
        float yw = rot.Y * rot.W;
        float zw = rot.Z * rot.W;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale.Y * (xy + zw);
        modelMatrix.M13 = scale.Z * (xz - yw);
        modelMatrix.M21 = scale.X * (xy - zw);
        modelMatrix.M23 = scale.Z * (yz + xw);
        modelMatrix.M31 = scale.X * (xz + yw);
        modelMatrix.M32 = scale.Y * (yz - xw);
        modelMatrix.M11 = scale.X * (1f - (yy + zz));
        modelMatrix.M22 = scale.Y * (1f - (xx + zz));
        modelMatrix.M33 = scale.Z * (1f - (xx + yy));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
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
        float x = rot.X;
        float y = rot.Y;
        float xx = x * x;
        float xy = x * rot.Y;
        float xz = x * rot.Z;
        float xw = x * rot.W;
        float yy = rot.Y * rot.Y;
        float zz = rot.Z * rot.Z;

        float yz = rot.Y * rot.Z;
        float yw = rot.Y * rot.W;
        float zw = rot.Z * rot.W;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale.Y * (xy + zw);
        modelMatrix.M13 = scale.Z * (xz - yw);
        modelMatrix.M21 = scale.X * (xy - zw);
        modelMatrix.M23 = scale.Z * (yz + xw);
        modelMatrix.M31 = scale.X * (xz + yw);
        modelMatrix.M32 = scale.Y * (yz - xw);
        modelMatrix.M11 = scale.X * (1f - (yy + zz));
        modelMatrix.M22 = scale.Y * (1f - (xx + zz));
        modelMatrix.M33 = scale.Z * (1f - (xx + yy));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixOldNonVectorized(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        float xx = rotation.X * rotation.X;
        float xy = rotation.X * rotation.Y;
        float xz = rotation.X * rotation.Z;
        float xw = rotation.X * rotation.W;

        float yy = rotation.Y * rotation.Y;
        float yz = rotation.Y * rotation.Z;
        float yw = rotation.Y * rotation.W;

        float zz = rotation.Z * rotation.Z;
        float zw = rotation.Z * rotation.W;

        float scaleX2 = scale.X + scale.X;
        float scaleY2 = scale.Y + scale.Y;
        float scaleZ2 = scale.Z + scale.Z;

        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scaleY2 * (xy + zw);
        modelMatrix.M13 = scaleZ2 * (xz - yw);
        modelMatrix.M21 = scaleX2 * (xy - zw);
        modelMatrix.M23 = scaleZ2 * (yz + xw);
        modelMatrix.M31 = scaleX2 * (xz + yw);
        modelMatrix.M32 = scaleY2 * (yz - xw);
        modelMatrix.M11 = scaleX2 * (0.5f - (yy + zz));
        modelMatrix.M22 = scaleY2 * (0.5f - (xx + zz));
        modelMatrix.M33 = scaleZ2 * (0.5f - (xx + yy));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixOld2(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.X * Unsafe.As<Quaternion, Vector4>(ref rotation);

        float yy = rotation.Y * rotation.Y;
        float yz = rotation.Y * rotation.Z;
        float yw = rotation.Y * rotation.W;

        float zz = rotation.Z * rotation.Z;
        float zw = rotation.Z * rotation.W;
        var scale2 = scale + scale;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.Y * (x.Y + zw);
        modelMatrix.M13 = scale2.Z * (x.Z - yw);
        modelMatrix.M21 = scale2.X * (x.Y - zw);
        modelMatrix.M23 = scale2.Z * (x.W + yz);
        modelMatrix.M31 = scale2.X * (x.Z + yw);
        modelMatrix.M32 = scale2.Y * (-x.W + yz);
        modelMatrix.M11 = scale2.X * (0.5f - (yy + zz));
        modelMatrix.M22 = scale2.Y * (0.5f - (x.X + zz));
        modelMatrix.M33 = scale2.Z * (0.5f - (x.X + yy));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixOldFused(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.X * Unsafe.As<Quaternion, Vector4>(ref rotation);

        float yy = rotation.Y * rotation.Y;
        float yz = rotation.Y * rotation.Z;
        float yw = rotation.Y * rotation.W;

        float zz = rotation.Z * rotation.Z;
        float zw = rotation.Z * rotation.W;
        var scale2 = scale + scale;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.Y * (x.Y + zw);
        modelMatrix.M13 = scale2.Z * (x.Z - yw);
        modelMatrix.M21 = scale2.X * (x.Y - zw);
        modelMatrix.M23 = scale2.Z * (x.W + yz);
        modelMatrix.M31 = scale2.X * (x.Z + yw);
        modelMatrix.M32 = scale2.Y * (-x.W + yz);
        modelMatrix.M11 = MathF.FusedMultiplyAdd(-scale2.X, yy + zz, scale.X);
        modelMatrix.M22 = MathF.FusedMultiplyAdd(-scale2.Y, x.X + zz, scale.Y);
        modelMatrix.M33 = MathF.FusedMultiplyAdd(-scale2.Z, x.X + yy, scale.Z);
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.X * Unsafe.As<Quaternion, Vector4>(ref rotation);

        float yy = rotation.Y * rotation.Y;
        float yz = rotation.Y * rotation.Z;
        float yw = rotation.Y * rotation.W;

        float zz = rotation.Z * rotation.Z;
        float zw = rotation.Z * rotation.W;
        var scale2 = scale * 2;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.Y * (x.Y + zw);
        modelMatrix.M13 = scale2.Z * (x.Z - yw);
        modelMatrix.M21 = scale2.X * (x.Y - zw);
        modelMatrix.M23 = scale2.Z * (x.W + yz);
        modelMatrix.M31 = scale2.X * (x.Z + yw);
        modelMatrix.M32 = scale2.Y * (-x.W + yz);
        modelMatrix.M11 = scale.X - (scale2.X * (yy + zz));
        modelMatrix.M22 = scale.Y - (scale2.Y * (x.X + zz));
        modelMatrix.M33 = scale.Z - (scale2.Z * (x.X + yy));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }

    private static Matrix4x4 ModelMatrixCasted(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.X * Unsafe.As<Quaternion, Vector4>(ref rotation);

        Span<float> yzwSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<Quaternion, float>(ref rotation), 4)[1..];
        var y = MemoryMarshal.Cast<float, Vector3>(yzwSpan)[0] * rotation.Y;

        float zz = rotation.Z * rotation.Z;
        float zw = rotation.Z * rotation.W;
        var scale2 = scale * 2;
        Matrix4x4 modelMatrix = default;
        modelMatrix.M12 = scale2.Y * (x.Y + zw);
        modelMatrix.M13 = scale2.Z * (x.Z - y.Z);
        modelMatrix.M21 = scale2.X * (x.Y - zw);
        modelMatrix.M23 = scale2.Z * (x.W + y.Y);
        modelMatrix.M31 = scale2.X * (x.Z + y.Z);
        modelMatrix.M32 = scale2.Y * (-x.W + y.Y);
        modelMatrix.M11 = scale.X - (scale2.X * (y.X + zz));
        modelMatrix.M22 = scale.Y - (scale2.Y * (x.X + zz));
        modelMatrix.M33 = scale.Z - (scale2.Z * (x.X + y.X));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;
        modelMatrix.M44 = 1;

        return modelMatrix;
    }
}
