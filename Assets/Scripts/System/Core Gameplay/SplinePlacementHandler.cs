// ==============================
// SplinePlacementHandler.cs  (NO VISUALIZER)
// ==============================

using UnityEngine;
using System.Collections.Generic;

public class SplinePlacementHandler : MonoBehaviour
{
    [System.Serializable]
    public class WallInstance
    {
        public List<Vector3> splinePoints = new List<Vector3>();

        // draggable pillar anchors (tag = "Wall")
        public List<GameObject> activePillars = new List<GameObject>();

        // spawned wall meshes
        public List<GameObject> spawnedWalls = new List<GameObject>();
    }

    [Header("Wall Settings")]
    [SerializeField] private GameObject wallPrefab;

    // draggable pillar — ตั้ง tag = "Wall" ที่ prefab ใน Inspector
    [SerializeField] private GameObject pillarPrefab;

    [Header("Curve Interpolation")]
    [SerializeField] private bool useCurve = true;

    [SerializeField]
    private float wallSegmentLength = 1.0f;

    [Header("Layer Settings")]
    [SerializeField]
    private string targetWallNodeLayerName = "WallLayer";

    [Header("Dynamic Spline Settings")]
    [SerializeField]
    private float maxStretchMultiplier = 1.5f;

    [SerializeField]
    private float removeNodeDistanceMultiplier = 0.5f;

    [SerializeField]
    private float influenceRadius = 3f;

    [SerializeField]
    private float influenceStrength = 1f;

    private PlacementSystem system;

    private List<WallInstance> allWallInstances =
        new List<WallInstance>();

    private WallInstance currentDrawingWall;

    private bool isDrawing = false;

    public void Initialize(PlacementSystem sys)
    {
        system = sys;
    }

    public void HandleWallSpline(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            Mathf.Infinity,
            system.groundLayer))
        {
            Vector3 currentSnapPos =
                system.SnapToGrid(hit.point);

            if (Input.GetMouseButtonDown(0) &&
                !IsOccupied(currentSnapPos))
            {
                isDrawing = true;

                currentDrawingWall = new WallInstance();

                currentDrawingWall.splinePoints.Add(currentSnapPos);

                SpawnPillar(currentSnapPos, currentDrawingWall);
            }

            if (isDrawing &&
                Input.GetMouseButton(0) &&
                currentDrawingWall != null)
            {
                Vector3 lastPoint =
                    currentDrawingWall.splinePoints[
                        currentDrawingWall.splinePoints.Count - 1];

                if (Vector3.Distance(lastPoint, currentSnapPos)
                        >= wallSegmentLength
                    && !IsOccupied(currentSnapPos))
                {
                    currentDrawingWall.splinePoints.Add(currentSnapPos);

                    SpawnPillar(currentSnapPos, currentDrawingWall);

                    RebuildWallMesh(currentDrawingWall);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (isDrawing && currentDrawingWall != null)
                {
                    if (currentDrawingWall.splinePoints.Count >= 2)
                    {
                        allWallInstances.Add(currentDrawingWall);
                    }
                    else
                    {
                        // ยังไม่ครบ 2 จุด — ลบทิ้ง
                        foreach (GameObject pillar
                            in currentDrawingWall.activePillars)
                        {
                            if (pillar != null)
                                Destroy(pillar);
                        }
                    }
                }

                isDrawing = false;
                currentDrawingWall = null;
            }
        }
    }

    // -------------------------------------------------------
    // SPAWN PILLAR — pillar คือ drag target โดยตรง
    // tag = "Wall"  ทำให้ EditTransformHandler คลิกเจอ
    // -------------------------------------------------------
    private void SpawnPillar(Vector3 pos, WallInstance targetWall)
    {
        if (targetWall == null || pillarPrefab == null)
            return;

        int layerID =
            LayerMask.NameToLayer(targetWallNodeLayerName);

        int finalLayer = (layerID != -1) ? layerID : 0;

        GameObject pillar =
            Instantiate(pillarPrefab, pos, Quaternion.identity);

        pillar.layer = finalLayer;
        pillar.tag = "Wall";   // EditTransformHandler ใช้ tag นี้หา node

        // ตรวจสอบ collider ที่ root
        Collider col = pillar.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider bc = pillar.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            bc.size = new Vector3(1.2f, 1.2f, 1.2f);
            bc.center = Vector3.zero;
        }

        targetWall.activePillars.Add(pillar);

        Debug.Log("[PILLAR SPAWNED] => " + pillar.name);
    }

    // -------------------------------------------------------
    // REBUILD
    // -------------------------------------------------------
    public void RebuildWallMesh(WallInstance targetWall)
    {
        if (targetWall == null)
            return;

        foreach (GameObject wall in targetWall.spawnedWalls)
        {
            if (wall != null)
                Destroy(wall);
        }

        targetWall.spawnedWalls.Clear();

        if (targetWall.splinePoints.Count < 2)
            return;

        if (useCurve && targetWall.splinePoints.Count >= 3)
            DrawCurvedWalls(targetWall);
        else
            DrawStraightWalls(targetWall);
    }

    private void DrawStraightWalls(WallInstance targetWall)
    {
        for (int i = 0; i < targetWall.splinePoints.Count - 1; i++)
        {
            InstantiateWallSegment(
                targetWall.splinePoints[i],
                targetWall.splinePoints[i + 1],
                targetWall);
        }
    }

    private void DrawCurvedWalls(WallInstance targetWall)
    {
        List<Vector3> curvedPathPoints =
            GenerateSplinePath(targetWall.splinePoints, 10);

        if (curvedPathPoints.Count < 2)
            return;

        Vector3 lastWallEndPos = curvedPathPoints[0];

        for (int i = 1; i < curvedPathPoints.Count; i++)
        {
            Vector3 currentPathPos = curvedPathPoints[i];

            if (Vector3.Distance(lastWallEndPos, currentPathPos)
                    >= wallSegmentLength)
            {
                InstantiateWallSegment(
                    lastWallEndPos,
                    currentPathPos,
                    targetWall);

                lastWallEndPos = currentPathPos;
            }
        }

        Vector3 lastPoint =
            targetWall.splinePoints[targetWall.splinePoints.Count - 1];

        if (Vector3.Distance(lastWallEndPos, lastPoint) > 0.1f)
        {
            InstantiateWallSegment(
                lastWallEndPos,
                lastPoint,
                targetWall);
        }
    }

    private void InstantiateWallSegment(
        Vector3 start,
        Vector3 end,
        WallInstance targetWall)
    {
        Vector3 dir = end - start;

        if (dir == Vector3.zero)
            return;

        Quaternion wallRot = Quaternion.LookRotation(dir);

        GameObject wall =
            Instantiate(
                wallPrefab,
                start + (dir / 2f),
                wallRot);

        Vector3 scale = wall.transform.localScale;
        scale.z = dir.magnitude;
        wall.transform.localScale = scale;

        int layerID =
            LayerMask.NameToLayer(targetWallNodeLayerName);

        wall.layer = (layerID != -1) ? layerID : 0;

        if (!wall.GetComponent<Collider>())
        {
            MeshCollider mc = wall.AddComponent<MeshCollider>();
            mc.convex = true;
        }

        targetWall.spawnedWalls.Add(wall);
    }

    // -------------------------------------------------------
    // MOVE NODE — เรียกจาก EditTransformHandler
    // -------------------------------------------------------
    public void MoveNodeDynamicCheck(
        GameObject movedPillar,
        Vector3 newPosition)
    {
        if (movedPillar == null)
        {
            Debug.LogWarning("MoveNodeDynamicCheck : movedPillar NULL");
            return;
        }

        GameObject targetObject = movedPillar;
        WallInstance foundWall = null;
        int targetIndex = -1;

        // หา pillar ใน allWallInstances
        foreach (WallInstance wall in allWallInstances)
        {
            for (int i = 0; i < wall.activePillars.Count; i++)
            {
                GameObject node = wall.activePillars[i];
                if (node == null)
                    continue;

                if (movedPillar == node ||
                    movedPillar.transform.IsChildOf(node.transform))
                {
                    targetObject = node;
                    foundWall = wall;
                    targetIndex = i;
                    break;
                }
            }

            if (foundWall != null)
                break;
        }

        // ตรวจ currentDrawingWall ด้วย
        if (foundWall == null && currentDrawingWall != null)
        {
            for (int i = 0;
                i < currentDrawingWall.activePillars.Count;
                i++)
            {
                GameObject node =
                    currentDrawingWall.activePillars[i];

                if (node == null)
                    continue;

                if (movedPillar == node ||
                    movedPillar.transform.IsChildOf(node.transform))
                {
                    targetObject = node;
                    foundWall = currentDrawingWall;
                    targetIndex = i;
                    break;
                }
            }
        }

        if (foundWall == null)
        {
            Debug.LogError("MoveNodeDynamicCheck : FOUND WALL = NULL");
            return;
        }

        if (targetIndex < 0 ||
            targetIndex >= foundWall.splinePoints.Count)
        {
            Debug.LogError("MoveNodeDynamicCheck : INVALID INDEX");
            return;
        }

        Debug.Log(
            "MOVE PILLAR index=" + targetIndex +
            " => " + newPosition);

        Vector3 oldPos = foundWall.splinePoints[targetIndex];
        Vector3 delta = newPosition - oldPos;

        // อัปเดต spline point และขยับ pillar
        foundWall.splinePoints[targetIndex] = newPosition;
        targetObject.transform.position = newPosition;

        // INFLUENCE — ดึงโหนดข้างๆ
        for (int i = 0; i < foundWall.splinePoints.Count; i++)
        {
            if (i == targetIndex)
                continue;

            int dist = Mathf.Abs(i - targetIndex);

            float normalized =
                Mathf.Clamp01(1f - (dist / influenceRadius));

            float influence =
                normalized * normalized * influenceStrength;

            if (influence <= 0.001f)
                continue;

            foundWall.splinePoints[i] += delta * influence;

            if (i < foundWall.activePillars.Count &&
                foundWall.activePillars[i] != null)
            {
                foundWall.activePillars[i].transform.position =
                    foundWall.splinePoints[i];
            }
        }

        SubdivideIfNeeded(foundWall);
        RemoveDenseNodes(foundWall);
        RebuildWallMesh(foundWall);
    }

    // -------------------------------------------------------
    // SUBDIVIDE — แทรก pillar กลางถ้าระยะห่างเกิน
    // -------------------------------------------------------
    private void SubdivideIfNeeded(WallInstance wall)
    {
        if (wall == null)
            return;

        bool addedNode = false;

        int layerID =
            LayerMask.NameToLayer(targetWallNodeLayerName);

        int finalLayer = (layerID != -1) ? layerID : 0;

        for (int i = 0; i < wall.splinePoints.Count - 1; i++)
        {
            Vector3 a = wall.splinePoints[i];
            Vector3 b = wall.splinePoints[i + 1];

            float dist = Vector3.Distance(a, b);

            if (dist > wallSegmentLength * maxStretchMultiplier)
            {
                Vector3 mid = (a + b) * 0.5f;

                wall.splinePoints.Insert(i + 1, mid);

                if (pillarPrefab != null)
                {
                    GameObject pillar =
                        Instantiate(
                            pillarPrefab,
                            mid,
                            Quaternion.identity);

                    pillar.layer = finalLayer;
                    pillar.tag = "Wall";

                    Collider col =
                        pillar.GetComponent<Collider>();

                    if (col == null)
                    {
                        BoxCollider bc =
                            pillar.AddComponent<BoxCollider>();

                        bc.isTrigger = false;
                        bc.size = new Vector3(1.2f, 1.2f, 1.2f);
                        bc.center = Vector3.zero;
                    }

                    wall.activePillars.Insert(i + 1, pillar);
                }

                addedNode = true;
            }
        }

        if (addedNode)
            SubdivideIfNeeded(wall);
    }

    // -------------------------------------------------------
    // REMOVE DENSE NODES — ลบ pillar ที่อยู่ชิดกันเกิน
    // -------------------------------------------------------
    private void RemoveDenseNodes(WallInstance wall)
    {
        if (wall == null || wall.splinePoints.Count <= 2)
            return;

        for (int i = wall.splinePoints.Count - 2; i >= 1; i--)
        {
            float distPrev =
                Vector3.Distance(
                    wall.splinePoints[i - 1],
                    wall.splinePoints[i]);

            float distNext =
                Vector3.Distance(
                    wall.splinePoints[i],
                    wall.splinePoints[i + 1]);

            float threshold =
                wallSegmentLength * removeNodeDistanceMultiplier;

            if (distPrev < threshold || distNext < threshold)
            {
                if (i < wall.activePillars.Count &&
                    wall.activePillars[i] != null)
                {
                    Destroy(wall.activePillars[i]);
                    wall.activePillars.RemoveAt(i);
                }

                wall.splinePoints.RemoveAt(i);
            }
        }
    }

    // -------------------------------------------------------
    // SPLINE MATH
    // -------------------------------------------------------
    private List<Vector3> GenerateSplinePath(
        List<Vector3> nodes,
        int pointsPerSegment)
    {
        List<Vector3> path = new List<Vector3>();

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            Vector3 p0 = (i == 0) ? nodes[i] : nodes[i - 1];
            Vector3 p1 = nodes[i];
            Vector3 p2 = nodes[i + 1];
            Vector3 p3 = (i + 2 >= nodes.Count)
                ? nodes[i + 1]
                : nodes[i + 2];

            for (int j = 0; j < pointsPerSegment; j++)
            {
                float t = j / (float)pointsPerSegment;

                path.Add(
                    GetCatmullRomPosition(t, p0, p1, p2, p3));
            }
        }

        path.Add(nodes[nodes.Count - 1]);

        return path;
    }

    private Vector3 GetCatmullRomPosition(
        float t,
        Vector3 p0, Vector3 p1,
        Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private bool IsOccupied(Vector3 pos)
    {
        return Physics.CheckSphere(
            pos,
            0.2f,
            system.buildableLayer);
    }

    public void ResetSplines()
    {
        isDrawing = false;
        currentDrawingWall = null;
    }
}   