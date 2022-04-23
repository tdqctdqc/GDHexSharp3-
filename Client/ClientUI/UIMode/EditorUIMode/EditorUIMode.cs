using Godot;
using System;

public class EditorUIMode : IUIMode
{
    public IMouseInputModule Mouse { get; private set; }
    public IKeyboardInputModule Key { get; private set; }
    public EditorUIMode()
    {
        Mouse = new EditorMouseInputModule();
        Key = new MapKeyInput();
    }
    public void Activate()
    {
        Mouse.Activate();
    }

    public void Deactivate()
    {
        Mouse.Deactivate();
    }

    public void HandleDeltaInput(float delta)
    {
        Mouse?.HandleDeltaInput(delta);
        Key?.HandleDeltaInput(delta);
    }

    public void HandleInput(InputEvent input)
    {
        if(input is InputEventMouse) Mouse.HandleInput(input as InputEventMouse);
        else if (input is InputEventKey) Key.HandleInput(input as InputEventKey);
    }
}
