extends Node2D

@onready var pause_menu = $PhantomCamera2D/PauseMenu
var paused = false
func _process(_delta):
	if Input.is_action_just_pressed("ui_cancel"):
		pausemenu()
func pausemenu():
	if paused :
		pause_menu.hide()
		Engine.time_scale = 1
	else :
		pause_menu.show()
		Engine.time_scale = 0
	paused = !paused
func _ready():
	pause_menu.hide()
