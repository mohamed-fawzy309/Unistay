import os
import cv2
import numpy as np

from yunet import YuNet
from sface import SFace

BASE_DIR = os.path.dirname(os.path.dirname(__file__))
MODELS_DIR = os.path.join(BASE_DIR, "models")

detector = YuNet(os.path.join(MODELS_DIR, "face_detection_yunet_2023mar_int8.onnx"))
recognizer = SFace(os.path.join(MODELS_DIR, "face_recognition_sface_2021dec_int8.onnx"))

detector.setInputSize([640, 480])


def extract_all_features(image):
    """
    Detect ALL faces and return: [(feature_vector, bbox), ...]
    bbox format: [x, y, w, h, score]
    """
    faces = detector.infer(image)
    if faces is None or len(faces) == 0:
        return []

    results = []
    for face in faces:
        feature = recognizer.infer(image, face)
        results.append((feature, face))
    return results


def match_with_db(feature, db):
    """
    Match feature against all stored students.
    Return (best_name, best_score, matched_boolean)
    """
    best_name = None
    best_score = -9999
    best_match = False

    for name, base_feat in db.items():
        score, is_match = recognizer.match_features(base_feat, feature)
        score = float(score)
        if score > best_score:
            best_score = score
            best_name = name
            best_match = bool(is_match)

    return best_name, best_score, best_match
