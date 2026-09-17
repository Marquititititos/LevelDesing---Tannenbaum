using UnityEngine;
using UnityEngine.Playables;

public class Lever : MonoBehaviour
{
    public bool isOn = false;
    private bool canInteract = false;
    private Animator anim;
    public int leverID; // Unique identifier for the lever

    public PlayableDirector piston;
    public CircularMovingPlatform[] platforms;
    public RotatingObject[] rotatingObjects;

    public CircularMovingPlatform[] platforms2;
    public RotatingObject[] rotatingObjects2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canInteract = false;
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log(
            $"INTERACTING WITH {gameObject.name} | ID={leverID} | canInteract={canInteract}"
        );

            isOn = !isOn;
            anim.SetBool("isOn", isOn);
            Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canInteract = true;
            anim.SetBool("canInteract", true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            canInteract = false;
            anim.SetBool("canInteract", false);
        }
    }

    public void Interact()
    {
        switch (leverID)
        {
            case 1:
                piston.enabled = !piston.enabled;
                break;
            case 2:
                piston.enabled = !piston.enabled;
                foreach (CircularMovingPlatform platform in platforms)
                {
                    platform.isMoving = !platform.isMoving;
                }
                foreach (RotatingObject rotatingObject in rotatingObjects)
                {
                    rotatingObject.isRotating = !rotatingObject.isRotating;
                }
                break;
            case 3:
                piston.enabled = !piston.enabled;
                break;
            case 4:
                piston.enabled = !piston.enabled;
                foreach (CircularMovingPlatform platform in platforms)
                {
                    platform.isMoving = !platform.isMoving;
                }
                foreach (RotatingObject rotatingObject in rotatingObjects)
                {
                    rotatingObject.isRotating = !rotatingObject.isRotating;
                }

                foreach (CircularMovingPlatform platform in platforms2)
                {
                    if (isOn) { platform.speed *= -2f; } else { platform.speed /= -2f; }
                }
                foreach (RotatingObject rotatingObject in rotatingObjects2)
                {
                    if (isOn) { rotatingObject.spd *= -2f; } else { rotatingObject.spd /= -2f; }
                }
                break;
        }
    }
}
