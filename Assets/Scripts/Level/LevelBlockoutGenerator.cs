using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FavelaAmarela.Level
{
    public class LevelBlockoutGenerator : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField] private float wallThickness = 0.5f;
        [SerializeField] private float wallHeight = 2.0f; // Useful if using 3D blocks for 2D isometric layout
        [SerializeField] private Material wallMaterial;

        [Header("Zone 1: Rua de Entrada (goes East)")]
        [SerializeField] private float z1Length = 20.0f;
        [SerializeField] private float z1Width = 4.0f;

        [Header("Zone 2: Vila das Casas (goes South)")]
        [SerializeField] private float z2Length = 18.0f;
        [SerializeField] private float z2Width = 14.0f;
        [SerializeField] private float houseSize = 4.0f;

        [Header("Zone 3: Beco do Vento (goes West)")]
        [SerializeField] private float z3Length = 15.0f;
        [SerializeField] private float z3Width = 2.5f; // Narrower bottleneck

        [Header("Zone 4: Praça do Cerco (goes South)")]
        [SerializeField] private float z4Length = 12.0f;
        [SerializeField] private float z4Width = 12.0f; // Large arena cul-de-sac

        [Header("Generated Objects Root")]
        [SerializeField] private Transform generationRoot;

        [ContextMenu("Generate S-Path Blockout")]
        public void GenerateBlockout()
        {
            ClearExisting();

            if (generationRoot == null)
            {
                GameObject rootGo = new GameObject("Blockout_Root");
                rootGo.transform.parent = this.transform;
                generationRoot = rootGo.transform;
            }

            Vector3 currentOrigin = Vector3.zero;

            // ZONA 1: Rua de Entrada (Runs East -> +X)
            // Left (West) wall is blocked (entrance)
            // Bottom (South) and Top (North) walls form the street.
            BuildBoxRoom("Zona1_RuaEntrada", currentOrigin, z1Length, z1Width, 
                openDirections: new List<Vector2> { Vector2.right }, // Open to the East
                blockedDirections: new List<Vector2> { Vector2.left });

            // ZONA 2: Vila das Casas (Runs South -> -Y)
            // Transition offset: We connect the East end of Zone 1 to the North-East corner of Zone 2.
            currentOrigin = new Vector3(z1Length - z1Width, 0f, 0f);
            
            // Generate Vila Room (Open North to receive Zone 1, Open South at the bottom-left to connect Zone 3)
            BuildBoxRoom("Zona2_VilaDasCasas", currentOrigin + new Vector3(0f, -z2Length/2f + z1Width/2f, 0f), z2Width, z2Length,
                openDirections: new List<Vector2> { Vector2.up, Vector2.down }); // Open North & South

            // Spawn 3 Houses in the Vila (as simple box walls with a small opening)
            SpawnHouse("Casa_1", currentOrigin + new Vector3(2f, -4f, 0f), houseSize);
            SpawnHouse("Casa_2", currentOrigin + new Vector3(-2f, -8f, 0f), houseSize);
            SpawnHouse("Casa_3", currentOrigin + new Vector3(3f, -12f, 0f), houseSize);

            // ZONA 3: Beco do Vento (Runs West -> -X)
            // Starts at the bottom of the Vila and runs left (-X)
            currentOrigin = currentOrigin + new Vector3(0f, -z2Length + z1Width/2f, 0f);
            BuildBoxRoom("Zona3_BecoDoVento", currentOrigin + new Vector3(-z3Length/2f, 0f, 0f), z3Length, z3Width,
                openDirections: new List<Vector2> { Vector2.right, Vector2.left }); // Open East (from Vila) and West (to Praça)

            // ZONA 4: Praça do Cerco (Runs South -> -Y)
            // Connects to the West end of Zone 3, runs downwards
            currentOrigin = currentOrigin + new Vector3(-z3Length, 0f, 0f);
            BuildBoxRoom("Zona4_PracaDoCerco", currentOrigin + new Vector3(0f, -z4Length/2f + z3Width/2f, 0f), z4Width, z4Length,
                openDirections: new List<Vector2> { Vector2.up }, // Open North (from Beco)
                blockedDirections: new List<Vector2> { Vector2.down, Vector2.left, Vector2.right }); // Dead end!
        }

        [ContextMenu("Clear Blockout")]
        public void ClearExisting()
        {
            if (generationRoot == null) return;
            
            // Destroy all children in editor safely
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in generationRoot)
            {
                children.Add(child.gameObject);
            }

            foreach (var child in children)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child);
                else
#endif
                    Destroy(child);
            }
        }

        private void BuildBoxRoom(string name, Vector3 center, float widthX, float heightY, List<Vector2> openDirections, List<Vector2> blockedDirections = null)
        {
            GameObject roomParent = new GameObject(name);
            roomParent.transform.parent = generationRoot;
            roomParent.transform.position = center;

            float halfX = widthX * 0.5f;
            float halfY = heightY * 0.5f;

            // North Wall (+Y)
            if (openDirections == null || !openDirections.Contains(Vector2.up) || (blockedDirections != null && blockedDirections.Contains(Vector2.up)))
            {
                SpawnWall("Wall_North", roomParent.transform, new Vector3(0f, halfY, 0f), new Vector3(widthX, wallThickness, wallHeight));
            }
            
            // South Wall (-Y)
            if (openDirections == null || !openDirections.Contains(Vector2.down) || (blockedDirections != null && blockedDirections.Contains(Vector2.down)))
            {
                SpawnWall("Wall_South", roomParent.transform, new Vector3(0f, -halfY, 0f), new Vector3(widthX, wallThickness, wallHeight));
            }

            // East Wall (+X)
            if (openDirections == null || !openDirections.Contains(Vector2.right) || (blockedDirections != null && blockedDirections.Contains(Vector2.right)))
            {
                SpawnWall("Wall_East", roomParent.transform, new Vector3(halfX, 0f, 0f), new Vector3(wallThickness, heightY, wallHeight));
            }

            // West Wall (-X)
            if (openDirections == null || !openDirections.Contains(Vector2.left) || (blockedDirections != null && blockedDirections.Contains(Vector2.left)))
            {
                SpawnWall("Wall_West", roomParent.transform, new Vector3(-halfX, 0f, 0f), new Vector3(wallThickness, heightY, wallHeight));
            }

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "Floor";
            floor.transform.parent = roomParent.transform;
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(widthX, heightY, 1f);
            floor.transform.rotation = Quaternion.Euler(0, 0, 0); // Flat on XY plane for 2D
            
            // Remove 3D collider for 2D project compatibility
            DestroyImmediate(floor.GetComponent<Collider>());
            floor.AddComponent<BoxCollider2D>(); // 2D Trigger/Floor
            
            if (wallMaterial != null)
            {
                floor.GetComponent<Renderer>().sharedMaterial = wallMaterial;
            }
        }

        private void SpawnHouse(string name, Vector3 position, float size)
        {
            GameObject houseParent = new GameObject(name);
            houseParent.transform.parent = generationRoot;
            houseParent.transform.position = position;

            float half = size * 0.5f;
            float doorGap = 1.2f;

            // Spawn 3 closed walls
            SpawnWall("Wall_N", houseParent.transform, new Vector3(0f, half, 0f), new Vector3(size, wallThickness, wallHeight));
            SpawnWall("Wall_E", houseParent.transform, new Vector3(half, 0f, 0f), new Vector3(wallThickness, size, wallHeight));
            SpawnWall("Wall_W", houseParent.transform, new Vector3(-half, 0f, 0f), new Vector3(wallThickness, size, wallHeight));

            // South wall has a door (two wall segments with a gap)
            float wallSegmentWidth = (size - doorGap) * 0.5f;
            float segmentCenterOffset = half - (wallSegmentWidth * 0.5f);

            SpawnWall("Wall_S_Left", houseParent.transform, new Vector3(-segmentCenterOffset, -half, 0f), new Vector3(wallSegmentWidth, wallThickness, wallHeight));
            SpawnWall("Wall_S_Right", houseParent.transform, new Vector3(segmentCenterOffset, -half, 0f), new Vector3(wallSegmentWidth, wallThickness, wallHeight));
        }

        private void SpawnWall(string name, Transform parent, Vector3 localPosition, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.localPosition = localPosition;
            wall.transform.localScale = scale;

            // Convert standard 3D collider to 2D for our isometric topdown setup
            DestroyImmediate(wall.GetComponent<Collider>());
            
            // We use BoxCollider2D on the walls so the Rigidbody2D on the player collides with it
            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            // Since it's topdown 2D, the collision happens in the XY plane.
            // Scale represents thickness and length. We match size of 2D collider.
            col.size = new Vector2(1f, 1f);

            if (wallMaterial != null)
            {
                wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
            }
        }
    }
}
