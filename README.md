# sScreentify

Herramienta de capturas de pantalla para Windows. Vive en la bandeja del sistema: pulsas **Impr Pant**, eliges una zona, la anotas y la **copias** (Ctrl+C) para pegarla en un chat o la **guardas** (Ctrl+S) donde quieras.

Funciona sin conexión: no hace ninguna petición de red.

## Funciones

- **Selector** en todos los monitores, con soporte de DPI por monitor. La zona se ajusta con tiradores o con las flechas (Mayús + flechas cambia el tamaño). Ctrl+A selecciona el monitor y, si lo pulsas otra vez, todo el escritorio. Esc o clic derecho fuera de la zona lo cancelan.
- **Editor:** selección (V), lápiz, línea, flecha, rectángulo, círculo, resaltador, texto y color. La rueda del ratón cambia el grosor o el tamaño de letra. Mayús fija ángulos de 45° y cuadrados o círculos perfectos. Deshacer con Ctrl+Z y rehacer con Ctrl+Y o Ctrl+Mayús+Z.
- **Censurar:** pixelado, desenfoque o bloque sólido. Se calcula desde los píxeles originales y queda incrustado en la imagen final. Para datos sensibles usa el bloque sólido.
- **Acciones:** copiar (PNG + mapa de bits), guardar (PNG, JPG o BMP, sin metadatos) e imprimir. Atajo opcional para guardar la pantalla completa al instante.
- **Opciones:** avisos, incluir el cursor, recordar la última selección, iniciar con Windows, atajos, formato, calidad JPG y carpeta por defecto. La configuración se guarda en `%APPDATA%\sScreentify\settings.json`.

## Impr Pant en Windows 11

Windows 11 puede reservar la tecla Impr Pant para la herramienta Recortes. No hace falta cambiar nada: mientras sScreentify está abierto, lee los atajos que usan Impr Pant directamente del teclado, abre su selector y no deja que la pulsación llegue a Recortes. No modifica ninguna opción de Windows; al cerrar sScreentify, Impr Pant vuelve a comportarse como antes.

Limitación conocida (comprobada el 19-10-2026): si la ventana activa se ejecuta como administrador, Windows no deja que sScreentify lea el teclado y la tecla abre Recortes. En ese caso, haz clic en el icono de la bandeja.

## Compilar, probar y publicar

Requisitos: Windows 10 1903 o posterior y el SDK de .NET 8.

```powershell
dotnet test                       # pruebas de Core y de la App
dotnet run --project src\SScreentify.App
.\scripts\publish.ps1             # crea publish\sScreentify.exe (un solo archivo, autocontenido)
```

Argumentos útiles:

- `--perf`: escribe tiempos y memoria en `%APPDATA%\sScreentify\perf.log`.
- `--gpu-render`: usa la GPU en lugar del renderizado por software (ver «Rendimiento»).

## Rendimiento

Medido el 17-10-2026 en el equipo de Diego, con un monitor de 1920×1080 píxeles físicos. Se usó el ejecutable publicado (`scripts\publish.ps1`, un solo archivo, sin ReadyToRun) con `--perf`, y las pulsaciones de Impr Pant y Esc se simularon con `keybd_event`.

| Medida | Resultado | Objetivo |
|---|---|---|
| Pulsación → selector visible, en caliente (20 capturas) | mediana 30,6 ms · p95 33,6 ms | ≤ 150 ms |
| De ello, la captura de la pantalla | mediana 22,5 ms | — |
| Arranque en frío hasta la bandeja (3 arranques) | 488 · 480 · 489 ms | — |
| Memoria en reposo tras capturar (privada) | 54,7 MB (conjunto de trabajo 130 MB) | ≤ 60 MB |

Decisiones tomadas a partir de estas mediciones:

- **Renderizado por software por defecto.** Con la GPU, el driver retiene unos 70 MB después de la primera captura: la memoria privada en reposo se quedaba en 114–120 MB, con el mismo tiempo de apertura (~30 ms). Con renderizado por software se queda en unos 45 MB (build dependiente del framework) o 55 MB (build publicado). `--gpu-render` vuelve a usar la GPU.
- **Sin ReadyToRun.** El build con ReadyToRun no arrancaba más rápido en caliente (446 ms frente a 480 ms) y usaba 75 MB en reposo.
- Al cerrar el selector se sueltan las referencias a la captura y se fuerza una recolección de memoria cuando la app queda inactiva.

No se ha medido: varios monitores, escalas distintas del 100 % ni monitores 4K. Con renderizado por software, la fluidez al arrastrar la selección en un 4K está por comprobar.

## Continuar el desarrollo

Estado, mapa del código, pendientes y trampas conocidas: [docs/CONTINUAR.md](docs/CONTINUAR.md).
