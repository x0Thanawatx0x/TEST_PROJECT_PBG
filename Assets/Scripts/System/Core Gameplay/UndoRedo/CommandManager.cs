using UnityEngine;
using System.Collections.Generic;

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ObjectPlacementHandler objectPlacementHandler;

    [Header("Undo Settings")]

    [Tooltip("จำนวน Object ขั้นต่ำที่ต้องเหลือใน Scene")]
    [SerializeField] private int minimumUndoObjects = 1;

    // =========================
    // STACKS
    // =========================

    private Stack<ICommand> undoStack = new Stack<ICommand>();
    private Stack<ICommand> redoStack = new Stack<ICommand>();

    // =========================
    // UNITY
    // =========================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // =========================
    // ADD COMMAND
    // =========================

    public void AddCommand(ICommand cmd)
    {
        if (cmd == null)
            return;

        cmd.Execute();

        undoStack.Push(cmd);

        // เมื่อมี action ใหม่ ต้องล้าง redo
        redoStack.Clear();

        RefreshPreview();
    }

    // =========================
    // UNDO
    // =========================

    public void Undo()
    {
        // กัน Undo จน Scene ว่าง
        if (undoStack.Count <= minimumUndoObjects)
        {
            Debug.Log("Minimum object reserve reached");
            return;
        }

        ICommand command = undoStack.Pop();

        if (command == null)
            return;

        command.Undo();

        redoStack.Push(command);

        RefreshPreview();
    }

    // =========================
    // REDO
    // =========================

    public void Redo()
    {
        if (redoStack.Count <= 0)
        {
            Debug.Log("Nothing to Redo");
            return;
        }

        ICommand command = redoStack.Pop();

        if (command == null)
            return;

        command.Redo();

        undoStack.Push(command);

        RefreshPreview();
    }

    // =========================
    // HELPERS
    // =========================

    private void RefreshPreview()
    {
        if (objectPlacementHandler != null)
        {
            objectPlacementHandler.ForceRefreshPreview();
        }
    }

    public bool CanUndo()
    {
        return undoStack.Count > minimumUndoObjects;
    }

    public bool CanRedo()
    {
        return redoStack.Count > 0;
    }

    public int UndoCount()
    {
        return undoStack.Count;
    }

    public int RedoCount()
    {
        return redoStack.Count;
    }

    public void ClearHistory()
    {
        undoStack.Clear();
        redoStack.Clear();

        RefreshPreview();
    }

    // =========================
    // OPTIONAL DEBUG
    // =========================

    [ContextMenu("Debug Stack Counts")]
    private void DebugStacks()
    {
        Debug.Log("Undo Stack: " + undoStack.Count);
        Debug.Log("Redo Stack: " + redoStack.Count);
    }
}