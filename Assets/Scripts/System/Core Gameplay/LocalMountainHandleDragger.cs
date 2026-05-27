using UnityEngine;

public class LocalMountainHandleDragger : MonoBehaviour
{
    private MountainEditableAnchor mainAnchor;
    private bool isHeightHandle;
    private Camera cam;
    private Vector3 dragOffset;
    private Plane fixedDragPlane;

    public void Initialize(MountainEditableAnchor anchor, bool isHeight)
    {
        mainAnchor = anchor;
        isHeightHandle = isHeight;
        cam = Camera.main;

        Collider oldCol = GetComponent<Collider>();
        if (oldCol != null) Destroy(oldCol);

        // แปะสเฟียร์คอลไลเดอร์ขนาดใหญ่สะใจให้ดึงลากง่าย ๆ น่ารัก ๆ สไตล์ Sandbox
        SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
        sphereCol.radius = 2.0f;
        sphereCol.isTrigger = false;
    }

    private void OnMouseDown()
    {
        if (mainAnchor == null || cam == null) return;

        mainAnchor.SelectMountain(true);

        string handleType = isHeightHandle ? "แกนความสูง" : "แกนความกว้าง";
        Debug.Log($"<color=orange><b>[DRAGGING]</b></color> กำลังลากปรับ <b>{handleType}</b>");

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (isHeightHandle)
        {
            Vector3 camForwardHorizontal = cam.transform.forward;
            camForwardHorizontal.y = 0f;
            if (camForwardHorizontal == Vector3.zero) camForwardHorizontal = Vector3.forward;
            fixedDragPlane = new Plane(-camForwardHorizontal.normalized, transform.position);
        }
        else
        {
            fixedDragPlane = new Plane(Vector3.up, transform.position);
        }

        if (fixedDragPlane.Raycast(ray, out float enterDistance))
        {
            dragOffset = transform.position - ray.GetPoint(enterDistance);
        }
    }

    private void OnMouseDrag()
    {
        if (mainAnchor == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (fixedDragPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(enterDistance) + dragOffset;

            if (isHeightHandle)
            {
                float basePlaneY = mainAnchor.transform.position.y;
                float newHeightDelta = mouseWorldPos.y - basePlaneY;
                float calculatedHeightMultiplier = Mathf.Max(newHeightDelta / 10f, 0.1f);
                mainAnchor.UpdateScale(mainAnchor.GetCurrentSize(), calculatedHeightMultiplier);
            }
            else
            {
                float distanceToCenter = Vector3.Distance(new Vector3(mainAnchor.transform.position.x, 0f, mainAnchor.transform.position.z),
                                                         new Vector3(mouseWorldPos.x, 0f, mouseWorldPos.z));
                mainAnchor.UpdateScale(distanceToCenter, mainAnchor.GetCurrentHeightMultiplier());
            }
        }
    }

    private void OnMouseUp()
    {
        Debug.Log("<color=lime><b>[Drag End]</b></color> ปล่อยเมาส์ ล็อกรูปทรงภูเขาเรียบร้อย!");
    }
}