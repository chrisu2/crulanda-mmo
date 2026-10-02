"""Every camp of every zone and its nearest house (playtest note 1's rule: nothing hostile within about 120 m of a village's
houses). Homes here are props of kind house, inn, mill, treehouse, shelter and keep, and whatever a household names as its
house (Moss's lodge in Oakhaven is a barn), so the list also shows the enemy's own buildings (the Peaks' toll-house and
eyrie): read it with the zone in mind. ZoneGrowthTests.NearHomes is the list this makes, less those two buildings.

    python tools/wip/zonegrowth/rule120.py
"""
import json, math, os
Z = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Zones', '%s.json'))
for zn in ['oakhaven','khaven','peaks','ashrim','verdant']:
    z=json.load(open(Z%zn,encoding='utf-8-sig'))
    kinds={}
    for p in z['props']: kinds[p['kind']]=kinds.get(p['kind'],0)+1
    named={h.get('house') for h in z.get('life',{}).get('households',[])}
    homes=[(p.get('name') or p['kind'],p['at']['x'],p['at']['y'],p['kind']) for p in z['props'] if p['kind'] in('house','inn','mill','treehouse','shelter','keep') or (p.get('name') and p.get('name') in named)]
    print('==',zn,z['size'],'m,',len(homes),'homes')
    print('   kinds',{k:v for k,v in kinds.items() if k in('house','inn','mill','barn','ruined_house','treehouse','shelter','keep','cave','tower')})
    for i,c in enumerate(z['camps']):
        best=min(((math.hypot(c['center']['x']-h[1],c['center']['y']-h[2]),h[0]) for h in homes),default=(999,''))
        print('  %2d %-28s (%6.1f,%6.1f) r%-4s L%s-%s %s%s nearest %-22s %5.1f %s'%(i,c['name'],c['center']['x'],c['center']['y'],c['radius'],c.get('levelMin'),c.get('levelMax'),'E' if c.get('elite') else ' ','A' if c.get('ambush') else ' ',best[1],best[0],'<<<' if best[0]<120 else ''))
    for e in z['spawns'].get('enemies',[]): print('   story',e['name'],e['at'])
