using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private StartScreen startScreen;
    [SerializeField] private TrashSpawner spawner;
    [SerializeField] private GraffitiTask graffitiTask;
    [SerializeField] private UIHandler ui;
    [SerializeField] private ItemBin[] bins;
    [SerializeField] private Transform head;   // the Main Camera under the XR Origin


    [Header("Items spawned at each difficulty")]
    [SerializeField] private int[] itemsPerDifficulty = { 6, 10, 16 };

    [Header("Editor testing")]
    [SerializeField] private bool skipStartScreen;
    [SerializeField] private int testDifficulty = 0; 
    public int Difficulty { get; private set; }

    private IGameState current;
    private int spawnedCount;

    void Start()
    {
        // for the sake of in-editor testing we just skip the start screen and go into whatever difficult in the inspector
        #if UNITY_EDITOR
        if (skipStartScreen)
        {
            SetDifficulty(testDifficulty);
            startScreen.Hide();
            ChangeState(new PlayingState(this));
            return;
        }
    #endif
        ChangeState(new StartState(this, false));
    }

    void Update()
    {
        current?.Tick();
    }

    // Called by the start screen buttons and the summary state
    public void SetDifficulty(int level)
    {
        Difficulty = Mathf.Clamp(level, 0, itemsPerDifficulty.Length - 1);
    }

    //  changes game state to playing
    public void StartGame()
    {
        ChangeState(new PlayingState(this));
    }

    // Enables a restart of the game mid-round, not currently wired
    public void ReturnToStart()
    {
        ChangeState(new StartState(this, true));
    }

    //  Changes state
    private void ChangeState(IGameState next)
    {
        current?.Exit();
        current = next;
        current.Enter();
    }

    //  resets the bins
    private void ResetBins()
    {
        foreach (ItemBin bin in bins) bin.ResetBin();
    }

    // basically forces the bin capacity to and the spawned items of that type to match
    private void MatchBinCapacities()
    {
        foreach (ItemBin bin in bins)
        {
            bin.SetCapacity(spawner.CountOf(bin.AcceptedItem));
        }
    }


    //  Game state interface
    private interface IGameState
    {
        void Enter();
        void Tick();
        void Exit();
    }

    // the starting state for the game, shows the start UI.
    private class StartState : IGameState
    {
        private readonly GameManager gm;
        private readonly bool bringToPlayer;

        public StartState(GameManager gm, bool bringToPlayer)
        {
            this.gm = gm;
            this.bringToPlayer = bringToPlayer;
        }

        public void Enter()
        {
            gm.spawner.ClearAll();
            gm.ResetBins();
            gm.ui.Show(false);
            gm.startScreen.Show("", bringToPlayer ? gm.head : null);
        }

        public void Tick() { }

        public void Exit()
        {
            gm.startScreen.Hide();
        }
    }

    // the gameplay state
    private class PlayingState : IGameState
    {
        private readonly GameManager gm;
        private float elapsed;

        public PlayingState(GameManager gm)
        {
            this.gm = gm;
        }

        public void Enter()
        {
            gm.spawnedCount = gm.spawner.Spawn(gm.itemsPerDifficulty[gm.Difficulty]);
            gm.MatchBinCapacities();

            gm.graffitiTask.Begin(gm.Difficulty);

            gm.ui.Begin(gm.spawnedCount);
            gm.ui.Show(true);
        }

        public void Tick()
        {
            elapsed += Time.deltaTime;

            if (gm.spawnedCount > 0 && gm.ui.TotalBinned >= gm.spawnedCount && gm.graffitiTask.IsComplete)
            {
                gm.ChangeState(new SummaryState(gm, elapsed));
            }
        }

        public void Exit()
        {
            gm.ui.Show(false);
        }
    }

    // end of round state showing scores, crosses over with start game state 
    private class SummaryState : IGameState
    {
        private readonly GameManager gm;
        private readonly float elapsed;

        public SummaryState(GameManager gm, float elapsed)
        {
            this.gm = gm;
            this.elapsed = elapsed;
        }

        public void Enter()
        {
            int correct = gm.ui.TotalBinned;
            int wrong = gm.ui.WrongAttempts;
            float accuracy = correct / (float)(correct + wrong);

            // simple adaptation is step up when the round went well, step down when it went poorly
            int next = gm.Difficulty;
            if (accuracy >= 0.85f) next++;
            else if (accuracy < 0.5f) next--;

            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            string results = $"Park cleaned!\nTime: {minutes}:{seconds:00}\nAccuracy: {accuracy:P0}";

            gm.startScreen.Show(results, gm.head);
            gm.startScreen.SelectDifficulty(next);   // updates the label and calls SetDifficulty
        }

        public void Tick() { }

        public void Exit()
        {
            gm.startScreen.Hide();
        }
    }
}