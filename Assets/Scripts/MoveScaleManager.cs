using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveScaleManager : MonoBehaviour {

    public GameObject subnet;
    public GameObject moveQuad;
    public GameObject scaleQuad;

    bool updatePositions = true;

    Vector3 initialMovePosition;

    Quaternion moveQuat;
    Plane scalePlane;
    float initialScaleDist;
    float initialScaleVal;
    float currScaleVal;

    // Use this for initialization
    void Start () {
        currScaleVal = subnet.transform.localScale.x;
    }
	
	// Update is called once per frame
	void Update () {
        if (updatePositions) updateQuadPositions();
	}

    public void initMove(Transform controllerTransform, Vector3 rayOrigin)
    {
        updatePositions = true;

        moveQuat = Quaternion.Inverse(controllerTransform.rotation) * subnet.transform.rotation;
        Vector3 pos = subnet.transform.position - rayOrigin;
        initialMovePosition.Set(Vector3.Dot(controllerTransform.up, pos),
            Vector3.Dot(controllerTransform.right, pos),
            Vector3.Dot(controllerTransform.forward, pos));

    }

    public void updateMove(Transform controllerTransform, Vector3 rayOrigin)
    {
        subnet.transform.rotation = controllerTransform.rotation * moveQuat;
        subnet.transform.position = rayOrigin +
            initialMovePosition.x * controllerTransform.up +
            initialMovePosition.y * controllerTransform.right +
            initialMovePosition.z * controllerTransform.forward;

        updatePositions = true;
    }

    public void endMove()
    {
        updatePositions = false;
    }

    public void initScale()
    {
        updatePositions = true;
        scalePlane = new Plane(subnet.transform.forward, subnet.transform.position);

        //0.05657f

        Vector3 v = subnet.transform.position - scaleQuad.transform.position;
        initialScaleDist = v.magnitude - 0.05657f;
        initialScaleVal = subnet.transform.localScale.x;
    }

    public void updateScale(Ray ray)
    {
        updatePositions = true;
        float dist;
        if (!scalePlane.Raycast(ray, out dist)) return;

        Vector3 intPt = ray.GetPoint(dist);
        Vector3 v = subnet.transform.position - intPt;
        float tDist = v.magnitude - 0.05657f;
        if (tDist == 0.1f) return;

        float tScale = tDist / initialScaleDist * initialScaleVal;

        subnet.transform.localScale = new Vector3(tScale, tScale, 1.0f);
        currScaleVal = tScale;
        
    }

    public void endScale()
    {
        updatePositions = false;
    }



    void updateQuadPositions()
    {
        scaleQuad.transform.rotation = subnet.transform.rotation;
        scaleQuad.transform.position = subnet.transform.position + subnet.transform.right * (0.5f * currScaleVal +0.04f) + subnet.transform.up * (0.5f * currScaleVal + 0.08f);

        moveQuad.transform.rotation = subnet.transform.rotation;
        moveQuad.transform.position = scaleQuad.transform.position - subnet.transform.right * 0.1f;

        updatePositions = false;
    }
}
