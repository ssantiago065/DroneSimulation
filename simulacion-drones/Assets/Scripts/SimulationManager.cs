using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[System.Serializable]
public struct XZCoordinate
{
    public float x;
    public float z;

    public Vector3 ToVector3(float y = 0f)
    {
        return new Vector3(x, y, z);
    }
}

public class SimulationManager : MonoBehaviour
{
    //Configuraciones para la simulacion
    [Header("Configuración del Área de Misión")]
    public SpawnInArea personSpawner;
    public XZCoordinate cornerA, cornerB, cornerC, cornerD;
    public Terrain missionTerrain;

    [Header("Configuración de la Misión")]
    public string targetDescription = "una persona con gorra roja";
    public float landingRadius = 5.0f;

    [Header("Referencias de la Simulación")]
    public List<DroneController> drones;
    public List<Transform> droneSpawnPoints;

    [Header("Configuración de Vuelo de Drones")]
    public float cruisingAltitude = 80f; // Altura segura para viajar
    public Camera droneCameraPrefab;

    private Dictionary<PersonIdentity, Dictionary<string, float>> missionReports;
    // Lleva un registro de qué drones han terminado su ciclo de escaneo
    private HashSet<string> dronesFinishedScanning;



    void Start()
    {
        // colocar los drones en sus bases
        if (droneSpawnPoints.Count == drones.Count)
        {
            for (int i = 0; i < drones.Count; i++)
            {
                drones[i].transform.position = droneSpawnPoints[i].position;
                drones[i].transform.rotation = droneSpawnPoints[i].rotation;
            }
        }
        else
        {
            Debug.LogError("La cantidad de drones y de puntos de spawn no coincide.");
        }

        // spawnear a las personas en el area designada
        if (personSpawner != null)
        {
            personSpawner.SpawnAll(
                cornerA.ToVector3(), cornerB.ToVector3(),
                cornerC.ToVector3(), cornerD.ToVector3()
            );
        }
        else
        {
            Debug.LogError("El Spawner de personas no está asignado en el SimulationManager.");
            return;
        }

        missionReports = new Dictionary<PersonIdentity, Dictionary<string, float>>();
        dronesFinishedScanning = new HashSet<string>();


        // llamar a la funcion para que los drones se coloquen en triangulo alrededor de la zona
        AssignInitialTrianglePositions();
    }

    void AssignInitialTrianglePositions()
    {
        if (missionTerrain == null)
        {
            Debug.LogError("El terreno no ha sido asignado en el SimulationManager.");
            return;
        }

        // calcular donde esta el area de busqueda y la altura para escanear con la camara
        Vector3 center = (cornerA.ToVector3() + cornerB.ToVector3() + cornerC.ToVector3() + cornerD.ToVector3()) / 4f;
        float searchRadius = Vector3.Distance(center, cornerA.ToVector3()) * 1.1f;
        float fov = droneCameraPrefab.fieldOfView;
        float scanningAltitude = searchRadius / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        scanningAltitude = Mathf.Clamp(scanningAltitude, 20f, 150f);

        Debug.Log("Altura de escaneo relativa calculada: " + scanningAltitude);

        // calcular las posiciones en triangulo alrededor del area de busqueda
        Vector3 pos1 = center + Quaternion.Euler(0, 0, 0) * Vector3.forward * searchRadius;
        Vector3 pos2 = center + Quaternion.Euler(0, 120, 0) * Vector3.forward * searchRadius;
        Vector3 pos3 = center + Quaternion.Euler(0, 240, 0) * Vector3.forward * searchRadius;

        // ajustar las posiciones a la altura del terreno
        pos1.y = missionTerrain.SampleHeight(pos1) + missionTerrain.transform.position.y;
        pos2.y = missionTerrain.SampleHeight(pos2) + missionTerrain.transform.position.y;
        pos3.y = missionTerrain.SampleHeight(pos3) + missionTerrain.transform.position.y;

        // Le pasamos al dron su destino final, su altura de escaneo relativa y la altura de crucero.
        drones[0].GoToMissionArea(pos1, scanningAltitude, cruisingAltitude);
        drones[1].GoToMissionArea(pos2, scanningAltitude, cruisingAltitude);
        drones[2].GoToMissionArea(pos3, scanningAltitude, cruisingAltitude);
    }

    // almacenar resultados de escaneos por persona
    public void SubmitDroneReport(string droneName, PersonIdentity person, float confidence)
    {
        if (!missionReports.ContainsKey(person))
        {
            missionReports[person] = new Dictionary<string, float>();
        }

        // Guardamos o actualizamos la confianza de este dron para esta persona
        missionReports[person][droneName] = confidence;

        Debug.Log($"<color=green>[Reporte Recibido]</color> Dron: {droneName}, Objetivo: {person.personID}, Confianza: {confidence * 100:F2}%");
    }

    public void DroneFinishedScanning(string droneName)
    {
        dronesFinishedScanning.Add(droneName);

        if (dronesFinishedScanning.Count == drones.Count)
        {
            AnalyzeMissionReports();
        }
    }

    // Analizar los resultados y elegir a la persona que buscamos
    private void AnalyzeMissionReports()
    {
        Debug.Log("<color=magenta>--- ANÁLISIS DE MISIÓN COMPLETO ---</color>");
        if (missionReports.Count == 0)
        {
            Debug.LogWarning("No se recibieron reportes. Misión terminada sin resultados.");
            return;
        }

        var analysisResults = new Dictionary<PersonIdentity, float>();
        foreach (var personEntry in missionReports)
        {
            float totalConfidence = personEntry.Value.Values.Sum();
            analysisResults[personEntry.Key] = totalConfidence;
        }

        var sortedResults = analysisResults.OrderByDescending(kvp => kvp.Value);
        PersonIdentity bestCandidate = sortedResults.First().Key;
        float highestTotalConfidence = sortedResults.First().Value;

        Debug.Log($"El candidato con mayor confianza colectiva es {bestCandidate.personID} con una puntuación de {highestTotalConfidence * 100:F2}.");


        Debug.Log($"<color=lime>Confianza suficiente. Iniciando fase de aterrizaje.</color>");

        DroneController droneToLand = FindClosestDrone(bestCandidate.transform.position);

        Vector3 personPosition = bestCandidate.transform.position;
        Vector3 dronePosition = droneToLand.transform.position;

        Vector3 directionAwayFromPerson = (dronePosition - personPosition).normalized;

        directionAwayFromPerson.y = 0;

        float landingOffset = 5.0f;
        Vector3 offsetTargetPos = personPosition + directionAwayFromPerson * landingOffset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(offsetTargetPos, out hit, landingRadius, NavMesh.AllAreas))
        {
            Vector3 landingSpot = hit.position;
            Debug.Log($"Punto de aterrizaje seguro encontrado en {landingSpot}. Dando orden a {droneToLand.gameObject.name}.");
            droneToLand.LandAtTarget(landingSpot);
        }
        else
        {
            Debug.LogError($"No se pudo encontrar un punto de aterrizaje válido cerca de {bestCandidate.personID}.");
        }

    }

    // encontrar al drone mas cercano al objetivo
    private DroneController FindClosestDrone(Vector3 targetPosition)
    {
        DroneController closestDrone = null;
        float minDistance = float.MaxValue;

        foreach (var drone in drones)
        {
            float distance = Vector3.Distance(drone.transform.position, targetPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestDrone = drone;
            }
        }
        return closestDrone;
    }
}