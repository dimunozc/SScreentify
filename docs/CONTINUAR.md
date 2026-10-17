# Cómo continuar con sScreentify

Notas para retomar el proyecto otro día, con Claude o sin él. El README explica qué hace la app; este documento explica **dónde está cada cosa, qué falta y qué trampas hay**.

## Estado a 17-10-2026

- Las 14 tareas del plan y los arreglos de la revisión final están en `main`, con 129 pruebas en verde (62 de Core y 67 de la App).
- El repositorio es solo local: **no tiene remoto**.
- Documentos de referencia:
  - Diseño aprobado: `docs/superpowers/specs/2026-10-17-sscreentify-design.md` (lo que la app *debe* hacer; manda sobre todo lo demás).
  - Plan de implementación: `docs/superpowers/plans/2026-10-17-sscreentify.md`.

## Primeros pasos al retomar

```powershell
cd C:\Users\Diego\Documents\Repositorios\sScreentify
dotnet test                                   # tienen que pasar las 129
dotnet run --project src\SScreentify.App      # la app queda en la bandeja; Impr Pant para capturar
.\scripts\publish.ps1                         # genera publish\sScreentify.exe
```

Si `dotnet` no se encuentra, el SDK está en `C:\Program Files\dotnet\dotnet.exe`. En la terminal de Claude a veces no está en el PATH: `export PATH="/c/Program Files/dotnet:$PATH"`.

Antes de probar, cierra cualquier sScreentify que tengas abierto (icono de la bandeja → Salir). Solo puede haber una instancia, y una segunda se cierra sin avisar.

## Qué falta comprobar a mano

Nadie lo ha probado todavía:

1. Dibujar con el ratón con cada herramienta, incluida Selección (V) para mover, redimensionar y borrar con Supr.
2. Pegar con Ctrl+C en un chat (PNG) y en Paint (mapa de bits).
3. Los diálogos de Guardar e Imprimir, incluido cancelar la impresión.
4. El aspecto del icono de la bandeja y su menú.
5. Opciones: que cada cambio se aplique al momento, grabar un atajo nuevo e iniciar con Windows.
6. Varios monitores, escalas distintas del 100 % y monitores 4K. Lo más delicado es la fluidez al arrastrar la selección con renderizado por software en 4K.

## Pendientes conocidos (menores)

Salieron en la revisión final y se dejaron para más adelante:

- Si falla un guardado, puede quedar un archivo `*.tmp` junto al destino (`Export/SaveService.cs`, `Write`).
- Dos guardados instantáneos en el mismo segundo sobrescriben el primero (`Export/ExportActions.cs`, `SaveFullscreenInstant`).
- Si la última carpeta existe pero no tiene permiso de escritura, el diálogo empieza igualmente en ella. El error al escribir sí se avisa.
- El desenfoque de Censurar se recalcula en cada movimiento al arrastrar y puede ir lento en zonas grandes (sin medir). Arreglo propuesto: previsualizar pixelado mientras se arrastra y calcular el desenfoque al soltar.
- La etiqueta de tamaño puede salir dos veces si la selección empieza justo debajo del borde entre dos monitores (`Overlay/SelectionAdorner.cs`).
- Durante una captura, la imagen se guarda dos veces en memoria: el `PixelBuffer` y el `BitmapSource`.

## Mapa del código

**`src/SScreentify.Core`**: lógica pura, sin WPF y con pruebas en `tests/SScreentify.Core.Tests`.

| Carpeta | Qué hay |
|---|---|
| `Geometry/` | `PixelRect` y `PixelPoint` (píxeles físicos), `SelectionModel` (crear, mover, tiradores, flechas), `Constrain` (Mayús), `ToolbarPlacement`, `MonitorInfo` |
| `Annotations/` | Tipos de anotación (lápiz, segmento, forma, texto, censura), `AnnotationDocument` (orden z, deshacer y rehacer), `Rgba` |
| `Imaging/` | `PixelBuffer` (BGRA) y `Censor` (pixelado, desenfoque, sólido) |
| `Settings/` | `AppSettings` (valores por defecto y rangos) y `SettingsStore` (JSON atómico; si está corrupto, crea `.bak`) |
| `Hotkeys/`, `Files/`, `Platform/` | Validación y nombres de atajos, nombre de archivo por defecto, conflicto con Recortes |

**`src/SScreentify.App`**: Windows y WPF, con pruebas en `tests/SScreentify.App.Tests`.

| Carpeta | Qué hay |
|---|---|
| `App.xaml.cs` | Arranque: instancia única, ajustes, atajos, bandeja, avisos de inicio, Opciones y manejador global de errores |
| `Capture/` | Captura GDI del escritorio virtual en píxeles físicos y lista de monitores |
| `Overlay/` | `OverlayController` (ciclo de vida de cada captura y toda la entrada de ratón y teclado), `OverlayWindow` (una por monitor, creada de antemano y oculta), `SelectionAdorner` (velo, borde, tiradores y tamaño), `AnnotationLayer`, `ToolController` (herramientas de dibujo), `ChromeController` (coloca las barras), `TextEditorHost` |
| `Rendering/` | `AnnotationRenderer`, compartido por el selector y la exportación, y `CensorPatchCache` |
| `Export/` | `Flattener` (imagen final), `ImageEncoder`, `ClipboardService`, `SaveService`, `PrintService` y `ExportActions`, que los orquesta y muestra los avisos |
| `Ui/` | Tema Cristal oscuro (`Theme.xaml` y `Palette.cs`), barras, paleta, iconos, Opciones, avisos, Acerca de y textos (`Strings.resx` y `Strings.cs`) |
| `Services/` | Atajos globales, bandeja, avisos, inicio con Windows y rutas |
| `Diagnostics/` | `--perf` (tiempos y memoria) y `--export-icon` (generó `Assets/app.ico`) |

**Regla clave de diseño:** toda la geometría va en **píxeles físicos del escritorio virtual**. Cada ventana convierte a DIP con su propio DPI solo al dibujar (`OverlayWindow.PhysicalToDip` y `ToPhysical`). Las anotaciones se guardan en coordenadas de la captura, no de la selección.

## Trampas que conviene saber

- **Textos de la interfaz:** WPF no genera la clase de `Strings.resx`. Para añadir textos usa `node scripts/sync-strings.js "Clave=Texto"`, que regenera `Ui/Strings.cs`. Después actualiza el número esperado en `ShellTests.Strings_EveryPropertyResolves`.
- **Renderizado por software por defecto** (`App.xaml.cs`): con la GPU, el driver retenía unos 70 MB y no se cumplía el objetivo de 60 MB en reposo. `--gpu-render` vuelve a la GPU. Si cambias esto, vuelve a medir.
- **Impr Pant en Windows 11:** si Recortes la tiene reservada, la app avisa. Para probar con otra tecla, cambia el atajo en Opciones.
- **Recursos de WPF compartidos entre hilos** (pinceles y geometrías `static`): tienen que estar congelados (`Freeze()`). Si no, las pruebas fallan con «el propietario es otro subproceso».
- **Pruebas con WPF:** usa `StaRunner.Run(...)`. `OverlayController` y `ExportActions` aceptan proveedores falsos de captura y monitores, e `IToastSink`, para probar sin abrir ventanas (ver `FinalReviewTests`).
- **Revisión visual sin abrir la app:** `VisualSnapshotTests` renderiza el selector, Opciones y los avisos sobre un escritorio sintético. Con `SSCREENTIFY_SNAPSHOT_DIR=<carpeta>` guarda los PNG.
- **Instalar software con Claude:** el modo automático bloquea `winget`, así que hay que ejecutarlo con `!` en el prompt, añadiendo `--source winget --accept-source-agreements --accept-package-agreements`.

## Medir el rendimiento

Arranca `publish\sScreentify.exe --perf`, haz unas capturas y cierra. El registro queda en `%APPDATA%\sScreentify\perf.log`:

- `hotkey` → `rendered`: tiempo hasta ver el selector.
- `captured`: tiempo de captura.
- `tray-ready`: arranque en frío.
- `idle-memory`: memoria 10 s después de cerrar una captura.

La tabla de referencia está en el README (mediana 30,6 ms, 54,7 MB en reposo).

## Ideas para siguientes versiones

Ninguna está aprobada; son puntos de partida para una conversación:

- Probar y ajustar en varios monitores, DPI mixtos y 4K.
- Resolver los pendientes menores de arriba.
- Reducir el tamaño del `.exe` (178 MB), por ejemplo con compresión del ejecutable o un instalador, midiendo el arranque.
- Más idiomas: los textos ya están en `Strings.resx`.
- Funciones que el diseño dejó fuera: captura con desplazamiento, OCR o grabación (ver «Lo que no es» en el diseño).
