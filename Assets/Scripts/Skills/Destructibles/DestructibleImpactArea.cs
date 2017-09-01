﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DestructibleImpactArea : MonoBehaviour
{
    MeshRenderer[] all;
    public Vector3[] rotAngles;
    CamRotationController cam;

    void Start()
    {
        all = GetComponentsInChildren<MeshRenderer>().Where(x => x.gameObject != gameObject).ToArray();
        rotAngles = GetComponentInParent<DestructibleObject>().rotAngles;
        FindCamera();
    }

    private void FindCamera()
    {
        if (GameManager.screenDivided)
        {
            if (gameObject.layer == Utilities.IntLayers.VISIBLETOP1)
            {
                cam = GameObject.Find("Player1").GetComponent<Player1Input>().GetCamera;
            }
            else if (gameObject.layer == Utilities.IntLayers.VISIBLETOP2)
            {
                cam = GameObject.Find("Player2").GetComponent<Player1Input>().GetCamera;
            }
        }
        else cam = GameObject.FindObjectOfType<CamRotationController>();
    }

    void LateUpdate()
    {
        if (rotAngles.Length != 0)
        {
            if (!cam.gameObject.activeInHierarchy) FindCamera();
            CheckRotation(cam.transform.forward, cam.AngleVision);
        }
    }

    void CheckRotation(Vector3 rot, float angle)
    {
        var minAngle = angle;
        Vector3 vectorRot = transform.forward;

        foreach (var a in rotAngles)
        {
            var tempAngle = Vector3.Angle(rot, a);
            if (tempAngle < minAngle)
            {
                minAngle = tempAngle;
                vectorRot = a;
            }
        }

        var parent = this.GetComponentInParent<DestructibleObject>().transform;

        if (this.gameObject.name == "SingleDoorImpactArea" && Vector3.Angle(parent.forward, rot) <= 90)
        {    
            this.transform.forward = parent.forward * -1;
        } 
        else transform.rotation = Quaternion.Euler(vectorRot);
    }

    public void SetVisible(bool activate)
    {
        foreach (var m in all)
        {
            m.enabled = activate;
        }
    }
}
