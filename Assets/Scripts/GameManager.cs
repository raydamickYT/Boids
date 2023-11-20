using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    const int threadGroupSize = 1024;

    public BoidSettings settings;
    public int BoidsToSpawn;
    public GameObject Boids;
    public Renderer PlayAreaCubeRenderer;
    public Bounds PlayAreaBounds;
    public ComputeShader compute;
    //dit is alleen voor organisatie
    [SerializeField]
    private Transform BoidsParent;
    public readonly List<BoidsControlScript> BoidsControls = new List<BoidsControlScript>();

    //boids list
    private BoidsControlScript[] boids;
    void Start()
    {
        boids = new BoidsControlScript[BoidsToSpawn];
        Initialize();
    }
    // Update is called once per frame
    void Update()
    {
        // Observer.UpdateBoids?.Invoke();

        if (boids != null)
        {
            int numBoids = boids.Length;
            var boidData = new BoidData[numBoids];

            //load in all the data for the current boids in the scene
            for (int i = 0; i < boids.Length; i++)
            {
                boidData[i].position = boids[i].position;
                boidData[i].direction = boids[i].forward;
            }

            var boidBuffer = new ComputeBuffer(numBoids, BoidData.Size);
            boidBuffer.SetData(boidData);

            //compute is de computeshader die we assignen in de inspector
            compute.SetBuffer(0, "boids", boidBuffer);
            compute.SetInt("numBoids", boids.Length);
            compute.SetFloat("viewRadius", settings.perceptionRadius);
            compute.SetFloat("avoidRadius", settings.avoidanceRadius);

            //voor dit soort tasks wil je graag naar boven afronden
            int threadGroup = Mathf.CeilToInt(numBoids / (float)threadGroupSize);
            compute.Dispatch(0, threadGroup, 1, 1);

            //hier wordt alle data van de computeshader in de boidData geladen
            boidBuffer.GetData(boidData);

            //en hier wordt alle data overgeschreven naar de monobehaviour zodat we de boid behaviour kunnen updaten.
            for (int i = 0; i < boids.Length; i++)
            {
                boids[i].avgFlockHeading = boidData[i].flockHeading;
                boids[i].centreOfFlockmates = boidData[i].flockCentre;
                boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
                boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

                boids[i].UpdateBoids();
            }
            
            boidBuffer.Release();
        }
    }

    private void Initialize()
    {
        if (Boids == null)
        {
            Debug.LogWarning("Boids is not defined in GameManager");
        }
        if (PlayAreaCubeRenderer != null)
        {
            PlayAreaBounds = PlayAreaCubeRenderer.bounds;
        }
        else
        {
            Debug.LogWarning("PlayAreaCubeRenderer is undefined in GameManager");
        }
        for (int i = 0; i < BoidsToSpawn; i++)
        {
            var tempBoid = Instantiate(Boids);
            tempBoid.name += i;
            tempBoid.transform.SetParent(BoidsParent);
            BoidsControlScript tempBoidsControlScript = new BoidsControlScript(tempBoid, PlayAreaBounds, settings);
            boids[i] = tempBoidsControlScript;
        }
    }


    public struct BoidData
    {
        public Vector3 position;
        public Vector3 direction;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 avoidanceHeading;
        public int numFlockmates;

        public static int Size
        {
            get
            {
                return sizeof(float) * 3 * 5 + sizeof(int);
            }
        }
    }
}
