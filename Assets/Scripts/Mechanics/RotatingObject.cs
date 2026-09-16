using UnityEngine;

public class RotatingObject : MonoBehaviour
{
    public bool dir;
    public float spd;
    public bool isRotating = true;

    void Update()
    {
        if (isRotating)
        {
            transform.Rotate(new Vector3(0, 0, spd * (dir ? 1 : -1)) * Time.deltaTime);
        }
    }
}
