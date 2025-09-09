// SpawnArea.cs
using UnityEngine;

public class SpawnInArea : MonoBehaviour
{
    // --- CAMBIO 1: El array ahora es de tipo PersonIdentity ---
    // Esto hace que en el Inspector solo puedas arrastrar prefabs que tengan ese script.
    public PersonIdentity[] personPrefabs;
    public Terrain terrain;
    private int personCounter = 0; // Un contador simple para los IDs

    public void SpawnAll(Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD)
    {
        // Ahora iteramos sobre la lista de prefabs de personas
        foreach (var personPrefab in personPrefabs)
        {
            Vector3 pos = RandomPointInQuad(cornerA, cornerB, cornerC, cornerD);
            float terrainHeight = terrain.SampleHeight(pos) + terrain.transform.position.y;
            pos.y = terrainHeight;
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // --- CAMBIO 2: Instanciamos y obtenemos una referencia al componente ---
            PersonIdentity newPerson = Instantiate(personPrefab, pos, rot);

            // --- CAMBIO 3: Asignamos un ID único a la persona recién creada ---
            personCounter++;
            newPerson.personID = "Person_" + personCounter;
            newPerson.gameObject.name = newPerson.personID; // Cambiamos el nombre del objeto para claridad
        }
    }

    // ... (El resto de las funciones RandomPointInQuad, etc., se mantienen exactamente igual) ...
    private static Vector3 RandomPointInQuad(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        float areaABC = TriangleArea(A, B, C);
        float areaACD = TriangleArea(A, C, D);
        float t = Random.value;
        return t < areaABC / (areaABC + areaACD)
            ? RandomPointInTriangle(A, B, C)
            : RandomPointInTriangle(A, C, D);
    }

    private static Vector3 RandomPointInTriangle(Vector3 A, Vector3 B, Vector3 C)
    {
        float u = Random.value;
        float v = Random.value;
        if (u + v > 1f) {
            u = 1f - u;
            v = 1f - v;
        }
        return A + u * (B - A) + v * (C - A);
    }

    private static float TriangleArea(Vector3 A, Vector3 B, Vector3 C)
    {
        return Vector3.Cross(B - A, C - A).magnitude * 0.5f;
    }
}