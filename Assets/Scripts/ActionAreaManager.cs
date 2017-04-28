using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionAreaManager : MonoBehaviour {

    public GameObject listObject;
    public GameObject listOpenObject;
    public GameObject labelTxtPrefab;

    List<GameObject> nodeList = new List<GameObject>();
    Dictionary<string, NodeStatus> currentActiveNodes = new Dictionary<string, NodeStatus>();
    List<GameObject> listLabelList = new List<GameObject>();

    string activeNodeIpAddress = "";


    // Use this for initialization
    void Start () {
        updateList();
	}
	
	// Update is called once per frame
	void Update () {
        
    }

    public void openNodeList()
    {
        listObject.SetActive(true);
        listOpenObject.SetActive(false);
    }

    public void closeNodeList()
    {
        listObject.SetActive(false);
        listOpenObject.SetActive(true);
    }

    public void addActiveNode(NodeStatus ns)
    {
        NodeStatus tns;

        if (currentActiveNodes.Count >= 20) return;

        if( !currentActiveNodes.TryGetValue(ns.currIpInfo.ipAddress, out tns))
        {
            currentActiveNodes.Add(ns.currIpInfo.ipAddress, ns);
        }

        updateList();
    }

    public void removeActiveNode(NodeStatus ns)
    {
        NodeStatus tns;

        if (currentActiveNodes.TryGetValue(ns.currIpInfo.ipAddress, out tns))
        {
            currentActiveNodes.Remove(ns.currIpInfo.ipAddress);
        }

        if(activeNodeIpAddress.Equals(ns.currIpInfo.ipAddress))
        {
            activeNodeIpAddress = "";
            updateActiveNode();
        }

        updateList();
    }

    public void activateSelectedNode(NodeStatus ns)
    {
        if (!activeNodeIpAddress.Equals(ns.currIpInfo.ipAddress))
        {
            Debug.Log("Making node " + ns.currIpInfo.ipAddress + " active");
            activeNodeIpAddress = ns.currIpInfo.ipAddress;
            updateList();
            updateActiveNode();
        }
    }

    public void updateActiveNode()
    {

    }

    public void updateList()
    {
        foreach(GameObject go in listLabelList)
        {
            GameObject.Destroy(go);
        }

        listLabelList.Clear();

        Vector3 offset = new Vector3(0.0f, 0.0f, -0.01f);
        Vector3 yOffsetInc = listObject.transform.up * -0.05f;

        if ( currentActiveNodes.Count < 1 )
        {
            GameObject txtObj = (GameObject)Instantiate(labelTxtPrefab);
            ListLabelManager man = txtObj.GetComponent<ListLabelManager>();
            man.nodeStatus = null;

            txtObj.transform.SetParent(listObject.transform);

            txtObj.transform.position = listObject.transform.position;
            txtObj.transform.rotation = listObject.transform.rotation;

            listLabelList.Add(txtObj);

            return;
        }

        //float yOffsetInc = -0.05f;
        
        foreach(KeyValuePair<string, NodeStatus> entry in currentActiveNodes )
        {
            GameObject txtObj = (GameObject)Instantiate(labelTxtPrefab);
            ListLabelManager man = txtObj.GetComponent<ListLabelManager>();
            man.nodeStatus = entry.Value;

            TextMesh mesh = man.labelText.GetComponent<TextMesh>();
            mesh.text = entry.Key;

            if( activeNodeIpAddress.Equals(entry.Key) )
            {
                mesh.color = Color.yellow;
            }

            txtObj.transform.SetParent(listObject.transform);

            listLabelList.Add(txtObj);

            txtObj.transform.position = listObject.transform.position + offset;
            txtObj.transform.rotation = listObject.transform.rotation;

            offset += yOffsetInc;

        }
    }
}
