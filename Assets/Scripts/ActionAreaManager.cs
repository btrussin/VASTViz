using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionAreaManager : MonoBehaviour {

    public GameObject listObject;

    public GameObject labelTxtPrefab;

    List<GameObject> nodeList = new List<GameObject>();
    Dictionary<string, NodeStatus> currentActiveNodes = new Dictionary<string, NodeStatus>();
    List<GameObject> listLabelList = new List<GameObject>();


    // Use this for initialization
    void Start () {
        updateList();
	}
	
	// Update is called once per frame
	void Update () {
        
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

        updateList();
    }

    public void updateList()
    {
        foreach(GameObject go in listLabelList)
        {
            GameObject.Destroy(go);
        }

        listLabelList.Clear();

        Vector3 offset = new Vector3(0.0f, 0.0f, -0.01f);

        if ( currentActiveNodes.Count < 1 )
        {
            GameObject txtObj = (GameObject)Instantiate(labelTxtPrefab);
            ListLabelManager man = txtObj.GetComponent<ListLabelManager>();
            man.nodeStatus = null;

            txtObj.transform.SetParent(listObject.transform);

            txtObj.transform.position = listObject.transform.position;

            listLabelList.Add(txtObj);

            return;
        }

        float yOffsetInc = -0.05f;
        
        foreach(KeyValuePair<string, NodeStatus> entry in currentActiveNodes )
        {
            GameObject txtObj = (GameObject)Instantiate(labelTxtPrefab);
            ListLabelManager man = txtObj.GetComponent<ListLabelManager>();
            man.nodeStatus = entry.Value;

            TextMesh mesh = man.labelText.GetComponent<TextMesh>();
            mesh.text = entry.Key;

            txtObj.transform.SetParent(listObject.transform);

            listLabelList.Add(txtObj);

            txtObj.transform.position = listObject.transform.position + offset;

            offset.y += yOffsetInc;

        }
    }
}
