# Delta Engine
This repository contains the editor and game engine project.
This is the very beginning of development and, like any other similar project, it can cease to exist even without becoming a minimum viable product. At least we tried.


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


