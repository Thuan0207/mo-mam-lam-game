using System;
using Godot;

public partial class Camera : Camera2D
{
    //How quickly shaking will stop [0,1].
    [Export]
    float decay = 0.8f;

    [Export]
    Vector2 max_offset = new(100, 75); //Maximum displacement in pixels.

    [Export]
    float max_roll = 0.0f; //Maximum rotation in radians (use sparingly).

    FastNoiseLite noise; //The source of random values.

    float noise_y = 0; //Value used to move through the noise

    float trauma = 0.0f; //Current shake strength
    int trauma_pwr = 3; //Trauma exponent. Use [2,3]

    public override void _Ready()
    {
        noise = new() { Seed = (int)GD.Randi() };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (trauma > 0)
        {
            trauma = Mathf.Max(trauma - decay * (float)delta, 0);
            Shake();
        }
        else if (Offset.X != 0 || Offset.Y != 0 || Rotation != 0)
        {
            Mathf.Lerp(Offset.X, 0.0, 1);
            Mathf.Lerp(Offset.Y, 0.0, 1);
            Mathf.Lerp(Rotation, 0.0, 1);
        }
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Min(trauma + amount, 1.0f);
    }

    void Shake()
    {
        var amt = Mathf.Pow(trauma, trauma_pwr);
        Vector2 offset = Vector2.Zero;
        noise_y += 1;
        Rotation = max_roll * amt * noise.GetNoise2D(noise.Seed, noise_y);
        offset.X = max_offset.X * amt * noise.GetNoise2D(noise.Seed * 2, noise_y);
        offset.Y = max_offset.Y * amt * noise.GetNoise2D(noise.Seed * 3, noise_y);
        Offset = offset;
    }
}
