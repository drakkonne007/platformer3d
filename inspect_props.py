import sys
import os
import json

def load_graph(path):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    objs = []
    decoder = json.JSONDecoder()
    pos = 0
    while pos < len(content):
        while pos < len(content) and content[pos].isspace():
            pos += 1
        if pos >= len(content):
            break
        try:
            obj, e = decoder.raw_decode(content[pos:])
            objs.append(obj)
            pos += e
        except json.JSONDecodeError as e: break
            
    return objs

objs = load_graph('Assets/SoStylized/Environment/Foliage/Shaders/S_FoliageShader.shadergraph')

properties = [o for o in objs if 'UnityEditor.ShaderGraph' in o.get('m_Type', '') and 'Property' in o.get('m_Type', '')]

with open('props_summary.txt', 'w', encoding='utf-8') as f:
    for p in properties:
        name = p.get('m_Name', '')
        ref = p.get('m_DefaultReferenceName', '')
        oref = p.get('m_OverrideReferenceName', '')
        typ = p.get('m_Type', '')
        f.write(f"Name '{name}', DefaultRef '{ref}', OverrideRef '{oref}' ({typ})\n")
