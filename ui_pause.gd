extends Control
@onready var  main = $"../../"




func _on_quit_pressed():
	get_tree().quit()


func _on_resume_pressed():
	main.pausemenu()


func _on_restar_pressed():
	main.pausemenu()
	get_tree().reload_current_scene()
