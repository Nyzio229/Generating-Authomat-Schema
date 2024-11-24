from flask import Flask, request, send_file
import networkx as nx
import matplotlib.pyplot as plt
import io
import re
import matplotlib

# Ustawienie backendu Matplotlib na nieinteraktywny
matplotlib.use('Agg')

app = Flask(__name__)

def parse_and_draw_graph(api_response):
    try:
        # Parsowanie stanów, stanów początkowych, akceptujących i funkcji przejścia
        states = re.findall(r'{([^}]+)}', api_response)
        initial_state = re.search(r'Stan początkowy: {([^}]+)}', api_response).group(1).strip()
        accepting_states = re.search(r'Stany akceptujące: {([^}]+)}', api_response).group(1).split(',')
        transitions = re.findall(r'δ\(([^,]+),\s*([^,]+)\)\s*=\s*([^\s]+)', api_response)

        # Tworzenie grafu
        graph = nx.DiGraph()
        for state in states[0].split(','):
            graph.add_node(state.strip())

        # Dodawanie przejść na podstawie funkcji przejścia
        for src, symbol, dest in transitions:
            graph.add_edge(src.strip(), dest.strip(), label=symbol.strip())

        # Rysowanie grafu
        pos = nx.spring_layout(graph)
        plt.figure(figsize=(8, 6))
        nx.draw(graph, pos, with_labels=True, node_size=2000, node_color="skyblue", font_size=10, font_weight="bold", arrows=True)
        edge_labels = nx.get_edge_attributes(graph, 'label')
        nx.draw_networkx_edge_labels(graph, pos, edge_labels=edge_labels, font_color='red')

        # Zapis grafu do bufora pamięci
        img_buf = io.BytesIO()
        plt.savefig(img_buf, format='png')
        plt.savefig("graph.png")
        img_buf.seek(0)
        plt.close('all')  # Zwolnienie zasobów Matplotlib
        return img_buf

    except Exception as e:
        print(f"Błąd parsowania lub rysowania grafu: {e}")
        raise e  # Prześlij wyjątek do logów Flask dla pełnych szczegółów

@app.route('/generate-graph', methods=['POST'])
def generate_graph():
    try:
        api_response = request.form['ApiResponse']
        graph_img = parse_and_draw_graph(api_response)
        return send_file(graph_img, mimetype='image/png')
    except Exception as e:
        return f"Błąd generowania grafu: {e}", 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)  # Serwer Flask na porcie 5001
