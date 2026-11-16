# Sistema Solar en RA con cubo marcador (estilo Merge Cube)

**Idea:** el usuario sostiene un cubo de papel impreso. Al apuntarle con el celular aparece el Sol en el lugar del cubo, con los 8 planetas girando a su alrededor. Si gira el cubo, el sistema solar gira con él. Al tocar un planeta se abre una ficha con sus datos. Hay botones para pausar las órbitas y cambiar su velocidad.

**Tecnología:** Unity 2022.3 LTS (2022.3.62f3), Vuforia Engine (Multi Target con forma de cubo) y Android.

## Qué hay en esta carpeta

| Carpeta / archivo | Para qué sirve |
|---|---|
| `SistemaSolarRA/` | **El proyecto de Unity**, con los scripts y el menú que arma la escena. |
| `Marcadores/cara_*.png` | Las 6 caras del cubo. Se suben a Vuforia. |
| `Marcadores/cubo_imprimible_A4.png` | La plantilla para imprimir y armar el cubo (6 cm de lado). |
| `Herramientas/generar_marcadores.py` | Vuelve a generar las caras, por ejemplo con otro tamaño: `python generar_marcadores.py 7` |

Qué hace cada script (en `SistemaSolarRA/Assets/Scripts`):
- **PlanetData**: datos de los planetas y del Sol (diámetro, distancia, lunas, dato curioso).
- **SolarSystemBuilder**: crea el Sol, los planetas, las órbitas y el anillo de Saturno de forma automática.
- **PlanetOrbit**: mueve cada planeta alrededor del Sol y sobre su propio eje.
- **PlanetInfo** y **PlanetSelector**: detectan qué planeta se tocó y lo resaltan.
- **InfoPanelUI**: muestra la ficha con los datos del planeta.
- **SolarSystemController**: maneja el botón de pausa y el control de velocidad.
- **CubeTrackingUI**: muestra el mensaje "Apunta la cámara al cubo" cuando la app no ve el cubo.
- **AppUIBuilder**: crea toda la interfaz y conecta los scripts anteriores.
- **Editor/SistemaSolarSetup**: agrega el menú **Sistema Solar** en Unity, que arma la escena y configura Android.

---

## Resumen: lo que tienen que hacer ustedes

1. Iniciar sesión en Unity Hub (paso 1).
2. Crear la cuenta de Vuforia, la licencia y la base de datos del cubo (pasos 2 y 3).
3. Imprimir y armar el cubo (paso 4).
4. Abrir el proyecto, importar 2 paquetes, pegar la licencia y usar 2 menús (paso 5).
5. Probar (pasos 6 y 7).

---

## 1. Unity (ya instalado en esta PC)
Unity Hub y Unity **2022.3.62f3**, con el módulo de Android, ya están instalados.
1. Abran **Unity Hub** desde el menú Inicio.
2. Inicien sesión con una cuenta Unity gratuita (o créenla ahí mismo).
3. Cuando lo pida, acepten la licencia **Personal**.

## 2. Cuenta y licencia de Vuforia
1. Créense una cuenta en https://developer.vuforia.com
2. Vayan a **Plan & Licenses** y generen una **Basic License**, que es gratuita. Copien la *license key*.
3. En la sección **Downloads**, descarguen **Vuforia Engine for Unity**. Es un archivo `add-vuforia-package-X-Y-Z.unitypackage`.

> La licencia gratuita deja una marca de agua de Vuforia en la app. Para un proyecto del curso no es problema.

## 3. Crear el cubo en Vuforia (Target Manager)
1. Vayan a **Target Manager**, luego **Generate Database**. Pónganle el nombre `CuboSistemaSolar` y elijan el tipo **Device**.
2. Dentro de la base de datos usen **Add Target** y elijan el tipo **Cuboid**.
   - Width = **0.06**, Height = **0.06**, Length = **0.06**. Son metros: así el cubo mide 6 cm.
   - Name: `CuboSolar`
3. Suban cada imagen en su cara:

   | Cara en Vuforia | Archivo |
   |---|---|
   | Front | `cara_front.png` (número 1) |
   | Back | `cara_back.png` (2) |
   | Left | `cara_left.png` (3) |
   | Right | `cara_right.png` (4) |
   | Top | `cara_top.png` (5) |
   | Bottom | `cara_bottom.png` (6) |

4. Revisen las **estrellas** (*augmentable rating*) de cada cara. Deben tener 4 o 5. Si alguna tiene menos, avísenme y genero otra versión de esa cara.
5. Usen **Download Database**, elijan **Unity Editor** y descarguen el `.unitypackage`.

## 4. Imprimir y armar el cubo
1. Impriman `cubo_imprimible_A4.png` **al 100 %**, sin la opción "Ajustar a la página". Mejor en cartulina o papel grueso, y mate (el papel brillante produce reflejos).
2. Midan con una regla: cada cara debe medir **6 cm**. Si mide otra cosa, cambien el tamaño en Vuforia para que coincida.
3. Recorten, doblen y peguen las pestañas grises por dentro.

## 5. Abrir el proyecto y armar la escena
1. En Unity Hub usen **Add**, luego **Add project from disk**, y elijan la carpeta `SistemaSolarRA`. Ábranlo. La primera vez tarda unos minutos.
2. Si aparece el aviso **Import TMP Essentials**, acéptenlo. Si no aparece, usen **Window > TextMeshPro > Import TMP Essential Resources**.
3. Importen Vuforia: usen **Assets > Import Package > Custom Package**, elijan `add-vuforia-package-*.unitypackage` y acepten todo. Cuando pregunte **Update**, acepten. *(En esta PC ya está hecho: Vuforia 11.4.4 está en `Packages/`.)*
4. Importen la base de datos del cubo (`CuboSistemaSolar.unitypackage`) de la misma forma.
5. **Licencia:** guarden la clave en `licencia-vuforia.txt`, junto a la carpeta `SistemaSolarRA` (ese archivo no se sube a git). El menú del paso 6 la carga sola. Si prefieren, péguenla a mano en **Window > Vuforia Configuration**.
6. Usen el menú **Sistema Solar > 1. Crear escena**. Crea la AR Camera, el Multi Target, los materiales, el sistema solar y la interfaz, y guarda la escena.
7. Usen el menú **Sistema Solar > 2. Configurar Android**. Deja el proyecto listo para Android; el cambio de plataforma puede tardar.
8. Seleccionen **MultiTarget** y, en el Inspector, revisen que estén elegidos la base `CuboSistemaSolar` y el target `CuboSolar`.
9. **Revisen la orientación.** En la vista Scene se ve el cubo con sus 6 caras. Compárenlo con el cubo armado: los números de caras vecinas deben coincidir. Si una cara está en otro lugar o girada, corrijan esa imagen en Target Manager y descarguen la base de nuevo.

**Texturas:** ya están en `Assets/Texturas`: los planetas, el Sol, la Luna, las nubes de la Tierra, el anillo de Saturno y el cielo estrellado de la vista previa. Son de https://www.solarsystemscope.com/textures/, con licencia CC BY 4.0, así que **hay que citar la fuente en la exposición**. El menú del paso 6 las asigna solo según el nombre del archivo.

Si el sistema se ve muy grande o muy chico, seleccionen **SistemaSolar** (dentro de MultiTarget) y cambien **Radio Sistema** y **Tamaño Base**.

## 6. Probar en la PC
- **Sin cubo ni Vuforia:** abran `SistemaSolarRA/Builds/VistaPreviaPC/SistemaSolar.exe`, o en Unity usen el menú **Sistema Solar > 0. Vista previa en PC (sin RA)** y presionen Play. Para girar la cámara usen clic derecho y arrastren; la rueda acerca y aleja. Si no la tocan, gira sola.
- **Con RA:** conecten una webcam y presionen **Play** en la escena `SistemaSolar`. Vuforia usa la webcam. Muestren el cubo impreso: debe aparecer el sistema solar, y con clic sobre un planeta se abre su ficha.

## 7. Crear el APK e instalarlo en el celular (Android)
El APK se compila **una sola vez**. Después queda instalado como cualquier app y no hay que volver a compilar para mostrarlo.
1. En Unity usen el menú **Sistema Solar > 3. Compilar APK**. Al terminar se abre la carpeta `SistemaSolarRA/Builds/Android/` con `SistemaSolarRA.apk`.
2. Pasen el APK al celular por cable, Google Drive o WhatsApp y ábranlo. Android pedirá permiso para "instalar apps de origen desconocido": acéptenlo.
3. Abran la app, denle permiso de cámara y apunten al cubo.

Otra opción: con el celular conectado por USB y la **Depuración USB** activada, usen **File > Build And Run**.

## Qué hace la app (para contarlo en la exposición)
- **Aparición animada:** al detectar el cubo, el Sol aparece desde cero con un rebote y los planetas salen uno por uno hacia sus órbitas, con sonido. Si el cubo se pierde más de un segundo, la animación se repite al volver a verlo.
- **El Sol ilumina de verdad:** es la única luz de la escena, así que cada planeta tiene un lado de día y otro de noche. Además tiene resplandor, una corona de chispas y un halo naranja.
- **Detalles reales:** texturas de la NASA y Solar System Scope, inclinación real del eje (Urano gira "acostado" y Venus está "de cabeza"), nubes y atmósfera en la Tierra, la Luna (que siempre muestra la misma cara), el anillo de Saturno con su textura real y el cinturón de asteroides entre Marte y Júpiter.
- **Estelas y estrellas:** cada planeta deja una estela de su color, y alrededor del cubo hay una cúpula de estrellas que titilan.
- **Interacción:** al tocar un planeta, crece, brilla y se abre su ficha con una animación. La Luna también se puede tocar. Los planetas pequeños tienen un área de toque más grande para que sea fácil tocarlos en el celular.
- **Sonido:** música ambiental espacial, un sonido al tocar y otro al aparecer. Todo se genera por código y se apaga con el botón **Sonido**.

## 8. Problemas frecuentes
| Problema | Solución |
|---|---|
| No aparece el menú "Sistema Solar" | Hay errores en la consola (ventana *Console*). Pásenme el mensaje. |
| El menú dice "Falta Vuforia" | Importen primero el paquete de Vuforia (paso 5.3). |
| No detecta el cubo | Usen más luz, papel mate, y revisen que el cubo esté impreso al tamaño correcto. Comprueben que las caras tengan 4 o 5 estrellas. |
| El sistema aparece torcido o se "salta" | Hay caras mal ubicadas o giradas: revisen el paso 5.9. |
| Los planetas quedan flotando cuando no se ve el cubo | En el MultiTarget, componente **Default Observer Event Handler**, pongan la opción de visibilidad en **Tracked**. |
| No se pueden tocar los planetas | Revisen que la **ARCamera** tenga el tag *MainCamera*. |
| El texto de la interfaz no aparece | Falta importar **TMP Essentials** (paso 5.2). |
| La pantalla queda negra en el celular | Revisen la license key y den permiso de cámara a la app. |

## 9. Mejoras opcionales (si sobra tiempo)
- **Modo guiado** (como Dynamics 365 Guides): botones Anterior y Siguiente que recorren los planetas uno por uno.
- **Quiz**: preguntas al final, como "¿Cuál es el planeta más caliente?".
- **Narración**: una voz que lea los datos de cada planeta.
- **Controlar con el cubo**: por ejemplo, inclinar el cubo para cambiar la velocidad.

## 10. Para la exposición
- Cambien la diapositiva 14. En lugar de "Modelo para nuestro proyecto: Merge Cube y Dynamics 365 Guides", pongan: **"Modelo para nuestro proyecto: la interacción con marcadores de Merge Cube"**.
- Fuentes que deben citar: NASA Planetary Fact Sheet (datos de los planetas), Solar System Scope (texturas, CC BY 4.0) y la documentación de Vuforia Engine.
- Lleven el APK instalado, el `.exe` de vista previa como respaldo y un video grabado por si falla algo. Para proyectar la pantalla del celular usen **scrcpy**.
- **Para impresionar:** denle el cubo al profesor para que él mismo lo gire.
