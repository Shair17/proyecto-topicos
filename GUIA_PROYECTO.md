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
3. Importen Vuforia: usen **Assets > Import Package > Custom Package**, elijan `add-vuforia-package-*.unitypackage` y acepten todo.
4. Importen la base de datos del cubo (`CuboSistemaSolar.unitypackage`) de la misma forma.
5. Abran **Window > Vuforia Configuration** y peguen la **App License Key**.
6. Usen el menú **Sistema Solar > 1. Crear escena**. Crea la AR Camera, el Multi Target, los materiales, el sistema solar y la interfaz, y guarda la escena.
7. Usen el menú **Sistema Solar > 2. Configurar Android**. Deja el proyecto listo para Android; el cambio de plataforma puede tardar.
8. Seleccionen **MultiTarget** y, en el Inspector, revisen que estén elegidos la base `CuboSistemaSolar` y el target `CuboSolar`.
9. **Revisen la orientación.** En la vista Scene se ve el cubo con sus 6 caras. Compárenlo con el cubo armado: los números de caras vecinas deben coincidir. Si una cara está en otro lugar o girada, corrijan esa imagen en Target Manager y descarguen la base de nuevo.

**Texturas reales de los planetas (opcional, pero se ve mucho mejor):** descárguenlas de https://www.solarsystemscope.com/textures/ (licencia CC BY 4.0: hay que citar la fuente en la exposición). Pónganlas en `Assets/Texturas` **antes** del paso 6 y el menú las asigna solo según el nombre del archivo (`2k_mars`, `2k_jupiter`, etc.).

Si el sistema se ve muy grande o muy chico, seleccionen **SistemaSolar** (dentro de MultiTarget) y cambien **Radio Sistema** y **Tamaño Base**.

## 6. Probar en la PC
Conecten una webcam y presionen **Play**. Vuforia usa la webcam. Muestren el cubo impreso: debe aparecer el sistema solar, y con clic sobre un planeta se abre su ficha.

## 7. Instalar en el celular (Android)
1. En el celular activen **Opciones de desarrollador** y luego **Depuración USB**, y conéctenlo con un cable.
2. En Unity usen **File > Build And Run**.

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
- **Audio**: narración para cada planeta.

## 10. Para la exposición
- Cambien la diapositiva 14. En lugar de "Modelo para nuestro proyecto: Merge Cube y Dynamics 365 Guides", pongan: **"Modelo para nuestro proyecto: la interacción con marcadores de Merge Cube"**.
- Fuentes que deben citar: NASA Planetary Fact Sheet (datos de los planetas), Solar System Scope (texturas, si las usan), y la documentación de Vuforia Engine.
