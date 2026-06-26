import os
import sys
import threading
import time
import logging
import signal
from collections import deque
from datetime import datetime, time as dtime
from pathlib import Path

import cv2
import numpy as np
import requests
from flask import Flask, jsonify, request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from model.face_utils import extract_all_features, match_with_db
from config import Config

import urllib3
if not Config.VERIFY_SSL:
    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

app = Flask(__name__)

session_marked: set = set()
attendance_running: bool = False
active_session_name: str | None = None
recognition_thread: threading.Thread | None = None
recognition_lock = threading.Lock()

retry_queue: deque = deque(maxlen=1000)

MAX_CAMERA_RETRIES = 10
CAMERA_RETRY_DELAY = 3
API_RETRY_INTERVAL = 5
MAX_RETRY_ATTEMPTS = 3

current_settings = {
    "startTime": "23:00",
    "endTime": "04:00",
    "confidenceThreshold": 0.85,
    "isEnabled": True,
}

os.makedirs(Config.LOG_DIR, exist_ok=True)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.FileHandler(os.path.join(Config.LOG_DIR, "recognition.log"), encoding="utf-8"),
        logging.StreamHandler(sys.stdout),
    ],
)
logger = logging.getLogger(__name__)

if not Config.VERIFY_SSL:
    logger.warning("Running in Development Mode. SSL certificate verification is disabled.")

db_path = os.path.join(Config.BASE_DIR if hasattr(Config, "BASE_DIR") else os.path.dirname(__file__), "database", "students.npy")
db: dict = {}
if os.path.exists(db_path):
    try:
        db = np.load(db_path, allow_pickle=True).item()
        logger.info("Loaded %d students from database", len(db))
    except Exception as e:
        logger.error("Failed to load student database: %s", e)
else:
    logger.warning("No student database found at %s", db_path)


# def sync_settings():
#     global current_settings
#     try:
#         resp = requests.get(
#             f"{Config.UNISTAY_BASE_URL}/api/attendance/settings",
#             timeout=5,
#         )
#         if resp.status_code == 200:
#             data = resp.json()
#             current_settings.update(data)
#             logger.info("Settings synced: confidenceThreshold=%.2f, startTime=%s, endTime=%s, isEnabled=%s",
#                         current_settings.get("confidenceThreshold", 0.85),
#                         current_settings.get("startTime", "23:00"),
#                         current_settings.get("endTime", "04:00"),
#                         current_settings.get("isEnabled", True))
#         else:
#             logger.warning("Settings sync returned HTTP %d", resp.status_code)
#     except requests.ConnectionError:
#         logger.warning("Settings sync failed: cannot reach %s", Config.UNISTAY_BASE_URL)
#     except Exception as e:
#         logger.warning("Settings sync failed: %s", e)

def sync_settings():
    global current_settings

    try:
        resp = requests.get(
            f"{Config.UNISTAY_BASE_URL}/api/attendance/settings",
            timeout=5,
            verify=Config.VERIFY_SSL,
            headers={"X-Internal-Token": Config.INTERNAL_TOKEN},
        )

        resp.raise_for_status()

        data = resp.json()
        current_settings.update(data)

        logger.info("Settings synced successfully")

    except Exception as e:
        import traceback

        logger.exception("Settings sync failed")
        traceback.print_exc()

def is_within_hours():
    now = datetime.now().time()
    start_str = current_settings.get("startTime", "23:00")
    end_str = current_settings.get("endTime", "04:00")
    try:
        start = dtime.fromisoformat(start_str)
        end = dtime.fromisoformat(end_str)
    except (ValueError, TypeError):
        logger.warning("Invalid time format: start=%s, end=%s", start_str, end_str)
        return True

    if start <= end:
        return start <= now <= end
    else:
        return now >= start or now <= end


def process_checkin(student_id: int, best_name: str, best_score: float, timestamp: str) -> str:
    payload = {
        "studentID": student_id,
        "confidence": round(float(best_score), 4),
        "timestamp": timestamp,
    }
    try:
        resp = requests.post(
            f"{Config.UNISTAY_BASE_URL}/api/attendance/checkin",
            headers={"X-Internal-Token": Config.INTERNAL_TOKEN, "Content-Type": "application/json"},
            json=payload,
            timeout=10,
            verify=Config.VERIFY_SSL,
        )
        if resp.status_code == 200:
            logger.info("Checkin success: %s (ID=%d, score=%.4f)", best_name, student_id, best_score)
            return "success"
        if resp.status_code == 409:
            logger.info("Checkin duplicate (treat as success): %s (ID=%d)", best_name, student_id)
            return "success"
        if 400 <= resp.status_code < 500:
            logger.warning("Checkin discarded (HTTP %d): %s ID=%d", resp.status_code, best_name, student_id)
            return "discard"
        logger.warning("Checkin server error (HTTP %d): %s ID=%d", resp.status_code, best_name, student_id)
        return "retry"
    except (requests.ConnectionError, requests.Timeout) as e:
        logger.error("Checkin failed (queued): %s ID=%d — %s", best_name, student_id, e)
        return "retry"
    except Exception as e:
        logger.error("Checkin error: %s ID=%d — %s", best_name, student_id, e)
        return "retry"


def flush_retry_queue():
    if not retry_queue:
        return
    logger.info("Flushing retry queue (%d items)...", len(retry_queue))
    processed = 0
    while retry_queue:
        student_id, best_name, best_score, timestamp, retry_count = retry_queue.popleft()
        result = process_checkin(student_id, best_name, best_score, timestamp)
        if result == "success":
            processed += 1
            continue
        if result == "discard":
            logger.info("Discarded (non-retryable): %s ID=%d", best_name, student_id)
            processed += 1
            continue
        if retry_count >= MAX_RETRY_ATTEMPTS:
            logger.warning("Max retries (%d) reached, discarding: %s ID=%d", MAX_RETRY_ATTEMPTS, best_name, student_id)
            processed += 1
            continue
        retry_queue.append((student_id, best_name, best_score, timestamp, retry_count + 1))
        break
    if processed:
        logger.info("Processed %d items from retry queue (%d remaining)", processed, len(retry_queue))


def init_camera():
    for attempt in range(MAX_CAMERA_RETRIES):
        logger.info("[DIAG] Attempt %d/%d: calling cv2.VideoCapture(%s)", attempt + 1, MAX_CAMERA_RETRIES, Config.CAMERA_INDEX)
        import datetime as _dt
        _t0 = _dt.datetime.now()
        cam = cv2.VideoCapture(Config.CAMERA_INDEX)
        _elapsed = (_dt.datetime.now() - _t0).total_seconds()
        logger.info("[DIAG] cv2.VideoCapture() returned after %.2fs", _elapsed)
        _opened = cam.isOpened()
        logger.info("[DIAG] cam.isOpened() = %s", _opened)
        if _opened:
            _w = cam.get(cv2.CAP_PROP_FRAME_WIDTH)
            _h = cam.get(cv2.CAP_PROP_FRAME_HEIGHT)
            _backend = cam.get(cv2.CAP_PROP_BACKEND)
            logger.info("[DIAG] CAP_PROP_FRAME_WIDTH=%.0f, CAP_PROP_FRAME_HEIGHT=%.0f, CAP_PROP_BACKEND=%.0f", _w, _h, _backend)
            cam.set(cv2.CAP_PROP_FRAME_WIDTH, Config.FRAME_WIDTH)
            cam.set(cv2.CAP_PROP_FRAME_HEIGHT, Config.FRAME_HEIGHT)
            _w2 = cam.get(cv2.CAP_PROP_FRAME_WIDTH)
            _h2 = cam.get(cv2.CAP_PROP_FRAME_HEIGHT)
            logger.info("[DIAG] After set: width=%.0f, height=%.0f", _w2, _h2)
            return cam
        cam.release()
        logger.warning("Camera not available (attempt %d/%d), retrying in %ds...",
                       attempt + 1, MAX_CAMERA_RETRIES, CAMERA_RETRY_DELAY)
        time.sleep(CAMERA_RETRY_DELAY)
    return None


def recognition_loop():
    global attendance_running

    cam = init_camera()
    if cam is None:
        logger.error("Cannot open camera after %d attempts. Aborting recognition loop.", MAX_CAMERA_RETRIES)
        attendance_running = False
        return

    logger.info("Camera opened (index=%d, %dx%d)", Config.CAMERA_INDEX, Config.FRAME_WIDTH, Config.FRAME_HEIGHT)
    logger.info("[DIAG] recognition_loop entered. attendance_running=%s", attendance_running)

    last_settings_sync = 0.0
    last_queue_flush = 0.0
    consecutive_read_failures = 0
    max_read_failures = 30
    first_frame_logged = False
    loop_count = 0

    while attendance_running:
        loop_count += 1
        try:
            now = time.time()
            if now - last_settings_sync > Config.SETTINGS_SYNC_INTERVAL:
                logger.info("[DIAG] Performing settings sync (loop_count=%d)", loop_count)
                sync_settings()
                last_settings_sync = now

            if now - last_queue_flush > API_RETRY_INTERVAL:
                logger.info("[DIAG] Flushing retry queue (loop_count=%d)", loop_count)
                flush_retry_queue()
                last_queue_flush = now

            _is_enabled = current_settings.get("isEnabled", True)
            if not _is_enabled:
                logger.info("[DIAG] Loop blocked: isEnabled=False (loop_count=%d)", loop_count)
                time.sleep(1)
                continue

            _in_hours = is_within_hours()
            if not _in_hours:
                logger.info("[DIAG] Loop blocked: outside attendance hours (loop_count=%d)", loop_count)
                time.sleep(5)
                continue

            logger.info("[DIAG] Calling cam.read() (loop_count=%d)", loop_count)
            _t0 = time.time()
            ret, frame = cam.read()
            _elapsed = time.time() - _t0
            logger.info("[DIAG] cam.read() returned ret=%s, frame=%s, elapsed=%.3fs", ret, type(frame).__name__ if frame is not None else "None", _elapsed)
            if not ret:
                consecutive_read_failures += 1
                _width = cam.get(cv2.CAP_PROP_FRAME_WIDTH)
                _height = cam.get(cv2.CAP_PROP_FRAME_HEIGHT)
                logger.info("[DIAG] cam.read() ret=False. CAP_PROP_FRAME_WIDTH=%.0f, CAP_PROP_FRAME_HEIGHT=%.0f, consecutive_failures=%d", _width, _height, consecutive_read_failures)
                logger.warning("Camera read failed (%d/%d)", consecutive_read_failures, max_read_failures)
                if consecutive_read_failures >= max_read_failures:
                    logger.error("Too many read failures, reinitializing camera...")
                    cam.release()
                    cam = init_camera()
                    if cam is None:
                        logger.error("Camera reinitialization failed. Stopping loop.")
                        attendance_running = False
                        return
                    consecutive_read_failures = 0
                time.sleep(0.1)
                continue

            if not first_frame_logged:
                logger.info("[DIAG] FIRST SUCCESSFUL FRAME captured at loop_count=%d. frame.shape=%s, dtype=%s", loop_count, frame.shape, frame.dtype)
                first_frame_logged = True

            consecutive_read_failures = 0
            frame = cv2.resize(frame, (Config.FRAME_WIDTH, Config.FRAME_HEIGHT))
            results = extract_all_features(frame)
            logger.info("[DIAG] extract_all_features returned %d faces (loop_count=%d)", len(results) if results else 0, loop_count)

            display_frame = frame.copy()

            if results:
                threshold = float(current_settings.get("confidenceThreshold", Config.RECOGNITION_THRESHOLD))

                for feature, bbox in results:
                    x, y, w, h, _ = bbox
                    best_name, best_score, is_match = match_with_db(feature, db)

                    if not is_match or best_score < threshold:
                        cv2.rectangle(display_frame, (int(x), int(y)), (int(x + w), int(y + h)), (0, 0, 255), 2)
                        label = f"{best_score:.0%}" if best_name else "?"
                        cv2.putText(display_frame, label, (int(x), int(y) - 10),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)
                        continue

                    try:
                        student_id = int(best_name.split("_")[0])
                    except (ValueError, IndexError):
                        logger.warning("Invalid key format: %s", best_name)
                        cv2.rectangle(display_frame, (int(x), int(y)), (int(x + w), int(y + h)), (0, 0, 255), 2)
                        continue

                    with recognition_lock:
                        if student_id in session_marked:
                            logger.info("Duplicate ignored: %s (ID=%d)", best_name, student_id)
                            cv2.rectangle(display_frame, (int(x), int(y)), (int(x + w), int(y + h)), (0, 255, 255), 2)
                            cv2.putText(display_frame, f"{best_name.split('_', 1)[-1]} ({best_score:.0%})", (int(x), int(y) - 10),
                                        cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 255), 2)
                            continue
                        session_marked.add(student_id)

                    timestamp = datetime.now().strftime("%Y-%m-%dT%H:%M:%S")

                    result = process_checkin(student_id, best_name, best_score, timestamp)
                    if result == "success":
                        cv2.rectangle(display_frame, (int(x), int(y)), (int(x + w), int(y + h)), (0, 255, 0), 3)
                        cv2.putText(display_frame, f"{best_name.split('_', 1)[-1]} ({best_score:.0%})", (int(x), int(y) - 10),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
                        cv2.putText(display_frame, "✓", (int(x + w + 5), int(y + h // 2)),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
                    else:
                        cv2.rectangle(display_frame, (int(x), int(y)), (int(x + w), int(y + h)), (0, 255, 0), 2)
                        cv2.putText(display_frame, f"{best_name.split('_', 1)[-1]} ({best_score:.0%})", (int(x), int(y) - 10),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
                        if result == "retry":
                            retry_queue.append((student_id, best_name, best_score, timestamp, 0))
                            with recognition_lock:
                                session_marked.discard(student_id)

            cv2.imshow("UniStay - التعرف على الوجه", display_frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                logger.info("Camera preview closed by user (pressed 'q')")
                attendance_running = False
                break
            try:
                if cv2.getWindowProperty("UniStay - التعرف على الوجه", cv2.WND_PROP_VISIBLE) < 1:
                    logger.info("Camera preview window closed by user")
                    attendance_running = False
                    break
            except:
                pass

            time.sleep(Config.RECOGNITION_LOOP_DELAY)

        except Exception as e:
            logger.error("Recognition loop error: %s", e, exc_info=True)
            time.sleep(0.5)

    # Flush remaining queue before stopping
    flush_retry_queue()
    cam.release()
    cv2.destroyAllWindows()
    logger.info("Camera released, recognition loop ended")


@app.route("/api/session/start", methods=["POST"])
def start_session():
    global attendance_running, recognition_thread, active_session_name

    data = request.get_json(silent=True) or {}
    session_name = data.get("sessionName", f"Flask-{datetime.now():%Y%m%d-%H%M%S}")

    if attendance_running:
        return jsonify({"message": "Session already running", "sessionName": active_session_name}), 200

    sync_settings()

    with recognition_lock:
        session_marked.clear()
    retry_queue.clear()

    attendance_running = True
    active_session_name = session_name

    recognition_thread = threading.Thread(target=recognition_loop, daemon=True)
    recognition_thread.start()

    logger.info("Session started: %s", session_name)
    return jsonify({"message": "Session started", "sessionName": session_name}), 200


@app.route("/api/session/stop", methods=["POST"])
def stop_session():
    global attendance_running, recognition_thread, active_session_name

    if not attendance_running:
        return jsonify({"message": "No active session"}), 200

    attendance_running = False

    if recognition_thread and recognition_thread.is_alive():
        recognition_thread.join(timeout=10)

    logger.info("Session stopped: %s", active_session_name)
    active_session_name = None
    return jsonify({"message": "Session stopped"}), 200


@app.route("/api/status", methods=["GET"])
def get_status():
    with recognition_lock:
        marked_count = len(session_marked)
    return jsonify({
        "running": attendance_running,
        "sessionName": active_session_name,
        "markedCount": marked_count,
        "retryQueueSize": len(retry_queue),
        "settings": current_settings,
    }), 200


def shutdown_handler(sig, frame):
    global attendance_running
    logger.info("Received signal %d, initiating graceful shutdown...", sig)
    attendance_running = False
    if recognition_thread and recognition_thread.is_alive():
        recognition_thread.join(timeout=10)
    logger.info("Shutdown complete.")
    sys.exit(0)


# =============================================================================
# Phase 5.5 — Face Enrollment
# =============================================================================

import base64
import glob
import shutil

BACKUP_DIR = Config.BACKUP_DIR
MAX_BACKUPS = Config.MAX_BACKUPS
MIN_ENROLLMENT_FRAMES = 5
BLUR_THRESHOLD = Config.BLUR_THRESHOLD


def create_backup():
    os.makedirs(BACKUP_DIR, exist_ok=True)
    if not os.path.exists(db_path):
        return
    timestamp = datetime.now().strftime("%Y%m%d_%H%M")
    backup_path = os.path.join(BACKUP_DIR, f"students_{timestamp}.npy")
    shutil.copy2(db_path, backup_path)
    logger.info("Backup created: %s", backup_path)
    cleanup_backups()


def cleanup_backups():
    backups = sorted(glob.glob(os.path.join(BACKUP_DIR, "students_*.npy")))
    while len(backups) > MAX_BACKUPS:
        os.remove(backups[0])
        logger.info("Removed old backup: %s", backups[0])
        backups = backups[1:]


def save_student_db():
    create_backup()
    np.save(db_path, db)
    logger.info("Student database saved (%d students)", len(db))


def average_features(features):
    return np.mean(features, axis=0).tolist()


def is_blurry(image, threshold=BLUR_THRESHOLD):
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    return cv2.Laplacian(gray, cv2.CV_64F).var() < threshold


@app.route("/api/enrollment/list", methods=["GET"])
def enrollment_list():
    student_ids = sorted(db.keys())
    return jsonify({"enrolledStudents": student_ids, "total": len(student_ids)}), 200


@app.route("/api/enrollment/register", methods=["POST"])
def enrollment_register():
    data = request.get_json(silent=True) or {}
    student_id = data.get("studentID")
    images_b64 = data.get("images", [])

    if not student_id:
        return jsonify({"error": "معرف الطالب مطلوب"}), 400
    if not images_b64 or len(images_b64) < MIN_ENROLLMENT_FRAMES:
        return jsonify({"error": f"يلزم {MIN_ENROLLMENT_FRAMES} صور على الأقل"}), 400

    features = []
    errors = []

    for idx, img_b64 in enumerate(images_b64):
        try:
            if "," in img_b64:
                img_b64 = img_b64.split(",")[1]
            img_bytes = base64.b64decode(img_b64)
            nparr = np.frombuffer(img_bytes, np.uint8)
            frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            if frame is None:
                errors.append(f"الصورة {idx + 1} غير صالحة")
                continue

            gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            blur_score = cv2.Laplacian(gray, cv2.CV_64F).var()
            logger.info("[BLUR] Frame %d -> Blur Score = %.1f (threshold=%.1f)", idx + 1, blur_score, BLUR_THRESHOLD)
            if blur_score < BLUR_THRESHOLD:
                errors.append(f"الصورة {idx + 1} غير واضحة")
                continue

            results = extract_all_features(frame)
            if len(results) == 0:
                errors.append(f"لم يتم اكتشاف وجه في الصورة {idx + 1}")
                continue
            if len(results) > 1:
                errors.append(f"تم اكتشاف أكثر من وجه في الصورة {idx + 1}")
                continue

            feature, bbox = results[0]
            features.append(feature)

        except Exception as e:
            errors.append(f"خطأ في الصورة {idx + 1}: {e}")
            continue

    if len(features) < 3:
        return jsonify({
            "error": "لم يتم استخراج ملامح كافية",
            "details": errors,
            "validFrames": len(features)
        }), 400

    avg_feature = average_features(features)
    db[str(student_id)] = avg_feature
    save_student_db()

    logger.info("Enrolled student %s (%d/%d valid frames)", student_id, len(features), len(images_b64))
    return jsonify({
        "message": "تم تسجيل الوجه بنجاح",
        "studentID": student_id,
        "validFrames": len(features),
        "warnings": errors if errors else None
    }), 200


@app.route("/api/enrollment/delete", methods=["POST"])
def enrollment_delete():
    data = request.get_json(silent=True) or {}
    student_id = data.get("studentID")

    if not student_id:
        return jsonify({"error": "معرف الطالب مطلوب"}), 400

    sid = str(student_id)
    if sid not in db:
        return jsonify({"error": "الطالب غير مسجل في قاعدة بيانات الوجوه"}), 404

    del db[sid]
    save_student_db()

    logger.info("Deleted student %s from face database", student_id)
    return jsonify({"message": "تم حذف الوجه بنجاح", "studentID": student_id}), 200


@app.route("/api/enrollment/test", methods=["POST"])
def enrollment_test():
    data = request.get_json(silent=True) or {}
    image_b64 = data.get("image")

    if not image_b64:
        return jsonify({"error": "الصورة مطلوبة"}), 400

    try:
        if "," in image_b64:
            image_b64 = image_b64.split(",")[1]
        img_bytes = base64.b64decode(image_b64)
        nparr = np.frombuffer(img_bytes, np.uint8)
        frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if frame is None:
            return jsonify({"error": "الصورة غير صالحة"}), 400

        results = extract_all_features(frame)
        if len(results) == 0:
            return jsonify({"match": False, "message": "لم يتم اكتشاف وجه"}), 200
        if len(results) > 1:
            return jsonify({"match": False, "message": "تم اكتشاف أكثر من وجه"}), 200

        feature, bbox = results[0]
        best_name, best_score, is_match = match_with_db(feature, db)

        if not is_match:
            return jsonify({
                "match": False,
                "message": "لا يوجد تطابق"
            }), 200

        return jsonify({
            "match": True,
            "studentID": best_name,
            "confidence": round(float(best_score), 4),
            "message": f"تم التعرف على الطالب بنسبة ثقة {best_score:.2%}"
        }), 200

    except Exception as e:
        logger.error("Test recognition error: %s", e)
        return jsonify({"error": f"خطأ في اختبار التعرف: {e}"}), 500


@app.route("/api/shutdown", methods=["POST"])
def shutdown():
    logger.info("Shutdown requested via API")
    global attendance_running
    attendance_running = False
    if recognition_thread and recognition_thread.is_alive():
        recognition_thread.join(timeout=10)
    logger.info("Shutdown complete. Exiting process.")
    os._exit(0)

signal.signal(signal.SIGINT, shutdown_handler)
signal.signal(signal.SIGTERM, shutdown_handler)

if __name__ == "__main__":
    logger.info("Starting Flask Attendance Recognition API on port %d", Config.FLASK_PORT)
    sync_settings()
    app.run(host="0.0.0.0", port=Config.FLASK_PORT, debug=False, use_reloader=False)
