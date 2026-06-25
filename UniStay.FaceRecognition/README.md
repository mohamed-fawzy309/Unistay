# Face GUI App

1. Put your ONNX models into `models/`:
   - face_detection_yunet_2023mar_int8.onnx
   - face_recognition_sface_2021dec_int8.onnx

2. Install dependencies:
   pip install -r requirements.txt

3. Run:
   python app.py

# Notes
- The first time you register a student, `database/students.npy` will be created.
- Models are loaded once at app start for performance.
- This GUI uses your Yunet + SFace wrappers. 
