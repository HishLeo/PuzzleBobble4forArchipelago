# Experimento: acceso directo a la RAM de DuckStation (sin GDB)

Esta es una versión experimental basada en el cliente actual. En vez de conectar al puerto GDB 2345, busca el proceso de DuckStation, localiza el export PE `RAM` y usa `ReadProcessMemory` / `WriteProcessMemory` para acceder a la RAM emulada.

## Probar en Windows

1. Cerrá DuckStation y cualquier instancia anterior del cliente.
2. Compilá el proyecto con .NET 8 SDK instalado ejecutando `build-windows-x64.ps1`.
3. Abrí DuckStation y cargá Puzzle Bobble 4. Dejá el servidor GDB desactivado.
4. Ejecutá `publish\PuzzleBobble4ArchipelagoClient.exe`.
5. Confirmá que la consola muestre `RAM de DuckStation localizada...` y que lea el estado del juego sin errores.
6. Probá navegación, cambio de modos, HUD y el temporizador. Si aparece un error de `OpenProcess`, probá ejecutar el cliente con el mismo nivel de permisos que DuckStation; si persiste, guardá el mensaje de error.

## Limitaciones importantes

- Esta implementación experimental requiere Windows x64.
- Depende de que el ejecutable de DuckStation exporte un símbolo PE llamado `RAM`, igual que el mecanismo observado en la biblioteca de MediEvil. Si tu build no expone ese símbolo, la detección fallará y habrá que adaptar la localización de RAM.
- No pude compilar ni ejecutar esta versión en el entorno de trabajo actual porque no hay SDK de .NET instalado y no hay acceso a internet para instalarlo. El script de publicación permite compilarla en una máquina Windows con .NET 8 SDK.
- Este proyecto mantiene el alcance funcional de la versión base. La conexión real a un servidor Archipelago y el envío/recepción de ítems no quedan implementados por este cambio de memoria; los mensajes de la aplicación indican qué partes siguen siendo prototipo.
- Conservá el ZIP anterior como respaldo. Esta versión no se ha probado todavía con DuckStation real.


## Ajustes recientes

- Los bytes `0x0011A000` y `0x0011A010` se inicializan en `0x20` (espacio ASCII).
- Al usar un Level Skip, el cliente prepara el modo asignado al siguiente nivel de la misma letra antes de escribir `0x03` en `0x00167A10`. Esto evita depender de que el sondeo alcance a observar la transición `0x02 -> 0x03` mientras DuckStation sigue ejecutándose.
