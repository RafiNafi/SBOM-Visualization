using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFloatingMode : MonoBehaviour
{

    //public float movementSpeed = 2.0f;  
    public float verticalSpeed = 10f;
    public float rampup = 0f;
    public float rampupSpeed = 50f;

    public Transform playerTransform;

    //private Vector2 inputAxis;
    //public InputAction moveAction;
    public InputAction ascendAction;
    public InputAction descendAction;


    private bool isButtonPressed = false;

    void Start()
    {
        playerTransform = GetComponent<Transform>();
    }

    void Update()
    {
        /*
        inputAxis = moveAction.ReadValue<Vector2>();
        Vector3 horizontalMovement = new Vector3(inputAxis.x, 0, inputAxis.y) * movementSpeed * Time.deltaTime;
        playerTransform.Translate(horizontalMovement);
        */

        if (isButtonPressed)
        {
            if (rampup < rampupSpeed)
            {
                rampup += 0.01f;
            }
        }

        if (ascendAction.ReadValue<float>() > 0)
        {
            playerTransform.Translate(Vector3.up * (verticalSpeed + rampup) * Time.deltaTime);
        }
        if (descendAction.ReadValue<float>() > 0)
        {
            playerTransform.Translate(Vector3.down * (verticalSpeed + rampup) * Time.deltaTime);
        }
    }

    private void OnEnable()
    {
        //moveAction.Enable();
        ascendAction.Enable();
        descendAction.Enable();

        ascendAction.started += OnButtonPressed;
        ascendAction.canceled += OnButtonReleased;

        descendAction.started += OnButtonPressed;
        descendAction.canceled += OnButtonReleased;
    }

    private void OnDisable()
    {
        //moveAction.Disable();
        ascendAction.Disable();
        descendAction.Disable();
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        isButtonPressed = true;
    }

    private void OnButtonReleased(InputAction.CallbackContext context)
    {
        isButtonPressed = false;
        rampup = 0f;
    }
}
