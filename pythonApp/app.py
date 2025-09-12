from flask import Flask, request, jsonify
from PIL import Image
from transformers import CLIPProcessor, CLIPModel
import torch
import io

print("Cargando modelo")
modelo = CLIPModel.from_pretrained("openai/clip-vit-base-patch32")
procesador = CLIPProcessor.from_pretrained("openai/clip-vit-base-patch32")
print("Modelo cargado. Servidor listo.")

app = Flask(__name__)

@app.route('/analyze', methods=['POST'])
def analyze_image():
    # 1. Verificar que la petición contiene todos los datos
    if 'image' not in request.files or 'specific_description' not in request.form or 'general_description' not in request.form:
        return jsonify({'error': 'Petición inválida, faltan datos'}), 400

    # 2. Obtener los datos
    file = request.files['image']
    descripcion_especifica = request.form['specific_description']
    descripcion_general = request.form['general_description']
    
    try:
        imagen_bytes = file.read()
        imagen = Image.open(io.BytesIO(imagen_bytes))

        # 3. Usar el modelo con ambas descripciones para obtener un resultado contextual
        descripciones = [descripcion_especifica, descripcion_general]
        inputs = procesador(text=descripciones, images=imagen, return_tensors="pt", padding=True)

        with torch.no_grad():
            outputs = modelo(**inputs)
        
        logits_per_image = outputs.logits_per_image
        probs = logits_per_image.softmax(dim=1)
        # La confianza que nos interesa es la de la primera descripción (la específica)
        confianza = probs[0][0].item()

        print(f"Análisis para '{descripcion_especifica}': {confianza*100:.2f}%")

        # 4. Devolver el resultado
        return jsonify({'confidence': confianza})

    except Exception as e:
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)