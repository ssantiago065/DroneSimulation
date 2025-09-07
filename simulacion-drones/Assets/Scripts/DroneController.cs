// DroneController.cs
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DroneController : MonoBehaviour
{
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
        }
    }
}