using UnityEngine;

public class Bird : MonoBehaviour
{
    private enum State { Flying, Fleeing, Landing, Grounded }

    [Header("Area")]
    [SerializeField] private BoxCollider area;
    [SerializeField] private LayerMask groundMask;          
    [SerializeField] private float groundOffset = 0.05f;    

    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float speedVariation = 0.3f;
    [SerializeField] private float steering = 2f;
    [SerializeField] private float arriveDistance = 0.5f;

    [Header("Scatter")]
    [SerializeField] private Transform player;              
    [SerializeField] private float scareDistance = 4f;
    [SerializeField] private float fleeSpeedMultiplier = 2f;
    [SerializeField] private float fleeDuration = 3f;

    [Header("Landing")]
    [Range(0f, 1f)] [SerializeField] private float landChance = 0.3f;    // rolled each time it reaches a flight target
    [Range(0f, 1f)] [SerializeField] private float litterChance = 0.6f;  // chance the landing spot is near litter
    [SerializeField] private float litterRadius = 1f;
    [SerializeField] private float minLandDistanceFromPlayer = 6f;
    [Min(0f)] [SerializeField] private float minGroundTime = 4f;
    [Min(0f)] [SerializeField] private float maxGroundTime = 8f;
    [Header("Wings")]
    [SerializeField] private Transform wingR;   // the +X wing pivot
    [SerializeField] private Transform wingL;   // the -X wing pivot
    [SerializeField] private float flapSpeed = 18f;
    [SerializeField] private float flapAngle = 45f;
    [SerializeField] private float foldedAngle = -70f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] chirps;
    [SerializeField] private AudioClip flapClip;
    [Min(0.5f)] [SerializeField] private float minChirpInterval = 4f;
    [Min(0.5f)] [SerializeField] private float maxChirpInterval = 12f;
    [Min(1)] [SerializeField] private int maxSimultaneousChirps = 3;
    [Range(0f, 1f)] [SerializeField] private float chirpVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float flapVolume = 0.8f;

    private float nextChirpTime;
    private int activeChirps;
    private int lastChirpIndex = -1;

    private BoxCollider landingArea;
    private BirdManager manager;

    private State state = State.Flying;
    private Vector3 target;
    private Vector3 velocity;
    private float mySpeed;
    private float stateTimer;
    private float wingAngle;
    private float flapPhase;


   public void Init(BirdManager m)
    {
        manager = m;
        area = m.FlightArea;
        landingArea = m.LandingArea;
        player = m.Player;
    }

    void Start()
    {
        if (player == null && Camera.main != null) player = Camera.main.transform;

        mySpeed = speed * (1f + Random.Range(-speedVariation, speedVariation));
        transform.position = RandomPoint();
        target = RandomPoint();
        velocity = (target - transform.position).normalized * mySpeed;
        flapPhase = Random.value * 6.28f; //  this is to preent synchronised flapping
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.pitch = Random.Range(0.8f, 1.2f);   // gives each bird its own voice
        ScheduleChirp();
    }

    void Update()
    {
        if (player != null && state != State.Fleeing &&
            FlatDistance(transform.position, player.position) < scareDistance)
            StartFlee();

        switch (state)
        {
            case State.Flying:
                Steer(mySpeed);
                if (Vector3.Distance(transform.position, target) < arriveDistance) PickNextFlightTarget();
                break;

            case State.Fleeing:
                Steer(mySpeed * fleeSpeedMultiplier);
                stateTimer -= Time.deltaTime;
                if (Vector3.Distance(transform.position, target) < arriveDistance) target = FleeTarget();
                if (stateTimer <= 0f)
                {
                    state = State.Flying;
                    target = RandomPoint();
                }
                break;

            case State.Landing:
                float dist = Vector3.Distance(transform.position, target);
                Steer(Mathf.Clamp(dist * 2f, 0.5f, mySpeed));
                if (dist < 0.15f)
                {
                    transform.position = target;
                    velocity = Vector3.zero;
                    stateTimer = Random.Range(minGroundTime, maxGroundTime);
                    state = State.Grounded;
                }
                break;

            case State.Grounded:
                Vector3 flat = transform.forward; flat.y = 0f;
                if (flat.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(flat), Time.deltaTime * 4f);

                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) TakeOff();
                break;
        }

        if (state != State.Fleeing && Time.time >= nextChirpTime)
        {
            PlayChirp();
            ScheduleChirp();
        }

        AnimateWings();
    }

    private void PickNextFlightTarget()
    {
        if (Random.value < landChance && TryFindLandingSpot(out Vector3 spot))
        {
            target = spot;
            state = State.Landing;
        }
        else target = RandomPoint();
    }

    private void StartFlee()
    {
        if (state == State.Grounded) PlayFlap();

        state = State.Fleeing;
        stateTimer = fleeDuration;
        target = FleeTarget();

        // Burst away from the player so grounded birds launch instead of sliding off
        Vector3 away = transform.position - player.position;
        away.y = 0f;
        velocity = (away.normalized + Vector3.up).normalized * mySpeed * fleeSpeedMultiplier * 0.5f;
    }

    private void TakeOff()
    {
        state = State.Flying;
        target = RandomPoint();
        velocity = (Vector3.up + Random.insideUnitSphere * 0.3f).normalized * mySpeed * 0.5f;
        PlayFlap();
    }

    // Samples a few points in the area and takes the one furthest from the player
    private Vector3 FleeTarget()
    {
        Vector3 best = RandomPoint();
        float bestDist = -1f;
        for (int i = 0; i < 6; i++)
        {
            Vector3 p = RandomPoint();
            float d = FlatDistance(p, player.position);
            if (d > bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    private bool TryFindLandingSpot(out Vector3 spot)
    {
        Item[] litter = Random.value < litterChance ? FindObjectsByType<Item>(FindObjectsSortMode.None): null;

        for (int i = 0; i < 10; i++)
        {
            Vector3 guess = RandomPoint();

            if (litter != null && litter.Length > 0)
            {
                Vector3 p = litter[Random.Range(0, litter.Length)].transform.position;
                Vector2 off = Random.insideUnitCircle.normalized * Random.Range(litterRadius * 0.5f, litterRadius);
                guess = p + new Vector3(off.x, 0f, off.y);
            }

            Vector3 origin = new Vector3(guess.x, area.bounds.max.y + 5f, guess.z);
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, groundMask, QueryTriggerInteraction.Ignore)) continue;

            Vector3 flatPoint = hit.point;
            flatPoint.y = area.bounds.center.y;
            if (!area.bounds.Contains(flatPoint)) continue;

            Vector3 candidate = hit.point + Vector3.up * groundOffset;
            if (player != null && FlatDistance(candidate, player.position) < minLandDistanceFromPlayer) continue;

            spot = candidate;
            return true;
        }

        spot = default;
        return false;
    }

    private void Steer(float moveSpeed)
    {
        Vector3 desired = (target - transform.position).normalized * moveSpeed;
        velocity = Vector3.Lerp(velocity, desired, Time.deltaTime * steering);
        transform.position += velocity * Time.deltaTime;

        if (velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * steering * 2f);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Vector3 RandomPoint()
    {
        Vector3 half = area.size * 0.5f;
        Vector3 local = area.center + new Vector3(
            Random.Range(-half.x, half.x),
            Random.Range(-half.y, half.y),
            Random.Range(-half.z, half.z));
        return area.transform.TransformPoint(local);
    }

    private void AnimateWings()
    {
        if (wingR == null || wingL == null) return;

        float target = state != State.Grounded
            ? Mathf.Sin(Time.time * flapSpeed + flapPhase) * flapAngle
            : foldedAngle;

        wingAngle = Mathf.Lerp(wingAngle, target, Time.deltaTime * 15f);

        wingR.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
        wingL.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
    }

    private void ScheduleChirp()
    {
        nextChirpTime = Time.time + Random.Range(minChirpInterval, maxChirpInterval);
    }

    //  There are extra limits here to limit the number of bird calls at once, it can get uncomfortable
    private void PlayChirp()
    {
        if (chirps == null || chirps.Length == 0)
            return;

        if (activeChirps >= maxSimultaneousChirps)
            return;

        int index;

        if (chirps.Length == 1)
        {
            index = 0;
        }
        else
        {
            do
            {
                index = Random.Range(0, chirps.Length);
            }
            while (index == lastChirpIndex);
        }

        lastChirpIndex = index;

        AudioClip clip = chirps[index];

        activeChirps++;

        audioSource.PlayOneShot(clip, chirpVolume);

        StartCoroutine(ReleaseChirp(clip));
    }

    private System.Collections.IEnumerator ReleaseChirp(AudioClip clip)
    {
        yield return new WaitForSeconds(clip.length / audioSource.pitch);
        activeChirps--;
    }

    private void PlayFlap()
    {
        if (flapClip != null) audioSource.PlayOneShot(flapClip, flapVolume);
    }
}