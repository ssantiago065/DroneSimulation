using UnityEngine;

public class SpawnInArea : MonoBehaviour
{
    [Header("Prefabs a spawnear (exactamente 7)")]
    public GameObject[] prefabs; // Los 7 modelos 3D distintos

    [Header("Opciones")]
    public bool randomYRotation = true; // Rotación aleatoria en Y
    public float altitudeOffset = 0f;   // Ajuste en la altura (Y)
    public bool parentToThis = true;    // Parent al objeto que tiene este script

    /// <summary>
    /// Spawnea una instancia de cada prefab dentro de un área definida por 4 esquinas.
    /// </summary>
    /// <param name="cornerA">Esquina 1 (Vector3)</param>
    /// <param name="cornerB">Esquina 2 (Vector3)</param>
    /// <param name="cornerC">Esquina 3 (Vector3)</param>
    /// <param name="cornerD">Esquina 4 (Vector3)</param>
    public void SpawnAll(Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD)
    {
        // Validaciones
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("[SpawnInArea] No hay prefabs asignados.");
            return;
        }

        foreach (var prefab in prefabs)
        {
            if (prefab == null) 
            {
                Debug.LogWarning("[SpawnInArea] Hay un prefab nulo, se ignora.");
                continue;
            }

            // Generar posición aleatoria dentro del área
            Vector3 pos = RandomPointInQuad(cornerA, cornerB, cornerC, cornerD);
            pos.y += altitudeOffset;

            // Rotación
            Quaternion rot = Quaternion.identity;
            if (randomYRotation)
                rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Instanciar
            var go = Instantiate(prefab, pos, rot);
            if (parentToThis)
                go.transform.SetParent(transform, true);
        }
    }

    /// <summary>
    /// Devuelve un punto uniforme aleatorio dentro de un cuadrilátero convexo ABCD.
    /// Divide en dos triángulos (ABC y ACD) y selecciona según área.
    /// </summary>
    private static Vector3 RandomPointInQuad(Vector3 A, Vector3 B, Vector3 C, Vector3 D)
    {
        float areaABC = TriangleArea(A, B, C);
        float areaACD = TriangleArea(A, C, D);

        float t = Random.value;
        return t < areaABC / (areaABC + areaACD)
            ? RandomPointInTriangle(A, B, C)
            : RandomPointInTriangle(A, C, D);
    }

    /// <summary>
    /// Genera un punto aleatorio dentro de un triángulo usando coordenadas baricéntricas.
    /// </summary>
    private static Vector3 RandomPointInTriangle(Vector3 A, Vector3 B, Vector3 C)
    {
        float u = Random.value;
        float v = Random.value;

        // Garantiza distribución uniforme
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }

        return A + u * (B - A) + v * (C - A);
    }

    /// <summary>
    /// Calcula el área de un triángulo en 3D.
    /// </summary>
    private static float TriangleArea(Vector3 A, Vector3 B, Vector3 C)
    {
        return Vector3.Cross(B - A, C - A).magnitude * 0.5f;
    }
}
