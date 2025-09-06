using UnityEngine;

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

public class ExampleSpawner : MonoBehaviour
{
    public SpawnInArea spawner;

    public XZCoordinate cornerA;
    public XZCoordinate cornerB;
    public XZCoordinate cornerC;
    public XZCoordinate cornerD;

    void Start()
    {
        spawner.SpawnAll(
            cornerA.ToVector3(),
            cornerB.ToVector3(),
            cornerC.ToVector3(),
            cornerD.ToVector3()
        );
    }
}
