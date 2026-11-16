# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Contexto del proyecto (para continuar en otra sesión de Claude Code)

Responder siempre en **español**. El usuario es estudiante de Informática (UNT, curso *Tópicos en Tecnologías Inmersivas*) y no es experto en Unity: dar pasos concretos y cortos.

## De dónde viene
- `Ejemplos de Realidad Aumentada - Diapositivas.pdf`: la exposición del grupo sobre 7 casos de RA (Merge Cube era el "modelo del proyecto").
- El proyecto empezó como **RA** (Vuforia + cubo de papel, al estilo Merge Cube). El 2026-11-16 el usuario aclaró que **tenía que ser realidad virtual**: se cambió a **RV sin visor** (PC + celular con giroscopio), manteniendo el tema Sistema Solar.
- La versión RA completa está en la rama git **`realidad-aumentada`**. El `.tgz` de Vuforia (132 MB) no está versionado: se reinstala con `Descargas\add-vuforia-package-11-4-4.unitypackage`. La licencia está en `licencia-vuforia.txt` (en .gitignore).
- Pendiente para la exposición: ajustar las diapositivas (hablan de RA), agregar referencias y explicar que es *RV no inmersiva*.

## El proyecto: Sistema Solar en Realidad Virtual
- El usuario flota dentro del sistema solar, se mueve libremente, toca los planetas para ver su ficha y "viaja" volando hasta ellos (después los sigue en su órbita).
- Herramientas: **Unity 2022.3.62f3** (Built-in Render Pipeline, Input Manager clásico: `activeInputHandler: 0`). Salidas: **Windows** (`Builds/PC/SistemaSolarVR.exe`, 1280x720 en ventana) y **Android** (`Builds/Android/SistemaSolarVR.apk`, IL2CPP, ARM64, API 24, LandscapeLeft, id `pe.edu.unt.sistemasolarvr`).
- La escala es pequeña: Neptuno queda a unos 0,29 m del Sol y Mercurio mide 0,007 m. Por eso la cámara usa near 0.002 y far 20. La cámara arranca en (0, 0.11, -0.42) mirando al Sol.
- `GUIA_PROYECTO.md`: la guía completa para el usuario. Mantenerla actualizada.
- La carpeta del proyecto se sigue llamando `SistemaSolarRA` (no se renombró para no romper Unity Hub).

### Archivos
- `SistemaSolarRA/Assets/Scripts/`:
  - `CamaraExplorador`: modos Libre, Viajando, Siguiendo y Volviendo.
    - Entrada: WASD/QE/Shift, arrastrar para mirar, rueda o pellizco para avanzar, giroscopio y `avanceBotones` (estático).
    - `Viajar()`: al llegar, la cámara mira el lado de día del planeta (`DireccionIluminada`, usa `objetivo.root` = el Sol en el origen). Si la ficha está abierta (`fichaAbierta`, estático, lo escribe `InfoPanelUI.OnEnable/OnDisable`), la cámara se corre a la derecha.
    - El giroscopio se calibra al primer dato válido.
  - `PlanetSelector`: elige un planeta al **soltar**, solo si el puntero se movió menos de 20 px en menos de 0,5 s (arrastrar = mirar). `ViajarAlSeleccionado()`.
  - `AppUIBuilder`: UI horizontal (referencia 1920x1080).
    - Ayuda arriba, controles abajo (Pausa, Sonido, Vista general, Giroscopio y slider).
    - Ficha a la derecha con el botón "Viajar hasta aquí".
    - En el celular también crea `BotonMantener` (Avanzar y Retroceder).
    - El giroscopio se conecta en `Start` y en el celular empieza encendido.
  - `SolarSystemBuilder`: crea el sistema en tiempo de ejecución desde `PlanetData`. Las estrellas forman una esfera de radio 4.
  - `AnchoSegunDistancia`: ajusta el ancho de las órbitas y estelas según la distancia a la cámara.
  - `PlanetOrbit`, `PlanetInfo` (crece y brilla), `InfoPanelUI`, `SolarSystemController`, `Procedural`, `SonidoEspacial` (audio con `AudioClip.Create`).
  - `AparicionSistema`: animación de entrada. Arranca cuando `Renderer.enabled` del Sol está encendido, es decir, al iniciar.
  - `CapturaAutomatica`: solo actúa si el `.exe` se abre con `-capturas <carpeta>`. Guarda capturas de 1280x720: aparición, sistema, viajes a la Tierra, Saturno y el Sol, y vista general.
- `Assets/Shaders/Atmosfera.shader`: halo de atmósfera.
- `Assets/Texturas`: texturas 2k de Solar System Scope (CC BY 4.0). `estrellas` se usa como skybox panorámico.
- `Assets/Editor/SistemaSolarSetup.cs`: menú **Sistema Solar** con tres opciones. `CompilarPC` y `CompilarAPK` crean la escena si falta.
  - **1. Crear escena** (`CrearEscena`)
  - **2. Compilar para PC** (`CompilarPC`)
  - **3. Compilar APK** (`CompilarAPK`)

## Comandos
No hay tests ni linter: se verifica compilando y con las capturas.
- Compilar el `.exe`:
  ```
  "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -quit -projectPath SistemaSolarRA -executeMethod SistemaSolarSetup.CompilarPC -logFile pc.log
  ```
  Después buscar `error CS` y `RESULTADO_BUILD` en el log. Para el APK, usar `SistemaSolarSetup.CompilarAPK`.
- Capturas para revisar visualmente:
  ```
  SistemaSolarRA/Builds/PC/SistemaSolarVR.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -capturas <carpeta>
  ```
  Sin `-batchmode`, para que se vea el texto TMP.

## Cómo se conecta todo
1. En el editor, `CrearEscena` deja en la escena:
   - La Main Camera con `CamaraExplorador` y el skybox.
   - `SistemaSolar` en el origen, con `SolarSystemBuilder` y sus materiales y texturas asignados.
   - `GestorApp` con `AppUIBuilder`.

   En la escena **no** se guardan planetas ni UI.
2. Al ejecutar la app, `SolarSystemBuilder` crea los cuerpos: pivote con `PlanetOrbit` > ancla > esfera con `PlanetInfo`. `AppUIBuilder` crea el Canvas y los componentes, y los conecta.
3. **Entrada:** los scripts usan `#if ENABLE_INPUT_SYSTEM` para soportar los dos sistemas de input. Si tocas la entrada, mantén las dos ramas.
4. El Sol es la única luz: una luz puntual con rango escalado por `AparicionSistema`, más una luz ambiental baja.

## Estado (al 2026-11-16)
- La versión VR **compila sin errores** (.exe y APK). Se revisó con capturas: vista general, viaje a la Tierra (lado de día), a Saturno y al Sol.
- Sin probar en un celular real: el giroscopio (la conversión `Euler(90,0,0) * (x, y, -z, -w)` en LandscapeLeft), los botones Avanzar/Retroceder, el pellizco y el rendimiento.

## Próximos pasos
1. Que el usuario pruebe el `.exe` y el APK y cuente cómo le va, en especial el giroscopio.
2. Extras ofrecidos: modo Cardboard (RV inmersiva), recorrido guiado, quiz, narración, escala real.
3. Exposición: actualizar las diapositivas (de RA a RV), agregar referencias (NASA, Solar System Scope) y llevar el `.exe`, el APK y un video de respaldo.

## Si se abre en otra computadora
- Instalar Unity Hub (`winget install Unity.UnityHub`) y el editor 2022.3.62f3, changeset `96770f904ca7`, con el módulo de Android:
  `"<Unity Hub.exe>" -- --headless install --version 2022.3.62f3 --changeset 96770f904ca7 --module android --childModules`
  - Unity Hub de winget es MSIX: el exe está en `(Get-AppxPackage *UnityHub*).InstallLocation\app\Unity Hub.exe`.
- No copiar las carpetas `SistemaSolarRA/Library`, `Temp`, `Logs` ni `obj`: Unity las regenera.
