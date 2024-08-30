using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static Unity.Burst.Intrinsics.X86.Avx;

public class VRInteractionDetection : MonoBehaviour
{
    public ActionBasedController controller;
    public MenuInteraction menu;
    public ActionBasedController controllerRight;

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

        if (IsTriggerPressed(controllerRight))
        {
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

                        if (hit.transform.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI tmp))
                        {
                            int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmp, hit.point, Camera.main);

                            if (linkIndex != -1)
                            {
                                TMP_LinkInfo linkInfo = tmp.textInfo.linkInfo[linkIndex];
                                Debug.Log("Clicked on link: " + linkInfo.GetLinkID());

                            }
                        }
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
