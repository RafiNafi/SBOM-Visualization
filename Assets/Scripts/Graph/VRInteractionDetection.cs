using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class VRInteractionDetection : MonoBehaviour
{
    public ActionBasedController controller;
    public MenuInteraction menu;

    public float cooldownTime = 1.0f; 
    private float lastClickTime = 0f;

    void Update()
    {
        if (IsTriggerPressed(controller))
        {
            // Prevent multiple clicks
            if (IsClickAllowed())
            {
                Ray ray = new Ray(controller.transform.position, controller.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider != null)
                    {
                        lastClickTime = Time.time;
                        Debug.Log("Interacted: " + hit.collider.gameObject.name);
                        menu.ShowNodePositionInMenu(hit.collider.gameObject);
                    }
                }
            }
        }
    }

    bool IsClickAllowed()
    {
        return Time.time >= lastClickTime + cooldownTime;
    }

    bool IsTriggerPressed(ActionBasedController controller)
    {
        if (controller.activateAction.action != null)
        {
            return controller.activateAction.action.ReadValue<float>() > 0.1f;
        }
        return false;
    }
}
