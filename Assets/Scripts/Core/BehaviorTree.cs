using System;
using System.Collections.Generic;

public struct EvaluateResult
{
    public BTNode.State state;
    public string actionName;

    public EvaluateResult(BTNode.State state, string actionName)
    {
        this.state = state;
        this.actionName = actionName;
    }

    public void Deconstruct(out BTNode.State state, out string actionName)
    {
        state = this.state;
        actionName = this.actionName;
    }
}

public interface BTNode
{
    public enum State
    {
        Running,
        Success,
        Failure
    }

    EvaluateResult Evaluate();
}

public abstract class ActionNode : BTNode
{
    public abstract EvaluateResult Evaluate();
}

public class SequenceNode : BTNode
{
    private List<ActionNode> children = new List<ActionNode>();
    public string action { get; private set; }

    public SequenceNode(List<ActionNode> children, string action)
    {
        this.children = children;
        this.action = action;
    }

    public void Add(ActionNode node) => children.Add(node);
    public EvaluateResult Evaluate()
    {
        foreach (var child in children)
        {
            var (state, role) = child.Evaluate();
            if (state == BTNode.State.Failure)
                return new EvaluateResult(BTNode.State.Failure, $"{action}/{role}");
            if (state == BTNode.State.Running)
                return new EvaluateResult(BTNode.State.Running, $"{action}/{role}");
        }
        return new EvaluateResult(BTNode.State.Success, $"{action}/END");
    }
}

public class SelectorNode : BTNode
{
    private List<SequenceNode> children = new List<SequenceNode>();

    public string action { get; private set; }
    
    public SelectorNode(List<SequenceNode> children, string action)
    {
        this.children = children;
        this.action = action;
    }
    public void Add(SequenceNode node) => children.Add(node);
    public EvaluateResult Evaluate()
    {
        foreach (var child in children)
        {
            var (state, role) = child.Evaluate();
            if (state == BTNode.State.Success)
                return new EvaluateResult(BTNode.State.Success, $"{action}/{role}");
            if (state == BTNode.State.Running)
                return new EvaluateResult(BTNode.State.Running, $"{action}/{role}");
        }
        return new EvaluateResult(BTNode.State.Failure, $"{action}/END");
    }
}
