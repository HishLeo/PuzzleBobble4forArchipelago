using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PuzzleBobble4ArchipelagoClient;

/// <summary>
/// Experimental direct access to DuckStation's emulated PS1 RAM.
/// Locates the exported RAM pointer in DuckStation's PE export table and then
/// reads/writes offsets from that RAM block. This deliberately does not use GDB.
/// </summary>
internal sealed class DuckstationMemory : IDisposable
{
    private const uint ProcessVmOperation = 0x0008;
    private const uint ProcessVmRead = 0x0010;
    private const uint ProcessVmWrite = 0x0020;
    private const uint ProcessQueryInformation = 0x0400;
    private const uint RequiredAccess = ProcessVmOperation | ProcessVmRead | ProcessVmWrite | ProcessQueryInformation;

    private readonly Process _process;
    private readonly IntPtr _processHandle;
    private readonly IntPtr _ramBase;
    private bool _disposed;

    public DuckstationMemory()
    {
        if (!OperatingSystem.IsWindows())
            throw new IOException("El acceso directo a la memoria solo está implementado para Windows.");
        if (IntPtr.Size != 8)
            throw new IOException("Esta versión experimental requiere ejecutarse como proceso de 64 bits.");

        var failures = new List<string>();
        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                string processName;
                try { processName = process.ProcessName; }
                catch { process.Dispose(); continue; }

                if (!processName.Contains("duckstation", StringComparison.OrdinalIgnoreCase))
                {
                    process.Dispose();
                    continue;
                }

                IntPtr handle = OpenProcess(RequiredAccess, false, process.Id);
                if (handle == IntPtr.Zero)
                {
                    failures.Add($"{processName}: OpenProcess falló (Win32 {Marshal.GetLastWin32Error()}).");
                    process.Dispose();
                    continue;
                }

                try
                {
                    ProcessModule? module = null;
                    try
                    {
                        module = process.Modules.Cast<ProcessModule>()
                            .FirstOrDefault(m => m.ModuleName.Contains("duckstation", StringComparison.OrdinalIgnoreCase));
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{processName}: no se pudieron enumerar los módulos ({ex.Message}).");
                    }

                    module ??= process.MainModule;
                    if (module is null)
                        throw new IOException("No se pudo identificar el módulo principal de DuckStation.");

                    IntPtr moduleBase = module.BaseAddress;
                    uint ramExportRva = FindExportRva(handle, moduleBase, "RAM");
                    byte[] pointerBytes = ReadRemote(handle, Add(moduleBase, ramExportRva), 8);
                    long ramAddress = BitConverter.ToInt64(pointerBytes, 0);
                    if (ramAddress <= 0x10000)
                        throw new IOException($"El símbolo RAM devolvió un puntero no válido: 0x{ramAddress:X}.");

                    _process = process;
                    _processHandle = handle;
                    _ramBase = new IntPtr(ramAddress);
                    ClientLog.Debug($"DuckStation detectado: {processName} (PID {process.Id}).");
                    ClientLog.Debug($"RAM de DuckStation localizada en 0x{ramAddress:X} mediante el export 'RAM'.");
                    return;
                }
                catch (Exception ex)
                {
                    failures.Add($"{processName}: {ex.Message}");
                    CloseHandle(handle);
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
                failures.Add(ex.Message);
                try { process.Dispose(); } catch { }
            }
        }

        string detail = failures.Count == 0 ? "No se encontró ningún proceso cuyo nombre contenga 'duckstation'." : string.Join(" | ", failures.Take(4));
        throw new IOException("No se pudo localizar la RAM exportada por DuckStation. " + detail);
    }

    public byte ReadByte(uint address)
    {
        ThrowIfDisposed();
        return ReadRemote(_processHandle, Add(_ramBase, address), 1)[0];
    }

    public void WriteByte(uint address, byte value)
    {
        ThrowIfDisposed();
        byte[] bytes = [value];
        if (!WriteProcessMemory(_processHandle, Add(_ramBase, address), bytes, new IntPtr(bytes.Length), out IntPtr written) || written.ToInt64() != 1)
            throw new IOException($"No se pudo escribir en RAM+0x{address:X8} (Win32 {Marshal.GetLastWin32Error()}).");
    }

    // Kept so the existing game-monitor code can share the same interface.
    // Direct process-memory access does not pause DuckStation.
    public void ContinueExecution() { }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DuckstationMemory));
        if (_process.HasExited) throw new IOException("DuckStation se cerró.");
    }

    private static IntPtr Add(IntPtr address, uint offset) => new(address.ToInt64() + offset);

    private static byte[] ReadRemote(IntPtr processHandle, IntPtr address, int count)
    {
        byte[] buffer = new byte[count];
        if (!ReadProcessMemory(processHandle, address, buffer, new IntPtr(count), out IntPtr read) || read.ToInt64() != count)
            throw new IOException($"No se pudo leer la memoria remota en 0x{address.ToInt64():X} (Win32 {Marshal.GetLastWin32Error()}).");
        return buffer;
    }

    private static uint FindExportRva(IntPtr handle, IntPtr moduleBase, string exportName)
    {
        byte[] dos = ReadRemote(handle, moduleBase, 0x40);
        if (dos[0] != (byte)'M' || dos[1] != (byte)'Z') throw new IOException("El módulo no tiene una cabecera PE válida (MZ).");
        int peOffset = BitConverter.ToInt32(dos, 0x3C);
        if (peOffset <= 0 || peOffset > 0x100000) throw new IOException("Offset PE inválido.");

        byte[] nt = ReadRemote(handle, Add(moduleBase, (uint)peOffset), 0x100);
        if (nt[0] != (byte)'P' || nt[1] != (byte)'E' || nt[2] != 0 || nt[3] != 0)
            throw new IOException("La firma PE del módulo no es válida.");

        int optionalHeaderSize = BitConverter.ToUInt16(nt, 20);
        int optionalStart = 24;
        ushort magic = BitConverter.ToUInt16(nt, optionalStart);
        int dataDirectoryOffset = magic switch
        {
            0x20B => optionalStart + 112, // PE32+
            0x10B => optionalStart + 96,  // PE32
            _ => throw new IOException($"Formato PE no reconocido: 0x{magic:X}.")
        };
        if (optionalHeaderSize < dataDirectoryOffset - optionalStart + 8)
            throw new IOException("La cabecera opcional PE no contiene la tabla de exports.");

        uint exportRva = BitConverter.ToUInt32(nt, dataDirectoryOffset);
        uint exportSize = BitConverter.ToUInt32(nt, dataDirectoryOffset + 4);
        if (exportRva == 0 || exportSize == 0) throw new IOException("El módulo DuckStation no tiene tabla de exports.");

        byte[] exportDirectory = ReadRemote(handle, Add(moduleBase, exportRva), 40);
        uint numberOfNames = BitConverter.ToUInt32(exportDirectory, 24);
        uint functionsRva = BitConverter.ToUInt32(exportDirectory, 28);
        uint namesRva = BitConverter.ToUInt32(exportDirectory, 32);
        uint ordinalsRva = BitConverter.ToUInt32(exportDirectory, 36);
        if (numberOfNames > 200000) throw new IOException("La tabla de exports parece inválida.");

        for (uint i = 0; i < numberOfNames; i++)
        {
            uint nameRva = BitConverter.ToUInt32(ReadRemote(handle, Add(moduleBase, namesRva + i * 4), 4), 0);
            string name = ReadRemoteCString(handle, Add(moduleBase, nameRva), 256);
            if (!string.Equals(name, exportName, StringComparison.Ordinal)) continue;

            ushort ordinal = BitConverter.ToUInt16(ReadRemote(handle, Add(moduleBase, ordinalsRva + i * 2), 2), 0);
            uint functionRva = BitConverter.ToUInt32(ReadRemote(handle, Add(moduleBase, functionsRva + (uint)ordinal * 4), 4), 0);
            if (functionRva >= exportRva && functionRva < exportRva + exportSize)
                throw new IOException($"El export '{exportName}' es un forwarder y no un dato de RAM.");
            return functionRva;
        }

        throw new IOException($"No se encontró el export '{exportName}' en el módulo de DuckStation.");
    }

    private static string ReadRemoteCString(IntPtr handle, IntPtr address, int maxLength)
    {
        var bytes = new List<byte>();
        for (int i = 0; i < maxLength; i++)
        {
            byte value = ReadRemote(handle, Add(address, (uint)i), 1)[0];
            if (value == 0) return Encoding.ASCII.GetString(bytes.ToArray());
            bytes.Add(value);
        }
        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseHandle(_processHandle);
        _process.Dispose();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(IntPtr process, IntPtr baseAddress, [Out] byte[] buffer, IntPtr size, out IntPtr bytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(IntPtr process, IntPtr baseAddress, byte[] buffer, IntPtr size, out IntPtr bytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
