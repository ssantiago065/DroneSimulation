# Simulación de Drones Multiagente con Búsqueda por IA

Este proyecto es una simulación desarrollada en Unity que demuestra el comportamiento de un sistema multiagente cooperativo. Tres drones autónomos son desplegados en un entorno 3D para localizar a una persona u objeto basándose en una descripción en lenguaje natural.

## Descripción del Funcionamiento

La simulación sigue un ciclo de misión completo:
1.  **Despliegue:** Tres drones despegan de sus bases y forman un cerco de vigilancia alrededor del área de búsqueda.
2.  **Análisis:** Cada dron escanea el área de forma autónoma, captura imágenes de posibles objetivos y las envía a un servidor de IA externo.
3.  **Inteligencia Artificial:** Un servidor en Python, utilizando el modelo de Visión y Lenguaje **CLIP**, analiza cada imagen junto con la descripción del objetivo para generar un índice de confianza.
4.  **Cooperación:** Los drones reportan sus hallazgos a un gestor central en Unity, que utiliza un sistema de votación **Borda Count** para fusionar los datos y determinar el candidato más probable.
5.  **Aterrizaje:** Una vez que el candidato supera un umbral de confianza, el dron más cercano recibe la orden de aterrizar en una zona segura cerca del objetivo para completar la misión.

## Versiones y Requisitos

Para poder configurar y ejecutar este proyecto, necesitarás el siguiente software:

#### **Entorno de Simulación**
* **Unity Hub**
* **Editor de Unity:** `6000.0.45f1` (LTS)
* **Paquetes de Unity Requeridos:**
    * `AI Navigation`
    * `Newtonsoft.Json`

#### **Servidor de Inteligencia Artificial**
* **Python:** `3.12.3` o superior.
* **Librerías de Python:** Las dependencias exactas se encuentran en el archivo `requirements.txt` dentro de la carpeta del servidor. Las principales son:
    * `Flask`
    * `transformers`
    * `torch`
    * `Pillow`

Para instrucciones detalladas sobre la instalación y ejecución, por favor, consulta la guía completa de instalación.
