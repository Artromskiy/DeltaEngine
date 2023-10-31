using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaEngine.Rendering;
public class RenderData
{
    public Transform transform;
    public MeshVariant mesh;
    public Material material;
    public bool isStatic;
}
