# FALSE.NET

FALSE Interpreter and Compiler written in `.NET`. Compilation only works for Linux binaries, however, you can run the compiler on Windows (it uses `bash.exe` for assembling and linking).

If you don't have `bash.exe`, you can also just create the assembly file.

Both the interpreter and compiler have configurable type safety. There are three levels of safety: `{NONE, LAMBDA, FULL}`.

Read Wouter van Oortmerssen's (The creator of FALSE) website for more: [strlen.com/false-language](https://strlen.com/false-language/)

Also, the esolang wiki: [esolang.org/wiki/FALSE](https://esolangs.org/wiki/FALSE)

## Quick Start

`cd` into `FalseDotNet.Cli`.

```ps
$ dotnet run help
FalseDotNet.Cli 0.0.6
Copyright (C) 2022 FalseDotNet.Cli

  compile      compile FALSE code.

  interpret    interpret FALSE code.

  help         Display more information on a specific command.

  version      Display version information.
```

### Interpret a program:

`cd` into `FalseDotNet.Cli`.

Example:
```ps
dotnet run interpret ..\samples\simple.f
```

Usage:
```ps
$ dotnet run help interpret
FalseDotNet.Cli 0.0.6
Copyright (C) 2022 FalseDotNet.Cli

  -i, --input               Read from file instead of stdin for program input.

  -p, --print-operations    Print operations before executing them.

  -t, --type-safety         (Default: None) What level of type safety to enforce.
                            LAMBDA only enforces lambda execution, but allows integers
                            to work as references, since they are masked anyway.

  --help                    Display this help screen.

  --version                 Display version information.

  PATH (pos. 0)             Required. File containing FALSE code.
```

### Compile a program:

`cd` into `FalseDotNet.Cli`.

Examples:
```ps
dotnet run -- compile -o ..\out\simple.asm ..\samples\simple.f
dotnet run -- compile -al -o ..\out\simple.asm ..\samples\simple.f
```

On Linux, `nasm` and `ld` are executed as Child processes directly. On Windows, those commands are passed to `bash.exe -c "..."` (Make sure you have WSL installed!)

For now, this compiler can only write assembly for Linux, since it uses syscalls (no glibc at all!). While it may make more sense to use glibc, it is not as cool. Compiling Windows executables is planned for the future; for now, WSL is required. Or use dotnet on Linux directly.

These are the commands used for assembling and linking, respectively:

```sh
nasm -felf64 -o file.o file.asm
ld -o file file.o
```

Usage:
```ps
$ dotnet run help compile
FalseDotNet.Cli 0.0.6
Copyright (C) 2022 FalseDotNet.Cli

  -i, --input               Read from file instead of stdin for program input.

  -o PATH, --output=PATH    File path to write assembly to. Defaults to '<input>.asm'.

  -a, --assemble            Assemble using nasm.

  -l, --link                Link using ld.

  -r, --run                 Run after compilation.

  -O, --optimization        Level of optimization: O0, O1, O2.

  -t, --type-safety         (Default: None) What level of type safety to enforce.
                            LAMBDA only enforces lambda execution, but allows integers
                            to work as references, since they are masked anyway.

  --help                    Display this help screen.

  --version                 Display version information.

  PATH (pos. 0)             Required. File containing FALSE code.
```
