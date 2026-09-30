using System.Collections.Generic;
using UnityEngine;

public class BirdManager : MonoBehaviour
{
    [SerializeField] private Bird birdPrefab;
    [SerializeField] private BoxCollider flightArea;          // flight volume
    [SerializeField] private BoxCollider landingArea;   // trash spawn volume
    [SerializeField] private Transform player;          // defaults to the main camera
    [SerializeField] private int startCount = 5;
    [SerializeField] private float litterRefreshInterval = 2f;

    private readonly List<Bird> birds = new List<Bird>();
    private Item[] litter = new Item[0];
    private float nextRefresh;

    public BoxCollider FlightArea => flightArea;
    public BoxCollider LandingArea => landingArea;
    public Transform Player => player;
    public Item[] Litter => litter;

    void Awake()
    {
        if (player == null && Camera.main != null) player = Camera.main.transform;
    }

    void Start()
    {
        litter = FindObjectsByType<Item>(FindObjectsSortMode.None);
        SetCount(startCount);
    }

    void Update()
    {
        if (Time.time < nextRefresh) return;
        litter = FindObjectsByType<Item>(FindObjectsSortMode.None);
        nextRefresh = Time.time + litterRefreshInterval;
    }

    public void SetCount(int count)
    {
        while (birds.Count < count)
        {
            Bird bird = Instantiate(birdPrefab);
            bird.Init(this);
            birds.Add(bird);
        }
        while (birds.Count > count)
        {
            Destroy(birds[birds.Count - 1].gameObject);
            birds.RemoveAt(birds.Count - 1);
        }
    }
}