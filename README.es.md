<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/assets/logo-dark.svg">
    <img src="docs/assets/logo-light.svg" alt="SScreentify" width="420">
  </picture>
</p>

<p align="center">
  <b>Pulsa Impr Pant, elige una zona, anótala y pégala donde quieras.</b><br>
  Una herramienta de capturas rápida y sin conexión para Windows.
</p>

<p align="center">
  <a href="../../releases/latest">Descargar</a> ·
  <a href="#funciones">Funciones</a> ·
  <a href="#atajos-de-teclado">Atajos</a> ·
  <a href="#compilar-desde-el-código">Compilar</a> ·
  <a href="README.md">English</a>
</p>

<p align="center">
  <img src="docs/assets/editor.png" alt="El selector de SScreentify con un rectángulo, una flecha, un texto y un resaltado sobre una selección, y sus barras de herramientas y acciones" width="640"><br>
  <sub>El selector y sus barras, dibujados con el propio código de la interfaz sobre un escritorio sintético.</sub>
</p>

## Para qué sirve

SScreentify vive en la bandeja del sistema. Al pulsar **Impr Pant**, la pantalla entera se congela bajo un velo oscuro. Arrastras un rectángulo, dibujas encima si quieres (flechas, recuadros, texto, resaltador, censura) y después la **copias** (Ctrl+C) para pegarla en un chat, un correo o un documento, o la **guardas** (Ctrl+S) como archivo.

Está pensada para la captura de todos los días, la de «mira, es esto»: se abre al instante, se anota rápido y no estorba. Nunca usa la red.

### Un banco de pruebas para REA

SScreentify también es una prueba práctica de ingeniería inversa con [REA (Reverse Engineer Anything)](https://github.com/morluto/rea). El proyecto sirvió para probar el flujo de trabajo de REA con una aplicación de escritorio real: estudiar cómo funciona una función y luego reconstruirla con código propio y limpio.

## Funciones

- **Selector instantáneo** en todos los monitores, con soporte de DPI por monitor. Se abre en unos 30 ms.
- **Selección precisa:** 8 tiradores, flechas del teclado para moverla y Mayús + flechas para cambiar su tamaño. La etiqueta de tamaño muestra píxeles físicos. Ctrl+A selecciona el monitor bajo el cursor y, si lo pulsas otra vez, todo el escritorio.
- **Editor:** lápiz, línea, flecha, rectángulo, círculo, resaltador, texto y color, con deshacer y rehacer. La rueda del ratón cambia el grosor o el tamaño de letra, y Mayús fija ángulos de 45° y cuadrados o círculos perfectos. Las anotaciones siguen siendo editables: selecciónalas con V para moverlas, cambiar su tamaño o borrarlas.
- **Censurar:** pixelado, desenfoque o bloque sólido. Se calcula siempre desde los píxeles originales y queda incrustado en la imagen exportada. Para datos sensibles usa el bloque sólido, porque el pixelado y el desenfoque pueden revertirse en parte.
- **Copiar:** pone la imagen en el portapapeles como PNG y como mapa de bits estándar, así que se puede pegar tanto en aplicaciones modernas como en antiguas.
- **Guardar:** PNG, JPG (con calidad ajustable) o BMP, sin metadatos. Los archivos se nombran por fecha y hora, y un guardado instantáneo nunca reemplaza a uno anterior.
- **Imprimir** directamente desde el selector.
- **Guardar la pantalla completa al instante** con un segundo atajo opcional (desactivado por defecto).
- **Le quita Impr Pant a Recortes de Windows** mientras SScreentify está abierto, sin cambiar ninguna opción de Windows.
- **Español e inglés.** Por defecto sigue el idioma de Windows, y se puede cambiar en Opciones.
- **Opciones:** avisos, incluir el cursor, recordar la última selección, iniciar con Windows, idioma, atajos, formato por defecto, calidad JPG y carpeta por defecto.
- **Consume poco:** unos 55 MB de memoria en reposo.

## Descargar e instalar

1. Descarga `SScreentify-Setup-<versión>.exe` desde la [última versión publicada](../../releases/latest).
2. Ejecútalo. Se instala solo para tu usuario (en `%LOCALAPPDATA%\Programs\SScreentify`), así que no pide permisos de administrador.
3. SScreentify arranca en la bandeja del sistema. Pulsa **Impr Pant** para capturar, o haz clic izquierdo en el icono de la bandeja.

Requisitos: Windows 10 versión 1903 o posterior, de 64 bits. El instalador incluye todo lo necesario, así que no hace falta instalar .NET.

> **Windows SmartScreen:** el instalador todavía no está firmado digitalmente, así que Windows puede mostrar «Windows protegió su PC». Elige **Más información › Ejecutar de todas formas** si confías en la descarga.

Para desinstalarlo, ve a **Configuración › Aplicaciones › Aplicaciones instaladas › SScreentify**. Se conservan tus ajustes (`%APPDATA%\SScreentify`) y las capturas que hayas guardado.

## Atajos de teclado

| Dónde | Atajo | Acción |
|---|---|---|
| En cualquier sitio | Impr Pant | Abrir el selector (configurable) |
| En cualquier sitio | *(opcional)* | Guardar la pantalla completa al instante |
| Selector | Ctrl+C | Copiar y cerrar |
| Selector | Ctrl+S | Guardar como… |
| Selector | Ctrl+P | Imprimir |
| Selector | Esc, Ctrl+X o clic derecho fuera | Cerrar sin guardar |
| Selector | Ctrl+A | Seleccionar el monitor; otra vez para todo el escritorio |
| Selector | Flechas / Mayús + flechas | Mover / cambiar el tamaño de la selección |
| Editor | Ctrl+Z, Ctrl+Y o Ctrl+Mayús+Z | Deshacer, rehacer |
| Editor | V | Seleccionar anotaciones; Supr borra la seleccionada |
| Editor | Rueda del ratón | Grosor o tamaño de letra |
| Editor | Mayús mientras dibujas | Ángulos de 45°, cuadrados y círculos |

### Impr Pant en Windows 11

Windows 11 puede reservar la tecla Impr Pant para la herramienta Recortes. Mientras SScreentify está abierto, lee la tecla directamente del teclado, así que Impr Pant abre SScreentify. Al cerrarlo, la tecla vuelve a Recortes. No se cambia ninguna opción de Windows.

**Limitación conocida:** mientras una ventana ejecutada como administrador está en primer plano, Windows no deja que una aplicación normal lea el teclado, así que en ese caso Impr Pant abre Recortes. Haz clic izquierdo en el icono de la bandeja.

## Privacidad

SScreentify no hace ninguna petición de red y no tiene telemetría ni cuentas. Las capturas solo van donde tú las copies o las guardes. La configuración es un pequeño archivo JSON en `%APPDATA%\SScreentify\settings.json`.

## Compilar desde el código

Necesitas Windows y el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet test                                # pruebas unitarias y de interfaz
dotnet run --project src\SScreentify.App   # ejecutar desde el código
.\scripts\publish.ps1                      # publish\SScreentify.exe, autocontenido y en un solo archivo
.\scripts\build-installer.ps1              # dist\SScreentify-Setup-<versión>.exe (necesita Inno Setup 6)
```

Para instalar Inno Setup: `winget install --id JRSoftware.InnoSetup -e --source winget`. La versión se toma de `<Version>` en `src/SScreentify.App/SScreentify.App.csproj`.

Argumentos útiles: `--perf` escribe tiempos y memoria en `%APPDATA%\SScreentify\perf.log`, y `--gpu-render` usa la GPU en lugar del renderizado por software, que es el predeterminado.

### Cómo está hecho

- **C# y WPF sobre .NET 8.** `SScreentify.Core` tiene la lógica pura: geometría, anotaciones, censura, ajustes y atajos. `SScreentify.App` tiene la parte de Windows: captura, selector, renderizado, exportación e interfaz.
- **Captura:** GDI captura todo el escritorio virtual en píxeles físicos, y cada monitor tiene su propia ventana de selector, que convierte a su DPI solo al dibujar.
- **Renderizado:** por defecto es por software, porque con la GPU el driver retenía unos 70 MB después de la primera captura.

Las notas de desarrollo, con el mapa del código y las trampas conocidas, están en [docs/CONTINUAR.md](docs/CONTINUAR.md).

### Rendimiento

Medido con un monitor de 1920×1080 y el ejecutable publicado:

| Medida | Resultado |
|---|---|
| Pulsación → selector visible (en caliente, 20 veces) | mediana 30,6 ms, p95 33,6 ms |
| Arranque en frío hasta la bandeja | ~0,5 s |
| Memoria en reposo tras una captura (privada) | 54,7 MB |
| Desenfoque al arrastrar, zona de 600×400 | 8–10 ms por actualización |

Sin medir todavía: varios monitores, escalas mixtas y 4K.

## Licencia

[MIT](LICENSE) © 2026 Diego Muñoz.

El logo y el nombre son propios de SScreentify; el logotipo usa la fuente [Space Grotesk](https://fonts.google.com/specimen/Space+Grotesk) (SIL Open Font License), convertida en trazados.
