import os

BASE_DIR = os.path.dirname(os.path.abspath(__file__))


class Config:
    UNISTAY_BASE_URL = os.environ.get("UNISTAY_BASE_URL", "https://localhost:7003")
    INTERNAL_TOKEN = os.environ.get("UNISTAY_INTERNAL_TOKEN", "your-internal-token-here")
    CAMERA_INDEX = int(os.environ.get("CAMERA_INDEX", "0"))
    RECOGNITION_THRESHOLD = float(os.environ.get("RECOGNITION_THRESHOLD", "0.85"))
    FLASK_PORT = int(os.environ.get("FLASK_PORT", "5050"))
    FRAME_WIDTH = 640
    FRAME_HEIGHT = 480
    SETTINGS_SYNC_INTERVAL = 60
    RECOGNITION_LOOP_DELAY = 0.05
    LOG_DIR = os.path.join(BASE_DIR, "logs")
    VERIFY_SSL = os.environ.get("FLASK_VERIFY_SSL", "false").lower() == "true"
    ENROLLMENT_FRAMES = 15
    BACKUP_DIR = os.path.join(BASE_DIR, "database", "backups")
    MAX_BACKUPS = 20
    BLUR_THRESHOLD = 5.0
