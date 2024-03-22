using Delta.Scenes;
using System;
using System.IO;

namespace Delta.Files;
internal class SceneCollection
{
    private readonly GuidAsset<Scene>[] _scenes;

    public ReadOnlySpan<GuidAsset<Scene>> GetScenes() => _scenes;

    const string Directory = "SceneCollection";

    public SceneCollection(GuidAsset<Scene>[] scenes)
    {
        _scenes = scenes;
    }

    public SceneCollection(string path)
    {
        _scenes = Serialization.Deserialize<GuidAsset<Scene>[]>(Path.Combine(path, Directory));
    }
}
