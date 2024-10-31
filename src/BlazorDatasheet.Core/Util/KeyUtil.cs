namespace BlazorDatasheet.Core.Util;

public static class KeyUtil
{
    public static bool IsArrowKey(string key)
    {
        return IsKeyUp(key) || IsKeyDown(key) || IsKeyLeft(key) || IsKeyRight(key);
    }

    public static bool IsKeyRight(string key)
    {
        return key == "ArrowRight";
    }

    public static bool IsKeyLeft(string key)
    {
        return key == "ArrowLeft";
    }

    public static bool IsKeyUp(string key)
    {
        return key == "ArrowUp";
    }

    public static bool IsKeyDown(string key)
    {
        return key == "ArrowDown";
    }

    public static bool IsEnter(string key)
    {
        return key == "Enter";
    }

    public static (int row, int col) GetKeyMovementDirection(string key)
    {
        if (IsKeyDown(key)) return (1, 0);
        if (IsKeyUp(key)) return (-1, 0);
        if (IsKeyLeft(key)) return (0, -1);
        if (IsKeyRight(key)) return (0, 1);

        return (0, 0);
    }
}