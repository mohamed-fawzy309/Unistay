import os
import sys
import threading
import time
import logging
import signal
import base64
import glob
import shutil
import json
from collections import deque
from datetime import datetime, time as dtime
from pathlib import Path

import cv2
import numpy as np
import requests
from flask import Flask, jsonify, request, Response

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
CAMERA_INIT_TIMEOUT = 3.0

current_settings = {
    "startTime": "23:00",
    "endTime": "04:00",
    "confidenceThreshold": 0.85,
    "isEnabled": True,
}

latest_frame = None
latest_frame_lock = threading.Lock()
latest_faces: list = []
latest_faces_lock = threading.Lock()
recognition_events: deque = deque(maxlen=200)
student_details_cache: dict = {}
student_details_cache_lock = threading.Lock()
STUDENT_CACHE_MAX_SIZE = 500

fps_counter = {
    "fps": 0,
    "count": 0,
    "last_time": time.time(),
    "resolution": "640x480",
}
session_start_time: float = 0.0
total_faces_detected: int = 0
total_students_recognized: int = 0
total_unknown_faces: int = 0
camera_is_open: bool = False
dev_mode: bool = False
selected_camera_index: int = Config.CAMERA_INDEX
jpeg_quality: int = Config.JPEG_QUALITY
selected_resolution: tuple | None = None  # None = auto
target_fps: int = Config.TARGET_FPS
camera_backend_id: int = 0
actual_width: int = 0
actual_height: int = 0
actual_fps: float = 0.0
camera_settings_version: int = 0  # incremented when resolution/fps changes

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
        raw_db = np.load(db_path, allow_pickle=True).item()
        db = {}
        for key, val in raw_db.items():
            if isinstance(val, np.ndarray) and val.dtype in (np.float32, np.float64):
                if val.ndim == 1:
                    val = val.reshape(1, -1)
                db[key] = val.astype(np.float32)
            elif isinstance(val, list):
                arr = np.array(val, dtype=np.float32)
                if arr.ndim == 1:
                    arr = arr.reshape(1, -1)
                elif arr.ndim == 0:
                    logger.warning("Skipping empty embedding for key '%s'", key)
                    continue
                db[key] = arr
            elif isinstance(val, np.ndarray) and val.dtype == object:
                logger.warning("Skipping object-dtype embedding for key '%s'", key)
                continue
            else:
                logger.warning("Skipping invalid embedding for key '%s': type=%s", key, type(val).__name__)
        logger.info("Loaded %d students from database (%d raw entries migrated)", len(db), len(raw_db))
    except Exception as e:
        logger.error("Failed to load student database: %s", e)
else:
    logger.warning("No student database found at %s", db_path)


from functools import wraps


def require_api_token(f):
    @wraps(f)
    def decorated(*args, **kwargs):
        token = request.headers.get("X-Internal-Token")
        if token != Config.INTERNAL_TOKEN:
            logger.warning(
                "Unauthorized request from %s — %s %s (invalid token)",
                request.remote_addr, request.method, request.path,
            )
            return jsonify({"success": False, "message": "Unauthorized"}), 401
        return f(*args, **kwargs)
    return decorated


def push_event(event_type: str, message: str, **kwargs):
    entry = {
        "time": datetime.now().strftime("%H:%M:%S"),
        "type": event_type,
        "message": message,
    }
    entry.update(kwargs)
    recognition_events.appendleft(entry)


def sync_settings():
    global current_settings
    try:
        resp = requests.get(
            f"{Config.UNISTAY_BASE_URL}/api/attendance/settings",
            headers={"X-Internal-Token": Config.INTERNAL_TOKEN},
            timeout=5,
            verify=Config.VERIFY_SSL,
            headers={"X-Internal-Token": Config.INTERNAL_TOKEN},
        )
        resp.raise_for_status()
        data = resp.json()
        current_settings.update(data)
        logger.info("Settings synced successfully")
    except Exception as e:
        logger.exception("Settings sync failed")


def fetch_student_details(student_id: int) -> dict | None:
    with student_details_cache_lock:
        if student_id in student_details_cache:
            return student_details_cache[student_id]
    try:
        resp = requests.get(
            f"{Config.UNISTAY_BASE_URL}/api/attendance/student-accommodation/{student_id}",
            headers={"X-Internal-Token": Config.INTERNAL_TOKEN},
            timeout=5,
            verify=Config.VERIFY_SSL,
        )
        if resp.status_code == 200:
            data = resp.json()
            with student_details_cache_lock:
                if len(student_details_cache) >= STUDENT_CACHE_MAX_SIZE:
                    try:
                        student_details_cache.pop(next(iter(student_details_cache)))
                    except StopIteration:
                        pass
                student_details_cache[student_id] = data
            return data
    except Exception as e:
        logger.debug("Failed to fetch details for student %d: %s", student_id, e)
    return None


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


# ---------------------------------------------------------------------------
# Robust camera initialization helper
# ---------------------------------------------------------------------------
def _open_camera_backend(index, backend, backend_name, timeout=3.0):
    """Try opening camera with a specific backend in the CALLING thread.
    Returns (VideoCapture_or_None, error_str_or_None)."""
    _tid = threading.current_thread().ident
    logger.info("Trying backend %s (%d) for camera index %d  [thread=%s]",
                backend_name, backend, index, _tid)
    t0 = time.time()

    cap = cv2.VideoCapture(index, backend)
    elapsed = time.time() - t0
    logger.info("Backend %s: cv2.VideoCapture returned after %.1fs  [thread=%s]",
                backend_name, elapsed, _tid)

    if elapsed > timeout:
        logger.warning("Backend %s took %.1fs (exceeds %.1fs timeout), releasing  [thread=%s]",
                       backend_name, elapsed, timeout, _tid)
        cap.release()
        return None, "timeout"

    opened = cap.isOpened()
    w = cap.get(cv2.CAP_PROP_FRAME_WIDTH)
    h = cap.get(cv2.CAP_PROP_FRAME_HEIGHT)
    fps = cap.get(cv2.CAP_PROP_FPS)
    actual_backend = cap.get(cv2.CAP_PROP_BACKEND)
    logger.info(
        "Backend %s (%d): isOpened=%s  %dx%d  fps=%.1f  actual_backend=%.0f  [thread=%s]",
        backend_name, backend, opened, int(w), int(h), fps, actual_backend, _tid,
    )

    if not opened:
        logger.info("Backend %s: isOpened=False, releasing  [thread=%s]", backend_name, _tid)
        cap.release()
        return None, "not_opened"

    ret, frame = cap.read()
    if not ret or frame is None:
        logger.warning("Backend %s: frame read FAILED after open  [thread=%s]", backend_name, _tid)
        cap.release()
        return None, "read_failed"

    logger.info(
        "Backend %s: frame read OK  %dx%d  %.1fms  [thread=%s]",
        backend_name, frame.shape[1], frame.shape[0],
        (time.time() - t0) * 1000, _tid,
    )
    return cap, None


def init_camera(index=None):
    """Open camera by trying backends in priority order. Returns VideoCapture or None."""
    global camera_backend_id, actual_width, actual_height, actual_fps, camera_settings_version
    if index is None:
        index = selected_camera_index

    if sys.platform == "win32":
        backends = [
            (cv2.CAP_DSHOW, "DSHOW"),
            (cv2.CAP_MSMF, "MSMF"),
            (cv2.CAP_ANY, "DEFAULT"),
        ]
    else:
        backends = [
            (cv2.CAP_V4L2, "V4L2"),
            (cv2.CAP_ANY, "DEFAULT"),
        ]

    # Determine resolutions to try
    if selected_resolution is not None:
        res_list = [selected_resolution]
    else:
        res_list = Config.RESOLUTION_PRESETS

    last_error = None
    for backend, name in backends:
        cap, err = _open_camera_backend(index, backend, name, timeout=CAMERA_INIT_TIMEOUT)
        if cap is None:
            last_error = err
            continue

        # Try resolutions in order, accept first that works
        best_cap = None
        best_w = 0
        best_h = 0
        for target_w, target_h in res_list:
            logger.info("Requesting resolution %dx%d on backend %s", target_w, target_h, name)
            cap.set(cv2.CAP_PROP_FRAME_WIDTH, target_w)
            cap.set(cv2.CAP_PROP_FRAME_HEIGHT, target_h)
            actual_w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
            actual_h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
            logger.info("  Actual resolution: %dx%d (requested %dx%d)", actual_w, actual_h, target_w, target_h)
            if actual_w > 0 and actual_h > 0:
                best_cap = cap
                best_w = actual_w
                best_h = actual_h
                break

        if best_cap is None and cap is not None:
            # Camera opened but no resolution matched — accept whatever we got
            actual_w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
            actual_h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
            if actual_w > 0 and actual_h > 0:
                best_cap = cap
                best_w = actual_w
                best_h = actual_h

        if best_cap is not None:
            # Set additional properties
            cap.set(cv2.CAP_PROP_FPS, target_fps)
            cap.set(cv2.CAP_PROP_BUFFERSIZE, Config.CAMERA_BUFFERSIZE)
            try:
                cap.set(cv2.CAP_PROP_AUTOFOCUS, 1)
            except Exception:
                pass

            # Read actual FPS
            actual_fps_val = cap.get(cv2.CAP_PROP_FPS)
            ab = cap.get(cv2.CAP_PROP_BACKEND)
            camera_backend_id = int(ab)
            actual_width = best_w
            actual_height = best_h
            actual_fps = actual_fps_val if actual_fps_val > 0 else float(target_fps)

            logger.info(
                "Camera opened  backend=%s  backend_id=%d  resolution=%dx%d  fps=%.1f  buffersize=%d",
                name, int(ab), best_w, best_h, actual_fps_val, Config.CAMERA_BUFFERSIZE,
            )
            return cap

        cap.release()
        last_error = err

    logger.error("NO CAMERA AVAILABLE for index %d (last error: %s)", index, last_error)
    return None


def recognition_loop():
    global attendance_running, latest_frame, latest_faces, camera_is_open
    global total_faces_detected, total_students_recognized, total_unknown_faces
    global session_start_time, fps_counter, recognition_thread

    logger.info("Recognition loop started (camera index=%d)", selected_camera_index)
    push_event("recognition_started", "بدء حلقة التعرف")

    cam = None
    try:
        session_start_time = time.time()
        cam = init_camera()
        if cam is None:
            logger.error("Cannot open camera. Recognition loop entering offline monitoring.")
            camera_is_open = False
            push_event("camera_offline", "الكاميرا غير متصلة - وضع المراقبة")
            while attendance_running:
                time.sleep(CAMERA_RETRY_DELAY)
                logger.info("Retrying camera init (offline monitoring)...")
                push_event("camera_retry", "محاولة إعادة فتح الكاميرا")
                cam = init_camera()
                if cam is not None:
                    camera_is_open = True
                    push_event("camera_started", "تم فتح الكاميرا بنجاح بعد المراقبة")
                    break
            if cam is None:
                logger.info("Recognition loop ending (camera never became available, attendance_running=%s)", attendance_running)
                return

        camera_is_open = True
        _w = cam.get(cv2.CAP_PROP_FRAME_WIDTH)
        _h = cam.get(cv2.CAP_PROP_FRAME_HEIGHT)
        _backend = cam.get(cv2.CAP_PROP_BACKEND)
        fps_counter["resolution"] = f"{int(_w)}x{int(_h)}"
        logger.info("Camera opened (index=%d, %dx%d, backend=%s)", selected_camera_index, int(_w), int(_h), _backend)
        push_event("camera_started", f"تم فتح الكاميرا ({int(_w)}x{int(_h)})")

        _tid = threading.current_thread().ident
        _frame_num = 0
        _last_settings_ver = camera_settings_version
        last_log_time = 0.0
        last_settings_sync = 0.0
        last_queue_flush = 0.0
        consecutive_read_failures = 0
        max_read_failures = 30
        loop_count = 0
        recog_loop_iterations = 0

        session_start_time = time.time()

        while attendance_running:
            loop_count += 1
            try:
                # Hot-reload camera when resolution/FPS changes
                if camera_settings_version != _last_settings_ver:
                    logger.info("Camera settings changed (version %d -> %d), reinitializing...",
                                _last_settings_ver, camera_settings_version)
                    _last_settings_ver = camera_settings_version
                    cam.release()
                    logger.info("cam.release() called by recognition_loop for settings change  [thread=%s]", _tid)
                    camera_is_open = False
                    with latest_frame_lock:
                        latest_frame = None
                        logger.info("latest_frame set to None (settings change)  [thread=%s]", _tid)
                    cam = init_camera()
                    if cam is not None:
                        camera_is_open = True
                        _w = cam.get(cv2.CAP_PROP_FRAME_WIDTH)
                        _h = cam.get(cv2.CAP_PROP_FRAME_HEIGHT)
                        fps_counter["resolution"] = f"{int(_w)}x{int(_h)}"
                        logger.info("Camera reinit after settings change: %dx%d", int(_w), int(_h))
                        push_event("camera_started", f"تم إعادة فتح الكاميرا ({int(_w)}x{int(_h)})")
                    else:
                        logger.error("Camera reinit after settings change failed, entering offline monitoring")
                        push_event("camera_offline", "فشل إعادة فتح الكاميرا بعد تغيير الإعدادات")
                        while attendance_running:
                            time.sleep(CAMERA_RETRY_DELAY)
                            cam = init_camera()
                            if cam is not None:
                                camera_is_open = True
                                break
                        if cam is None:
                            return

                now = time.time()

                if now - last_settings_sync > Config.SETTINGS_SYNC_INTERVAL:
                    sync_settings()
                    last_settings_sync = now

                if now - last_queue_flush > API_RETRY_INTERVAL:
                    flush_retry_queue()
                    last_queue_flush = now

                _is_enabled = current_settings.get("isEnabled", True)
                if not _is_enabled:
                    time.sleep(1)
                    continue

                _in_hours = is_within_hours()
                if not _in_hours:
                    time.sleep(5)
                    continue

                ret, frame = cam.read()
                if not ret:
                    consecutive_read_failures += 1
                    logger.warning("Frame read #%d FAILED (%d/%d)  [thread=%s]",
                                   _frame_num, consecutive_read_failures, max_read_failures, _tid)
                    if consecutive_read_failures >= max_read_failures:
                        logger.error("Too many read failures, reinitializing camera...")
                        push_event("camera_error", "إعادة تهيئة الكاميرا بعد فشل متكرر")
                        cam.release()
                        logger.info("cam.release() called by recognition_loop for reinit  [thread=%s]", _tid)
                        camera_is_open = False
                        with latest_frame_lock:
                            latest_frame = None
                            logger.info("latest_frame set to None (camera reinit)  [thread=%s]", _tid)
                        cam = init_camera()
                        if cam is None:
                            logger.error("Camera reinitialization failed. Entering offline monitoring  [thread=%s]", _tid)
                            push_event("camera_offline", "الكاميرا غير متصلة - وضع المراقبة")
                            camera_is_open = False
                            while attendance_running:
                                time.sleep(CAMERA_RETRY_DELAY)
                                logger.info("Retrying camera reinit (offline monitoring)...")
                                push_event("camera_retry", "محاولة إعادة فتح الكاميرا")
                                cam = init_camera()
                                if cam is not None:
                                    camera_is_open = True
                                    push_event("camera_started", "تم فتح الكاميرا بنجاح بعد المراقبة")
                                    break
                            if cam is None:
                                return
                        camera_is_open = True
                        _w2 = cam.get(cv2.CAP_PROP_FRAME_WIDTH)
                        _h2 = cam.get(cv2.CAP_PROP_FRAME_HEIGHT)
                        logger.info("Camera reinit (index=%d, %dx%d)  [thread=%s]", selected_camera_index, int(_w2), int(_h2), _tid)
                        push_event("camera_started", "تم إعادة فتح الكاميرا بنجاح")
                        consecutive_read_failures = 0
                    time.sleep(0.1)
                    continue

                consecutive_read_failures = 0
                _frame_num += 1
                frame = cv2.resize(frame, (Config.FRAME_WIDTH, Config.FRAME_HEIGHT))

                with latest_frame_lock:
                    latest_frame = frame.copy()
                    if _frame_num <= 5 or _frame_num % 50 == 0:
                        logger.info("Frame #%d  %dx%d  timestamp=%.3f  thread=%s",
                                    _frame_num, frame.shape[1], frame.shape[0], time.time(), _tid)

                fps_counter["count"] += 1
                if now - fps_counter["last_time"] >= 1.0:
                    fps_counter["fps"] = fps_counter["count"]
                    fps_counter["count"] = 0
                    fps_counter["last_time"] = now
                    if loop_count % 100 == 0:
                        logger.info("Loop stats: iter=%d, fps=%d, faces=%d, recognized=%d, unknown=%d",
                                    loop_count, fps_counter["fps"], total_faces_detected,
                                    total_students_recognized, total_unknown_faces)

                results = extract_all_features(frame)
                current_faces = []

                if results:
                    recog_loop_iterations += 1
                    total_faces_detected += 1

                    threshold = float(current_settings.get("confidenceThreshold", Config.RECOGNITION_THRESHOLD))

                    for feature, bbox in results:
                        try:
                            best_name, best_score, is_match = match_with_db(feature, db)
                        except Exception as e:
                            logger.error("match_with_db failed: %s", e)
                            continue

                        face_info = {
                            "bbox": [int(v) for v in bbox],
                            "name": best_name if is_match else "UNKNOWN",
                            "score": round(float(best_score), 4) if is_match else 0,
                            "isMatch": is_match,
                            "aboveThreshold": is_match and best_score >= threshold,
                            "studentID": 0,
                            "city": "",
                            "building": "",
                            "room": "",
                            "bed": "",
                        }
                        if is_match:
                            try:
                                sid = int(best_name.split("_")[0])
                                face_info["studentID"] = sid
                                details = fetch_student_details(sid)
                                if details:
                                    face_info["city"] = details.get("city", "")
                                    face_info["building"] = details.get("building", "")
                                    face_info["room"] = details.get("room", "")
                                    face_info["bed"] = details.get("bed", "")
                            except (ValueError, IndexError):
                                pass
                        current_faces.append(face_info)

                        if not is_match:
                            total_unknown_faces += 1
                            push_event("unknown_face", "وجه غير معروف", confidence=round(float(best_score), 4))
                            continue

                        if best_score < threshold:
                            push_event("low_confidence", f"ثقة منخفضة: {best_name} ({best_score:.2%})",
                                       studentName=best_name, confidence=round(float(best_score), 4))
                            continue

                        try:
                            student_id = int(best_name.split("_")[0])
                        except (ValueError, IndexError):
                            logger.warning("Invalid key format: %s", best_name)
                            continue

                        with recognition_lock:
                            if student_id in session_marked:
                                push_event("already_marked", f"مسجل مسبقًا: {best_name}",
                                           studentName=best_name, studentID=student_id,
                                           confidence=round(float(best_score), 4))
                                continue
                            session_marked.add(student_id)

                        timestamp = datetime.now().strftime("%Y-%m-%dT%H:%M:%S")
                        result = process_checkin(student_id, best_name, best_score, timestamp)

                        if result == "success":
                            total_students_recognized += 1
                            push_event("recognition_success", f"تم تسجيل {best_name}",
                                       studentName=best_name, studentID=student_id,
                                       confidence=round(float(best_score), 4),
                                       attendanceResult="MARKED")
                        elif result == "retry":
                            retry_queue.append((student_id, best_name, best_score, timestamp, 0))
                            with recognition_lock:
                                session_marked.discard(student_id)
                            push_event("retry_queued", f"في انتظار إعادة المحاولة: {best_name}",
                                       studentName=best_name, studentID=student_id)

                with latest_faces_lock:
                    latest_faces = current_faces

                disp = frame.copy()
                for f in current_faces:
                    bx, by, bw, bh = f["bbox"][:4]
                    name = f["name"]
                    score = f["score"]
                    is_match = f["isMatch"]
                    above = f["aboveThreshold"]
                    sid = f["studentID"]

                    if not is_match:
                        color = (0, 0, 255)
                        label = "?"
                    elif not above:
                        color = (0, 165, 255)
                        label = f"{score:.0%}"
                    elif sid and sid in session_marked:
                        color = (0, 255, 255)
                        label = f"{name.split('_', 1)[-1] if '_' in name else name} ({score:.0%})"
                    else:
                        color = (0, 255, 0)
                        label = f"{name.split('_', 1)[-1] if '_' in name else name} ({score:.0%})"

                    cv2.rectangle(disp, (bx, by), (bx + bw, by + bh), color, 2)
                    cv2.putText(disp, label, (bx, by - 10),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)

                cv2.imshow("UniStay - التعرف على الوجه", disp)
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

            except cv2.error as e:
                logger.error("OpenCV error in recognition loop: %s", e)
                push_event("recognition_error", f"خطأ OpenCV: {str(e)[:50]}")
                time.sleep(0.5)
            except Exception as e:
                logger.error("Recognition loop error: %s", e, exc_info=True)
                push_event("recognition_error", f"خطأ عام: {str(e)[:50]}")
                time.sleep(0.5)

    except Exception as e:
        logger.error("Unhandled exception in recognition_loop: %s", e, exc_info=True)
        push_event("recognition_error", f"خطأ غير متوقع: {str(e)[:50]}")
    finally:
        _tid_exit = threading.current_thread().ident
        flush_retry_queue()
        if cam is not None:
            cam.release()
            logger.info("cam.release() called by recognition_loop finally  [thread=%s]", _tid_exit)
        camera_is_open = False
        cv2.destroyAllWindows()
        with latest_frame_lock:
            latest_frame = None
            logger.info("latest_frame set to None (recognition loop ended)  [thread=%s]", _tid_exit)
        logger.info("Recognition loop ended  [thread=%s]", _tid_exit)
        push_event("camera_stopped", "تم إيقاف الكاميرا")

        if attendance_running:
            logger.warning("Recognition loop exited unexpectedly while attendance_running=True — restarting")
            push_event("recognition_restart", "إعادة تشغيل حلقة التعرف تلقائياً")
            recognition_thread = threading.Thread(target=recognition_loop, daemon=True)
            recognition_thread.start()


@app.route("/api/session/start", methods=["POST"])
@require_api_token
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
    push_event("session_started", f"بدء الجلسة: {session_name}")
    return jsonify({"message": "Session started", "sessionName": session_name}), 200


@app.route("/api/session/stop", methods=["POST"])
@require_api_token
def stop_session():
    global attendance_running, recognition_thread, active_session_name

    if not attendance_running:
        return jsonify({"message": "No active session"}), 200

    push_event("session_stopped", f"إيقاف الجلسة: {active_session_name}")
    attendance_running = False

    if recognition_thread and recognition_thread.is_alive():
        recognition_thread.join(timeout=10)

    logger.info("Session stopped: %s", active_session_name)
    with recognition_lock:
        session_marked.clear()
    active_session_name = None
    return jsonify({"message": "Session stopped"}), 200


@app.route("/api/status", methods=["GET"])
@require_api_token
def get_status():
    with recognition_lock:
        marked_count = len(session_marked)
    return jsonify({
        "running": attendance_running,
        "sessionName": active_session_name,
        "markedCount": marked_count,
        "retryQueueSize": len(retry_queue),
        "settings": current_settings,
        "cameraOpen": camera_is_open,
        "fps": fps_counter["fps"],
        "resolution": fps_counter["resolution"],
        "sessionDuration": round(time.time() - session_start_time, 1) if attendance_running and session_start_time > 0 else 0,
        "facesDetected": total_faces_detected,
        "studentsRecognized": total_students_recognized,
        "unknownFaces": total_unknown_faces,
        "devMode": dev_mode,
        "cameraIndex": selected_camera_index,
        "actualWidth": actual_width,
        "actualHeight": actual_height,
        "actualFPS": round(actual_fps, 1),
        "jpegQuality": jpeg_quality,
        "cameraBackend": camera_backend_id,
    }), 200


@app.route("/api/camera/stream")
def camera_stream():
    token = request.headers.get("X-Internal-Token")
    if token != Config.INTERNAL_TOKEN:
        logger.warning(
            "Unauthorized camera stream access from %s (invalid token)",
            request.remote_addr,
        )
        return jsonify({"success": False, "message": "Unauthorized"}), 401

    def generate():
        empty_jpeg = _make_empty_jpeg()
        while True:
            try:
                with latest_frame_lock:
                    if latest_frame is None:
                        yield b'--frame\r\nContent-Type: image/jpeg\r\n\r\n' + empty_jpeg + b'\r\n'
                        time.sleep(0.1)
                        continue
                    display = latest_frame.copy()

                with latest_faces_lock:
                    faces = list(latest_faces)

                for face in faces:
                    bbox = face.get("bbox")
                    if not bbox:
                        continue
                    x, y, w, h = bbox
                    is_match = face.get("isMatch", False)
                    above = face.get("aboveThreshold", False)

                    if above:
                        color = (0, 200, 0)
                    elif is_match:
                        color = (0, 165, 255)
                    else:
                        color = (0, 0, 200)

                    cv2.rectangle(display, (x, y), (x + w, y + h), color, 2)

                    name = face.get("name", "?")
                    score = face.get("score", 0)
                    if above:
                        status = "MARKED"
                        status_color = (0, 255, 0)
                    elif is_match:
                        status = "LOW CONF"
                        status_color = (0, 165, 255)
                    else:
                        status = "UNKNOWN"
                        status_color = (0, 0, 200)

                    label_status = f"{status}"
                    label_name = f"{name}"
                    label_score = f"{score:.1%}"
                    city = face.get("city", "")
                    building = face.get("building", "")
                    room = face.get("room", "")
                    bed = face.get("bed", "")

                    (tw_status, th_status), _ = cv2.getTextSize(label_status, cv2.FONT_HERSHEY_SIMPLEX, 0.55, 2)
                    (tw_name, _), _ = cv2.getTextSize(label_name, cv2.FONT_HERSHEY_SIMPLEX, 0.5, 1)

                    cv2.rectangle(display, (x, y - th_status - 4), (x + max(tw_status, tw_name) + 8, y), status_color, -1)
                    cv2.putText(display, label_status, (x + 4, y - 4), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (255, 255, 255), 2)

                    cv2.putText(display, label_name, (x, y + h + 18), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 1)

                    if city or building or room:
                        loc_y = y + h + 36
                        loc_line = ""
                        if city: loc_line += city
                        if building: loc_line += f" / {building}"
                        if room: loc_line += f" / {room}"
                        cv2.putText(display, loc_line, (x, loc_y), cv2.FONT_HERSHEY_SIMPLEX, 0.4, (200, 200, 200), 1)

                    if bed:
                        cv2.putText(display, f"Bed: {bed}", (x, y + h + 52), cv2.FONT_HERSHEY_SIMPLEX, 0.4, (200, 200, 200), 1)

                    percent_x = x + w + 6
                    if percent_x + 80 < display.shape[1]:
                        cv2.putText(display, label_score, (percent_x, y + 18), cv2.FONT_HERSHEY_SIMPLEX, 0.45, color, 1)

                cv2.putText(display, f"FPS: {fps_counter['fps']}", (10, 30),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 255), 2)
                cv2.putText(display, f"Cam: {selected_camera_index} | {fps_counter['resolution']}",
                            (10, 55), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 255), 1)

                ret2, jpeg = cv2.imencode(".jpg", display, [cv2.IMWRITE_JPEG_QUALITY, jpeg_quality])
                if not ret2:
                    time.sleep(0.033)
                    continue
                yield b'--frame\r\nContent-Type: image/jpeg\r\n\r\n' + jpeg.tobytes() + b'\r\n'
                time.sleep(0.033)
            except Exception:
                time.sleep(0.1)

    return Response(generate(), mimetype="multipart/x-mixed-replace; boundary=frame")


_empty_jpeg_cache: bytes | None = None


def _make_empty_jpeg():
    global _empty_jpeg_cache
    if _empty_jpeg_cache is not None:
        return _empty_jpeg_cache
    blank = np.zeros((480, 640, 3), dtype=np.uint8)
    cv2.putText(blank, "No camera feed", (180, 240), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)
    _, buf = cv2.imencode(".jpg", blank, [cv2.IMWRITE_JPEG_QUALITY, 50])
    _empty_jpeg_cache = buf.tobytes()
    return _empty_jpeg_cache


@app.route("/api/events/recent", methods=["GET"])
@require_api_token
def recent_events():
    return jsonify(list(recognition_events)[:100]), 200


@app.route("/api/camera/list", methods=["GET"])
@require_api_token
def camera_list():
    available = []
    is_windows = sys.platform == "win32"
    probe_backend = cv2.CAP_DSHOW if is_windows else cv2.CAP_V4L2
    probe_name = "DSHOW" if is_windows else "V4L2"

    for i in range(8):
        cap, err = _open_camera_backend(i, probe_backend, probe_name, timeout=2.0)
        if cap is not None:
            w = cap.get(cv2.CAP_PROP_FRAME_WIDTH)
            h = cap.get(cv2.CAP_PROP_FRAME_HEIGHT)
            ab = cap.get(cv2.CAP_PROP_BACKEND)
            available.append({
                "index": i,
                "name": f"Camera {i}",
                "backend": int(ab),
                "resolution": f"{int(w)}x{int(h)}",
                "opened": True,
            })
            cap.release()
    return jsonify({"cameras": available, "selected": selected_camera_index}), 200


@app.route("/api/camera/test", methods=["POST"])
@require_api_token
def camera_test():
    data = request.get_json(silent=True) or {}
    index = data.get("index", selected_camera_index)

    if sys.platform == "win32":
        backends = [
            (cv2.CAP_DSHOW, "DSHOW"),
            (cv2.CAP_MSMF, "MSMF"),
            (cv2.CAP_ANY, "DEFAULT"),
        ]
    else:
        backends = [
            (cv2.CAP_V4L2, "V4L2"),
            (cv2.CAP_ANY, "DEFAULT"),
        ]

    results = []
    for backend, name in backends:
        t0 = time.time()
        cap, err = _open_camera_backend(index, backend, name, timeout=CAMERA_INIT_TIMEOUT)
        elapsed_ms = round((time.time() - t0) * 1000)
        if cap is not None:
            w = cap.get(cv2.CAP_PROP_FRAME_WIDTH)
            h = cap.get(cv2.CAP_PROP_FRAME_HEIGHT)
            ab = cap.get(cv2.CAP_PROP_BACKEND)
            results.append({
                "backend": name,
                "status": "SUCCESS",
                "time_ms": elapsed_ms,
                "resolution": f"{int(w)}x{int(h)}",
                "backend_id": int(ab),
            })
            cap.release()
        else:
            results.append({
                "backend": name,
                "status": "FAILED",
                "time_ms": elapsed_ms,
                "error": err,
            })

    return jsonify({"results": results}), 200


@app.route("/api/camera/select", methods=["POST"])
@require_api_token
def camera_select():
    global selected_camera_index
    data = request.get_json(silent=True) or {}
    idx = data.get("index", 0)
    if not isinstance(idx, int) or idx < 0:
        return jsonify({"error": "رقم كاميرا غير صالح"}), 400
    selected_camera_index = idx
    logger.info("Camera selector: changing to index %d (effective on next session start)", idx)
    return jsonify({"message": f"تم تحديد الكاميرا {idx}", "index": idx}), 200


@app.route("/api/camera/resolution", methods=["POST"])
@require_api_token
def camera_set_resolution():
    global selected_resolution, camera_settings_version
    data = request.get_json(silent=True) or {}
    res_str = data.get("resolution", "Auto")
    if res_str.lower() == "auto":
        selected_resolution = None
        logger.info("Camera resolution set to Auto")
    else:
        parts = res_str.lower().split("x")
        if len(parts) == 2:
            try:
                w = int(parts[0])
                h = int(parts[1])
                if (w, h) in Config.RESOLUTION_PRESETS:
                    selected_resolution = (w, h)
                    logger.info("Camera resolution set to %dx%d", w, h)
                else:
                    return jsonify({"error": f"Unsupported resolution: {res_str}"}), 400
            except ValueError:
                return jsonify({"error": f"Invalid resolution: {res_str}"}), 400
        else:
            return jsonify({"error": f"Invalid format: {res_str}"}), 400
    camera_settings_version += 1
    return jsonify({"resolution": res_str, "settingsVersion": camera_settings_version}), 200


@app.route("/api/camera/quality", methods=["POST"])
@require_api_token
def camera_set_quality():
    global jpeg_quality
    data = request.get_json(silent=True) or {}
    q = data.get("quality", Config.JPEG_QUALITY)
    if not isinstance(q, int) or q < 60 or q > 100:
        return jsonify({"error": "Quality must be an integer between 60 and 100"}), 400
    jpeg_quality = q
    logger.info("JPEG quality set to %d", q)
    return jsonify({"quality": jpeg_quality}), 200


@app.route("/api/camera/fps", methods=["POST"])
@require_api_token
def camera_set_fps():
    global target_fps, camera_settings_version
    data = request.get_json(silent=True) or {}
    fps = data.get("fps", Config.TARGET_FPS)
    if fps not in (15, 20, 30):
        return jsonify({"error": "FPS must be 15, 20, or 30"}), 400
    target_fps = fps
    camera_settings_version += 1
    logger.info("Target FPS set to %d", fps)
    return jsonify({"fps": target_fps, "settingsVersion": camera_settings_version}), 200


@app.route("/api/dev/mode", methods=["POST"])
@require_api_token
def dev_mode_toggle():
    global dev_mode
    data = request.get_json(silent=True) or {}
    dev_mode = data.get("enabled", not dev_mode)
    logger.info("Dev mode set to %s", dev_mode)
    return jsonify({"devMode": dev_mode}), 200


def shutdown_handler(sig, frame):
    global attendance_running
    logger.info("Received signal %d, initiating graceful shutdown...", sig)
    push_event("system", "إيقاف تشغيل النظام")
    attendance_running = False
    if recognition_thread and recognition_thread.is_alive():
        recognition_thread.join(timeout=10)
    logger.info("Shutdown complete.")
    sys.exit(0)


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
    avg = np.mean(features, axis=0)
    return avg.reshape(1, -1).astype(np.float32)


def is_blurry(image, threshold=BLUR_THRESHOLD):
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    return cv2.Laplacian(gray, cv2.CV_64F).var() < threshold


@app.route("/api/enrollment/list", methods=["GET"])
@require_api_token
def enrollment_list():
    student_ids = sorted(db.keys())
    return jsonify({"enrolledStudents": student_ids, "total": len(student_ids)}), 200


@app.route("/api/enrollment/register", methods=["POST"])
@require_api_token
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
@require_api_token
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
@require_api_token
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
    if Config.INTERNAL_TOKEN == "your-internal-token-here":
        logger.warning("SECURITY: INTERNAL_TOKEN is set to the default value. Generate a secure random token and set the UNISTAY_INTERNAL_TOKEN environment variable.")

    host = "0.0.0.0" if Config.ALLOW_REMOTE_ACCESS else "127.0.0.1"
    logger.info("Starting Flask Attendance Recognition API on %s:%d", host, Config.FLASK_PORT)
    sync_settings()
    app.run(host=host, port=Config.FLASK_PORT, debug=False, use_reloader=False)
