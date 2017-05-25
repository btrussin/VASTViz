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
    int nodesMask;
    int moveScaleMask;
    int playAreaMask;

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

    public GameObject nodeStatusObject;
    public GameObject nodeStatusText;
    GameObject currNodeObject = null;

    GameObject currMoveQuad = null;
    GameObject currScaleQuad = null;
    bool activeScale = false;
    bool activeMove = false;
    MoveScaleManager activeMoveScaleManager = null;

    public GameObject actionAreaObject;
    ActionAreaManager actionAreaManager;

    // Use this for initialization
    void Start () {

        vrSystem = OpenVR.System;

        otherControllerScript = otherController.GetComponent<ViveController>();

        actionAreaManager = actionAreaObject.GetComponent<ActionAreaManager>();

        sliderMask = 1 << LayerMask.NameToLayer("slider");
        nodesMask = 1 << LayerMask.NameToLayer("nodes");
        moveScaleMask = 1 << LayerMask.NameToLayer("moveScale");
        playAreaMask = 1 << LayerMask.NameToLayer("playArea");

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

        // try to hit the various interaction objects
        projLineObj.SetActive(false);
        tryHitObjects();

        updateControllerStates();

        if (moveSlider) updateSliderPosition();
        else if (activeMove) activeMoveScaleManager.updateMove(transform, deviceRay.origin);
        else if( activeScale) activeMoveScaleManager.updateScale(deviceRay);

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

            



            // trigger is being pressed
            if ( (state.ulButtonPressed & SteamVR_Controller.ButtonMask.Trigger) != 0)
            {
                // trigger completely pressed
                if (prevState.rAxis1.x < 1.0f && state.rAxis1.x >= 1.0f)
                {
                    
                    handleTriggerClick();
                }
                // trigger released from completely pressed
                else if (state.rAxis1.x < 1.0f && prevState.rAxis1.x >= 1.0f)
                {
                
                    handleTriggerUnclick();
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
                else
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

    void handleTriggerClick()
    {
        if (onSlider)
        {
            moveSlider = true;
            currSliderScript.activateControlLine();
        }
        else if (currScaleQuad != null)
        {
            // start scaling
            activeMoveScaleManager = currScaleQuad.transform.parent.gameObject.GetComponent<MoveScaleManager>();
            activeScale = true;

            activeMoveScaleManager.initScale();
        }
        else if (currMoveQuad != null)
        {
            // start move
            activeMoveScaleManager = currMoveQuad.transform.parent.gameObject.GetComponent<MoveScaleManager>();
            activeMove = true;

            activeMoveScaleManager.initMove(transform, deviceRay.origin);
        }
        else
        {
            RaycastHit hitInfo;

            if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 30.0f, playAreaMask))
            {
                GameObject hitObj = hitInfo.collider.gameObject;

                if (hitObj.name.Equals("moveQuad"))
                {
                    activeMoveScaleManager = hitObj.GetComponent<MoveScaleManager>();
                    activeMove = true;

                    activeMoveScaleManager.initMove(transform, deviceRay.origin);
                }

                else if (hitObj.name.Equals("listExpandQuad"))
                {
                    actionAreaManager.openNodeList();
                }
                else if (hitObj.name.Equals("closeActiveQuad"))
                {
                    actionAreaManager.closeNodeList();
                }
                else if (hitObj.name.Equals("closeQuad"))
                {
                    NodeStatus ns = hitObj.transform.parent.gameObject.GetComponent<ListLabelManager>().nodeStatus;
                    actionAreaManager.removeActiveNode(ns);
                    ns.unselectNode();
                }

                else if (hitObj.name.Equals("listLabel"))
                {
                    NodeStatus ns = hitObj.transform.parent.gameObject.GetComponent<ListLabelManager>().nodeStatus;
                    if (ns != null) actionAreaManager.activateSelectedNode(ns);
                    else actionAreaManager.activateSelectedExteriorNode(hitObj);
                }
                else if (hitObj.name.Equals("prevPage"))
                {
                    actionAreaManager.prevNodePage();
                }
                else if (hitObj.name.Equals("nextPage"))
                {
                    actionAreaManager.nextNodePage();
                }
                else if (hitObj.name.Equals("prevPageExt"))
                {
                    actionAreaManager.prevExtNodePage();
                }
                else if (hitObj.name.Equals("nextPageExt"))
                {
                    actionAreaManager.nextExtNodePage();
                }
            }

            else if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 30.0f, nodesMask))
            {
                NodeStatus ns = currNodeObject.GetComponent<NodeStatus>();

                if(actionAreaManager.nodeIsActive(ns))
                {
                    actionAreaManager.removeActiveNode(ns);
                    ns.unselectNode();
                }
                else
                {
                    actionAreaManager.addActiveNode(ns);
                }
            }




        }
    }

    void handleTriggerUnclick()
    {
        if (onSlider)
        {
            moveSlider = false;
            currSliderScript.deactivateControlLine();

            Vector3 currPos = currSliderScript.getCurrentPosition();
            sqlConnClass.setTimeSlice(currPos.x);
        }
        else if (activeScale)
        {
            // end scaling
            activeMoveScaleManager.endScale();
            activeScale = false;
            currScaleQuad = null;
        }
        else if (activeMove)
        {
            // end scaling
            activeMoveScaleManager.endMove();
            activeMove = false;
            currMoveQuad = null;
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

    void tryHitObjects()
    {
        RaycastHit hitInfo;

        bool stopSearching = false;

        // try for timeline slider
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

            stopSearching = true;

            currMoveQuad = null;
            currScaleQuad = null;

        }
        else if(!moveSlider)
        {
            onSlider = false;
            currSliderScript.deactivateControlLine();
        }

        if (stopSearching) return;

        NodeStatus ns;



        // try for play area
        if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 30.0f, playAreaMask))
        {
            projLineObj.SetActive(true);
            linePts[0] = deviceRay.origin;
            linePts[1] = deviceRay.GetPoint(hitInfo.distance);
            lineRend.SetPositions(linePts);

            GameObject obj = hitInfo.collider.gameObject;
            if( obj.name.Equals("listLabel") )
            {
                ns = obj.transform.parent.gameObject.GetComponent<ListLabelManager>().nodeStatus;
                if( ns != null )
                {
                    
                }
            }
            else if (obj.name.Equals("closeQuad"))
            {

            }


            stopSearching = true;

            currMoveQuad = null;
            currScaleQuad = null;
        }

        if (stopSearching) return;



        // try for node info
        if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 30.0f, nodesMask))
        {
            GameObject hitObj = hitInfo.collider.gameObject;
            projLineObj.SetActive(true);
            linePts[0] = deviceRay.origin;
            linePts[1] = deviceRay.GetPoint(hitInfo.distance);
            lineRend.SetPositions(linePts);

            if (currNodeObject != hitObj)
            {
                if (currNodeObject == null)
                {
                    showNodeInfo();
                }
                else
                {
                    ns = currNodeObject.GetComponent<NodeStatus>();
                    ns.unhighlightNode();
                }

                currNodeObject = hitObj;
                ns = hitObj.GetComponent<NodeStatus>();
                ns.highlightNode();
                updateNodeInfo(ns);
            }

            stopSearching = true;

            currMoveQuad = null;
            currScaleQuad = null;

        }
        else if (currNodeObject != null)
        {
            ns = currNodeObject.GetComponent<NodeStatus>();
            ns.unhighlightNode();
            currNodeObject = null;
            hideNodeInfo();
        }

        if (stopSearching) return;

        // try for move or scale functions
        if (Physics.Raycast(deviceRay.origin, deviceRay.direction, out hitInfo, 30.0f, moveScaleMask))
        {
            GameObject hitObj = hitInfo.collider.gameObject;
            if (hitObj.name.Equals("moveQuad"))
            {
                currMoveQuad = hitObj;
                currScaleQuad = null;
            }
            else if (hitObj.name.Equals("scaleQuad"))
            {
                currMoveQuad = null;
                currScaleQuad = hitObj;
            }


            projLineObj.SetActive(true);
            linePts[0] = deviceRay.origin;
            linePts[1] = deviceRay.GetPoint(hitInfo.distance);
            lineRend.SetPositions(linePts);
            currSliderDist = hitInfo.distance;

            stopSearching = true;
        }
        else if( activeScale || activeMove )
        {
            projLineObj.SetActive(true);
            linePts[0] = deviceRay.origin;
            linePts[1] = deviceRay.GetPoint(currSliderDist);
            lineRend.SetPositions(linePts);
        }

    }

    void updateNodeInfo(NodeStatus nodeStatus)
    {
        TextMesh tm = nodeStatusText.GetComponent<TextMesh>();
        string txt = "" + nodeStatus.currIpInfo.ipAddress;
        switch(nodeStatus.currIpInfo.type)
        {
            case ipType.SERVER:
                txt += " (Server)";
                break;
            case ipType.WORKSTATION:
                txt += " (WS)";
                break;
        }

        int numHits, min;

        long ipNum = TestSQLiteConn.getIpNumFromString(nodeStatus.currIpInfo.ipAddress);

        sqlConnClass.getNFTrafficCountForNode(ipNum, out numHits, out min);
        txt += "\nTime Increment: " + min + (min > 1 ? " minutes" : " minute");
        txt += "\nNetflow Hits: " + numHits;

        int status;
        sqlConnClass.getMostRecentBigBrotherStatusForNode(ipNum, out status);

        txt += "\nBigBrother Status: ";

        switch(status)
        {
            case 1:
                txt += "Good";
                break;
            case 2:
                txt += "Warning";
                break;
            case 3:
                txt += "Problem";
                break;
            default:
                txt += "[no report]";
                break;
        }

        tm.text = txt;
    }

    void showNodeInfo()
    {
        nodeStatusObject.SetActive(true);
    }

    void hideNodeInfo()
    {
        nodeStatusObject.SetActive(false);
    }

}
