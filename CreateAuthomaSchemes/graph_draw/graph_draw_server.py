from flask import Flask, request, send_file, jsonify
import networkx as nx
import matplotlib.pyplot as plt
import io
import re
import xml.etree.ElementTree as ET
import matplotlib

# Ustawienie backendu Matplotlib na nieinteraktywny
matplotlib.use('Agg')

app = Flask(__name__)

# Funkcja do parsowania ApiResponse i tworzenia grafu
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
        nx.draw(graph, pos, with_labels=True, node_size=2000, node_color="skyblue",
                font_size=10, font_weight="bold", arrows=True)
        edge_labels = nx.get_edge_attributes(graph, 'label')
        nx.draw_networkx_edge_labels(graph, pos, edge_labels=edge_labels, font_color='red')

        # Rysowanie stanów akceptujących
        for state in accepting_states:
            if state.strip() in pos:
                nx.draw_networkx_nodes(
                    graph, pos, nodelist=[state.strip()],
                    node_size=2200, node_color="none", edgecolors="green", linewidths=2
                )

        # Zapis grafu do bufora pamięci
        img_buf = io.BytesIO()
        plt.savefig(img_buf, format='png')
        img_buf.seek(0)
        plt.close()
        return img_buf

    except Exception as e:
        print(f"Błąd parsowania lub rysowania grafu: {e}")
        raise e

# Funkcja do generowania pliku JFLAP
def generate_jflap(api_response):
    try:
        # Parsowanie stanów, stanów początkowych, akceptujących i funkcji przejścia
        states = re.findall(r'{([^}]+)}', api_response)
        initial_state = re.search(r'Stan początkowy: {([^}]+)}', api_response).group(1).strip()
        accepting_states = re.search(r'Stany akceptujące: {([^}]+)}', api_response).group(1).split(',')
        transitions = re.findall(r'δ\(([^,]+),\s*([^,]+)\)\s*=\s*([^\s]+)', api_response)

        # Tworzenie struktury XML dla JFLAP
        root = ET.Element("structure")
        ET.SubElement(root, "type").text = "fa"  # Typ: finite automaton

        automaton = ET.SubElement(root, "automaton")
        for idx, state in enumerate(states[0].split(',')):
            state_elem = ET.SubElement(automaton, "state", id=str(idx), name=state.strip())
            ET.SubElement(state_elem, "x").text = str(100 * (idx + 1))  # Pozycja X
            ET.SubElement(state_elem, "y").text = str(100 * (idx + 1))  # Pozycja Y
            if state.strip() == initial_state:
                ET.SubElement(state_elem, "initial")
            if state.strip() in [s.strip() for s in accepting_states]:
                ET.SubElement(state_elem, "final")

        for src, symbol, dest in transitions:
            transition = ET.SubElement(automaton, "transition")
            ET.SubElement(transition, "from").text = src.strip()
            ET.SubElement(transition, "to").text = dest.strip()
            ET.SubElement(transition, "read").text = symbol.strip()

        # Konwersja XML do stringa
        tree = ET.ElementTree(root)
        xml_buf = io.BytesIO()
        tree.write(xml_buf, encoding='utf-8', xml_declaration=True)
        xml_buf.seek(0)
        return xml_buf.getvalue().decode('utf-8')

    except Exception as e:
        print(f"Błąd generowania JFLAP: {e}")
        raise e

# Endpoint Flask do przyjmowania ApiResponse i zwracania obrazu grafu
@app.route('/generate-graph', methods=['POST'])
def generate_graph():
    try:
        api_response = request.form['ApiResponse']
        graph_img = parse_and_draw_graph(api_response)
        return send_file(graph_img, mimetype='image/png')
    except Exception as e:
        return f"Błąd generowania grafu: {e}", 500

# Endpoint Flask do przyjmowania ApiResponse i zwracania pliku JFLAP
@app.route('/generate-jflap', methods=['POST'])
def generate_jflap_endpoint():
    try:
        api_response = request.form['ApiResponse']
        jflap_data = generate_jflap(api_response)
        return jsonify({"jflap": jflap_data})
    except Exception as e:
        return f"Błąd generowania JFLAP: {e}", 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5001, debug=True)
