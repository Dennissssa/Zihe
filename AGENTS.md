# Project conventions

- Author and save scene objects, UI, and prefabs in the Unity project. They must be visible and editable before entering Play Mode. Do not build the level or spawn the initial setup from Awake/Start or runtime initialization hooks.
- Modify the existing scene in place. Do not create another scene unless the user explicitly requests a different or additional scene.
- Current scope: first-person walking and center-screen E pickup/drop. Do not add the larger strategy game systems unless requested.
