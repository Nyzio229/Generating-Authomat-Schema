# import sys
# import io
# import matplotlib.pyplot as plt
# import networkx as nx
# import re
# import json

# def parse_and_draw_graph(api_response):
#     try:
#         # Parsowanie stanów, stanów początkowych, akceptujących i funkcji przejścia
#         states = re.findall(r'{([^}]+)}', api_response)
#         initial_state = re.search(r'Stan początkowy: {([^}]+)}', api_response).group(1).strip()
#         accepting_states = re.search(r'Stany akceptujące: {([^}]+)}', api_response).group(1).split(',')
#         transitions = re.findall(r'δ\(([^,]+),\s*([^,]+)\)\s*=\s*([^\s]+)', api_response)

#         # Tworzenie grafu
#         graph = nx.DiGraph()
#         for state in states[0].split(','):
#             graph.add_node(state.strip())

#         # Dodawanie przejść na podstawie funkcji przejścia
#         for src, symbol, dest in transitions:
#             graph.add_edge(src.strip(), dest.strip(), label=symbol.strip())

#         # Rysowanie grafu
#         pos = nx.spring_layout(graph)
#         plt.figure(figsize=(8, 6))
#         nx.draw(graph, pos, with_labels=True, node_size=2000, node_color="skyblue", font_size=10, font_weight="bold", arrows=True)
#         edge_labels = nx.get_edge_attributes(graph, 'label')
#         nx.draw_networkx_edge_labels(graph, pos, edge_labels=edge_labels, font_color='red')

#         # Zapis grafu do bieżącego katalogu jako plik "generated_graph.png"
#         # plt.savefig("generated_graph.png")  # <-- Zapis grafu do pliku w bieżącym katalogu
#         # plt.close()
        
#         img_buf = io.BytesIO()
#         plt.savefig(img_buf, format='png')
#         plt.savefig("generated_graph.png") # test zapisu
#         img_buf.seek(0)
#         plt.close()
#         return img_buf
#     except Exception as e:
#         print(f"Błąd parsowania lub rysowania grafu: {e}")
#         sys.exit(1)

# if __name__ == "__main__":
#     if len(sys.argv) < 2:
#         print("No API response provided")
#         sys.exit(1)
    
#     api_response = sys.argv[1]
    
#     # Logowanie danych wejściowych
#     try:
#         print("Received API response:")
#         print(api_response)
        
#         data = json.loads(api_response)  # Próba parsowania JSON
#     except json.JSONDecodeError as e:
#         print(f"Error decoding JSON: {e}")
#         sys.exit(1)
print("xd")