import sys
import os
import json

def load_graph(path):
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Unity shader graph files are composed of multiple JSON objects
    # We can split them by "\n\n{" basically or parse them bracket by bracket
    objs = []
    decoder = json.JSONDecoder()
    pos = 0
    while pos < len(content):
        # skip whitespace
        while pos < len(content) and content[pos].isspace():
            pos += 1
        if pos >= len(content):
            break
        try:
            obj, e = decoder.raw_decode(content[pos:])
            objs.append(obj)
            pos += e
        except json.JSONDecodeError as e:
            print("Error at", pos, e)
            break
    
    # The first object is always the GraphData
    graph_data = [o for o in objs if o.get('m_Type') == 'UnityEditor.ShaderGraph.GraphData'][0]
    
    nodes = {o['m_ObjectId']: o for o in objs if 'UnityEditor.ShaderGraph' in o.get('m_Type', '') and 'Node' in o.get('m_Type', '')}
    properties = {o['m_ObjectId']: o for o in objs if 'UnityEditor.ShaderGraph' in o.get('m_Type', '') and 'Property' in o.get('m_Type', '')}
    
    # Identify property names
    for pid, prop in properties.items():
        pass
        
    return objs, graph_data, nodes, properties

objs, graph_data, nodes, properties = load_graph('Assets/SoStylized/Environment/Foliage/Shaders/S_FoliageShader.shadergraph')

name_lookup = {}
for p in properties.values():
    if 'm_Name' in p:
        name_lookup[p['m_ObjectId']] = p['m_Name']

with open('graph_summary.txt', 'w', encoding='utf-8') as f:
    for n_id, n in nodes.items():
        t = n.get('m_Type', '').split('.')[-1]
        name = ""
        if t == 'PropertyNode':
            prop_id = n.get('m_Property', {}).get('m_Id', '')
            name = name_lookup.get(prop_id, 'UNKNOWN_PROP')
        elif t == 'MultiplyNode':
            name = "Multiply"
        elif t == 'AddNode':
            name = "Add"
        elif t == 'TransformNode':
            name = "Transform"
        elif t == 'TimeNode':
            name = "Time"
        elif t == 'GradientNoiseNode':
            name = "GradientNoise"
        elif t == 'TilingAndOffsetNode':
            name = "TilingAndOffset"
            
        f.write(f"{n_id} [{t}] {name}\n")
        
    f.write("\nEDGES:\n")
    for e in graph_data.get('m_Edges', []):
        o = e['m_OutputSlot']
        i = e['m_InputSlot']
        o_node = o['m_Node']['m_Id']
        i_node = i['m_Node']['m_Id']
        o_name = ""
        i_name = ""
        if o_node in nodes:
            t = nodes[o_node].get('m_Type', '').split('.')[-1]
            if t == 'PropertyNode':
                o_name = name_lookup.get(nodes[o_node].get('m_Property', {}).get('m_Id', ''), 'PROP')
            else:
                o_name = t
        if i_node in nodes:
            t = nodes[i_node].get('m_Type', '').split('.')[-1]
            if t == 'PropertyNode':
                i_name = name_lookup.get(nodes[i_node].get('m_Property', {}).get('m_Id', ''), 'PROP')
            else:
                i_name = t
                
        f.write(f"{o_node} ({o_name}:{o['m_SlotId']}) -> {i_node} ({i_name}:{i['m_SlotId']})\n")
