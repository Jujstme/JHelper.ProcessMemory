## JHelper.ProcessMemory

ProcessMemory is a high performance, small footprinted library aimed to provide `low-cost abstractions` for memory manipulation in C#.  

It is designed to be fast, with little overhead, and is mainly aimed to provide easy access to the memory of an external process.

## Sample usage

Gain access to an external process:

```cs
ProcessMemory process = ProcessMemory.HookProcess("explorer.exe");
```


Read Memory from the external process' address space:
```cs
int value = process.Read<int>((IntPtr)0xDEADBEEF);

bool success = process.Read<int>((IntPtr)0x5ADFACE, out int value2);
```


Other functions are provided to enumerate modules, exported functions and to perform signature scanning.


## Feedback

If you have questions/bug reports/etc. feel free to [Open an Issue](https://github.com/Jujstme/JHelper.ProcessMemory/issues/new/).

Contributions are welcome and encouraged.