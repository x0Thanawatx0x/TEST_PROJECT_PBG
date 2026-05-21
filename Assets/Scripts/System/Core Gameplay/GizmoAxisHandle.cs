// ==============================
// GizmoAxisHandle.cs
// ==============================
// ใส่บน child แต่ละอันของ Gizmo Prefab
// แล้วตั้ง axisName ใน Inspector

using UnityEngine;

public class GizmoAxisHandle : MonoBehaviour
{
    // ตั้งใน Inspector ตามแต่ละ child:
    // "Move"         → handle เคลื่อนย้าย
    // "Axis_Rotate"  → handle หมุน
    // "Axis_X"       → ขยาย X
    // "Axis_Y"       → ขยาย Y
    // "Axis_Z"       → ขยาย Z
    // "Axis_Uniform" → ขยายทุกด้าน
    [SerializeField] public string axisName = "Move";
}