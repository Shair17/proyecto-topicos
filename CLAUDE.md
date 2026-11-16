# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Contexto del proyecto (para continuar en otra sesión de Claude Code)

Responder siempre en **español**. El usuario es estudiante de Informática (UNT, curso *Tópicos en Tecnologías Inmersivas*) y no es experto en Unity: dar pasos concretos y cortos.

## De dónde viene
- `Ejemplos de Realidad Aumentada - Diapositivas.pdf`: la exposición del grupo sobre 7 casos de RA. Dos casos se marcaron como "ejemplos del proyecto": Merge Cube y Dynamics 365 Guides.
- Ya se le entregó un análisis del PDF. Lo pendiente de ese análisis: agregar una diapositiva de referencias, usar una sola clasificación de tipos de RA, decidir qué hacer con Lowe's Holoroom (es realidad virtual) y cambiar la diapositiva 14 a "Modelo para nuestro proyecto: la interacción con marcadores de Merge Cube".
- **Decisión:** el proyecto se basa **solo en Merge Cube**. Dynamics 365 se descartó porque requiere HoloLens.

## El proyecto: Sistema Solar en RA con cubo marcador
- Un cubo de papel de 6 cm muestra el Sol en su lugar, con 8 planetas en órbita. Al tocar un planeta se abre una ficha con sus datos; hay botones de pausa y velocidad.
- Herramientas: **Unity 2022.3.62f3** (Built-in Render Pipeline), **Vuforia Engine** (Multi Target con forma de cubo, licencia Basic gratuita) y **Android** (IL2CPP, ARM64, API mínima 24).
- `GUIA_PROYECTO.md`: la guía completa para el usuario. Mantenerla actualizada.

### Archivos
- `SistemaSolarRA/`: el proyecto de Unity, abierto desde Unity Hub con "Add project from disk".
  - `Assets/Scripts/`:
    - `PlanetData`: datos de la NASA.
    - `SolarSystemBuilder`: crea los planetas en tiempo de ejecución.
    - `PlanetOrbit`, `PlanetInfo` (crece y brilla al seleccionarlo), `PlanetSelector`, `InfoPanelUI` (con animación de entrada), `SolarSystemController`.
    - `Procedural`: crea por código la textura de brillo, la malla del anillo, las partículas y el anillo de respaldo.
    - `AparicionSistema`: animación de entrada. Usa `Renderer.enabled` del Sol, igual que `CubeTrackingUI`. También escala el rango de la luz.
    - `SonidoEspacial`: música ambiental y efectos generados con `AudioClip.Create`, sin archivos de audio.
    - `CapturaAutomatica`: solo actúa si el `.exe` se abre con `-capturas <carpeta>`. Con `-batchmode` renderiza la cámara a una textura, pero en esas capturas el texto TMP no aparece.
    - `CubeTrackingUI`: no usa la API de Vuforia. Mira `Renderer.enabled` del Sol, que Vuforia apaga cuando pierde el cubo, para que el proyecto compile aunque Vuforia no esté importado.
    - `AppUIBuilder`: crea el Canvas y conecta todo por código.
  - `Assets/Shaders/Atmosfera.shader`: halo de atmósfera (caras traseras, aditivo).
  - `Assets/Texturas`: texturas 2k de Solar System Scope (CC BY 4.0), renombradas en español. `tierra_nubes` se importa con alfa desde gris.
  - `Assets/Editor/SistemaSolarSetup.cs`: menú **Sistema Solar > 0. Vista previa**, **1. Crear escena**, **2. Configurar Android** y **3. Compilar APK** (también `-executeMethod SistemaSolarSetup.CompilarAPK`). `CrearMaterial` actualiza los materiales que ya existen.
    - Usa `EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/AR Camera" | ".../Multi Target")`.
    - Pone el filtro de estado del DefaultObserverEventHandler en "Tracked" mediante SerializedObject.
    - Asigna solas las texturas que estén en `Assets/Texturas`.
  - `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt` se escribieron a mano. Unity genera el resto al abrir el proyecto.
- `Marcadores/`: 6 caras (`cara_front`…`cara_bottom`) para subir a Vuforia Target Manager (Cuboid de 0.06 m) y `cubo_imprimible_A4.png`.
- `Herramientas/generar_marcadores.py`: regenera las caras. Requiere `pip install pillow`. Acepta el lado del cubo en cm como argumento.

## Comandos
No hay tests ni linter: la única verificación posible es que compile.
- Compilar (requiere sesión iniciada en Unity Hub):
  `"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -quit -projectPath SistemaSolarRA -logFile build.log`, y luego buscar `error CS` en `build.log`.
- Vista previa en PC sin Vuforia (escena `VistaPreviaPC` con `CamaraVistaPrevia` + `.exe` en `SistemaSolarRA/Builds/VistaPreviaPC/`): el mismo comando con `-executeMethod SistemaSolarSetup.CompilarVistaPreviaPC`. En el editor está el menú **Sistema Solar > 0. Vista previa en PC (sin RA)**.
- Regenerar los marcadores: `python Herramientas/generar_marcadores.py [lado_cm]` (6 cm por defecto). Las semillas de cada cara son fijas, así que el resultado es reproducible. Si cambias el lado, también hay que cambiar el tamaño del Cuboid en Vuforia.

## Cómo se conecta todo
1. **En el editor**, el menú `1. Crear escena` (`SistemaSolarSetup`) crea la AR Camera y el Multi Target de Vuforia. Debajo del target cuelga un objeto con `SolarSystemBuilder` (le asigna las texturas cuyo nombre contenga claves como "mars"), y aparte crea `GestorApp` con `AppUIBuilder`. En la escena **no** se guardan planetas ni UI.
2. **Al ejecutar la app**, `SolarSystemBuilder` genera el Sol y los planetas desde `PlanetData`: a cada uno le pone un pivote con `PlanetOrbit`, un `PlanetInfo` y una línea de órbita. `AppUIBuilder` crea el Canvas, el EventSystem y los paneles, y agrega `InfoPanelUI`, `PlanetSelector`, `SolarSystemController` y `CubeTrackingUI`, que se conectan entre sí por código.
3. **Entrada:** `PlanetSelector` y `AppUIBuilder` usan `#if ENABLE_INPUT_SYSTEM` para funcionar con cualquiera de los dos sistemas de input de Unity. Si tocas la entrada, mantén las dos ramas.
4. Vuforia vuelve a encender todos los Renderers hijos cuando encuentra el cubo. Por eso nada se oculta con `renderer.enabled`: el halo de selección se maneja emitiendo y limpiando partículas.
5. El Sol es la única luz: `SolarSystemBuilder` apaga las luces direccionales y pone una luz ambiental baja. El módulo `com.unity.modules.particlesystem` está en `manifest.json`.
6. Los scripts de `Assets/Scripts` no deben depender de Vuforia (ver `CubeTrackingUI`). Vuforia está en `manifest.json` como tarball local (`file:`). Lo único que usa Vuforia es `Assets/Editor`, y lo hace por rutas de menú, `SerializedObject` y reflexión.

## Estado (al 2026-11-15)
- Hecho: los scripts, el menú de un clic, los marcadores y la guía. Unity Hub y Unity 2022.3.62f3 con Android están instalados y con sesión iniciada.
- Mejoras visuales hechas el 2026-11-03: texturas reales, el Sol como única luz con resplandor, corona y atmósferas, el anillo real de Saturno, nubes, la Luna, la inclinación de los ejes, estelas, el cinturón de asteroides, estrellas, la animación de aparición, el resaltado al seleccionar, el sonido procedural y la animación de la ficha.
- **Compila sin errores** y el `.exe` de vista previa funciona. Se revisó con capturas generadas por `-capturas`.
- 2026-11-15: Vuforia 11.4.4 importado (`Packages/com.ptc.vuforia.engine-11.4.4.tgz` + línea `file:` en `manifest.json`; en batchmode el instalador `Assets/Editor/Migration/AddVuforiaEnginePackage.cs` no actúa, se hizo a mano). Compila sin errores. La licencia está en `licencia-vuforia.txt` (raíz, en .gitignore); `SistemaSolarSetup.CargarLicenciaVuforia` (reflexión, campo `vuforiaLicenseKey`) la copia a `Assets/Resources/VuforiaConfiguration.asset` (también en .gitignore) y se llama desde el menú 1.
- 2026-11-15: base `CuboSistemaSolar` importada (`Assets/StreamingAssets/Vuforia/`, Cuboid 0.06 m), escena `SistemaSolar.unity` creada con `-executeMethod SistemaSolarSetup.CrearEscena` (9/9 texturas, filtro Tracked OK) y **APK con RA compilado**: `Builds/Android/SistemaSolarRA.apk` (~58 MB). Sin probar en un celular.
- Sin verificar:
  - (Verificado 2026-11-15 con captura del Target Manager: la plantilla pliega igual que la red de Vuforia, con 2 boca abajo arriba de 5; en la plantilla el 2 va debajo del 6, también boca abajo, y queda equivalente.)
  - Las rutas de menú de Vuforia y StatusFilter ya funcionaron (2026-11-15).
  - (Verificado 2026-11-15: las 6 caras tienen 5 estrellas.)
  - El rendimiento en el celular: 1 luz puntual, unas 1300 partículas y texturas de 2k.

## Próximos pasos
1. Instalar `SistemaSolarRA.apk` en un celular, imprimir el cubo al 100 % y probar: reconocimiento, orientación de caras, rendimiento y estrellas de las caras.
2. Para la exposición: APK en un celular Android, pantalla proyectada con scrcpy, el `.exe` de vista previa como respaldo y un video grabado por si falla algo.
3. Extras opcionales que se ofrecieron: controlar con el cubo (inclinarlo para cambiar la velocidad), modo escala real, modo guiado (Anterior/Siguiente), quiz, narración, y una diapositiva de referencias para la exposición.

## Si se abre en otra computadora
- Instalar Unity Hub (`winget install Unity.UnityHub`) y el editor 2022.3.62f3, changeset `96770f904ca7`, con el módulo de Android:
  `"<Unity Hub.exe>" -- --headless install --version 2022.3.62f3 --changeset 96770f904ca7 --module android --childModules`
  - Unity Hub de winget es MSIX: el exe está en `(Get-AppxPackage *UnityHub*).InstallLocation\app\Unity Hub.exe`.
- No copiar las carpetas `SistemaSolarRA/Library`, `Temp`, `Logs` ni `obj`: Unity las regenera.
