This is for an easy set up of the dialogue player system (as elaborated by the repository name).

!! This is designed for Unity and not for Unreal Engine nor Godot !!

> Explanation

SoundInfo: to create a scriptable object to effectively organize information (DOT approach)
DialogueData: to control dialogue information smoothly with a single csv file
DialoguePlayer: to play a dialogue

> Cool mechanisms behind each file

SoundInfo: collaboration with AudioManager is interesting is but not for this class design specifically...
DialogueData: asynchronous load of resources (Sprite and SoundInfo) using addressables and a parse of Color info (and its extendability, although another modification may not come)
DialoguePlayer: sophiticated information retrieval for a quick access to the necessary information and the OOP implementation

This is open for a pull request!
