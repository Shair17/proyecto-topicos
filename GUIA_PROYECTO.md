# Sistema Solar en Realidad Virtual

**Idea:** el usuario flota dentro del sistema solar. Puede moverse libremente entre los planetas, mirar a su alrededor y "viajar" volando hasta cualquiera de ellos, que luego sigue en su órbita. Al tocar o hacer clic en un planeta se abre una ficha con sus datos. Hay botones para pausar las órbitas, cambiar su velocidad y volver a la vista general.

**Tecnología:** Unity 2022.3 LTS (2022.3.62f3), sin visor. Funciona en **PC (Windows, .exe)** y en **celular (Android, .apk)**.

**Tipo de realidad virtual:** es **RV no inmersiva** (también llamada *RV de escritorio*). El usuario explora un mundo 100 % virtual a través de una pantalla, sin visor. En el celular, el **giroscopio** hace que al girar el teléfono la vista gire igual, como si fuera una ventana al espacio. La versión con visor (RV inmersiva, por ejemplo con Google Cardboard) se podría agregar como extra.

> La versión anterior de realidad aumentada (con Vuforia y el cubo de papel) está guardada en la rama de git `realidad-aumentada`.

## Qué hay en esta carpeta

| Carpeta / archivo | Para qué sirve |
|---|---|
| `SistemaSolarRA/` | **El proyecto de Unity**, con los scripts y el menú que arma la escena. *(El nombre de la carpeta viene de la versión anterior; no afecta.)* |
| `SistemaSolarRA/Builds/PC/SistemaSolarVR.exe` | La app para Windows. |
| `SistemaSolarRA/Builds/Android/SistemaSolarVR.apk` | La app para celulares Android. |

Qué hace cada script (en `SistemaSolarRA/Assets/Scripts`):
- **PlanetData**: datos de los planetas, el Sol y la Luna (diámetro, distancia, lunas, dato curioso). Fuente: NASA.
- **SolarSystemBuilder**: crea el Sol, los planetas, las órbitas, el anillo de Saturno, el cinturón de asteroides y las estrellas.
- **PlanetOrbit**: mueve cada planeta alrededor del Sol y sobre su propio eje.
- **CamaraExplorador**: la "nave" del usuario. Se mueve con el teclado o con los botones, mira con el mouse, el dedo o el giroscopio, y viaja volando a los planetas.
- **PlanetInfo** y **PlanetSelector**: detectan qué planeta se tocó y lo resaltan.
- **InfoPanelUI**: muestra la ficha con los datos del planeta.
- **SolarSystemController**: maneja el botón de pausa y el control de velocidad.
- **AppUIBuilder**: crea toda la interfaz y conecta los scripts anteriores.
- **Otros**:
  - `AparicionSistema`: animación de entrada.
  - `SonidoEspacial`: música y efectos generados por código.
  - `AnchoSegunDistancia`: mantiene finas las líneas de órbita.
  - `BotonMantener`: botones Avanzar y Retroceder.
  - `Procedural`: brillos, partículas y la malla del anillo.
- **Editor/SistemaSolarSetup**: agrega el menú **Sistema Solar** en Unity, que arma la escena y compila.

---

## 1. Usar la app (ya está compilada)

### En la PC
Abran `SistemaSolarRA/Builds/PC/SistemaSolarVR.exe`. Para pantalla completa, presionen **Alt + Enter**.

| Acción | Control |
|---|---|
| Moverse | **W A S D** o flechas |
| Bajar / subir | **Q / E** |
| Ir más rápido | Mantener **Shift** |
| Mirar alrededor | **Arrastrar** con el mouse |
| Avanzar / retroceder rápido | **Rueda** del mouse |
| Ver la ficha de un planeta | **Clic** sobre él |
| Viajar al planeta | Botón **Viajar hasta aquí** de la ficha |
| Acercarse o alejarse del planeta que se sigue | Rueda del mouse |
| Volver al inicio | Botón **Vista general** |

> Si copian el `.exe` a otra computadora, copien **toda la carpeta** `Builds/PC`, no solo el `.exe`.

### En el celular (Android)
1. Pasen `SistemaSolarVR.apk` al celular por cable, Google Drive o WhatsApp (como *documento*) y ábranlo.
2. Android pedirá permiso para "instalar apps de origen desconocido": acéptenlo. Si aparece **Play Protect**, toquen *Más detalles* y luego *Instalar de todas formas*.
3. Abran **SistemaSolarVR**. La app se usa en **horizontal**.

| Acción | Control |
|---|---|
| Mirar alrededor | **Girar el celular** (giroscopio) o **arrastrar un dedo** |
| Avanzar / retroceder | Botones **Avanzar** y **Retroceder** (mantener presionados), o **pellizcar** con dos dedos |
| Ver la ficha de un planeta | **Tocarlo** (un toque corto) |
| Viajar al planeta | Botón **Viajar hasta aquí** |
| Encender o apagar el giroscopio | Botón **Giroscopio**. Al encenderlo se recentra hacia donde apunta el teléfono. |

## 2. Abrir y modificar el proyecto en Unity
1. Abran **Unity Hub** e inicien sesión con una cuenta Unity gratuita.
2. Usen **Add**, luego **Add project from disk**, y elijan la carpeta `SistemaSolarRA`. Ábranlo. La primera vez tarda unos minutos.
3. Si aparece el aviso **Import TMP Essentials**, acéptenlo.
4. Abran la escena `Assets/Scenes/SistemaSolar` y presionen **Play** para probar dentro de Unity.
5. Menú **Sistema Solar**:
   - **1. Crear escena**: vuelve a armar la escena desde cero (cámara, sistema solar, interfaz).
   - **2. Compilar para PC (.exe)**: genera `Builds/PC/SistemaSolarVR.exe`.
   - **3. Compilar APK (Android)**: genera `Builds/Android/SistemaSolarVR.apk`. Tarda varios minutos.

**Texturas:** están en `Assets/Texturas` (planetas, Sol, Luna, nubes, anillo de Saturno y cielo estrellado). Son de https://www.solarsystemscope.com/textures/, con licencia CC BY 4.0, así que **hay que citar la fuente en la exposición**.

## Qué hace la app (para contarlo en la exposición)
- **Inmersión:** el usuario está *dentro* del sistema solar, rodeado de estrellas, y lo recorre con libertad. En el celular, girar el teléfono gira la vista.
- **Viaje a los planetas:** al elegir "Viajar hasta aquí", la cámara vuela hasta el planeta y llega por su lado de día. Luego lo acompaña en su órbita y se puede mirar a su alrededor.
- **Aparición animada:** al abrir la app, el Sol aparece con un rebote y los planetas salen uno por uno hacia sus órbitas, con sonido.
- **El Sol ilumina de verdad:** es la única luz de la escena, así que cada planeta tiene un lado de día y otro de noche. Además tiene resplandor, una corona de chispas y un halo.
- **Detalles reales:**
  - Texturas de Solar System Scope y datos de la NASA.
  - La inclinación real del eje: Urano gira "acostado" y Venus está "de cabeza".
  - Nubes y atmósfera en la Tierra.
  - La Luna, que siempre muestra la misma cara.
  - El anillo de Saturno.
  - El cinturón de asteroides.
- **Interacción:** al tocar un planeta, crece, brilla y se abre su ficha con una animación. La Luna también se puede tocar.
- **Sonido:** música ambiental, un sonido al tocar y otro al aparecer. Todo se genera por código y se apaga con el botón **Sonido**.

## Problemas frecuentes
| Problema | Solución |
|---|---|
| No aparece el menú "Sistema Solar" | Hay errores en la consola (ventana *Console*). Pásenme el mensaje. |
| El texto de la interfaz no aparece | Falta importar **TMP Essentials** (*Window > TextMeshPro > Import TMP Essential Resources*). |
| En el celular la vista gira raro o "al revés" | Toquen **Giroscopio** dos veces para recentrar, o apáguenlo y usen el dedo. |
| Me perdí en el espacio | Botón **Vista general**. |
| El celular va lento | Cierren otras apps. Si sigue lento, avísenme y bajo la calidad de las texturas. |

## Mejoras opcionales (si sobra tiempo)
- **Modo con visor (Google Cardboard)**: RV inmersiva con el celular dentro de un visor de cartón.
- **Recorrido guiado**: botones Anterior y Siguiente que viajan por los planetas en orden.
- **Quiz**: preguntas al final, como "¿Cuál es el planeta más caliente?".
- **Narración**: una voz que lea los datos de cada planeta.
- **Escala real**: un modo que muestre las distancias reales para comparar.

## Para la exposición
- Expliquen el **tipo de RV**: no inmersiva (pantalla de PC o celular con giroscopio). Compárenla con la RV inmersiva (visores como Meta Quest).
- Las diapositivas actuales hablan de realidad aumentada. Revisen la diapositiva del "modelo para nuestro proyecto" (Merge Cube era un ejemplo de RA).
- **Fuentes que deben citar:**
  - NASA Planetary Fact Sheet (datos de los planetas)
  - Solar System Scope (texturas, CC BY 4.0)
- **Lleven:**
  - El `.exe` en la laptop conectada al proyector.
  - El APK instalado en un celular. Para proyectarlo usen **scrcpy**.
  - Un video grabado por si falla algo.
- **Para impresionar:** pásenle el celular a alguien del público para que mire alrededor girándolo.
