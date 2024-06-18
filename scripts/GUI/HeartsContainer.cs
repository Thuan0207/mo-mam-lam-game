using Godot;

public partial class HeartsContainer : HBoxContainer
{
    // Called when the node enters the scene tree for the first time.
    int _maxHealth = 5;
    public int MaxHealth
    {
        get => _maxHealth;
        set
        {
            for (int i = 0; i < value; i++)
            {
                var heart = _heartGuiTscn.Instantiate<Panel>();
                AddChild(heart);
            }
            _maxHealth = 5;
        }
    }
    PackedScene _heartGuiTscn;

    public override void _Ready()
    {
        _heartGuiTscn = ResourceLoader.Load<PackedScene>("uid://bu6donrunmeqx");
    }

    public void UpdateHearts(int currentHealth)
    {
        var hearts = GetChildren();

        for (int i = 0; i < hearts.Count; i++)
        {
            var currentHeart = (Heart)hearts[i];
            if (i < currentHealth)
                currentHeart.IsBroken = false;
            else
                currentHeart.IsBroken = true;
        }
    }
}
