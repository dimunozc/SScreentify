# Cómo continuar con SScreentify

Notas para retomar el proyecto otro día, con Claude o sin él. El README explica qué hace la app; este documento explica **dónde está cada cosa, qué falta y qué trampas hay**.

## Estado a 19-10-2026

- Las 14 tareas del plan y los arreglos de la revisión final están en `main`.
- 19-10-2026: Impr Pant tiene prioridad sobre Recortes de Windows (ver «Trampas») y un clic izquierdo en el icono de la bandeja inicia una captura. También se arreglaron tres pendientes de la revisión final: el `.tmp` tras un guardado fallido, los guardados instantáneos en el mismo segundo (ahora se numeran: «Captura_… (2).png») y la etiqueta de tamaño duplicada entre monitores. Después, el diálogo de guardar ya no empieza en una carpeta sin permiso de escritura y el desenfoque es unas 5 veces más rápido (ver «Trampas»). 148 pruebas en verde (77 de Core y 71 de la App).
- El repositorio es solo local: **no tiene remoto**.
- Documentos de referencia:
  - Diseño aprobado: `docs/superpowers/specs/2026-10-17-sscreentify-design.md` (lo que la app *debe* hacer; manda sobre todo lo demás).
  - Plan de implementación: `docs/superpowers/plans/2026-10-17-sscreentify.md`.

## Primeros pasos al retomar

```powershell
cd C:\Users\Diego\Documents\Repositorios\sScreentify
dotnet test                                   # tienen que pasar las 148
dotnet run --project src\SScreentify.App      # la app queda en la bandeja; Impr Pant para capturar
.\scripts\publish.ps1                         # genera publish\SScreentify.exe
```

Si `dotnet` no se encuentra, el SDK está en `C:\Program Files\dotnet\dotnet.exe`. En la terminal de Claude a veces no está en el PATH: `export PATH="/c/Program Files/dotnet:$PATH"`.

Antes de probar, cierra cualquier SScreentify que tengas abierto (icono de la bandeja → Salir). Solo puede haber una instancia, y una segunda se cierra sin avisar.

## Pruebas a mano

El 19-10-2026 Diego informó de que había probado todo lo de esta lista. No se anotaron el hardware ni los resultados detallados; repite lo que toques:

1. Dibujar con el ratón con cada herramienta, incluida Selección (V) para mover, redimensionar y borrar con Supr.
2. Pegar con Ctrl+C en un chat (PNG) y en Paint (mapa de bits).
3. Los diálogos de Guardar e Imprimir, incluido cancelar la impresión.
4. El aspecto del icono de la bandeja y su menú.
5. Opciones: que cada cambio se aplique al momento, grabar un atajo nuevo e iniciar con Windows.
6. Varios monitores, escalas distintas del 100 % y monitores 4K. Lo más delicado es la fluidez al arrastrar la selección con renderizado por software en 4K.
7. Clic izquierdo en el icono de la bandeja, también desde el menú de iconos ocultos (que ese menú no salga en la captura).

## Pendientes conocidos (menores)

Todos los de la revisión final están resueltos salvo uno, que se dejó así a propósito:

- Durante una captura, la imagen está dos veces en memoria: el `PixelBuffer` (lo usan la censura y la exportación) y el `BitmapSource` (lo que se ve). Son unos 8 MB con 1920×1080 y unos 33 MB por monitor 4K (estimación por el tamaño de la imagen, sin medir), y se liberan al cerrar el selector, así que no afectan a la memoria en reposo. Quitar la copia obligaría a que WPF muestre la imagen desde la memoria del `PixelBuffer` (por ejemplo, con `Imaging.CreateBitmapSourceFromMemorySection`) y a cambiar cómo guarda los píxeles el núcleo de la app. No compensaba. Retómalo solo si la memoria durante la captura llega a ser un problema.

## Mapa del código

**`src/SScreentify.Core`**: lógica pura, sin WPF y con pruebas en `tests/SScreentify.Core.Tests`.

| Carpeta | Qué hay |
|---|---|
| `Geometry/` | `PixelRect` y `PixelPoint` (píxeles físicos), `SelectionModel` (crear, mover, tiradores, flechas), `Constrain` (Mayús), `ToolbarPlacement`, `MonitorInfo` |
| `Annotations/` | Tipos de anotación (lápiz, segmento, forma, texto, censura), `AnnotationDocument` (orden z, deshacer y rehacer), `Rgba` |
| `Imaging/` | `PixelBuffer` (BGRA) y `Censor` (pixelado, desenfoque, sólido) |
| `Settings/` | `AppSettings` (valores por defecto y rangos) y `SettingsStore` (JSON atómico; si está corrupto, crea `.bak`) |
| `Hotkeys/`, `Files/` | Validación y nombres de atajos, `KeyInterceptor` (qué pulsaciones de Impr Pant se quedan y cuáles disparan un atajo), nombre de archivo por defecto |

**`src/SScreentify.App`**: Windows y WPF, con pruebas en `tests/SScreentify.App.Tests`.

| Carpeta | Qué hay |
|---|---|
| `App.xaml.cs` | Arranque: instancia única, ajustes, atajos, bandeja, avisos de inicio, Opciones y manejador global de errores |
| `Capture/` | Captura GDI del escritorio virtual en píxeles físicos y lista de monitores |
| `Overlay/` | `OverlayController` (ciclo de vida de cada captura y toda la entrada de ratón y teclado), `OverlayWindow` (una por monitor, creada de antemano y oculta), `SelectionAdorner` (velo, borde, tiradores y tamaño), `AnnotationLayer`, `ToolController` (herramientas de dibujo), `ChromeController` (coloca las barras), `TextEditorHost` |
| `Rendering/` | `AnnotationRenderer`, compartido por el selector y la exportación, y `CensorPatchCache` |
| `Export/` | `Flattener` (imagen final), `ImageEncoder`, `ClipboardService`, `SaveService`, `PrintService` y `ExportActions`, que los orquesta y muestra los avisos |
| `Ui/` | Tema Cristal oscuro (`Theme.xaml` y `Palette.cs`), barras, paleta, iconos, Opciones, avisos, Acerca de y textos (`Strings.resx` y `Strings.cs`) |
| `Services/` | Atajos globales (`HotkeyService`, y `KeyboardHook` para Impr Pant), bandeja, avisos, inicio con Windows y rutas |
| `Diagnostics/` | `--perf` (tiempos y memoria) y `--export-icon` (genera `Assets/app.ico` a partir de `Ui/BrandMark.cs`; si cambias el logo, regenéralo con `SScreentify.exe --export-icon src\SScreentify.App\Assets\app.ico`) |

**Regla clave de diseño:** toda la geometría va en **píxeles físicos del escritorio virtual**. Cada ventana convierte a DIP con su propio DPI solo al dibujar (`OverlayWindow.PhysicalToDip` y `ToPhysical`). Las anotaciones se guardan en coordenadas de la captura, no de la selección.

## Trampas que conviene saber

- **Textos de la interfaz:** WPF no genera la clase de `Strings.resx`. Para añadir textos usa `node scripts/sync-strings.js "Clave=Texto"`, que regenera `Ui/Strings.cs`. Después actualiza el número esperado en `ShellTests.Strings_EveryPropertyResolves`.
- **Renderizado por software por defecto** (`App.xaml.cs`): con la GPU, el driver retenía unos 70 MB y no se cumplía el objetivo de 60 MB en reposo. `--gpu-render` vuelve a la GPU. Si cambias esto, vuelve a medir.
- **Impr Pant en Windows 11:** los atajos con Impr Pant no usan `RegisterHotKey` (Recortes puede tenerla reservada), sino un gancho de teclado de bajo nivel (`Services/KeyboardHook.cs`) en un hilo propio, que se queda con la pulsación. Windows quita sin avisar un gancho que tarda en responder, así que su función no debe esperar nunca al hilo de la interfaz: solo consulta `KeyInterceptor` y encola el atajo con `BeginInvoke`. Con una ventana de administrador en primer plano no recibe teclas (protección de Windows, comprobado el 19-10-2026) y Recortes gana. Se descartó a propósito: arreglarlo exigía ejecutar la app como administrador, firmarla con `uiAccess`, o desactivar la opción de Recortes y registrar también la tecla con `RegisterHotKey`. En este equipo, `RegisterHotKey` acepta Impr Pant aunque Recortes la tenga, pero Recortes gana igualmente.
- **Desenfoque de Censurar** (`Imaging/Censor.cs`, `Blur`): se recalcula en cada movimiento del ratón, así que tiene que ser rápido. Son tres desenfoques de caja por eje con sumas acumuladas, que dan casi el mismo resultado que el gaussiano (diferencia máxima de 10/255 en bordes duros, comprobada contra una referencia en `CensorTests`). Los métodos llevan `AggressiveOptimization` para que el primer arrastre no vaya con el código sin optimizar. Medido el 19-10-2026 (Release, mediana): 600×400 en 8–10 ms (antes 44–54), 1200×700 en 26–37 ms (antes 150), pantalla completa 1920×1080 en 82 ms (antes 377) y 4K en 256 ms (antes 1430).
- **Carpeta de guardado** (`Export/SaveService.cs`, `ResolveInitialFolder`): para saber si se puede escribir en una carpeta, crea un archivo vacío `.SScreentify-….tmp` que Windows borra al cerrarlo. Predecir los permisos es poco fiable.
- **Recursos de WPF compartidos entre hilos** (pinceles y geometrías `static`): tienen que estar congelados (`Freeze()`). Si no, las pruebas fallan con «el propietario es otro subproceso».
- **Pruebas con WPF:** usa `StaRunner.Run(...)`. `OverlayController` y `ExportActions` aceptan proveedores falsos de captura y monitores, e `IToastSink`, para probar sin abrir ventanas (ver `FinalReviewTests`).
- **Revisión visual sin abrir la app:** `VisualSnapshotTests` renderiza el selector, Opciones y los avisos sobre un escritorio sintético. Con `SSCREENTIFY_SNAPSHOT_DIR=<carpeta>` guarda los PNG.
- **Instalar software con Claude:** el modo automático bloquea `winget`, así que hay que ejecutarlo con `!` en el prompt, añadiendo `--source winget --accept-source-agreements --accept-package-agreements`.

## Medir el rendimiento

Arranca `publish\SScreentify.exe --perf`, haz unas capturas y cierra. El registro queda en `%APPDATA%\SScreentify\perf.log`:

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
