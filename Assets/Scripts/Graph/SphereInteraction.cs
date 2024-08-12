using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SphereInteraction : MonoBehaviour
{
    private XRGrabInteractable interactable;
    private Rigidbody rb;

    private void Awake()
    {
        interactable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("Sphere " + gameObject.name);
        GetComponent<Renderer>().material.color = Color.red;
    }
}
