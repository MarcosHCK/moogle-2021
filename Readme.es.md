# Moogle!

![](MoogleServer/Resources/Svg/org.hck.moogle.svg)

> Proyecto de Programación I. Facultad de Matemática y Computación. Universidad de La Habana. Curso 2021.

> Marcos Antonio Pérez Lorenzo. C112

## `MoogleEngine` classlib

Mi propuesta de proyecto consiste en tres clases principales

### Estructura interna

- `Corpus`: es donde se almacenan todas los datos necesarios para ejecutar una búsqueda. Almacena todas la palabras contenidas en la estructura abstracta homónima parte del modelo TF-IDF. Anidados dentro de esta clases tenemos
  - `Corpus`.`Document`: Almacena los datos de un documento individual
  - `Corpus`.`Factory`: Se encarga de construir un objeto `Corpus` a partir de conjuntos de datos específicos
  - `Corpus`.`Query`: Contiene toda la lógica necesaria para ejecutar una búsqueda. También contiene algunos tipos anidados
    - `Corpus`.`Query`.`ImportanceOperator`: implementa la lógica del operador de relevancia '`*`' (soporte completo para múltiples instancias por término)
    - `Corpus`.`Query`.`Morph`: clase auxiliar para llevar constancia de las transformaciones hechas a la expresión de búsqueda. Usado principalmente para generar la sugerencia.
    - `Corpus`.`Query`.`MustExistsOperator`: implementa la lógica del operador de existencia '`^`'
    - `Corpus`.`Query`.`MustNotExistsOperator`: implementa la lógica del operador de no existencia '`!`'
    - `Corpus`.`Query`.`Operator`: clase base para la implementación de los operadores
    - `Corpus`.`Query`.`Operator`.`GlyphAttribute`: atributo necesario al implementar un operador para especificar el símbolo que denota el operador (sí, por ahora los operadores están *limitados a solo un caracter*) y si deben usarse junto a una palabra o no
    - `Corpus`.`Query`.`ProximityOperator`: implementa la lógica del operador de cercanía '`~`'
    - `Corpus`.`Query`.`Word`: Almacena los datos asociados a un término de búsqueda
  - `Corpus`.`Word`: Almacena los datos asociados a cada término presente en el `corpus`, así como su localización dentro de este
- `Loader`: Clase base para la implementación de los `cargadores`. Un `cargador` es una clase que se encarga de procesar los datos pasados a `Corpus`.`Factory` para incluírse en el `corpus`. Cada uno está asociado a un formato específico usando su tipo `MIME` (o *Multipurpose Internet Mail Extension*, se usa para denotar el formato de un conjunto de datos)
  - `Loader`.`MimeTypeAttribute`: atributo usado para especificar el formato que un `cargador` soporta usando su tipo `MIME`
  - `PlainLoader`: actualmente el `cargador` para el único formato suportado por mi propuesta. Carga archivos de texto sin formato, o sea, texto plano. Su tipo `MIME` es '`text/plain`'
- `SearchEngine`: este es el punto de entrado a esta *classlib*
  - `SearchItem`: en esta clase se almacenan los resultados individuales de una búsqueda
  - `SearchResult`: en esta clase se almacena el resultado de una búsqueda concreta
- `Utils`: contiene código no específico que se utiliza a todo lo largo de la *classlib*

## Detalles del proceso de carga

Cuando la *classlib* se inicia mediante una llamada a `SearchEngine`.`Preload()`, la carpeta apuntada en la propiedad `SearchEngine`.`Source` se carga recursivamente en busca de documentos para cargarlos dentro del `corpus`. Cada uno es analizado para determinar su tipo `MIME`, con el cual de busca un cargador compatible para su formato. Usando dicho cargador, se tokeniza (se transforma en un conjunto de términos), los cuales se asocian a un `Corpus`.`Word` y a un `Corpus`.`Document`.

## Detalles del proceso de búsqueda

Cuando se realiza una búsqueda llamando al método `SearchEngine`.`Query()`, esta se descompone en términos individuales. Cada término (nótese que un término denota al conjunto de las palabras individuales que ocurren una o más veces en una búsqueda) es almacenado en una clase `Corpus`.`Query`.`Word`, en la que además se almacena entre otras cosas los delegados que corresponden a los operadores que pudieran estar asociados al término. Cuando el preprocesado termina, se recorren los documentos individuales `Corpus`.`Document` contenidos en el `corpus`, y se calcula la similitud entre la búsqueda y el documento usando el **modelo vectorial** de mineria de datos (concretamenta mediante la **similitud cosénica** entre los vectores formados por el documento y la búsqueda usando el calculo **TF-IDF**). Después de que un documento obtiene su similitud entre este y la búsqueda, de ahora en adelante denominado `score`, cada término de la búsqueda es procesado para transformar el score usando las reglas instaladas por los operadores. Luego, el documento de mayor score se usa para calcular un corte mínimo para deshacerse de los documentos poco coincidentes y para normalizar los resultados (todos los scores se dividen entre el máximo y se obtiene una proporción de lo grande que es con respecto al mayor, que desde luego está entre 1 y 0). Para terminar, los documentos son transformados en `SearchItem` y ordenados y se calcula una sugerencia (no siempre, depende de si la búsqueda es precisa o no), con lo que se construye un objeto `SearchResult`, el cual es el valor de retorno de `SearchEngine`.`Query()`

## Sobre la interfaz gráfica

Elegí usar una interfaz gráfica (GUI) basada en `GTK+` porque esta es bastante más rápida que la interfaz web que el proyecto posee, pero esta no es totalmente necesaria, por los sientase en su derecho de remplazarla por la variante web, o cualquier otra. La única concesión necesaria para usar la interfaz basada en `GTK+` fue implementar la búsqueda y la carga de forma **asíncrona**, aunque de ninguna forma esto debe convertirse en un inconveniente para otras interfaces.

## Extensibilidad y mantenibilidad

Desde el inicio mi proyecto fue desarrollado con la extensibilidad en mente. El motor de búsqueda (en la classlib `MoogleEngine`) es completamente modular y sus componentes están bien delimitados, por lo que cualquier parte de esta puede ser modificada e incluso suprimida. Además, su diseño jerárquico posibilita la implementación de nuevos `cargadores`, implementando un tipo derivado de la clase `Loader` y anotado con el tipo `MIME` que soporta usando el atributo `Loader`.`MimeTypeAttribute`, o de operadores adicionales derivando de `Corpus`.`Query`.`Operator` y anotando con el atributo `Corpus`.`Query`.`Operator`.`GlyphAttribute`.
Todos los tipos adicionales son cargados bajo demanda cuando se invoca el método `SearchEngine`.`Preload()`, por lo que oportunísticamente se pueden cargar assemblies adicionales antes de esto.
Finalmente, una nota sobre la mentenibilidad. Diseñé la estructura interna de la *classlib* para que se puedan extender los procesos internos sin muchos cambios estructurales. Aún así, no puedo preveer todos los cambios que podrían ser necesarios, así que para que fuera lo más mantenible posible tuve que sacrificar algo de velocidad (en especial durante la `carga`).
