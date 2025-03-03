# Machine
This contains all the standard naming for MachineInfo

the tuple goes from most important to least.

An example would be to target compiling for an x86_64 windows machine: 

x86_64-windows

If you want to get more in detail then you can just keep going.



## Arch
| Arch | 32bit | 64bit  |
|------|-------|--------|
| X86  | x86   | x86_64 |
| ARM  | arm32 | arm64  |
| WASM | wa32  | wa64   |
| PPC  | ppc32 | ppc64  |

#### NOTES:
- Wasm might have a 64bit version with Memory64 so thats why it exists.

## OS

The standard is just the name but lowercase, if theres some exception
then it will be listed here.

- Linux: linux
- MacOS: macos
- Windows: windows
- Android: android
