using UnityEngine;

public class SpawnInArea : MonoBehaviour
{
    public GameObject[] prefabs;
    public Terrain terrain;

    public void SpawnAll(Vector3 cornerA, Vector3 cornerB, Vector3 cornerC, Vector3 cornerD)
    {

        foreach (var prefab in prefabs)
        {

            Vector3 pos = RandomPointInQuad(cornerA, cornerB, cornerC, cornerD);

            float terrainHeight = terrain.SampleHeight(pos) + terrain.transform.position.y;
            pos.y = terrainHeight;

            Quaternion rot = Quaternion.identity;
            rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var go = Instantiate(prefab, pos, rot);
        }
    }

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

        if (u + v > 1f)
        {
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
