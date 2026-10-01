"""The painted style pass, part 1: install. Usage: python install_p1.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda).

   Runs patch_p1.py on <root> and copies ZoneSceneBuilder.Painted.cs and its .meta into <root>\\Editor. The patch goes first
   (it checks every anchor before it writes anything), so a failed patch leaves no new file behind.
   Refuses to run twice: it stops if a new file is already in place, before anything is patched or copied."""
import os, shutil, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
NEW = [('ZoneSceneBuilder.Painted.cs', 'Editor'), ('ZoneSceneBuilder.Painted.cs.meta', 'Editor')]

for name, folder in NEW:
    assert os.path.isfile(os.path.join(HERE, name)), ('missing deliverable', name)
    assert os.path.isdir(os.path.join(ROOT, folder)), ('missing folder', os.path.join(ROOT, folder))
    assert not os.path.exists(os.path.join(ROOT, folder, name)), ('already installed', os.path.join(ROOT, folder, name))
# Patch first (it checks every anchor before it writes anything), so a failed patch leaves no new file behind.
subprocess.run([sys.executable, os.path.join(HERE, 'patch_p1.py'), ROOT], check=True)
for name, folder in NEW:
    shutil.copyfile(os.path.join(HERE, name), os.path.join(ROOT, folder, name)); print('copied', name, '->', folder)
print('installed p1')
