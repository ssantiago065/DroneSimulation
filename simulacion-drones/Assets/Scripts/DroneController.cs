// DroneController.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.IO;

[RequireComponent(typeof(NavMeshAgent))]
public class DroneController : MonoBehaviour
{
    [Header("Componentes del Dron")]
    public Transform cameraSupport;
    public Camera visionCamera;
    [Header("Parámetros de Escaneo")]
    public float analysisTimePerTarget = 1.0f;
    private float defaultFOV;
    private NavMeshAgent agent;

    private enum DroneState { Idle, Ascending, Cruising, Positioning, Scanning }
    private DroneState currentState;

    private Vector3 finalTargetPosition;
    private float scanningAltitude;
    private float cruisingAltitude;

    void Awake()
    {
        // Inicializamos el NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = true;
        currentState = DroneState.Idle;

        // --- NUEVO: Guardamos el FOV original y nos aseguramos de que la cámara esté apagada ---
        if (visionCamera != null)
        {
            defaultFOV = visionCamera.fieldOfView;
            visionCamera.enabled = false;
        }
    }

    

    // Apuntar la camara hacia unas coordenadas
    public void AimCameraAt(Vector3 targetPosition)
    {
        if (cameraSupport != null)
        {
            cameraSupport.LookAt(targetPosition);
        }
        else
        {
            Debug.LogWarning("El Soporte de camara no está asignado en el DroneController de " + gameObject.name);
        }
    }

    // Calcular encuadre para una persona
    private void FrameTarget(PersonIdentity person)
    {
        if (visionCamera == null || person == null) return;

        // 1. Obtenemos el tamaño del objetivo
        float targetSize = 2.0f; // Usamos un tamaño aproximado de 2m para una persona
        if (person.GetComponent<Collider>() != null)
        {
            targetSize = person.GetComponent<Collider>().bounds.size.y;
        }

        // 2. Calculamos la distancia al objetivo
        float distance = Vector3.Distance(visionCamera.transform.position, person.transform.position);

        // 3. Usamos trigonometría para calcular el FOV necesario
        float fov = 2.0f * Mathf.Atan(targetSize * 0.5f / distance) * Mathf.Rad2Deg;

        visionCamera.fieldOfView = fov;
    }

    // Reiniciar Fov
    private void ResetCameraFOV()
    {
        if (visionCamera != null)
        {
            visionCamera.fieldOfView = defaultFOV;
        }
    }

    // Guardar imagen en archivo png
    private void SaveRenderTextureToFile(RenderTexture rt, string personId)
    {
        // Preparamos los nombres y la ruta de la carpeta
        string folderPath = Path.Combine(Application.persistentDataPath, "DroneCaptures");
        // Creamos la carpeta si no existe
        Directory.CreateDirectory(folderPath);
        string fileName = $"{gameObject.name}_capture_{personId}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string fullPath = Path.Combine(folderPath, fileName);

        // Convertimos la Render Texture a Texture2D, luego a PNG y la guardamos
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);

        // Limpiamos el objeto Texture2D para no consumir memoria
        Destroy(tex);

        Debug.Log($"<color=lime>Captura guardada en: {fullPath}</color>");
    }

    // escanear a todos los objetivos visibles
    private IEnumerator ScanVisibleTargets()
    {
        PersonIdentity[] allPeople = FindObjectsOfType<PersonIdentity>();
        Debug.Log(gameObject.name + " encontró " + allPeople.Length + " personas para analizar.");

        yield return new WaitForSeconds(1f);

        foreach (PersonIdentity person in allPeople)
        {
            Vector3 directionToTarget = person.transform.position - cameraSupport.position;
            RaycastHit hit;

            // Calcular si el objetivo es visible con un RayCast
            if (Physics.Raycast(cameraSupport.position, directionToTarget, out hit))
            {
                if (hit.transform == person.transform)
                {
                    Debug.Log(gameObject.name + " analizando a " + person.gameObject.name);

                    // a. Apuntar la cámara
                    AimCameraAt(person.transform.position);

                    // b. Ajustar el zoom
                    FrameTarget(person);

                    // c. "Tomar la foto" activando y desactivando la cámara
                    visionCamera.enabled = true; // La cámara renderiza a la Render Texture
                    yield return new WaitForEndOfFrame(); // Esperamos a que termine de renderizar
                    //SaveRenderTextureToFile(visionCamera.targetTexture as RenderTexture, person.personID); // Guardar imagen en archivos de juego

                    visionCamera.enabled = false; // La apagamos. La "foto" ya está en la Render Texture.

                    // d. Simular tiempo de análisis (la IA estaría procesando la Render Texture)
                    yield return new WaitForSeconds(analysisTimePerTarget);

                    // e. Restaurar el zoom
                    ResetCameraFOV();

                    // TODO: Aquí es donde pasarías la Render Texture al modelo de Barracuda.
                }
            }
        }
        Debug.Log(gameObject.name + " ha completado su ciclo de escaneo.");
    }


    void Start()
    {
        // Verificamos que el drone esté sobre el NavMesh al iniciar
        if (!agent.isOnNavMesh)
        {
            Debug.LogError(gameObject.name + " no pudo conectarse al NavMesh en su punto de inicio.");
            this.enabled = false;
        }
    }

    // Funcion para iniciar el vuelo hacia el area de la mision
    public void GoToMissionArea(Vector3 destination, float finalAltitude, float cruiseAlt)
    {
        this.finalTargetPosition = destination;
        this.scanningAltitude = finalAltitude;
        this.cruisingAltitude = cruiseAlt;

        currentState = DroneState.Ascending;
        Debug.Log(gameObject.name + " iniciando ascenso a altitud de crucero: " + cruiseAlt + "m");
    }

    // Maquina de estados para controlar las etapas de vuelo del drone
    void Update()
    {
        switch (currentState)
        {
            case DroneState.Ascending:
                HandleAscendingState();
                break;
            case DroneState.Cruising:
                HandleCruisingState();
                break;
            case DroneState.Positioning:
                HandlePositioningState();
                break;
        }
    }

    // Estado de ascenso a altitud de crucero
    private void HandleAscendingState()
    {
        agent.baseOffset = Mathf.Lerp(agent.baseOffset, cruisingAltitude, Time.deltaTime * 1.0f);
        if (Mathf.Abs(agent.baseOffset - cruisingAltitude) < 0.1f)
        {
            agent.baseOffset = cruisingAltitude;
            currentState = DroneState.Cruising;
            agent.SetDestination(finalTargetPosition);
            Debug.Log(gameObject.name + " en altitud de crucero, moviéndose al destino.");
        }
    }

    // Estado de crucero hacia el area de la mision
    private void HandleCruisingState()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = DroneState.Positioning;
            Debug.Log(gameObject.name + " sobre el objetivo, descendiendo a posición de escaneo: " + scanningAltitude + "m");
        }
    }

    // Estado de posicionamiento para alcanzar la altura de escaneo
    private void HandlePositioningState()
    {
        agent.baseOffset = Mathf.Lerp(agent.baseOffset, scanningAltitude, Time.deltaTime * 1.0f);
        if (Mathf.Abs(agent.baseOffset - scanningAltitude) < 0.1f)
        {
            currentState = DroneState.Scanning;
            agent.baseOffset = scanningAltitude;
            agent.enabled = false;
            Debug.Log(gameObject.name + " en posición de escaneo. Listo para analizar.");

            StartCoroutine(ScanVisibleTargets());
        }
    }
}