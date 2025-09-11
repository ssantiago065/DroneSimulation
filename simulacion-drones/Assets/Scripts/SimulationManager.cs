// SimulationManager.cs
using System.Collections.Generic;
using UnityEngine;
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
        // Colocamos los drones en sus bases
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

        // Spawneamos a las personas en el area designada
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


        // Llamamos a la funcion para que los drones se coloquen en triangulo alrededor de la zona
        AssignInitialTrianglePositions();
    }

    void AssignInitialTrianglePositions()
    {
        // Si no hay terreno abortamos
        if (missionTerrain == null)
        {
            Debug.LogError("El terreno no ha sido asignado en el SimulationManager.");
            return;
        }

        // Calculamos donde esta el area de busqueda y la altura para escanear con nuestra camara
        Vector3 center = (cornerA.ToVector3() + cornerB.ToVector3() + cornerC.ToVector3() + cornerD.ToVector3()) / 4f;
        float searchRadius = Vector3.Distance(center, cornerA.ToVector3()) * 1.1f;
        float fov = droneCameraPrefab.fieldOfView;
        float scanningAltitude = searchRadius / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        scanningAltitude = Mathf.Clamp(scanningAltitude, 20f, 150f);

        Debug.Log("Altura de escaneo relativa calculada: " + scanningAltitude);

        // Calculamos las posiciones en triangulo alrededor del area de busqueda
        Vector3 pos1 = center + Quaternion.Euler(0, 0, 0) * Vector3.forward * searchRadius;
        Vector3 pos2 = center + Quaternion.Euler(0, 120, 0) * Vector3.forward * searchRadius;
        Vector3 pos3 = center + Quaternion.Euler(0, 240, 0) * Vector3.forward * searchRadius;

        // Ajustamos las posiciones a la altura del terreno
        pos1.y = missionTerrain.SampleHeight(pos1) + missionTerrain.transform.position.y;
        pos2.y = missionTerrain.SampleHeight(pos2) + missionTerrain.transform.position.y;
        pos3.y = missionTerrain.SampleHeight(pos3) + missionTerrain.transform.position.y;

        // Le pasamos al dron su destino final, su altura de escaneo relativa y la altura de crucero.
        drones[0].GoToMissionArea(pos1, scanningAltitude, cruisingAltitude);
        drones[1].GoToMissionArea(pos2, scanningAltitude, cruisingAltitude);
        drones[2].GoToMissionArea(pos3, scanningAltitude, cruisingAltitude);
    }


    public void SubmitDroneReport(string droneName, PersonIdentity person, float confidence)
    {
        // Si es la primera vez que vemos a esta persona, la añadimos al diccionario
        if (!missionReports.ContainsKey(person))
        {
            missionReports[person] = new Dictionary<string, float>();
        }

        // Guardamos o actualizamos la confianza de este dron para esta persona
        missionReports[person][droneName] = confidence;

        Debug.Log($"<color=green>[Reporte Recibido]</color> Dron: {droneName}, Objetivo: {person.personID}, Confianza: {confidence * 100:F2}%");
    }

    // --- NUEVA FUNCIÓN: Los drones la llaman al terminar su ciclo ---
    public void DroneFinishedScanning(string droneName)
    {
        dronesFinishedScanning.Add(droneName);

        // Si todos los drones han terminado, es hora de tomar una decisión
        if (dronesFinishedScanning.Count == drones.Count)
        {
            AnalyzeMissionReports();
        }
    }

    // --- NUEVA FUNCIÓN: El cerebro que analiza los datos (Borda Count) ---
    private void AnalyzeMissionReports()
    {
        Debug.Log("<color=magenta>--- ANÁLISIS DE MISIÓN COMPLETO ---</color>");
        if (missionReports.Count == 0)
        {
            Debug.LogWarning("No se recibieron reportes de ningún dron. Misión terminada sin resultados.");
            return;
        }

        PersonIdentity bestCandidate = null;
        float highestTotalConfidence = -1f;

        // Calculamos la puntuación total para cada persona
        var analysisResults = new Dictionary<PersonIdentity, float>();
        foreach (var personEntry in missionReports)
        {
            PersonIdentity person = personEntry.Key;
            Dictionary<string, float> reports = personEntry.Value;

            // Borda Count: Sumamos la confianza de todos los drones que vieron a esta persona
            float totalConfidence = reports.Values.Sum();
            analysisResults[person] = totalConfidence;

            Debug.Log($"Puntuación total para {person.personID}: {totalConfidence * 100:F2}");
        }

        // Encontramos al candidato con la mayor suma de confianzas
        var sortedResults = analysisResults.OrderByDescending(kvp => kvp.Value);
        bestCandidate = sortedResults.First().Key;
        highestTotalConfidence = sortedResults.First().Value;

        Debug.Log($"<color=yellow>--- DECISIÓN FINAL ---</color>");
        Debug.Log($"El candidato con mayor confianza colectiva es <color=lime>{bestCandidate.personID}</color> con una puntuación total de <color=lime>{highestTotalConfidence * 100:F2}</color>.");

        // TODO: Aquí iría la lógica para el siguiente paso:
        // 1. Comprobar si la confianza es suficiente para aterrizar.
        // 2. Si no, calcular un nuevo cerco alrededor de 'bestCandidate' y reiniciar la misión.
    }
}