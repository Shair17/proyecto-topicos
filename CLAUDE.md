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
  - `SolarSystemBuilder`: crea el sistema en tiempo de ejecución desde `PlanetData`. Las estrellas forman una esfera de radio 4. `CrearCometa` arma el cometa: Cometa (plano inclinado 18°) > ancla > núcleo. Tiene coma con `EmitirFija` y una cola de partículas en espacio World. `RadioOrbita` es público.
  - Funciones agregadas el 2026-11-16 (todas las crea y conecta `AppUIBuilder` sobre `GestorApp`):
    - `ModoVisor`: dos cámaras hijas de la principal (`CopyFrom`, viewport 0–0,5 y 0,5–1, separación 0,0025, FOV 75).
      - La cámara principal se apaga (`enabled = false`) y se oculta el Canvas normal; aparece `CanvasVisor` con el botón "Salir del visor".
      - Mira (quad aditivo) y `TextMeshPro` a 0,05 m frente a la cámara.
      - Raycast al centro: tras 2 s de mirada viaja al planeta. Un toque corto viaja de inmediato; tocando al vacío vuelve a la vista general. Esc sale.
      - Enciende el giroscopio si existe y luego restaura su estado anterior.
    - `RecorridoGuiado`: corrutina que recorre Sol → … → Neptuno → Cometa. Se detiene si `CamaraExplorador.Objetivo` deja de ser el planeta actual. Escribe su avance en el texto de ayuda.
    - `QuizPlanetas`: banco de 10 preguntas, de las que juega 8. Las respuestas son nombres de cuerpos de la escena, para poder viajar a ellos. Desactiva `PlanetSelector` mientras está abierto. Conecta los botones en `Start` y `Responder` es público (lo usa la captura).
    - `Cometa`: órbita elíptica con la segunda ley de Kepler y retrógrada. La cola apunta lejos del Sol y su emisión depende de la distancia.
    - `EtiquetasPlanetas`: TMP 3D por cada `PlanetInfo`, que mira a la cámara con escala = distancia × 0,2. Se oculta cuando el planeta llena la pantalla.
    - `IndicadorDistancia`: interpola el radio de la escena a UA reales (más allá de Neptuno extrapola: la cámara inicial marca unos 85 UA). Formatea los números a mano, sin `CultureInfo("es-ES")`.
    - `SonidoEspacial` tiene `Correcto()` e `Incorrecto()`. `PlanetData.Cometa()` tiene los datos del Halley.
  - Voz (agregada el 2026-11-16):
    - `Narrador`: texto a voz.
      - Android: `android.speech.tts.TextToSpeech` por JNI, con `AndroidJavaProxy` para `OnInitListener`. Prueba es-PE, luego es-US, es-ES y es. Usa `speak(texto, QUEUE_FLUSH, Bundle vacío, id)` y `isSpeaking`.
      - Windows: un proceso de `powershell.exe` 5.1 con `-EncodedCommand` que usa System.Speech con la primera voz `es*` (Sabina Desktop). Recibe cada línea en base64 UTF-8 por stdin; una línea vacía corta la lectura. En Windows, `Hablando` se estima con la longitud del texto (14 caracteres/s).
      - Se silencia con el botón Sonido.
    - `ReconocimientoVoz`:
      - Windows: `KeywordRecognizer` de Unity (lista cerrada de frases generada por `ComandosVoz.FrasesWindows`, `ConfidenceLevel.Low`). Usa el idioma de voz del sistema, que el usuario debe poner en Español (México).
      - Android: `SpeechRecognizer` por JNI en el hilo de UI (`runOnUiThread`), con `RecognitionListener` como `AndroidJavaProxy`, es-PE en forma libre. Se reinicia tras cada resultado o error (salvo el error 9, falta de permiso). Pide `Permission.Microphone`.
      - Los resultados se encolan con lock y se entregan en `Update`. Ignora lo que oye mientras `Narrador.Hablando` y hasta 0,6 s después.
    - `ComandosVoz`: normaliza el texto (minúsculas, sin tildes) y busca palabras clave. El quiz tiene prioridad: decir un planeta es responder. También actualiza el cartel `CartelVoz`.
    - Ganchos de narración: `PlanetSelector.Seleccionar` lee la ficha y `Deseleccionar` llama a `Callar`. `QuizPlanetas` lee la pregunta, la respuesta y el resultado, y tiene `ResponderPorNombre`, `LeerPregunta` y un `Responder` que devuelve bool. `ModoVisor.Elegir` lee la ficha. `RecorridoGuiado` espera a que termine la narración.
    - `Assets/Plugins/Android/AndroidManifest.xml`: manifest propio basado en la plantilla de Unity 2022.3. Agrega `RECORD_AUDIO` y `<queries>` para `android.speech.RecognitionService` y `TTS_SERVICE`, necesarios desde Android 11.
    - En esta PC se instaló `Language.Speech~~~es-MX` con elevación. `Language.TextToSpeech~~~es-MX` ya estaba.
  - `SistemaSolarSetup.CompilarPC` pone `runInBackground = true`. Sin eso, el `.exe` se congela al perder el foco, y las capturas fallaban a veces por eso.
  - `AnchoSegunDistancia`: ajusta el ancho de las órbitas y estelas según la distancia a la cámara.
  - `PlanetOrbit`, `PlanetInfo` (crece y brilla), `InfoPanelUI`, `SolarSystemController`, `Procedural`, `SonidoEspacial` (audio con `AudioClip.Create`).
  - `AparicionSistema`: animación de entrada. Arranca cuando `Renderer.enabled` del Sol está encendido, es decir, al iniciar.
  - `CapturaAutomatica`: solo actúa si el `.exe` se abre con `-capturas <carpeta>`. Guarda 10 capturas de 1280x720 (aparición, sistema, Tierra, Saturno, Sol, cometa, quiz, respuesta, visor y vista general) en unos 27 s. Si el usuario toca el teclado mientras corre, la cámara se mueve.
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
- La versión VR **compila sin errores** (.exe y APK) con: visor VR, recorrido, quiz, cometa, nombres y distancia real. Se revisó todo con capturas.
- Voz verificada en PC con comandos simulados en las capturas 8 y 9: el `KeywordRecognizer` arranca (`disponible=True activa=True`) y el narrador carga Sabina. Falta probarla hablando de verdad y el código de Android en un teléfono (compila).
- Sin probar en un celular real: el giroscopio (la conversión `Euler(90,0,0) * (x, y, -z, -w)` en LandscapeLeft), el modo visor dentro de un Cardboard (sin corrección de distorsión), los botones Avanzar/Retroceder, el pellizco y el rendimiento (unas 2200 partículas).
- La versión VR está en un commit local de `main`. No se ha subido nada a GitHub: preguntar antes de hacer push.

## Próximos pasos
1. Que el usuario pruebe el `.exe` y el APK y cuente cómo le va, en especial el giroscopio y el visor.
2. Extras que quedan: escala real y corrección de lentes del Cardboard.
3. Exposición: actualizar las diapositivas (de RA a RV), agregar referencias (NASA, Solar System Scope) y llevar el `.exe`, el APK y un video de respaldo.

## Si se abre en otra computadora
- Instalar Unity Hub (`winget install Unity.UnityHub`) y el editor 2022.3.62f3, changeset `96770f904ca7`, con el módulo de Android:
  `"<Unity Hub.exe>" -- --headless install --version 2022.3.62f3 --changeset 96770f904ca7 --module android --childModules`
  - Unity Hub de winget es MSIX: el exe está en `(Get-AppxPackage *UnityHub*).InstallLocation\app\Unity Hub.exe`.
- No copiar las carpetas `SistemaSolarRA/Library`, `Temp`, `Logs` ni `obj`: Unity las regenera.
