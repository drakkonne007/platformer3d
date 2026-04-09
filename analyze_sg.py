import json
import sys

def analyze(path):
    with open(path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    print("Graph loaded.")
    # Find all properties
    props = data.get('m_Properties', [])
    print(f"Total properties: {len(props)}")
    for p in props:
        # We need to find the actual property node definition.
        # However, m_Properties in graph just lists IDs.
        pass
    
    # We can iterate over m_Nodes
    prop_nodes = []
    multiply_nodes = []
    add_nodes = []
    transform_nodes = []
    for node in data.get('m_Nodes', []):
        if 'm_Type' not in node:
            continue
        t = node['m_Type']
        if 'UnityEditor.ShaderGraph.PropertyNode' in t:
            prop_nodes.append(node)
        elif 'MultiplyNode' in t:
            multiply_nodes.append(node)
        elif 'AddNode' in t:
            add_nodes.append(node)
        elif 'TransformNode' in t:
            transform_nodes.append(node)
            
    print("Properties:")
    for p in data.get('m_Properties', []):
        # they might be just { 'm_Id': '...' } but the actual definitions are at the top level? No, they are in a different list maybe?
        pass

    # The actual properties define their names. Wait, where are properties defined?
    # Usually in the top level of the JSON there is m_Properties? No, Unity 2021+ has properties as elements in m_Properties list, but they are nested objects or references? Let's check.

    with open('graph_summary.txt', 'w', encoding='utf-8') as f:
        # Find all nodes with names
        for node in data.get('m_Nodes', []):
            f.write(f"Node: {node.get('m_ObjectId')} - {node.get('m_Type')}\n")
            if 'UnityEditor.ShaderGraph.PropertyNode' in node.get('m_Type', ''):
                f.write(f"  Property: {node.get('m_Property', {}).get('m_Id', '')}\n")
        
        f.write("\nEdges:\n")
        for edge in data.get('m_Edges', []):
            i = edge.get('m_InputSlot', {})
            o = edge.get('m_OutputSlot', {})
            f.write(f"Edge: {o.get('m_Node', {}).get('m_Id')} slot {o.get('m_SlotId')} -> {i.get('m_Node', {}).get('m_Id')} slot {i.get('m_SlotId')}\n")

if __name__ == '__main__':
    analyze('Assets/SoStylized/Environment/Foliage/Shaders/S_FoliageShader.shadergraph')
