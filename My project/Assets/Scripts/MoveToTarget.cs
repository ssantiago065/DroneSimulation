using UnityEngine;

public class MoveToTarget : MonoBehaviour
{
    public Vector3 targetPosition; // Coordenadas a las que se moverá
    public float speed = 5f;       // Velocidad de movimiento
    public float stopDistance = 0.1f; // Distancia mínima para considerar que llegó al destino

    void Update()
    {
        // Mover hacia la posición objetivo
        if (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );
        }
    }

    // Método público para actualizar el objetivo desde otro script
    public void SetTarget(Vector3 newTarget)
    {
        targetPosition = newTarget;
    }
}
