using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 1. โครงสร้างข้อมูลสำหรับเก็บของ 1 ชิ้น
[System.Serializable]
public class ObjectData
{
    public string prefabName; // ชื่อ Prefab เพื่อให้รู้ว่าคือบล็อกอะไร (เช่น "Wall", "Tree")
    public Vector3 position;  // ตำแหน่ง
    public Vector3 rotation;  // มุมหมุน
    public Vector3 scale;     // ขนาด
}

// 2. โครงสร้างข้อมูลสำหรับเก็บทั้งแมพ (List ของ ObjectData)
[System.Serializable]
public class MapData
{
    public List<ObjectData> objects = new List<ObjectData>();
}

public class MapEditorManager : MonoBehaviour
{
    [Header("Map Settings")]
    [Tooltip("โฟลเดอร์ (Empty GameObject) ที่ใช้เก็บบล็อกทุกชิ้นในด่าน")]
    public Transform mapParent;

    [Tooltip("ใส่ Prefab ทั้งหมดที่มีในเกมไว้ที่นี่ เพื่อให้ระบบรู้จักตอนโหลด")]
    public List<GameObject> availablePrefabs;

    private string savePath;

    void Start()
    {
        // กำหนดตำแหน่งไฟล์เซฟ (จะไปเซฟอยู่ในโฟลเดอร์ระบบของเครื่องที่รันเกม)
        savePath = Path.Combine(Application.persistentDataPath, "MyCustomMap.json");
    }

    // ฟังก์ชันนี้เอาไปผูกกับ "ปุ่ม Save" (Button OnClick)
    public void SaveMap()
    {
        MapData mapData = new MapData();

        // วนลูปเก็บข้อมูลวัตถุทุกชิ้นที่อยู่ภายใต้ mapParent
        foreach (Transform child in mapParent)
        {
            ObjectData objData = new ObjectData();

            // ลบคำว่า "(Clone)" ออกไปเผื่อตอนสร้างมาแล้ว Unity เติมให้
            objData.prefabName = child.gameObject.name.Replace("(Clone)", "").Trim();
            objData.position = child.position;
            objData.rotation = child.rotation.eulerAngles;
            objData.scale = child.localScale;

            mapData.objects.Add(objData);
        }

        // แปลงเป็นข้อความ JSON แล้วเซฟลงเครื่อง
        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"✅ บันทึกแมพเรียบร้อยที่: {savePath}");
    }

    // ฟังก์ชันนี้เอาไปผูกกับ "ปุ่ม Load" (Button OnClick)
    public void LoadMap()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogError("❌ ไม่พบไฟล์เซฟแมพ!");
            return;
        }

        // ลบวัตถุเก่าๆ ในด่านทิ้งให้หมดก่อนโหลดของใหม่
        foreach (Transform child in mapParent)
        {
            Destroy(child.gameObject);
        }

        // อ่านไฟล์ JSON และแปลงกลับเป็นข้อมูล
        string json = File.ReadAllText(savePath);
        MapData mapData = JsonUtility.FromJson<MapData>(json);

        // วนลูปสร้างวัตถุขึ้นมาใหม่
        foreach (ObjectData data in mapData.objects)
        {
            // ค้นหา Prefab ต้นฉบับจากลิสต์ที่เตรียมไว้ โดยเทียบจากชื่อ
            GameObject prefabToSpawn = availablePrefabs.Find(p => p.name == data.prefabName);

            if (prefabToSpawn != null)
            {
                // สร้างวัตถุใหม่ที่ตำแหน่งและมุมเดิม
                GameObject newObj = Instantiate(prefabToSpawn, data.position, Quaternion.Euler(data.rotation));
                newObj.name = data.prefabName; // ตั้งชื่อให้เหมือนเดิมเป๊ะๆ
                newObj.transform.localScale = data.scale;
                newObj.transform.SetParent(mapParent); // จับใส่ใน Parent ให้เป็นระเบียบ
            }
            else
            {
                Debug.LogWarning($"⚠️ ไม่พบ Prefab ชื่อ '{data.prefabName}' ในช่อง availablePrefabs");
            }
        }

        Debug.Log("✅ โหลดแมพสำเร็จ!");
    }
}