using System.Linq;
using Godot;

public static class Util
{
    public static bool IsCurrentFrame(Sprite2D sprite, params int[] frames)
    {
        return frames.Contains(sprite.Frame);
    }

    public static bool IsCurrentFrame(Sprite2D sprite, (int, int) ranges)
    {
        if (ranges.Item1 < ranges.Item2)
            for (int i = ranges.Item1; i <= ranges.Item2; i++)
            {
                if (i == sprite.Frame)
                    return true;
            }
        else if (ranges.Item1 > ranges.Item2)
            for (int i = ranges.Item2; i <= ranges.Item1; i++)
            {
                if (i == sprite.Frame)
                    return true;
            }
        else
            return ranges.Item2 == sprite.Frame;

        return false;
    }
}
