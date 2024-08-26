using Delta.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
namespace DeltaEditorLib.Loader;

public static class ProjectCreator
{
    public static void CreateProject(IProjectPath projectPath)
    {
        if (IsDirectoryEmpty(projectPath.RootDirectory))
            SetupProjectDirectory(projectPath);
    }

    public static string GetExecutableDirectory()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        bool hasDirectory = arguments.Length > 1 && Directory.Exists(arguments[1]);
        return hasDirectory ? arguments[1] : Directory.CreateTempSubdirectory().FullName;
    }

    private static void SetupProjectDirectory(IProjectPath projectPath)
    {
        Directory.CreateDirectory(projectPath.AssetsDirectory);
        Directory.CreateDirectory(projectPath.ScriptsDirectory);
        Directory.CreateDirectory(projectPath.ResourcesDirectory);
        Directory.CreateDirectory(projectPath.ProjectDirectory);
        Directory.CreateDirectory(projectPath.DllsDirectory);

        File.Create(projectPath.ScenesFile).Dispose();
        File.Create(projectPath.SettingsFile).Dispose();
    }

    private static bool IsDirectoryEmpty(string path)
    {
        using IEnumerator<string> en = Directory.EnumerateFileSystemEntries(path).GetEnumerator();
        return !en.MoveNext();
    }
}
