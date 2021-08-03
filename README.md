# DeepStrip
DeepStrip is an advanced reference assembly creator. It deletes all members inaccessible to other assemblies. From those members, it
deletes all method bodies (including those of properties and events). Member deletion is done intelligently as to be compatible with
syntax-sugar C# features such as nullable reference types, init only setters, etc.

## Installation
Install DeepStrip via .NET CLI:
```bash
dotnet tool install --global DeepStrip
```

Update it similarly:
```bash
dotnet tool update --global DeepStrip
```

## Usage
DeepStrip uses files. Simply provide the path to original assembly (input) and path to the reference assembly (output).

For example, to read `Assembly-CSharp.dll` and output the stripped result to `Assembly-CSharp.stubbed.dll`:
```bash
deepstrip Assembly-CSharp.dll Assembly-CSharp.stubbed.dll 
```

By default, DeepStrip will resolve any dependencies in the current directory or the `bin` directory (also within current directory). If
those directories do not have all of the dependencies, the `--include` or `-i` option can be used:
```bash
deepstrip Assembly-CSharp.dll Assembly-CSharp.stubbed.dll --include "$PATH_TO_MANAGED_DIR"
```
*Note: due to [a bug in CommandLineParser](https://github.com/commandlineparser/commandline/issues/605), the include option must come after the input/output.*

DeepStrip runs quiet to prevent console spam in scripts, but verbose mode can be helpful when running manually. Simply set the `--verbose`
or `-v` flag:
```bash
deepstrip --verbose UnityEngine.dll UnityEngine.stubbed.dll
```
Which produces the following output:
```
Read 'UnityEngine.dll': UnityEngine, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
┌──────────────────────────────────┐
│ ########## Statistics ########## │
├──────────────────────────────────┤
│ Sizes                            │
│ ├── Source ............. 1.3 MiB │
│ ├── Result ............547.0 KiB │
│ └── Truncation Ratio ....... 60% │
│                                  │
│ Types ...................... 182 │
│ ├── Attributes ............. 134 │
│ ├── Fields ................. 859 │
│ │   └── Attributes ........... 0 │
│ ├── Properties .............. 84 │
│ │   ├── Attributes .......... 17 │
│ │   ├── Getters ............. 84 │
│ │   │   └── Attributes .... 1378 │
│ │   └── Setters ............. 70 │
│ │       └── Attributes ..... 939 │
│ ├── Events ................... 3 │
│ │   ├── Attributes ........... 0 │
│ │   ├── Adders ............... 3 │
│ │   │   └── Attributes ....... 0 │
│ │   └── Removers ............. 3 │
│ │       └── Attributes ....... 0 │
│ └── Methods ............... 2843 │
│     └── Attributes ......... 980 │
└──────────────────────────────────┘
```

Use `deepstrip --help` to view all options in a concise manner.

## Why use DeepStrip?
DeepStrip's intended purpose is to make reference assemblies for use in Git repositories. Reference assemblies contain the minimum metadata
and no executable code (CIL), meaning they are easier to redistribute. Storing reference assemblies in a Git repository makes it easier for
contributors to get started on a project, as it avoids the hassle of finding all the references needed. It also allows for automated
builds, as all the information needed to compile is already in the repository.

### Why not use `mono-cil-strip`?
`mono-cil-strip` does delete method bodies, but it leaves heaps of metadata that is unusable to other assemblies.
DeepStrip deletes this unnecessary metadata.

### Why not use [Reinms/Stubber-Publicizer](https://github.com/Reinms/Stubber-Publicizer)?
For one, Stubber-Publicizer *stubs* rather than *strips*. This means the files produced by it are slightly larger than `mono-cil-strip`.
Additionally, it's not the same use case. If a publicized reference assembly is needed, Stubber-Publicizer should be used. If a reference
assembly that contains members with identical visibility is needed, use DeepStrip.
