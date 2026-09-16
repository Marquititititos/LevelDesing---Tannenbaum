using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CircularMovingPlatform : MonoBehaviour
{
    public float radius = 3f;
    public float speed = 1f;
    public float startAngle = 0f;

    public Vector2 DeltaMovement { get; private set; }

    private Vector2 center;
    private float angle;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        angle = startAngle * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        ) * radius;

        center = rb.position - offset;
    }

    void FixedUpdate()
    {
        Vector2 oldPosition = rb.position;

        angle += speed * Time.fixedDeltaTime;

        Vector2 newPosition = center + new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        ) * radius;

        DeltaMovement = newPosition - oldPosition;

        rb.MovePosition(newPosition);
    }
}