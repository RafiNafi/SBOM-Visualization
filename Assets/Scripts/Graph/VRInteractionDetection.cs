using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using static Unity.Burst.Intrinsics.X86.Avx;

public class VRInteractionDetection : MonoBehaviour
{
    public ActionBasedController controller;
    public MenuInteraction menu;
    public ActionBasedController controllerRight;
    public GameObject jsonMenu;

    public XRRayInteractor rayInteractor;
    public Camera cam;
    public GameObject scrollViewContent;

    public float cooldownTime = 1.0f; 
    private float lastClickTime = 0f;

    void Update()
    {
        if(!jsonMenu.activeSelf)
        {
            if (IsTriggerPressed(controllerRight))
            {
                // Prevent multiple clicks
                if (IsClickAllowed())
                {
                    Ray ray = new Ray(controllerRight.transform.position, controllerRight.transform.forward);
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

        UnityEngine.EventSystems.RaycastResult hitNew;
        if (rayInteractor.TryGetCurrentUIRaycastResult(out hitNew))
        {
            TextMeshProUGUI text = scrollViewContent.GetComponent<TextMeshProUGUI>();

            if (hitNew.gameObject != null && hitNew.gameObject == text.gameObject)
            {
                Vector3 worldPoint = hitNew.worldPosition;
                Vector2 screenPoint = cam.WorldToScreenPoint(worldPoint);

                int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, screenPoint, cam);

                if (linkIndex != -1) 
                {
                    TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];

                    if (IsClickAllowed())
                    {
                        if (IsTriggerPressed(controllerRight))
                        {
                            lastClickTime = Time.time;
                            Debug.Log("Selected link: " + linkInfo.GetLinkID());
                            menu.ExpandNodeInMenu(linkInfo.GetLinkID());
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
