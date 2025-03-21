For an easy set up of the dialogue player system designed for Unity! Is open for a pull request!

・How easy it is

Dialogue information can be created by non-coders as the dialogue player reads information from a csv file.

You do not have to manually place an object and SetActive(false) it before using it.

・Explanation for each role

SoundInfo: to create a scriptable object to effectively organize information (which is very DOT I love it)

DialogueData: to control dialogue information smoothly with a single csv file

DialoguePlayer: to play a dialogue

・Cool mechanisms behind each

SoundInfo: collaboration with AudioManager is interesting is but not for this class design specifically...

DialogueData: asynchronous load of resources (Sprite and SoundInfo) using addressables and a parse of Color info (and its extendability, although another modification may not come)

DialoguePlayer: sophiticated information retrieval for a quick access to the necessary information and the OOP implementation
