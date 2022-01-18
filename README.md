# Dynamicium
This is a simple proof of concept for dynamically mapping library exports to delegates using the [`dynamic`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#the-dynamic-type) type.

## Examples
### `kernel32.dll`:
```cs
using (dynamic kernel32 = new Kernel32())
{
    kernel32.Beep(2000, 100);

    var currentThreadId = kernel32.GetCurrentThreadId<uint>();
    Console.WriteLine($"Current thread ID is {currentThreadId}.");
}
```

### `user32.dll`:
```cs
using (dynamic user32 = new User32())
{
    var monitorCount = user32.GetSystemMetrics<int>(0x50);
    var (x, y) = (user32.GetSystemMetrics<int>(0), user32.GetSystemMetrics<int>(1));

    var message = $"You have {monitorCount} monitors.\n"
        + $"Your primary monitor's resolution is {x}x{y}.";

    // Null arguments are replaced with null pointers.
    user32.MessageBoxA(null, message, "Important Info";, 0x40);
}
```

### Custom Library (`winmm.dll`):
```cs
using (dynamic winmm = new DynamicLibrary("winmm"))
{
    const string fileName = "bgm001.wav"; // Huh? You don't know, pal?
    winmm.PlaySoundA(fileName, null, 0x1 | 0x8 | 0x20000);
}
```

## Short Explanation
The only important type in this project is the [`DynamicLibrary`](/src/Dynamicium/DynamicLibrary.cs) type - a native library handle wrapper that implements [`DynamicObject`](https://docs.microsoft.com/en-us/dotnet/api/system.dynamic.dynamicobject). It overrides [TryInvokeMember](https://docs.microsoft.com/en-us/dotnet/api/system.dynamic.dynamicobject.tryinvokemember) which creates delegates from the provided arguments as necessary to be used with [Marshal.GetDelegateForFunctionPointer](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.getdelegateforfunctionpointer) and the library export that matches the invoked method's name.

Types such as [`User32`](/src/Dynamicium/Impl/User32.cs) and [`Kernel32`](/src/Dynamicium/Impl/Kernel32.cs) are nothing special, they are implementations of the above explained type with the respective library names passed to its constructor.