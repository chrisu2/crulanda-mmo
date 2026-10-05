"""Reads and writes the loot.*.json files in their own layout: top-level lists, one compact object per line."""
import io, json
def load(p): return json.load(open(p, encoding='utf-8'))
def save(p, d):
    out = ['{']; keys = list(d.keys())
    for i, k in enumerate(keys):
        v = d[k]; comma = ',' if i < len(keys) - 1 else ''
        if isinstance(v, list):
            if not v: out.append('  "%s": [' % k); out.append('  ]' + comma); continue
            out.append('  "%s": [' % k)
            out.append(',\n'.join('    ' + json.dumps(x, ensure_ascii=False) for x in v))
            out.append('  ]' + comma)
        else: out.append('  "%s": %s%s' % (k, json.dumps(v, ensure_ascii=False), comma))
    out.append('}')
    io.open(p, 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
