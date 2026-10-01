"""Review items 1 and 2: the Root-Mother fits under the Heart's roof (the hall stays tall to its back wall), and the cold in
   the root stays salted (the rod, its light and the hoarfrost go for good; the pale root stays)."""
import io
A = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts'
def patch(rel, reps):
    p = A + chr(92) + rel; s = io.open(p, encoding='utf-8', newline='').read()
    for a, b in reps:
        assert s.count(a) == 1, (rel, a[:80])
        s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8', newline='').write(s); print('patched', rel)
patch(r'World\ZoneBuilder.cs', [
 ("new[] { -1.5f, 90, 8.5f, 8, -16.8f }, new[] { -2f, 95, 5, 5.5f, -16.8f },\n                new[] { -2f, 98, 2, 2.5f, -16.8f }, new[] { -2f, 99.5f, .3f, .4f, -16.8f } };",
  "new[] { -1.5f, 90, 8.5f, 8, -16.8f }, new[] { -2f, 95, 7, 8.5f, -16.8f },\n                // The Heart stays tall to its back wall (the Root-Mother stands seven metres high against it), then closes at once.\n                new[] { -2f, 98, 5, 8, -16.8f }, new[] { -2f, 100.5f, .3f, .4f, -16.8f } };"),
 ("                var black = Tint(art.metal, new Color(.04f, .04f, .06f));\n                Part(PrimitiveType.Cylinder, husk, new Vector3(0, .9f, 0)",
  "                // The cold itself (rod, light, hoarfrost) is the usable part: salted once, it is gone for good; the pale root stays.\n                var rod = new GameObject(\"The cold\").transform; rod.SetParent(husk, false);\n                var black = Tint(art.metal, new Color(.04f, .04f, .06f));\n                Part(PrimitiveType.Cylinder, rod, new Vector3(0, .9f, 0)"),
 ("Part(PrimitiveType.Cylinder, husk, new Vector3(.02f, .9f, .02f)", "Part(PrimitiveType.Cylinder, rod, new Vector3(.02f, .9f, .02f)"),
 ("Part(PrimitiveType.Sphere, husk, new Vector3(0, 1.75f, 0)", "Part(PrimitiveType.Sphere, rod, new Vector3(0, 1.75f, 0)"),
 ("Part(PrimitiveType.Sphere, husk, new Vector3((D() - .5f) * 3.2f, .03f", "Part(PrimitiveType.Sphere, rod, new Vector3((D() - .5f) * 3.2f, .03f"),
 ("Glow(husk, new Vector3(0, 1.8f, 0), 6, .5f, new Color(.6f, .35f, .9f), .9f);", "Glow(rod, new Vector3(0, 1.8f, 0), 6, .5f, new Color(.6f, .35f, .9f), .9f);"),
 ('prompt = "Salt the cold root", kind = "crates", position = husk.position, root = husk });', 'prompt = "Salt the cold root", kind = "crates", once = true, position = husk.position, root = rod });'),
])
patch(r'Encounter\EncounterSession.cs', [
 ("static void HideProp(Transform t) { foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false; }",
  "static void HideProp(Transform t) { foreach (var r in t.GetComponentsInChildren<Renderer>()) r.enabled = false; foreach (var l in t.GetComponentsInChildren<Light>()) l.enabled = false; }   // its glow goes with it"),
])
print('FIXES 3 OK')
