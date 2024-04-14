Possible benefits:

Because the engine will use a very thin layer of C++, namely libraries for rendering, it is possible to get rid of the overhead of function calls of the engine itself. In game engines, where the scripting language is different from the engine language, all script interaction creates an overhead. Calling native libraries
from C# via PInvoke has a fixed overhead of 10-30 x86 instructions, for example, each change of position would create an overhead and this would be impossible to optimize, not to mention the difficulties with multi-threaded work and eternal caching of results

Third-party technologies:

Silk.NET bindings library  (https://github.com/dotnet/Silk.NET)
Arch ECS library (https://github.com/genaray/Arch)
Roslyn compiler (https://github.com/dotnet/roslyn)
MAUI framework (https://github.com/dotnet/maui)
