using Delta;
using System.Numerics;

namespace DeltaGenTest;

[Shader]
internal partial struct TestShader
{
    private struct Camera
    {
        public Matrix4x4 projView;
        public Matrix4x4 proj;
        public Matrix4x4 view;
        public Vector4 position;
        public Vector4 rotation;
    };

    [Layout(set: 0, binding: 0)] private readonly Matrix4x4[] InsTrs;
    [Layout(set: 0, binding: 0)] private readonly int[] InsIds;
    [Layout(set: 2, binding: 0)] private readonly Camera[] ScnCam;
    [Layout(set: 2, binding: 0)] private readonly Camera[] data;

    [Layout(location: 0)] private Vector4 fragColor;
    [Layout(location: 0)] private Vector4 outColor;

    [VertexEntry]
    public void VertMain()
    {
        int id = InsIds[InstanceIndex];
        Matrix4x4 model = InsTrs[id];
        var camera = ScnCam[0];
        //Position = camera.proj * camera.view * model * new Vector4(Pos2, 0.0f, 1.0f);
        fragColor = Color;
    }
    private readonly int a;
    [FragmentEntry]
    public void FragMain()
    {
        //Pos2 = new(1, 1);
        outColor = fragColor;
    }

}
