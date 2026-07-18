using System.Collections.Generic;
using UnityEngine;

public class FixedLayoutMazeGenerator : MonoBehaviour
{
    private static readonly string[] H = new string[]
    {
        "###########",
        "#.###.#....",
        ".#.#....##.",
        "#...#..#..#",
        ".###.##.##.",
        "#..##..####",
        "..#..#####.",
        "...###.....",
        ".#..#.#....",
        "###..#.#..#",
        "#.#.#.#..##",
        "###########",
    };

    private static readonly string[] V = new string[]
    {
        "#########.#",
        ".....###...",
        ".##.##.#...",
        "..##...#.#.",
        "..#..##..##",
        "##.##..##..",
        ".##..#...#.",
        ".###..###.#",
        ".#..#.##.#.",
        "#.##...###.",
        ".#....###..",
        "#########.#",
    };

    private const int GRID_SIZE = 11;

    [Header("Prefabs principais")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Dimensões")]
    public float cellSize = 4f;
    public float wallHeight = 1f;
    public float wallThickness = 0.2f;
    public bool wallPivotIsCentered = true;
    public float wallYOffset = 0f;
    public bool wallLongAxisIsZ = false;

    [Header("Chão")]
    public float floorThickness = 0.5f;
    public bool autoScaleFloor = true;

    [System.Serializable]
    public class ObstacleEntry
    {
        public GameObject prefab;
        public Vector3 scaleMultiplier = Vector3.one;
        public float yOffset = 0f;
        public float yRotation = 0f;
    }

    [Header("Obstáculos (colocados nos becos sem saída do labirinto)")]
    public List<ObstacleEntry> obstacles = new List<ObstacleEntry>();

    public int seed = -1;
    public bool generateOnStart = true;

    private Transform mazeParent;

    void Start()
    {
        if (generateOnStart) GenerateMaze();
    }

    [ContextMenu("Generate Maze")]
    public void GenerateMaze()
    {
        if (wallPrefab == null)
        {
            Debug.LogError("FixedLayoutMazeGenerator: arraste o Wall_Mesh no campo 'Wall Prefab'.");
            return;
        }

        if (seed != -1) Random.InitState(seed);

        ClearPrevious();
        BuildWalls();
        BuildFloor();
        BuildObstacles();
    }

    private void ClearPrevious()
    {
        Transform existing = transform.Find("MazeContainer");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }

        GameObject container = new GameObject("MazeContainer");
        container.transform.SetParent(transform, false);
        mazeParent = container.transform;
    }

    private bool WallTop(int r, int c) => H[r][c] == '#';
    private bool WallBottom(int r, int c) => H[r + 1][c] == '#';
    private bool WallLeft(int r, int c) => V[c][r] == '#';
    private bool WallRight(int r, int c) => V[c + 1][r] == '#';

    private void BuildWalls()
    {
        Transform parent = new GameObject("Walls").transform;
        parent.SetParent(mazeParent, false);

        for (int r = 0; r < GRID_SIZE; r++)
        {
            for (int c = 0; c < GRID_SIZE; c++)
            {
                Vector3 origin = new Vector3(c * cellSize, 0f, r * cellSize);

                if (WallTop(r, c))
                    PlaceWall(parent, origin + new Vector3(cellSize / 2f, 0f, cellSize), 0f);

                if (r == 0 && WallBottom(r, c))
                    PlaceWall(parent, origin + new Vector3(cellSize / 2f, 0f, 0f), 0f);

                if (WallLeft(r, c))
                    PlaceWall(parent, origin + new Vector3(0f, 0f, cellSize / 2f), 90f);

                if (c == GRID_SIZE - 1 && WallRight(r, c))
                    PlaceWall(parent, origin + new Vector3(cellSize, 0f, cellSize / 2f), 90f);
            }
        }
    }

    private void PlaceWall(Transform parent, Vector3 posXZ, float yRot)
    {
        GameObject wall = Instantiate(wallPrefab, parent);
        wall.transform.localPosition = Vector3.zero;
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = Vector3.one;

        Vector3 nativeSize = GetRendererSize(wall);
        Vector3 desired = wallLongAxisIsZ
            ? new Vector3(wallThickness, wallHeight, cellSize)
            : new Vector3(cellSize, wallHeight, wallThickness);

        Vector3 scale = new Vector3(
            nativeSize.x > 0.0001f ? desired.x / nativeSize.x : 1f,
            nativeSize.y > 0.0001f ? desired.y / nativeSize.y : 1f,
            nativeSize.z > 0.0001f ? desired.z / nativeSize.z : 1f
        );

        float pivotCorrection = wallPivotIsCentered ? (wallHeight / 2f) : 0f;
        wall.transform.localPosition = new Vector3(posXZ.x, pivotCorrection + wallYOffset, posXZ.z);
        wall.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
        wall.transform.localScale = scale;
    }

    private Vector3 GetRendererSize(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"FixedLayoutMazeGenerator: '{go.name}' não tem nenhum Renderer, não foi possível medir o tamanho automaticamente. Usando escala 1,1,1.");
            return Vector3.one;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.size;
    }

    private void BuildFloor()
    {
        if (floorPrefab == null) return;

        Transform parent = new GameObject("Floor").transform;
        parent.SetParent(mazeParent, false);

        for (int r = 0; r < GRID_SIZE; r++)
        {
            for (int c = 0; c < GRID_SIZE; c++)
            {
                Vector3 center = new Vector3(c * cellSize + cellSize / 2f, 0f, r * cellSize + cellSize / 2f);
                GameObject tile = Instantiate(floorPrefab, parent);
                tile.name = $"FloorTile_{r}_{c}";

                if (autoScaleFloor)
                {
                    tile.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
                    tile.transform.localPosition = new Vector3(center.x, -floorThickness / 2f, center.z);
                }
                else
                {
                    tile.transform.localPosition = new Vector3(center.x, 0f, center.z);
                }
            }
        }
    }

    private List<Vector2Int> FindDeadEnds()
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        for (int r = 0; r < GRID_SIZE; r++)
        {
            for (int c = 0; c < GRID_SIZE; c++)
            {
                int wallCount = 0;
                if (WallTop(r, c)) wallCount++;
                if (WallBottom(r, c)) wallCount++;
                if (WallLeft(r, c)) wallCount++;
                if (WallRight(r, c)) wallCount++;

                if (wallCount == 3)
                    deadEnds.Add(new Vector2Int(r, c));
            }
        }
        return deadEnds;
    }

    private void BuildObstacles()
    {
        if (obstacles == null || obstacles.Count == 0) return;

        Transform parent = new GameObject("Obstacles").transform;
        parent.SetParent(mazeParent, false);

        List<Vector2Int> deadEnds = FindDeadEnds();

        for (int i = deadEnds.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deadEnds[i], deadEnds[j]) = (deadEnds[j], deadEnds[i]);
        }

        for (int i = 0; i < deadEnds.Count; i++)
        {
            ObstacleEntry entry = obstacles[i % obstacles.Count];
            if (entry.prefab == null) continue;

            Vector2Int cell = deadEnds[i];
            Vector3 center = new Vector3(
                cell.y * cellSize + cellSize / 2f,
                entry.yOffset,
                cell.x * cellSize + cellSize / 2f
            );

            GameObject obj = Instantiate(entry.prefab, parent);
            obj.name = $"Obstacle_{entry.prefab.name}_{cell.x}_{cell.y}";
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            Vector3 nativeSize = GetRendererSize(obj);
            Vector3 desiredBase = new Vector3(cellSize * 0.6f, nativeSize.y, cellSize * 0.6f);
            Vector3 autoScale = new Vector3(
                nativeSize.x > 0.0001f ? desiredBase.x / nativeSize.x : 1f,
                1f,
                nativeSize.z > 0.0001f ? desiredBase.z / nativeSize.z : 1f
            );

            obj.transform.localPosition = center;
            obj.transform.localRotation = Quaternion.Euler(0f, entry.yRotation, 0f);
            obj.transform.localScale = Vector3.Scale(autoScale, entry.scaleMultiplier);
        }
    }
}