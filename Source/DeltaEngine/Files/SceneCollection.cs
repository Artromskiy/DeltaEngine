using Delta.Runtime;
using Delta.Scenes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Delta.Files;

internal class SceneCollection
{
    private readonly List<GuidAsset<Scene>> _scenes = [];
    public ReadOnlySpan<GuidAsset<Scene>> GetScenes() => CollectionsMarshal.AsSpan(_scenes);

    public SceneCollection(IProjectPath projectPath)
    {
        //if (File.Exists(projectPath.ScenesFile))
        //    _scenes = Serialization.Deserialize<List<GuidAsset<Scene>>>(projectPath.ScenesFile);
    }
}