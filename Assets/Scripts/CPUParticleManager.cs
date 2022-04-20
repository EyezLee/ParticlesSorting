using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum SearchingMethod
{
    BRUTE_FORCE,
    QUAD_TREE
}
public class CPUParticleManager : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] int row;
    [SerializeField] int column;
    [SerializeField] public int width;
    [SerializeField] public int height;
    [SerializeField] float neighborRange = 0.25f;
    [SerializeField] SearchingMethod searchingMethod;

    private static CPUParticleManager particleManager;
    public static CPUParticleManager ParticleManager { get { return particleManager; } set { particleManager = value; } }
    

    int particleCount;

    List<GameObject> particles = new List<GameObject>();

    private void Awake()
    {
        ParticleManager = this;    
    }

    private void Start()
    {

        // initiate particles
        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < column; j++)
            {
                var go = Instantiate(prefab, new Vector3(i * ((float)width / row), j * ((float)height / column), 0), Quaternion.identity, transform);
                particles.Add(go);
            }
        }
    }

    QuadTree quadTree = new QuadTree(1, 1, 1, 1, 4);

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            float aspectRatio = Screen.width / Screen.height;
            var pos = new Vector3(Input.mousePosition.x / Screen.width * aspectRatio * width, Input.mousePosition.y / Screen.height * height, 0);
            var go = Instantiate(prefab, pos, Quaternion.identity, transform);
            particles.Add(go);

        }

        //if (Input.GetMouseButtonDown(1)) particles.Clear();

        if (searchingMethod == SearchingMethod.QUAD_TREE)
        {
            // build quad tree
            quadTree = new QuadTree(width / 2.0f, height / 2.0f, width / 2.0f, height / 2.0f, 4);
            foreach (var p in particles)
            {
                quadTree.Insert(new Vector2(p.transform.position.x, p.transform.position.y));
            }

            // query quad tree to flush neighbor list 
            foreach (var p in particles)
            {
                List<Vector2> neighbors = quadTree.Query(new Vector2(p.transform.position.x, p.transform.position.y), neighborRange, new List<Vector2>());
                //p.GetComponent<Renderer>().material.SetColor("_Color", new Color(neighbors.Count / 5f, 0, 0));
                
            }
        }
        // --------------CPU robust neighbor searching-----------------------
        else if (searchingMethod == SearchingMethod.BRUTE_FORCE)
        {
            //neighbor search and change behavior
            for (int i = 0; i < particles.Count; i++)
            {
                int count = 0;
                var self = particles[i];
                for (int j = 0; j < particles.Count; j++)
                {
                    float dist = Vector3.Distance(self.transform.position, particles[j].transform.position);
                    if (dist <= neighborRange && j != i)
                        count++;
                }
                self.GetComponent<Renderer>().material.SetColor("_Color", new Color(count / 5f, 0, 0));
            }
        }
    }

    //private void OnDrawGizmos()
    //{
    //    //Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 1, 0));
    //    if(quadTree != null) quadTree.DebugQuadTree();
    //}
}
