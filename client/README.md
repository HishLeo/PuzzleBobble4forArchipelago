# PuzzleBobble4ArchipelagoClient

C# / .NET 8 external client for Puzzle Bobble 4 (PS1 NTSC-U). This experimental branch accesses DuckStation's emulated RAM directly on Windows x64 and does not use the GDB server.

## Startup and console behavior
- Waits for DuckStation RAM access. If DuckStation cannot be found, prints `Could not find DuckStation.` and retries every five seconds with `Retrying in 5 seconds...`.
- Once memory is accessible, prints `Connected to DuckStation.` and asks for `Enter AP domain:`, `Enter port:`, `Slot name:`, and `Room password:`. Leave the password blank for password-less rooms.
- Attempts a real Archipelago login using the game name `Puzzle Bobble 4` and the `Archipelago.MultiClient.Net` library. If login fails, it shows a concise error and asks for the connection details again.
- After connecting, the console accepts Archipelago server commands. Type `!help` and press Enter to list server commands; `/help` lists the client’s local command help. Console input runs on a separate thread and is not consumed by the game polling loop. Menu controls remain mapped from the game's controller-input memory byte.
- Internal memory diagnostics and local check spam are suppressed from the player's console.

## Current integration status
This build connects to Archipelago, sends a location check when the existing local detector sees a level transition, and queues received items for safe processing on the game-polling thread. The paired prototype APWorld assigns `Level Skip` at every normal level check. The client applies received `Level Skip` items to its counter/HUD. Other item effects and complete multiworld item-sending messages are not implemented yet.

## Run
1. Open DuckStation and load Puzzle Bobble 4. Leave its GDB server disabled for this experiment.
2. On the developer machine, install the .NET 8 SDK.
3. Run `build-windows-x64.ps1` to publish a self-contained Windows x64 executable, or run from this folder with `dotnet run --project .\src\PuzzleBobble4ArchipelagoClient.csproj`.
4. If direct memory access is denied, make sure the client and DuckStation run at the same privilege level; if needed, test running the client as administrator.

No menu coordinates are written at startup. Memory reads begin after finding the DuckStation process and its exported `RAM` pointer. This method is experimental and may fail on DuckStation builds that do not expose that export. A check transition that happens while DuckStation is closed or memory access is unavailable may be missed; test this behavior before relying on it for a full playthrough.

## Level letter ID mapping used by check detection

The check logger interprets the game's raw `0x00167A8F` value as:
- `00`-`13`: A-T
- `14`: U
- `15`: V
- `16`: W
- `17`: ?
- `18`: X
- `19`: Y
- `1A`: Z

Unknown values are logged as `ID-XX`. This mapping affects check names only; it does not change the selected-menu value written by keyboard navigation.


### Restablecimiento automático a Menu0

Al detectar una entrada a la pantalla de selección de letras (`0x00167A0C = 01`), el cliente restablece la posición lógica y las coordenadas de cámara a `Menu0`. También lo hace si se inicia el cliente estando ya en esa pantalla. No reescribe Menu0 continuamente: espera a detectar la entrada al estado.


### Corrección automática a Menu0 y navegación manual

Al entrar en la selección de letras, el cliente vigila cambios en `0x00167A8F` y puede corregir el primer cambio no nulo a `00`. Si elegís manualmente una letra con las flechas del programa, esa corrección automática se desactiva para no sobrescribir tu selección.


### Reset completo automático a Menu0

Al entrar en la pantalla de selección de letras, el cliente vigila `0x00167A8F`. Si ese byte cambia a `00`, aplica `Menu0` mediante la misma rutina `ApplyMenu` que usa Backspace, incluyendo el byte de letra y las coordenadas de cámara. Si se elige manualmente otra letra con las flechas del programa, la vigilancia automática se desactiva para no sobrescribir esa elección.


### Bloqueo de entradas y desbloqueos locales

- Para pruebas, todas las letras empiezan desbloqueadas (el conjunto `LockedLetters` está vacío).
- La mecánica de bloqueo sigue implementada: las salidas se consultan en el grafo, la navegación omite menús cuyas dos salidas estén bloqueadas y el código conserva los desbloqueos locales de A1-A5 para C, D, E, Y y Z. Esto permite volver a activar bloqueos para la lógica futura de Archipelago sin reimplementar el sistema.
- La tabla de vecinos conserva las salidas del grafo: S tiene X a la izquierda e Y a la derecha; T tiene Y a la izquierda y Z a la derecha. U-Z no tienen coordenadas de menú en esta versión, pero sus salidas sí se evalúan para bloquear o permitir la dirección de entrada.
- Los checks se detectan y se procesan localmente en consola; todavía no se envían ni reciben objetos de un servidor Archipelago.


### Navegación lineal filtrada por salidas disponibles

Las flechas recorren los menús en orden lineal, pero no se detienen en una letra solo porque su nombre no esté bloqueado. Para cada menú candidato, el cliente consulta sus dos salidas en el grafo definido y comprueba si alguna conduce a una letra cuya entrada esté desbloqueada. Si ninguna salida está disponible, omite ese menú y sigue en la dirección pulsada. Cuando no encuentra otra letra válida, vuelve o permanece en Menu0. Al llegar al menú seleccionado, corrige `0x00167D3A` para mirar hacia una salida desbloqueada si la dirección actual está bloqueada.

Para pruebas, todas las letras están desbloqueadas al iniciar. La lógica que permite bloquear destinos y desbloquearlos mediante A1-A5 se conserva para la futura integración con Archipelago.


### Bloqueo de la letra especial ?

La lógica de bloqueo también cubre la entrada a `?`, a través de Q hacia la derecha y R hacia la izquierda. Actualmente `?` comienza desbloqueada para pruebas; el mecanismo se conserva para su configuración futura desde Archipelago.


## Plan de modos generado por Archipelago

Los modos ya no se sortean ni se leen desde archivos locales del cliente. El YAML del mundo configura `level_mode_weights`; durante la generación, el APWorld usa el RNG determinista de la seed para asignar un modo a cada uno de los 135 niveles y envía el mapa completo en `slot_data` como `level_mode_plan`.

Ejemplo de YAML para el test inicial (todos Normal Original):

```yaml
level_mode_weights:
  normal_o: 100
  expert_o: 0
  normal_aw: 0
  expert_aw: 0
```

El cliente lee `level_mode_plan` al iniciar sesión en Archipelago, valida que contenga los 135 niveles y que cada modo sea reconocido, y usa exclusivamente ese plan al configurar DuckStation. Si el plan no llega o está incompleto, muestra una advertencia y no cambia los modos; no genera un plan local alternativo. Para cambiar la distribución, modificá los pesos en el YAML y generá una nueva seed/multidata.

### Aplicación de modos en DuckStation

El cliente aplica el modo del plan a través de las direcciones de memoria confirmadas: `0x00167A96` (00=Original, 01=Another World) y `0x00167A91` (01=Normal, 02=Expert). Durante la selección calcula la letra destino a partir del menú base y la dirección de mirada, y prepara el modo de su nivel 1. Conserva ese destino para aplicarlo al entrar al nivel, en vez de inferirlo de `0x00167A8F`, que identifica el menú base y usa `00` para Menu0. Al detectar un check de los niveles 1-4, prepara inmediatamente el modo del siguiente nivel de esa misma letra. La consola muestra los valores escritos y leídos de vuelta.


### Detección de niveles Letra1

El cliente detecta el flanco de entrada a `0x00167A10 = 05` mientras `0x00167A0C = 03`. Usa el destino capturado mientras el jugador estaba en el menú (la letra que miraba) para aplicar la asignación de `Letra1`; esto evita confundir Menu0 (`00`) con A y corrige A1/B1 y los demás destinos. La decodificación del ID de letra también respeta el mapeo Menu0=`00`, A=`01`, B=`02`, …, T=`14`, U=`15`, V=`16`, W=`17`, ?=`18`, X=`19`, Y=`1A`, Z=`1B`. Los niveles 2-5 continúan usando la detección del check de finalización.


### Mostrar la letra enfocada en consola

Mientras `0x00167A0C=01` (selección de letras), el cliente imprime `LETRA QUE ESTÁS MIRANDO` al entrar al menú y cada vez que cambia `0x00167A8F`. También muestra la letra leída después de aplicar una navegación desde el cliente.


### Diagnóstico de letra mirada y entrada a Letra1

La consola informa la letra objetivo según el menú base y la dirección de mirada (`0x00167D3A`: `00` izquierda, `01` derecha). También informa cada transición de `0x00167A10` a `05` y si el modo del nivel 1 de una letra se escribió y verificó en esta sesión. “Modificado y verificado” indica que ambas direcciones de modo devolvieron los valores esperados tras la escritura; no garantiza que el juego no los cambie posteriormente al cargar el nivel.


### Pesos de ítems desde el YAML de Archipelago (v12)

El cliente no lee ni genera asignaciones desde `item-weights.json`. El APWorld usa las opciones del YAML para crear el pool de la seed:

- `filler_weight`: pesos relativos de `Pointer-Guide`, `Star-Bubble`, `Metal-Bubble` y `Level-skip`.
- `trap_weight`: porcentaje del pool de relleno que se reserva para trampas.
- `trap_types`: pesos relativos de `Rainbow-Bubble`, `Obstacle-Block`, `Random-Color-Bubble` y `Falling-Ceiling`.

El RNG de Archipelago aplica esos pesos durante la generación. En el inicio de sesión, el cliente recibe `item_weight_config` con los valores efectivos y `level_item_plan` con la asignación final de ítems a las 135 ubicaciones. Guarda una copia informativa en `level-item-plan-received.json`; no vuelve a sortear ítems ni sustituye la entrega real de Archipelago.

## Level Skip mechanic (local prototype)

- The counter is stored internally as an integer from `0` to `99` and displayed as ASCII digits in `Skips 00`.
- `FILL_Level Skip` is handled by `Program.HandleReceivedItem(string itemName)`: each received item increments the counter by one, up to `99`, and updates the primary and mirrored HUD digits. This handler is prepared for the future Archipelago item-received callback; live item reception is not connected yet.
- To use a skip, the player must be in a level with `0x00167A0C = 03` and `0x00167A10 = 02`, then press R2 (`0x0016BCEB = FD`). The input is edge-triggered, so holding R2 does not consume multiple skips.
- If the counter is `00`, R2 does nothing. If a skip is available, the client writes `03` to `0x00167A10`, decrements the counter, updates `0x0011A007`–`0x0011A008`, and mirrors the digits to `0x0011A017`–`0x0011A018`.
- A skip-triggered `02 -> 03` transition is excluded from the local completed-level check detector, so using a skip does not incorrectly log a normal completion check.


## Initial Archipelago checks + Level Skip test
- Each existing local level-completion detection (`A1`, `A2`, etc.) is sent using the location ID resolved from the connected Archipelago session.
- The client prints `YOU sent A1 to Archipelago.` when it submits a check.
- The paired `puzzle_bobble_4_level_skip.apworld` uses `Level Skip` as the item in the item pool for every normal level location.
- Received `Level Skip` items are queued from the Archipelago callback and applied on the main polling loop, increasing the local skip counter/HUD.
- This is an initial test integration, not the complete item system. Confirm the check name mapping in-game and verify that each completion is marked in the server before relying on it.


## Goal-critical remote Unlock prerequisites (v7 source)

The matching Puzzle Bobble 4 world can provide `unlock_prerequisites` in slot data. When present, the client stores a received goal-world `Unlock` item as pending until all listed prerequisite Unlocks have themselves unlocked. This allows a multiplayer seed to require remote Unlock items before the goal world can be entered. Solo seeds send an empty prerequisite list and keep the usual behavior.

This source archive has not been compiled in this environment; build it on Windows with the included `build-windows-x64.ps1` and the .NET 8 SDK before replacing your existing client.



## Console commands

After a successful Archipelago login, type a server command such as `!help` and press Enter. The client forwards the line to Archipelago. `/help` is handled locally and explains how to access server commands. Console input is read on a dedicated background task; the game polling loop does not consume keyboard input.

## DeathLink

The client supports Archipelago DeathLink when the generated Puzzle Bobble 4 slot has `death_link: true` in its YAML. On a local Game Over, the client watches RAM address `0x00167A10` and sends one DeathLink when the state enters `04` while the game is in level mode (`0x00167A0C = 03`). When a DeathLink is received from another slot, the client waits until gameplay is active (`state 02`) and writes `00` to `0x00167A10` to use the game's tested reset transition. It deliberately does not write `04` for received deaths.

DeathLink is opt-in and disabled by default unless enabled in the generated YAML. Both the APWorld and this client must be updated, and a new seed must be generated so the slot data contains the option.
