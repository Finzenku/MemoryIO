# MemoryIO

A C# library designed around conveniently reading and writing data in memory. 
Provides interfaces and implementations to interact with process memory and perform data manipulation.

### MemoryIO

The core interface `IMemoryIO` defines methods for reading and writing data of generic unmanaged objects, strings, and byte arrays. 
`WindowsMemoryIO` and `LinuxMemoryIO` provide platform-specific access to a `Process` memory using `kernel32` and `libc` respectively.
The `ProcessMemoryIO` implementation is a wrapper class to allow for platform-independant `Process` memory manipulation based on `Environment.OSVersion.Platform`.  

`BaseMemoryIO` provides a simple way to implement an `IMemoryIO` object by only needing to provide implemention for `ReadData` and `WriteData`.

### MemoryMonitors

`MemoryMonitor`s are designed to allow for monitoring memory regions and raises a `MemoryChanged` event when the data in those regions changes. 
Each `MemoryMonitor` is designed to monitor different structures in memory. Such as an unmanaged generic, a region of bytes, or an array of unmanaged generics.

The `MemoryChangedEventArgs`, `MemoryRegionChangedEventArgs`, and `MemoryArrayChangedEventArgs` provide information about the address and value of the memory that changed. 
`MemoryArrayChangedEventArgs` also provides which index in the monitored array had changed.

### Examples

#### Reading

```csharp
ProcessMemoryIO MemoryIO = new(Process.GetCurrentProcess());

// ProcessMemoryIO exposes the underlying Process to easily access its Modules/BaseAddress
IntPtr address = MemoryIO.Process.MainModule.BaseAddress + 0x12345;

// Read an unmanaged struct from address
SomeStruct value = MemoryIO.Read<SomeStruct>(address);

// Read an array of five integers from address
int[] values = MemoryIO.ReadArray<int>(address, 5);

// Read a null-terminated string using Encoding.Default from address (reads up to 512 bytes by default)
string valueAsString = MemoryIO.ReadString(address, Encoding.Default);

// Read a null-terminated string up to 16 characters long using Encoding.Default from address
string valueAs16CharacterString = MemoryIO.ReadString(address, Encoding.Default, 16);
```

#### Writing

```csharp
ProcessMemoryIO MemoryIO = new(Process.GetCurrentProcess());
IntPtr address = MemoryIO.Process.MainModule.BaseAddress + 0x54321;

// Write a null-terminated string to the memory at address using Encoding.UTF8
MemoryIO.WriteString(address, "All your memory are belong to us", Encoding.UTF8);

// Write raw byte data to the memory at address
byte[] myData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
MemoryIO.WriteData(address, myData);

// Write an array of unmanaged generics to the memory at address
SomeUnmanagedStruct[] myStructs = new SomeUnmanagedStruct[3];
MemoryIO.WriteArray(address, myStructs);
```

#### Monitoring

```csharp
ProcessMemoryIO MemoryIO = new(Process.GetCurrentProcess());
IntPtr address = MemoryIO.Process.MainModule.BaseAddress + 0x54321;
CancellationTokenSource cts = new();

// MemoryMonitorFactory provides a convenient way to create multiple MemoryMonitors using a single IMemoryIO
MemoryMonitorFactory monitorFactory = new(MemoryIO);

SomeUnmanagedStruct[] myStructs = new SomeUnmanagedStruct[3];
var myStructArrayMonitor = monitorFactory.GetArrayMonitor<SomeUnmanagedStruct>(address, 3);

myStructArrayMonitor.MemoryChanged += (object? sender, MemoryArrayChangedEventArgs<SomeUnmanagedStruct> e) =>
{
	// MemoryArrayChangedEventArgs provides 
	//	the Address of the array being monitored, 
	//	the Value of the item changed, and
	//	the Index of the item in the array
	myStructs[e.Index] = e.Value;
};

var monitorTask = myStructArrayMonitor.StartMonitoringAsync(cts.Token);

```

### Credit

Inspired by https://github.com/JamesMenetrey/MemorySharp.
