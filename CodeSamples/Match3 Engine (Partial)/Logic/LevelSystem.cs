using Assets.Scripts.General.MonoGameRandom;
using Standard;
using UnityEditor;
using UnityEngine;

public class LevelSystem : MonoBehaviour
{
    public static LevelSystem Instance { get; private set; }
    
    [SerializeField] private Transform _position;
    [SerializeField] private Transform _scale;
    public Transform Scale => _scale;
    [SerializeField] public Transform CellsTransform;
    [SerializeField] public Transform ItemsTransform;
    
    // Debug:
    [SerializeField, Range(0, 3)] public float TickTime = 0.5f;
    [SerializeField] public bool InterpolationEnabled;
    public readonly DebugSpawnSpriteSystem DebugSpawnSpriteSystem = new();

    // Logic systems:
    public static XorShiftRandomController XorShiftRandomController;
    private readonly LevelConfigLoadSystem _levelConfigLoadSystem = new();
    private readonly LevelCalculatePathsSystem _levelCalculatePathsSystem = new();
    private readonly LevelSpawnItemsSystem _levelSpawnItemsSystem = new();
    public readonly LevelPlaySystem LevelPlaySystem = new();
    
    // Visual systems:
    [SerializeField] public VisualCacheSystem VisualCacheSystem;
    public readonly LevelSpawnVisualCellsSystem LevelSpawnVisualCellsSystem = new();
    public readonly LevelSpawnVisualItemsSystem LevelSpawnVisualItemsSystem = new();
    public readonly LevelSpawnVisualItemModifiersSystem LevelSpawnVisualItemModifiersSystem = new();
    
    private readonly LevelPositioningSystem _levelPositioningSystem = new();
    
    // Variables:
    public Level Level {get; private set;}
    private float _refreshCountdownTimer;
    public int TickId { get; private set; }

    private void Awake()
    {
        Instance = this;

        _levelPositioningSystem.Init(_position, _scale);
        
        VisualCacheSystem.Init();
        
        XorShiftRandomController = new XorShiftRandomController(1000);
        var debugLevelHelper = new DebugLevelHelper(this);
    }
    
    private void Start()
    {
        var levelConfig = _levelConfigLoadSystem.Load();
        Level = new Level(levelConfig);
    }

    private void Update()
    {
            CoreLoop();
    }

    private void CoreLoop()
    {
        _refreshCountdownTimer -= Time.deltaTime;
        if (_refreshCountdownTimer <= 0)
        {
            LevelSpawnVisualCellsSystem.SpawnOrDestroyCells(Level);
            _levelCalculatePathsSystem.CalculateCellPaths(Level);
            
            LevelPlaySystem.Play(Level);
            
            _levelSpawnItemsSystem.SpawnNewItems(Level);
            LevelSpawnVisualItemsSystem.SpawnNewItems(Level);
            
            _refreshCountdownTimer += TickTime;
            TickId++;
        }
        
        _levelPositioningSystem.FitLevelToCorrectPositionAndScaleInScreen(Level);
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(LevelSystem), true)]
    public class LevelSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Label("");
            
            var levelSystem = (LevelSystem) target;
            if (levelSystem.Level == null) return;            
            
            GUILayout.Label($"Cells order:");
            foreach (var cell in levelSystem.Level.Cells)
            {
                if (GUILayout.Button($"{cell}"))
                {
                    levelSystem.LevelSpawnVisualCellsSystem.CellAndCellVisuals[cell].gameObject.FocusOnGameObject();
                }
            }
        }
    }
#endif
}