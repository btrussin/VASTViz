using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Valve.VR;

public class ViveController : SteamVR_TrackedObject
{

    private Ray deviceRay;
    public GameObject projLineObj;

    LineRenderer lineRend;

    float rayAngle = 60.0f;

    int sliderMask;

    Vector3[] linePts = new Vector3[2];

    bool onSlider = false;
    bool moveSlider = false;

    CVRSystem vrSystem;

    VRControllerState_t state;
    VRControllerState_t prevState;

    public GameObject timelineObject;
    TimelineScript currSliderScript = null;

    float currSliderDist;

    public GameObject sqlConnObject;
    TestSQLiteConn sqlConnClass;

    public GameObject timelineGameObject;
    public GameObject otherController;
    ViveController otherControllerScript;

    public GameObject accordianObj;
    AccordianManager accordianManager;

    bool hasController = false;
    bool inAnimation = false;

    public GameObject playLabel;
    public GameObject pauseLabel;

    // Use this for initialization
    void Start () {

        vrSystem = OpenVR.System;

        otherControllerScript = otherController.GetComponent<ViveController>();

        sliderMask = 1 << LayerMask.NameToLayer("slider");
        lineRend = projLineObj.GetComponent<LineRenderer>();

        linePts[0] = Vector3.zero;

        sqlConnClass = sqlConnObject.GetComponent<TestSQLiteConn>();
        currSliderScript = timelineObject.GetComponent<TimelineScript>();

        accordianManager = accordianObj.GetComponent<AccordianManager>();

    }
	
	// Update is called once per frame
	void Update () {

        Quaternion rayRotation = Quaternion.AngleAxis(rayAngle, transform.right);
        deviceRay.origin = transform.position;
        deviceRay.direction = rayRotation * transform.forward;

        tryHitSlider();

        updateControllerStates();

        if (moveSlider) updateSliderPosition();

    }

    public void releaseController()
    {
        hasController = false;
        timelineGameObject.transform.SetParent(gameObject.transform.parent);
    }

    void updateSliderPosition()
    {
        if (currSliderScript == null) return;
        Vector3 tmpPos = deviceRay.GetPoint(currSliderDist);
        currSliderScript.tryMoveSlider(tmpPos);
    }

    void updateControllerStates()
    {
        bool stateIsValid = vrSystem.GetControllerState((uint)index, ref state);

        if (stateIsValid && state.GetHashCode() != prevState.GetHashCode())
        {
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.ApplicationMenu) != 0 &&
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.ApplicationMenu) == 0)
            {
                // hit the menu button
                if( hasController )
                {
                    releaseController();
                }
                else
                {
                    hasController = true;
                    otherControllerScript.releaseController();
                    timelineGameObject.SetActive(true);
                    timelineGameObject.transform.SetParent(gameObject.transform);
                    timelineGameObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.1f);
                    timelineGameObject.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                }
            }

            
            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) != 0 &&
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) == 0)
            {
                // hit the grip button
                Vector3 dir = transform.forward - transform.up;
                dir.y = 0.0f;
                dir.Normalize();
                Vector3 pos = transform.position;

                accordianManager.activate(pos, dir);
            }

            else if((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) == 0 && 
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) != 0)
            {
                accordianManager.deactivate();
            }


            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {
               
                // trigger is being pressed
                if (state.rAxis1.x >= 0.85f)
                {
                    if (onSlider)
                    {
                        moveSlider = true;
                        currSliderScript.activateControlLine();
                    }
                }
                else
                {
                    if (onSlider)
                    {
                        moveSlider = false;
                        currSliderScript.deactivateControlLine();

                        Vector3 currPos = currSliderScript.getCurrentPosition();
                        sqlConnClass.setTimeSlice(currPos.x);
                    }
                }

            }

            if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Touchpad) != 0 && 
                (prevState.ulButtonPressed & SteamVR_Controller.ButtonMask.Touchpad) == 0)
            {
                if (Mathf.Abs(state.rAxis0.y) >= Mathf.Abs(state.rAxis0.x) && state.rAxis0.y < 0.0f )
                {
                    togglePlayAnimation();
                    otherControllerScript.togglePlayAnimation();
                }
            }

            else if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Touchpad) != 0)
            {
                //Vector2 touchPos = new Vector2(state.rAxis0.x, state.rAxis0.y);

                if( Mathf.Abs(state.rAxis0.x) >= Mathf.Abs(state.rAxis0.y))
                {
                    int currTime = sqlConnClass.getTimeSliceIdx();
                    if (state.rAxis0.x < 0.0f) sqlConnClass.setTimeSlice(currTime - 1);
                    else if (state.rAxis0.x > 0.0f) sqlConnClass.setTimeSlice(currTime + 1);

                    float currPos = sqlConnClass.getTimeSliceFloat();
                    currSliderScript.updateSliderPosition(currPos);
                }
                

            }


            prevState = state;
        }


        if ((state.ulButtonPressed & SteamVR_Controller.ButtonMask.Grip) != 0)
        {
            accordianManager.tryToAdjustDistance(transform.position);
        }
    }

    public void togglePlayAnimation()
    {
        inAnimation = !inAnimation;

        if( inAnimation )
        {
            pauseLabel.SetActive(true);
            playLabel.SetActive(false);
            sqlConnClass.startAnimation();
        }
        else
        {
            playLabel.SetActive(true);
            pauseLabel.SetActive(false);
            sqlConnClass.stopAnimation();
        }
    }

    void tryHitSlider()
    {
        projLineObj.SetActive(false);

        RaycastHit hitInfo;

        if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 30.0f, sliderMask))
        {
            projLineObj.SetActive(true);
            linePts[0] = deviceRay.origin;
            linePts[1] = deviceRay.GetPoint(hitInfo.distance);
            lineRend.SetPositions(linePts);
            onSlider = true;
            currSliderDist = hitInfo.distance;

            if ( !moveSlider)
            {
                SliderManager timelineObj = hitInfo.collider.gameObject.GetComponent<SliderManager>();
                currSliderScript = timelineObj.mainTimeline.GetComponent<TimelineScript>();

                currSliderScript.hightlightControlLine();
            }
        }
        else if(!moveSlider)
        {
            onSlider = false;
            currSliderScript.deactivateControlLine();
        }
        
       
    }

}
