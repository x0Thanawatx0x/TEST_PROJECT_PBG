using UnityEngine;

public class DeleteObjectCommand : ICommand
{
    private GameObject targetObject;

    public DeleteObjectCommand(GameObject target)
    {
        targetObject = target;
    }

    public void Execute()
    {
        if (targetObject == null) return;

        ScaleEffect scale = targetObject.GetComponent<ScaleEffect>();

        if (scale != null)
        {
            scale.StartShrink();
        }

        targetObject.SetActive(false);
    }

    public void Undo()
    {
        if (targetObject == null) return;

        targetObject.SetActive(true);

        ScaleEffect scale = targetObject.GetComponent<ScaleEffect>();

        if (scale != null)
        {
            scale.StartGrow();
        }
    }

    public void Redo()
    {
        Execute();
    }
}