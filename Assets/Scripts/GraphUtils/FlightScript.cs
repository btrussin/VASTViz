using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class FlightScript : MonoBehaviour {

    public GameObject spherePrefab;
    public GameObject cylinderPrefab;
    public Material tmpMat;

    public ParticleSystem system;

    public float nodeScale;
    public float edgeScale;

    public bool includeEdges;

    public int numPoints = 5000;


    public bool useParticles;



    // Use this for initialization
    void Start () {
        
        init();
    }

    void init()
    {
        

       
        RandomWalk3D flight = RandomWalk3D.createWalk(numPoints, 100, 100);

        Vector3 min = new Vector3(-3.0f, 0.0f, -2.2f);
        Vector3 max = new Vector3(0.3f, 4.0f, 2.2f);

        flight.scale(min, max);

        List<Vector3> ptList = flight.getPoints();
        Vector3[] pts = ptList.ToArray();

        if( useParticles )
        {
            ParticleSystem.MainModule main = system.main;
            main.maxParticles = pts.Length;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emMod = system.emission;
            emMod.enabled = false;

            ParticleSystem.ShapeModule shMod = system.shape;
            shMod.enabled = false;


            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[pts.Length];

            for (int i = 0; i < pts.Length; i++)
            {

                particles[i] = new ParticleSystem.Particle();
                particles[i].position = pts[i];
                particles[i].velocity = Vector3.zero;
                particles[i].angularVelocity = 0.0f;
                particles[i].rotation = 0.0f;
                particles[i].startSize = 0.1f;
                particles[i].startLifetime = 10000.0f;
                particles[i].remainingLifetime = 10000.0f;
                particles[i].randomSeed = 0;
                particles[i].startColor = Color.white;


            }

            system.SetParticles(particles, pts.Length);
        }
        else
        {
            Vector3 cylTrans = new Vector3(0.0f, 0.1f, 0.0f);

            Vector3 sphereScale = new Vector3(nodeScale, nodeScale, nodeScale);
            Vector3 cylScale = new Vector3(edgeScale, 0.0f, edgeScale);

            for (int i = 0; i < pts.Length; i++)
            {

                GameObject sphere = getNewPoint();
                sphere.transform.position = pts[i];
                sphere.transform.localScale = sphereScale;
                GameObject sphere2 = getNewPoint();
                sphere2.transform.position = pts[i];
                sphere2.transform.localScale = sphereScale;
                sphere2.transform.localRotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                GameObject sphere3 = getNewPoint();
                sphere3.transform.position = pts[i];
                sphere3.transform.localScale = sphereScale;
                sphere3.transform.localRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

                if ( includeEdges )
                {
                    if (i == 0) continue;

                    GameObject cylinder = (GameObject)Instantiate(cylinderPrefab);

                    Vector3 dir = pts[i] - pts[i - 1];
                    float len = dir.magnitude;
                    dir = Vector3.Normalize(dir);


                    cylinder.transform.up = dir;
                    cylinder.transform.position = pts[i - 1] + dir * len * 0.5f;
                    Vector3 currScale = cylScale;
                    currScale.y = len * 0.5f;
                    cylinder.transform.localScale = currScale;
                }
                

            }
        }
        
    }

    GameObject getNewPoint()
    {
        /*
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sphere.transform.localScale = new Vector3(0.1f, 0.1f, 1.0f);
        MeshRenderer rend = sphere.GetComponent<MeshRenderer>();
        rend.material = tmpMat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        */
        GameObject sphere = (GameObject)Instantiate(spherePrefab);


        return sphere;
    }
	
	// Update is called once per frame
	void Update () {
        
    }
}
