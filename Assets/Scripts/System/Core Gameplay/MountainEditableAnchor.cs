using UnityEngine;

public class MountainEditableAnchor : MonoBehaviour
{
    private TerrainModifierHandler terrainHandler;
    private Vector3 lastPosition;
    private float mountainSize;
    private float mountainSpeed;

    // เก็บค่าเริ่มต้นตอนสร้างภูเขาลูกนี้
    public void SetupAnchor(TerrainModifierHandler handler, float size, float speed)
    {
        terrainHandler = handler;
        mountainSize = size;
        mountainSpeed = speed;
        lastPosition = transform.position;
    }

    // 🔄 เรียกฟังก์ชันนี้จาก PlacementSystem ตอนที่ "ย้ายตำแหน่งเสร็จสิ้น" (หรือเรียกใน Update ตอนกำลังลาก)
    public void OnPositionChanged()
    {
        if (terrainHandler == null) return;

        // 1. ลบภูเขาลูกเก่าที่ตำแหน่งเดิมออกก่อน (คืนค่าพื้นราบ)
        terrainHandler.FlattenMountainAtPosition(lastPosition, mountainSize);

        // 2. ปั้นภูเขาลูกใหม่ขึ้นมาในตำแหน่งปัจจุบัน
        terrainHandler.CreateMountainAtPosition(transform.position, mountainSize, mountainSpeed);

        // บันทึกตำแหน่งล่าสุดไว้
        lastPosition = transform.position;
    }

    // ❌ เรียกฟังก์ชันนี้ตอนที่ผู้เล่นกด "ลบ" (Destroy) ภูเขาลูกนี้ในโหมด Edit
    private void OnDestroy()
    {
        if (terrainHandler != null)
        {
            // คืนค่าพื้นราบ ปล่อยให้ภูเขาหายไปเลย
            terrainHandler.FlattenMountainAtPosition(transform.position, mountainSize);
        }
    }
}