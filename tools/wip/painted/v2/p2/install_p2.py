"""The painted style pass, part 2 (natural rock): install. Usage: python install_p2.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda). Run part 1's install first.

   Runs patch_p2.py on <root>, then copies PaintedRock.shader into <root>\\World\\Shaders and ZoneSceneBuilder.PaintedRock.cs
   into <root>\\Editor, each with its .meta. The patch goes first (it checks every anchor before it writes anything), so a
   failed patch leaves no new file behind. Refuses to run twice: it stops if a new file is already in place, before anything
   is patched or copied.
   After installing, build the art twice if the first build warns that the shader is not imported yet."""
import os, shutil, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
NEW = [('PaintedRock.shader', r'World\Shaders'), ('PaintedRock.shader.meta', r'World\Shaders'),
       ('ZoneSceneBuilder.PaintedRock.cs', 'Editor'), ('ZoneSceneBuilder.PaintedRock.cs.meta', 'Editor')]

assert os.path.isfile(os.path.join(ROOT, 'Editor', 'ZoneSceneBuilder.Painted.cs')), ('part 1 is not installed', ROOT)
for name, folder in NEW:
    assert os.path.isfile(os.path.join(HERE, name)), ('missing deliverable', name)
    assert os.path.isdir(os.path.join(ROOT, folder)), ('missing folder', os.path.join(ROOT, folder))
    assert not os.path.exists(os.path.join(ROOT, folder, name)), ('already installed', os.path.join(ROOT, folder, name))
subprocess.run([sys.executable, os.path.join(HERE, 'patch_p2.py'), ROOT], check=True)
for name, folder in NEW:
    shutil.copyfile(os.path.join(HERE, name), os.path.join(ROOT, folder, name)); print('copied', name, '->', folder)
print('installed p2')
