<h2>Everime Procedural World Generator</h2>

<h4>Setup Tutorial: https://youtu.be/wvXFUNEMBcE</h4>

<h4>Setup Instructions:</h4>

<ol>
<li><p>Drag the World Master to the scene from the prefabs folder.</p>
</li>
<li><p>Create a world settings scriptable object (Asset Menu &gt; Create &gt; World Generation &gt; World Settings). Modify the settings to fit your needs.</p>
</li>
<li><p>Assign the world settings in the World Master.</p>
</li>
<li><p>Enable world editor in the World Master inspector.</p>
</li>
<li><p>Click &quot;Create World&quot; to generate a world with the already assigned world settings.</p>
</li>
</ol>
<h4>Knon Issues:</h4>

<ol>
<li><p>The vertex normals for the terrain mesh do not allign smoothly between chunk edges. For curvy terrains the lighting seams maybe visible on the edges of the chunks.</p>
</li>
<li><p>World created in the editor sometimes do not generate fully and must be re-created to fix it.</p>
</li>
</ol>
<h4>DevNotes:</h4>

<blockquote>
<p>The spawn system for world objects (such as trees) is kind of primitive at the moment. Not yet sure of a good way to implement such a thing.</p>
</blockquote>
<blockquote>
<p>I have plans to fix any of the known issues when I have some more time.</p>
</blockquote>