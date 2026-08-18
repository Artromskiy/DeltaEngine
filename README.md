# Delta Engine

> Current development direction: the engine is being split into standalone
> `DeltaECS` and `DeltaRender` projects. The target stack is SDL3-CS, Vulkan,
> MoltenVK on macOS, a Delta-owned XAML UI, GLSH, and Delta.Maths. Avalonia and
> Arch remain only as migration dependencies in the existing source. See the
> [architecture roadmap](docs/architecture-roadmap.md) before making new engine
> changes.

This repository contains the editor and game engine project.
This is the very beginning of development and, like any other similar project, it can cease to exist even without becoming a minimum viable product. At least we tried.

**For those who stumbled upon this repository and think it's gone**

I realized that the current implementation heavily depends on Avalonia, since the engine is ultimately forced to work in the same thread with it, or very tightly synchronize with the ui thread, in addition, the render from the camera has to be copied from the GPU to the CPU to display it inside the ui of Avalonia. This leads to difficulties in development and support, and to the fact that a lot of processor time is spent copying the render. To solve this problem, I wanted to write a ui renderer on xaml and the current renderer, but it was necessary to standardize the shaders and the graphics pipeline in general. For this, I started the GLSH project, which is a mellinoe/ShaderGen recreated from scratch specifically for Vulcan. At the moment, I am working on developing the game and simultaneously developing and improving the code for GLSH.

**Motivation**

In my opinion, a game engine does not have to be stuffed with AAA graphics, a particle system, realistic physics, etc. All I need is a simple engine and a simple editor.

**By a simple engine** I mean the absence of C++ mixed with a scripting language. One graphic backend and 3D and UI renderer. Simple user input system and sound. Serialization, scenes. And a pipeline for executing user code.

**By a simple editor** I mean the ability to import 3D models, images, sounds. Import text files and various data in the form of json. Ability to create JSON based on JSONSchema from user classes. Ability to create and edit prefabs and scenes. A simple cross-platform build without situations where something always crashes, doesn’t work, works but not like that, but not everywhere, etc.

**Afterword**

This will be enough to create games. AAA graphics or cool shaders and particles are something that can always be added and changed, but cutting out a couple of graphic backends or a scripting language from the engine is clearly not easier.
There's also some [notes](https://github.com/Artromskiy/DeltaEngine/blob/main/Notes.md) about possible benefits of creating such an engine.

To know which libraries and other third-party solutions used see [**Third-party**](https://github.com/Artromskiy/DeltaEngine/blob/main/Third-party.md).

Here's editor app:

![image](https://github.com/user-attachments/assets/0f48b8f4-78c7-4ab7-914f-5a7e9ff61160)

Editor wip (old one using MAUI)
![image](https://github.com/Artromskiy/DeltaEngine/assets/47901401/c9ef1b42-5504-4191-b614-07a620dd166a)

Here's fourth letter :) (it was at the very beginning)
![image](https://github.com/Artromskiy/DeltaEngine/assets/47901401/442aabe0-061f-4497-aec7-f45e5c2b7bb1)
