Setup Tutorial Video: https://youtu.be/wvXFUNEMBcE

Setup Instructions:
	1. Drag the World Master to the scene from the prefabs folder.
	2. Create a world settings scriptable object (Asset Menu > Create > World Generation > World Settings). Modify the settings to fit your needs.
	3. Assign the world settings in the World Master.
	4. Enable world editor in the World Master inspector.
	5. Click "Create World" to generate a world with the already assigned world settings.

Known Issues:
	1. The vertex normals for the terrain mesh do not allign smoothly between chunk edges. For curvy terrains the lighting seams maybe visible on the edges of the chunks.
	2. World created in the editor sometimes do not generate fully and must be re-created to fix it.

Dev Notes:
	>> The spawn system for world objects (such as trees) is kind of primitive at the moment. Not yet sure of a good way to implement such a thing.
	>> I have plans to fix any of the known issues when I have some more time.
