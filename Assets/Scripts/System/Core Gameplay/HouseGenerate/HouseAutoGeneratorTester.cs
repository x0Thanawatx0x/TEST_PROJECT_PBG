using UnityEngine;
using System.Collections;

public class HouseAutoGeneratorTester : MonoBehaviour
{
    [Header("🔧 อ้างอิงสคริปต์หลัก")]
    [SerializeField] private HouseGenerationController houseController;

    [Header("📐 การจัดระเบียบตําแหน่งวางบ้านตัวอย่าง")]
    [SerializeField] private Vector3 startGridPosition = new Vector3(0, 0, 0); // จุดเริ่มต้นที่จะวางบ้านหลังแรก
    [SerializeField] private float spacingBetweenHouses = 15f;                 // ระยะห่างระหว่างบ้านแต่ละหลัง (เมตร)

    void Start()
    {
        if (houseController == null)
        {
            houseController = FindFirstObjectByType<HouseGenerationController>();
        }

        if (houseController != null)
        {
            // 🚀 สั่งรันระเบียบบอทจำลองสร้างบ้านหน้ากระดาน
            StartCoroutine(GenerateAllTestHouses());
        }
        else
        {
            Debug.LogError("❌ ไม่พบ HouseGenerationController ในฉาก! กรุณาลากใส่ช่องใน Inspector ด้วยนะครับปิ๊บ");
        }
    }

    private IEnumerator GenerateAllTestHouses()
    {
        Debug.Log("🚀 AI Tester: กำลังเริ่มกระบวนการเสกบ้านทดสอบขนาด 1x1 ถึง 10x10 เมตร...");

        yield return new WaitForSeconds(0.1f);

        // วนลูปเสกบ้านขนาด 1x1 เมตร ไปจนถึง 10x10 เมตร (รวม 10 หลังถ้วน)
        for (int size = 1; size <= 10; size++)
        {
            // คำนวณพิกัดจุดเริ่มลากเมาส์เสมือนจริง เรียงแถวแยกออกจากกันทอดตัวยาวตามแนวแกน X
            Vector3 houseStartPos = startGridPosition + new Vector3((size - 1) * spacingBetweenHouses, 0, 0);

            // 🎯 [ซ่อมคณิตศาสตร์ Offset ขอบชนขอบ] 
            // ลอจิก Side Snap วิ่งหาเลขขนาดระยะกรอบพอดีตัว 
            // เราต้องส่งจุดจบในรูปแบบการบวกค่าขนาดสเกลตาราง (size) เข้าล็อกตรงๆ ห้ามเศษปัดทศนิยมเบี้ยว
            Vector3 houseEndPos = houseStartPos + new Vector3(size, 0, size);

            Debug.Log($"🏠 [หลังที่ {size}] กำลังเสกบ้านขนาด {size}x{size} เมตร ที่พิกัดจุดเริ่ม {houseStartPos}");

            // ยิงสัญญาณคำสั่งส่งพิกัดข้ามหน้าต่าง Sandbox API สั่งทำงานจริงหน้างาน
            if (houseController != null)
            {
                ExecuteSimulatedBuild(houseStartPos, houseEndPos);
            }

            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("✨ AI Tester: เสกบ้านตัวอย่างโมดูลาร์ครบทั้ง 10 หลังเนี้ยบกริบเรียบร้อยแล้วครับปิ๊บ!");
    }

    private void ExecuteSimulatedBuild(Vector3 startPoint, Vector3 endPoint)
    {
        houseController.BuildHouseFromExternalAPI(startPoint, endPoint);
    }
}