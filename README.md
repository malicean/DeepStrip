# DeepStrip
DeepStrip is an advanced reference assembly creator. It deletes all members inaccessible to other assemblies, and deletes the remaining method bodies (including
those of properties and events).

## Usage
DeepStrip uses pipes. Simply pipe in the original assembly, and pipe out the reference assembly.

For example, to read `Assembly-CSharp.dll` and output the stripped result to `Assembly-CSharp.stubbed.dll`:
```bash
$ deepstrip < Assembly-CSharp.dll > Assembly-CSharp.stubbed.dll 
```

By default, DeepStrip will resolve any dependencies in the current directory or the `bin` directory within the current directory. If those
directories do not have all of the dependencies, the `--include` or `-i` option can be used:
```bash
$ deepstrip --include h3vr_Data/Managed < Assembly-CSharp.dll > Assembly-CSharp.stubbed.dll
```

Use `deepstrip --help` to view all options in a concise manner.

## Why use DeepStrip?
DeepStrip's intended purpose is to make reference assemblies for use in Git repositories. Reference assemblies contain the minimum metadata
and no executable code (CIL), meaning they are easier to redistribute. Storing reference assemblies in a Git repository makes it easier for
contributors to get started on your project, as it avoids the hassle of finding all the references needed. It also allows for automated
builds, as all the information needed to compile is already in the repository.

### Why not use `mono-cil-strip`?
`mono-cil-strip` does delete the method bodies (hence `cil-strip`), but it leaves heaps of metadata that is unusable to other assemblies.
DeepStrip deletes this unnecessary metadata.

### Why not use [Reinms/Stubber-Publicizer](https://github.com/Reinms/Stubber-Publicizer)?
For one, Stubber-Publicizer *stubs* rather than *strips*. This means the files produced by it are slightly larger than `mono-cil-strip`.
Additionally, it's not the same use case. If you need a publicized reference assembly, use Stubber-Publicizer. If you want a reference
assembly that contains members with identical visibility, use DeepStrip.
