    using UnityEngine;
    using System.Collections.Generic;

    public class SplinePlacementHandler : MonoBehaviour
    {
        [Header("Wall Settings")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject pillarPrefab;

        [Header("Nature Spline Settings")]
        [SerializeField] private float natureHeightOffset = 0.1f;

        private PlacementSystem system;
        private List<Vector3> splinePoints = new List<Vector3>();
        private bool isDrawing = false;

        // ฟังก์ชันเชื่อมต่อกับระบบหลัก
        public void Initialize(PlacementSystem sys)
        {
            system = sys;
        }

        // ระบบวาดกำแพง (Key 4)
        public void HandleWallSpline(Camera cam)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, system.groundLayer))
            {
                Vector3 currentSnapPos = system.SnapToGrid(hit.point);

                if (Input.GetMouseButtonDown(0) && !IsOccupied(currentSnapPos))
                {
                    isDrawing = true;
                    splinePoints.Clear();
                    splinePoints.Add(currentSnapPos);
                    SpawnPillar(currentSnapPos);
                }

                if (isDrawing && Input.GetMouseButton(0))
                {
                    Vector3 lastPoint = splinePoints[splinePoints.Count - 1];
                    // ตรวจสอบระยะห่างก่อนวางจุดถัดไป (Grid Size)
                    if (Vector3.Distance(lastPoint, currentSnapPos) >= system.gridSize && !IsOccupied(currentSnapPos))
                    {
                        BuildWallSegment(lastPoint, currentSnapPos);
                        splinePoints.Add(currentSnapPos);
                    }
                }

                if (Input.GetMouseButtonUp(0)) isDrawing = false;
            }
        }

        private void BuildWallSegment(Vector3 start, Vector3 end)
        {
            Vector3 dir = end - start;
            if (dir != Vector3.zero)
            {
                Quaternion wallRot = Quaternion.LookRotation(dir);
                GameObject wall = Instantiate(wallPrefab, start + (dir / 2f), wallRot);

                // ปรับ Scale ของกำแพงให้ยาวเท่ากับระยะที่ลาก
                Vector3 scale = wall.transform.localScale;
                scale.z = dir.magnitude;
                wall.transform.localScale = scale;

                wall.layer = 0; // ตั้งเป็น Default หรือ Layer ที่กำหนด
                if (!wall.GetComponent<Collider>())
                {
                    MeshCollider mc = wall.AddComponent<MeshCollider>();
                    mc.convex = true;
                }
            }
            SpawnPillar(end);
        }

        private void SpawnPillar(Vector3 pos)
        {
            if (pillarPrefab) Instantiate(pillarPrefab, pos, Quaternion.identity);
        }

        private bool IsOccupied(Vector3 pos)
        {
            return Physics.CheckSphere(pos, 0.2f, system.buildableLayer);
        }

        public void ResetSplines()
        {
            isDrawing = false;
            splinePoints.Clear();
        }
    }