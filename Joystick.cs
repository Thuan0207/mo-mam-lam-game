using Godot;
using System;

public partial class Joystick : Area2D
{
	private const float MAX_DISTANCE = 100;

	private Node2D _background;
	private Node2D _handle;

	public bool IsTouching;
	private Vector2 _direction = Vector2.Zero;
	[Export]
	TouchScreenButton _button;

	// Được gọi khi nút vào cảnh game lần đầu tiên.
	public override void _Ready()
	{
		// Lấy các nút con
		_background = GetNode<Node2D>("Background");
		_handle = GetNode<Node2D>("Handle");
	}

	// Được gọi mỗi khung hình. 'delta' là thời gian trôi qua kể từ khung hình trước.
	public override void _Process(double delta)
	{
		var joystickPosition = GlobalPosition;
		var mousePosition = GetGlobalMousePosition();

		_direction = mousePosition - joystickPosition;
		var position = mousePosition;

		if (IsTouching)
		{
			if (position.DistanceTo(joystickPosition) > MAX_DISTANCE)
			{
				position = joystickPosition + _direction.Normalized() * MAX_DISTANCE;
			}

			_handle.GlobalPosition = position;
		}
		else
		{
			_handle.GlobalPosition = joystickPosition;
			_direction = Vector2.Zero;
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent is InputEventScreenTouch screenTouchEvent)
		{
			if (_button.IsPressed())
				IsTouching = screenTouchEvent.Pressed;
			else IsTouching = false;
		}
	}

	public Vector2 GetDirection()
	{
		var normalized = _direction.Normalized();
		return new(Mathf.Sign(normalized.X), Mathf.Sign(normalized.Y));
	}
}
