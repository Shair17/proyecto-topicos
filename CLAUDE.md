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
    - `PlanetOrbit`, `PlanetInfo`, `PlanetSelector`, `InfoPanelUI`, `SolarSystemController`.
    - `CubeTrackingUI`: no usa la API de Vuforia. Mira `Renderer.enabled` del Sol, que Vuforia apaga cuando pierde el cubo, para que el proyecto compile aunque Vuforia no esté importado.
    - `AppUIBuilder`: crea el Canvas y conecta todo por código.
  - `Assets/Editor/SistemaSolarSetup.cs`: menú **Sistema Solar > 1. Crear escena** y **2. Configurar Android**.
    - Usa `EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/AR Camera" | ".../Multi Target")`.
    - Pone el filtro de estado del DefaultObserverEventHandler en "Tracked" mediante SerializedObject.
    - Asigna solas las texturas que estén en `Assets/Texturas`.
  - `Packages/manifest.json` y `ProjectSettings/ProjectVersion.txt` se escribieron a mano. Unity genera el resto al abrir el proyecto.
- `Marcadores/`: 6 caras (`cara_front`…`cara_bottom`) para subir a Vuforia Target Manager (Cuboid de 0.06 m) y `cubo_imprimible_A4.png`.
- `Herramientas/generar_marcadores.py`: regenera las caras. Requiere `pip install pillow`. Acepta el lado del cubo en cm como argumento.

## Estado (al 2026-10-23)
- Hecho: los scripts, el menú de un clic, los marcadores, la guía, y la instalación de Unity Hub y Unity 2022.3.62f3 con Android **en la PC original**.
- **Nada se ha compilado ni probado todavía.** Al ejecutar Unity en batchmode falló con "No valid Unity Editor license found": falta que el usuario inicie sesión en Unity Hub.
- Sin verificar:
  - Que la orientación de las caras en la plantilla coincida con la convención de cuboides de Vuforia. La guía pide comprobarlo en el paso 5.9.
  - Las rutas de menú de Vuforia y el nombre del campo StatusFilter.
  - Las estrellas (*augmentable rating*) de las caras.

## Próximos pasos
1. Cuando el usuario haya iniciado sesión en Unity Hub, compilar en batchmode y corregir los errores:
   `"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -quit -projectPath <ruta>\SistemaSolarRA -logFile <log>`, y buscar `error CS` en el log.
2. El usuario tiene que crear la cuenta de Vuforia, la licencia y la base de datos `CuboSistemaSolar` (target `CuboSolar`), imprimir el cubo e importar los paquetes. Luego usar los menús 1 y 2.
3. Para la exposición: APK en un celular Android, pantalla proyectada con scrcpy, Play con webcam en Unity como respaldo, y un video grabado por si falla algo.
4. Extras opcionales que se ofrecieron: modo guiado (Anterior/Siguiente), quiz, audio, y una diapositiva de referencias para la exposición.

## Si se abre en otra computadora
- Instalar Unity Hub (`winget install Unity.UnityHub`) y el editor 2022.3.62f3, changeset `96770f904ca7`, con el módulo de Android:
  `"<Unity Hub.exe>" -- --headless install --version 2022.3.62f3 --changeset 96770f904ca7 --module android --childModules`
  - Unity Hub de winget es MSIX: el exe está en `(Get-AppxPackage *UnityHub*).InstallLocation\app\Unity Hub.exe`.
- No copiar las carpetas `SistemaSolarRA/Library`, `Temp`, `Logs` ni `obj`: Unity las regenera.
