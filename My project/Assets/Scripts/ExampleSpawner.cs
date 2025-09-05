using UnityEngine;
public class ExampleSpawner : MonoBehaviour
{
    public SpawnInArea spawner;
    public Vector3 cornerA;
    public Vector3 cornerB;
    public Vector3 cornerC;
    public Vector3 cornerD;

    void Start()
    {
        // Spawnear
        spawner.SpawnAll(cornerA, cornerB, cornerC, cornerD);
    }
}