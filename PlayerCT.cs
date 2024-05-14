using Godot;
using System;
/*public partial class Character : CharacterBody2D
{
	private AnimatedSprite2D _animatedSprite;
	public override _Process(float _delta)
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
	}
	public override void _PhysicsProcess(double delta){
		 if (Input.IsActionPressed("ui_right")) {
			_animatedSprite.Play("run"); 
		 }
		 else{
			_animatedSprite.Stop(); 
		 }
	}
}*/

public partial class PlayerCT : CharacterBody2D
{

	public const float Speed = 250.0f;
	public const float JumpVelocity = -450.0f;
	

	// Khai báo trọng lực ( trọng lực mặc định của godot và có thể thay đổi ở giao diện)
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	

	public override void _PhysicsProcess(double delta)
	{
		// khai báo tọa độ của player
		Vector2 velocity = Velocity;

		var animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		// Áp dụng trọng lực khi ở trên không
		if (!IsOnFloor())
			velocity.Y += gravity * (float)delta;
			//animatedSprite2D.Animation = "jump";

		// nhảy khi player bấm space và ở trên sàn
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;
			

		// di chuyển khi player nhấn nút
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			// nhân vật đứng im khi player k bấm gì
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			animatedSprite2D.Animation ="idle";
		}
		if(velocity.X != 0){
			animatedSprite2D.Animation = "run";
			animatedSprite2D.FlipH = velocity.X < 0;
		}
		if (!IsOnFloor())
			{
			animatedSprite2D.Animation = "jump";
			}

		Velocity = velocity;
		MoveAndSlide();
	}
}
