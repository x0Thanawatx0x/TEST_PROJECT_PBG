public abstract class CommandBase : ICommand
{
    public abstract void Execute();

    public abstract void Undo();

    public virtual void Redo()
    {
        Execute();
    }
}