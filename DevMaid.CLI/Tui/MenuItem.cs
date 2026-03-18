using System;

namespace DevMaid.Tui;

public class MenuItem
{
    public string Name { get; }
    public string Description { get; }
    public Action Action { get; }

    public MenuItem(string name, string description, Action action)
    {
        Name = name;
        Description = description;
        Action = action;
    }
}
