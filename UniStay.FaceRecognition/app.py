import os
import cv2
import csv
import numpy as np
import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from datetime import datetime, time
from tkcalendar import DateEntry

from PIL import Image, ImageTk, ImageDraw, ImageFont

from model.face_utils import extract_all_features, match_with_db


BASE_DIR = os.path.dirname(__file__)
DB_STUDENTS = os.path.join(BASE_DIR, "database", "students.npy")
DB_ATTENDANCE = os.path.join(BASE_DIR, "database", "attendance.csv")
SETTINGS_FILE = os.path.join(BASE_DIR, "database", "settings.npy")
DB_ROOMS = os.path.join(BASE_DIR, "database", "rooms.npy")


os.makedirs(os.path.join(BASE_DIR, "database"), exist_ok=True)

# Color Scheme
COLORS = {
    'primary': '#2C3E50',      # Dark blue-gray
    'secondary': '#3498DB',    # Blue
    'success': '#27AE60',      # Green
    'warning': '#F39C12',      # Orange
    'danger': '#E74C3C',       # Red
    'light': '#ECF0F1',        # Light gray
    'white': '#FFFFFF',
    'dark': '#1A252F',
    'accent': '#9B59B6'        # Purple
}

def load_db():
    """Load student database with ID and name mapping"""
    if os.path.exists(DB_STUDENTS):
        try:
            return np.load(DB_STUDENTS, allow_pickle=True).item()
        except:
            return {}
    return {}

def save_db(db):
    """Save student database"""
    np.save(DB_STUDENTS, db, allow_pickle=True)

def load_settings():
    """Load application settings"""
    default_settings = {
        'camera_index': 0,
        'camera_resolution': '640x480',
        'recognition_threshold': 0.6,
        'attendance_start_time': '08:00',
        'attendance_end_time': '18:00',
        'enable_sound': True,
        'enable_notifications': True,
        'auto_export': False,
        'fps_limit': 30,
        'face_detection_confidence': 0.5
    }
    
    if os.path.exists(SETTINGS_FILE):
        try:
            settings = np.load(SETTINGS_FILE, allow_pickle=True).item()
            for key in default_settings:
                if key not in settings:
                    settings[key] = default_settings[key]
            return settings
        except:
            return default_settings
    return default_settings

def save_settings(settings):
    """Save application settings"""
    np.save(SETTINGS_FILE, settings, allow_pickle=True)

def get_name_from_key(key):
    """Extract name from student key (format: ID_Name)"""
    if '_' in key:
        parts = key.split('_', 1)
        return parts[1] if len(parts) > 1 else key
    return key

def get_id_from_key(key):
    """Extract ID from student key (format: ID_Name)"""
    if '_' in key:
        parts = key.split('_', 1)
        return parts[0] if len(parts) > 0 else key
    return key

def load_rooms():
    if os.path.exists(DB_ROOMS):
        try:
            return np.load(DB_ROOMS, allow_pickle=True).item()
        except:
            return {}
    return {}

def get_today_attendance():
    """Get set of student IDs who are present today"""
    today = datetime.now().strftime("%Y-%m-%d")
    present_students = set()

    if os.path.exists(DB_ATTENDANCE):
        try:
            with open(DB_ATTENDANCE, 'r', encoding='utf-8') as f:
                reader = csv.reader(f)
                next(reader)  # skip header

                for row in reader:
                    if len(row) >= 5 and row[3] == today:
                        present_students.add(row[0])  # Student ID
        except:
            pass

    return present_students

def get_absent_students_today():
    """
    Get list of absent students today with full details
    Returns: list of dicts with 'id', 'name', 'room', 'key'
    """
    today = datetime.now().strftime("%Y-%m-%d")
    
    # 1. Get all registered student IDs
    all_student_ids = set()
    student_data = {}  # Store student details
    
    for key in db.keys():
        student_id = get_id_from_key(key)
        student_name = get_name_from_key(key)
        room = rooms_db.get(key, "N/A")
        
        all_student_ids.add(student_id)
        student_data[student_id] = {
            'id': student_id,
            'name': student_name,
            'room': room,
            'key': key
        }
    
    # 2. Get present student IDs from attendance file
    present_ids = set()
    
    if os.path.exists(DB_ATTENDANCE):
        try:
            with open(DB_ATTENDANCE, 'r', encoding='utf-8') as f:
                reader = csv.reader(f)
                next(reader)  # skip header
                
                for row in reader:
                    if len(row) >= 5 and row[3] == today:
                        present_ids.add(row[0])  # Student ID
        except:
            pass
    
    # 3. Absent = All - Present
    absent_ids = all_student_ids - present_ids
    
    # 4. Build list with details
    absent_list = []
    for student_id in sorted(absent_ids):
        if student_id in student_data:
            absent_list.append(student_data[student_id])
    
    return absent_list

def save_rooms(rooms):
    np.save(DB_ROOMS, rooms, allow_pickle=True)


db = load_db()
rooms_db = load_rooms()
session_marked = set()
app_settings = load_settings()


class ModernButton(tk.Canvas):
    """Custom modern button with hover effect and animations"""
    def __init__(self, parent, text, command, width=200, height=40, 
                 bg_color=COLORS['secondary'], fg_color=COLORS['white'], **kwargs):
        super().__init__(parent, width=width, height=height, 
                        highlightthickness=0, **kwargs)
        self.command = command
        self.bg_color = bg_color
        self.hover_color = self._adjust_color(bg_color, 1.15)
        self.active_color = self._adjust_color(bg_color, 0.85)
        self.fg_color = fg_color
        self.text = text
        self.width = width
        self.height = height
        self.enabled = True
        self.is_hovered = False
        self.is_pressed = False
        self.animation_id = None
        self.current_scale = 1.0
        self.target_scale = 1.0
        
        self._draw_button()
        self.bind("<Button-1>", self._on_press)
        self.bind("<ButtonRelease-1>", self._on_release)
        self.bind("<Enter>", self._on_enter)
        self.bind("<Leave>", self._on_leave)
    
    def _adjust_color(self, hex_color, factor):
        """Lighten or darken a color"""
        hex_color = hex_color.lstrip('#')
        rgb = tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))
        rgb = tuple(min(255, int(c * factor)) for c in rgb)
        return '#%02x%02x%02x' % rgb
    
    def _draw_button(self, scale=1.0):
        self.delete("all")
        
        if not self.enabled:
            color = '#95A5A6'
        elif self.is_pressed:
            color = self.active_color
        elif self.is_hovered:
            color = self.hover_color
        else:
            color = self.bg_color
        
        pad = (1 - scale) * 5
        
        if self.is_hovered and self.enabled:
            shadow_offset = 2
            self.create_rounded_rect(
                shadow_offset + pad, shadow_offset + pad, 
                self.width - pad, self.height - pad,
                radius=10, fill='#34495E', outline=""
            )
        
        self.create_rounded_rect(
            pad, pad, self.width - pad, self.height - pad, 
            radius=10, fill=color, outline=""
        )
        
        if self.enabled and not self.is_pressed:
            gradient_color = self._adjust_color(color, 1.05)
            self.create_rounded_rect(
                pad, pad, self.width - pad, self.height/2 - pad,
                radius=10, fill=gradient_color, outline="", 
                stipple='gray50'
            )
        
        font_size = int(11 * scale)
        self.create_text(
            self.width/2, self.height/2, 
            text=self.text,
            fill=self.fg_color, 
            font=('Segoe UI', font_size, 'bold')
        )
    
    def create_rounded_rect(self, x1, y1, x2, y2, radius=25, **kwargs):
        points = [x1+radius, y1,
                 x1+radius, y1,
                 x2-radius, y1,
                 x2-radius, y1,
                 x2, y1,
                 x2, y1+radius,
                 x2, y1+radius,
                 x2, y2-radius,
                 x2, y2-radius,
                 x2, y2,
                 x2-radius, y2,
                 x2-radius, y2,
                 x1+radius, y2,
                 x1+radius, y2,
                 x1, y2,
                 x1, y2-radius,
                 x1, y2-radius,
                 x1, y1+radius,
                 x1, y1+radius,
                 x1, y1]
        return self.create_polygon(points, **kwargs, smooth=True)
    
    def _animate_scale(self):
        if abs(self.current_scale - self.target_scale) > 0.01:
            self.current_scale += (self.target_scale - self.current_scale) * 0.3
            self._draw_button(self.current_scale)
            self.animation_id = self.after(16, self._animate_scale)
        else:
            self.current_scale = self.target_scale
            self._draw_button(self.current_scale)
            self.animation_id = None
    
    def _on_press(self, event):
        if self.enabled:
            self.is_pressed = True
            self.target_scale = 0.95
            if self.animation_id:
                self.after_cancel(self.animation_id)
            self._animate_scale()
    
    def _on_release(self, event):
        if self.enabled:
            self.is_pressed = False
            self.target_scale = 1.02 if self.is_hovered else 1.0
            if self.animation_id:
                self.after_cancel(self.animation_id)
            self._animate_scale()
            
            if self.command:
                self.after(50, self.command)
    
    def _on_enter(self, event):
        if self.enabled:
            self.is_hovered = True
            self.target_scale = 1.02
            self.config(cursor="hand2")
            if self.animation_id:
                self.after_cancel(self.animation_id)
            self._animate_scale()
    
    def _on_leave(self, event):
        self.is_hovered = False
        self.is_pressed = False
        self.target_scale = 1.0
        self.config(cursor="")
        if self.animation_id:
            self.after_cancel(self.animation_id)
        self._animate_scale()
    
    def set_enabled(self, enabled):
        self.enabled = enabled
        self.target_scale = 1.0
        self.current_scale = 1.0
        self._draw_button()


class FaceApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.current_attendance_status = ""
        self.title("Face Recognition System - Graduation Project")
        self.geometry("1400x900")
        self.configure(bg=COLORS['light'])
        
        self._setup_styles()
        self._create_header()
        self._create_main_content()
        self.update_stats()
        
    def _setup_styles(self):
        style = ttk.Style(self)
        style.theme_use("clam")
        
        style.configure('TNotebook', background=COLORS['light'], borderwidth=0)
        style.configure('TNotebook.Tab', 
                       background=COLORS['white'],
                       foreground=COLORS['dark'],
                       padding=[20, 10],
                       font=('Segoe UI', 11, 'bold'))
        style.map('TNotebook.Tab',
                 background=[('selected', COLORS['secondary'])],
                 foreground=[('selected', COLORS['white'])],
                 expand=[('selected', [1, 1, 1, 0])])
        
        style.configure('Card.TFrame', background=COLORS['white'], relief='flat')
        style.configure('TFrame', background=COLORS['light'])
        
        style.configure('Title.TLabel', 
                       background=COLORS['white'],
                       foreground=COLORS['dark'],
                       font=('Segoe UI', 12, 'bold'))
        style.configure('TLabel', 
                       background=COLORS['white'],
                       foreground=COLORS['dark'],
                       font=('Segoe UI', 10))
        
        style.configure('TEntry', fieldbackground=COLORS['white'], 
                       foreground=COLORS['dark'],
                       borderwidth=1, relief='solid')
    
    def _create_header(self):
        header = tk.Frame(self, bg=COLORS['primary'], height=75)
        header.pack(fill='x', side='top')
        header.pack_propagate(False)
        
        logo_frame = tk.Frame(header, bg=COLORS['primary'])
        logo_frame.pack(side='left', padx=15, pady=5)
        
        logo_canvas = tk.Canvas(logo_frame, width=45, height=45, 
                               bg=COLORS['primary'], highlightthickness=0)
        logo_canvas.pack()
        
        logo_canvas.create_oval(7, 7, 38, 38, fill=COLORS['secondary'], outline=COLORS['white'], width=2)
        logo_canvas.create_oval(15, 16, 19, 20, fill=COLORS['white'])
        logo_canvas.create_oval(26, 16, 30, 20, fill=COLORS['white'])
        logo_canvas.create_arc(15, 20, 30, 34, start=0, extent=-180, fill=COLORS['white'], outline="")
        
        title_frame = tk.Frame(header, bg=COLORS['primary'])
        title_frame.pack(side='left', fill='y', pady=5)
        
        title = tk.Label(title_frame, text="Face Recognition System",
                        font=('Segoe UI', 17, 'bold'),
                        bg=COLORS['primary'], fg=COLORS['white'])
        title.pack(anchor='w')
        
        subtitle = tk.Label(title_frame, text="Graduation Project - Attendance Management System",
                           font=('Segoe UI', 9),
                           bg=COLORS['primary'], fg='#95A5A6')
        subtitle.pack(anchor='w', pady=(2, 0))
        
        stats_frame = tk.Frame(header, bg=COLORS['primary'])
        stats_frame.pack(side='right', padx=15, pady=5)
        
        self.stats_registered = tk.Label(stats_frame, 
                                        text=f"Registered: {len(db)}",
                                        font=('Segoe UI', 10, 'bold'),
                                        bg=COLORS['primary'], fg=COLORS['white'])
        self.stats_registered.pack(side='left', padx=8)
        
        self.stats_today = tk.Label(stats_frame,
                                   text=f"Present: {len(session_marked)}",
                                   font=('Segoe UI', 10, 'bold'),
                                   bg=COLORS['primary'], fg=COLORS['success'])
        self.stats_today.pack(side='left', padx=8)
        
        self.stats_absent = tk.Label(stats_frame,
                                     text=f"Absent: {max(0, len(db) - len(session_marked))}",
                                     font=('Segoe UI', 10, 'bold'),
                                     bg=COLORS['primary'], fg=COLORS['danger'])
        self.stats_absent.pack(side='left', padx=8)
    
    def update_stats(self):
        total_students = len(db)

        today = datetime.now().strftime("%Y-%m-%d")
        present_students = set()

        if os.path.exists(DB_ATTENDANCE):
            try:
                with open(DB_ATTENDANCE, 'r', encoding='utf-8') as f:
                    reader = csv.reader(f)
                    next(reader)
                    for row in reader:
                        if len(row) >= 5 and row[3] == today:
                            present_students.add(row[0])  # Student ID
            except:
                pass

        present_count = len(present_students)
        absent_count = max(0, total_students - present_count)

        self.stats_registered.config(text=f"Registered: {total_students}")
        self.stats_today.config(text=f"Present: {present_count}")
        self.stats_absent.config(text=f"Absent: {absent_count}")

    
    def _create_main_content(self):
        container = tk.Frame(self, bg=COLORS['light'])
        container.pack(fill='both', expand=True, padx=10, pady=10)
        
        canvas = tk.Canvas(container, bg=COLORS['light'], highlightthickness=0)
        scrollbar = tk.Scrollbar(container, orient="vertical", command=canvas.yview)
        scrollable_frame = tk.Frame(canvas, bg=COLORS['light'])
        
        scrollable_frame.bind(
            "<Configure>",
            lambda e: canvas.configure(scrollregion=canvas.bbox("all"))
        )
        
        canvas.create_window((0, 0), window=scrollable_frame, anchor="nw")
        canvas.configure(yscrollcommand=scrollbar.set)
        
        canvas.pack(side="left", fill="both", expand=True)
        scrollbar.pack(side="right", fill="y")
        
        def _on_mousewheel(event):
            canvas.yview_scroll(int(-1*(event.delta/120)), "units")
        
        def _bind_mousewheel(event):
            canvas.bind_all("<MouseWheel>", _on_mousewheel)
        
        def _unbind_mousewheel(event):
            canvas.unbind_all("<MouseWheel>")
        
        canvas.bind('<Enter>', _bind_mousewheel)
        canvas.bind('<Leave>', _unbind_mousewheel)
        
        notebook = ttk.Notebook(scrollable_frame)
        notebook.pack(fill="both", expand=True)

        self.tab_dashboard = ttk.Frame(notebook, style='TFrame')
        self.tab_register = ttk.Frame(notebook, style='TFrame')
        self.tab_realtime = ttk.Frame(notebook, style='TFrame')
        self.tab_students = ttk.Frame(notebook, style='TFrame')
        self.tab_absent = ttk.Frame(notebook, style='TFrame')  # NEW TAB
        self.tab_database = ttk.Frame(notebook, style='TFrame')
        self.tab_settings = ttk.Frame(notebook, style='TFrame')

        notebook.add(self.tab_dashboard, text="📊 Dashboard")
        notebook.add(self.tab_register, text="📝 Register Student")
        notebook.add(self.tab_realtime, text="📹 Real-Time Recognition")
        notebook.add(self.tab_students, text="👥 Student Management")
        notebook.add(self.tab_absent, text="❌ Absent Students")  # NEW TAB
        notebook.add(self.tab_database, text="📋 Attendance Records")
        notebook.add(self.tab_settings, text="⚙️ Settings")

        notebook.bind("<<NotebookTabChanged>>", self.on_tab_changed)
        self.notebook = notebook

        self._build_dashboard_tab()
        self._build_register_tab()
        self._build_realtime_tab()
        self._build_students_tab()
        self._build_absent_tab()  # NEW TAB BUILD
        self._build_database_tab()
        self._build_settings_tab()

    def on_tab_changed(self, event):
        current_tab = self.notebook.index(self.notebook.select())
        
        if current_tab != 1:
            if self.reg_cam_running:
                self.reg_cam_running = False
                if self.reg_cam:
                    self.reg_cam.release()
                self.reg_cam_preview.config(image="", text="Camera stopped")
                self.btn_capture.set_enabled(False)
            
            if self.reg_captured_image is not None:
                self.clear_captured_image()
        
        if current_tab != 2 and self.real_cam_running:
            self.stop_realtime()
        
        if current_tab == 0:
            self.refresh_dashboard()
        
        if current_tab == 3:
            self.refresh_students_view()
        
        if current_tab == 4:  # Absent Tab
            self.refresh_absent_view()
        
        if current_tab == 5:  # Database Tab (was 4, now 5)
            self.refresh_database_view()
    
    def clear_captured_image(self):
        self.reg_captured_image = None
        self.reg_captured_preview.config(image="", text="No image captured", bg=COLORS['light'])
        self.btn_register.set_enabled(False)
        self._update_reg_status("Ready to register new student", COLORS['dark'])

    def _create_card(self, parent, title="", compact=False):
        h_padding = 15 if compact else 120
        v_padding = 2 if compact else 10
        
        card = tk.Frame(parent, bg=COLORS['white'], relief='flat', borderwidth=0)
        card.pack(fill='both', expand=True, padx=h_padding, pady=v_padding)
        
        shadow3 = tk.Frame(parent, bg='#D5DBDB')
        shadow3.place(in_=card, x=4, y=4, relwidth=1, relheight=1)
        
        shadow2 = tk.Frame(parent, bg='#CCD1D1')
        shadow2.place(in_=card, x=3, y=3, relwidth=1, relheight=1)
        
        shadow1 = tk.Frame(parent, bg='#BDC3C7')
        shadow1.place(in_=card, x=2, y=2, relwidth=1, relheight=1)
        
        card.lift()
        
        if title:
            title_height = 35 if compact else 45
            title_bar = tk.Frame(card, bg=COLORS['secondary'], height=title_height)
            title_bar.pack(fill='x')
            title_bar.pack_propagate(False)
            
            gradient_height = 15 if compact else 19
            gradient = tk.Frame(title_bar, bg=self._adjust_color(COLORS['secondary'], 1.1), height=gradient_height)
            gradient.pack(fill='x')
            
            title_label = tk.Label(title_bar, text=title, 
                    font=('Segoe UI', 11 if compact else 12, 'bold'),
                    bg=COLORS['secondary'], fg=COLORS['white'])
            title_label.place(relx=0, rely=0.5, anchor='w', x=12)
        
        content_padding = 12 if compact else 120
        content_v_padding = 4 if compact else 10
        content = tk.Frame(card, bg=COLORS['white'])
        content.pack(fill='both', expand=True, padx=content_padding, pady=content_v_padding)
        
        card._alpha = 0
        self._fade_in_card(card)
        
        return content
    
    def _adjust_color(self, hex_color, factor):
        hex_color = hex_color.lstrip('#')
        rgb = tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))
        rgb = tuple(min(255, int(c * factor)) for c in rgb)
        return '#%02x%02x%02x' % rgb
    
    def _fade_in_card(self, card):
        if not hasattr(card, '_alpha'):
            card._alpha = 0
        
        if card._alpha < 1.0:
            card._alpha += 0.1
            self.after(20, lambda: self._fade_in_card(card))

    def _build_dashboard_tab(self):
        frame = self.tab_dashboard
        
        # Top row - Statistics cards with 4 cards
        stats_row = tk.Frame(frame, bg=COLORS['light'])
        stats_row.pack(fill='x', padx=5, pady=5)
        
        self._create_stat_card(stats_row, "👥 Total Students", "0", COLORS['secondary'], 'left')
        self._create_stat_card(stats_row, "✓ Present Today", "0", COLORS['success'], 'left')
        self._create_stat_card(stats_row, "✗ Absent Today", "0", COLORS['danger'], 'left')
        self._create_stat_card(stats_row, "📋 Total Records", "0", COLORS['accent'], 'left')
        
        # Quick Actions Card
        actions_card = self._create_card(frame, "⚡ Quick Actions", compact=True)
        actions_card.master.pack(fill='x', padx=5, pady=5)
        
        actions_frame = tk.Frame(actions_card, bg=COLORS['white'])
        actions_frame.pack(fill='x', pady=10)
        
        ModernButton(actions_frame, "📝 Register New Student", 
                    lambda: self.notebook.select(1), width=200, 
                    bg_color=COLORS['secondary']).pack(side='left', padx=10)
        
        ModernButton(actions_frame, "📹 Start Recognition", 
                    lambda: self.notebook.select(2), width=200,
                    bg_color=COLORS['success']).pack(side='left', padx=10)
        
        ModernButton(actions_frame, "👥 Manage Students", 
                    lambda: self.notebook.select(3), width=200,
                    bg_color=COLORS['accent']).pack(side='left', padx=10)
        
        ModernButton(actions_frame, "⚙️ Settings", 
                    lambda: self.notebook.select(6), width=200,
                    bg_color=COLORS['warning']).pack(side='left', padx=10)
        
        # Attendance Statistics Card
        stats_card = self._create_card(frame, "📈 Attendance Statistics", compact=True)
        stats_card.master.pack(fill='x', padx=5, pady=5)
        
        stats_grid = tk.Frame(stats_card, bg=COLORS['white'])
        stats_grid.pack(fill='x', pady=10)
        
        # Attendance rate
        rate_frame = tk.Frame(stats_grid, bg=COLORS['white'])
        rate_frame.pack(side='left', padx=20)
        
        tk.Label(rate_frame, text="Attendance Rate:",
                font=('Segoe UI', 11, 'bold'),
                bg=COLORS['white']).pack(anchor='w')
        
        self.dash_attendance_rate = tk.Label(rate_frame, text="0%",
                                            font=('Segoe UI', 24, 'bold'),
                                            bg=COLORS['white'], fg=COLORS['success'])
        self.dash_attendance_rate.pack(anchor='w')
        
        # Progress bar
        progress_frame = tk.Frame(stats_grid, bg=COLORS['white'])
        progress_frame.pack(side='left', fill='x', expand=True, padx=20)
        
        tk.Label(progress_frame, text="Today's Progress:",
                font=('Segoe UI', 11, 'bold'),
                bg=COLORS['white']).pack(anchor='w')
        
        self.progress_canvas = tk.Canvas(progress_frame, height=30, bg='#ECF0F1', 
                                        highlightthickness=0)
        self.progress_canvas.pack(fill='x', pady=5)
        
        # Recent Activity Card
        recent_card = self._create_card(frame, "📊 Recent Attendance Activity")
        
        tree_frame = tk.Frame(recent_card, bg=COLORS['white'])
        tree_frame.pack(fill='both', expand=True, padx=5, pady=5)
        
        vsb = ttk.Scrollbar(tree_frame, orient="vertical")
        
        self.dashboard_tree = ttk.Treeview(tree_frame, 
                                          columns=("ID", "Name","Room", "Date", "Time"),
                                          show='headings',
                                          yscrollcommand=vsb.set,
                                          height=10)
        
        vsb.config(command=self.dashboard_tree.yview)
        
        self.dashboard_tree.heading("ID", text="Student ID")
        self.dashboard_tree.heading("Name", text="Student Name")
        self.dashboard_tree.heading("Room", text="Room")
        self.dashboard_tree.heading("Date", text="Date")
        self.dashboard_tree.heading("Time", text="Time")
        
        self.dashboard_tree.column("ID", width=150, anchor='center')
        self.dashboard_tree.column("Name", width=250, anchor='w')
        self.dashboard_tree.column("Date", width=150, anchor='center')
        self.dashboard_tree.column("Time", width=150, anchor='center')
        
        vsb.pack(side='right', fill='y')
        self.dashboard_tree.pack(side='left', fill='both', expand=True)
        
        self.refresh_dashboard()
    
    def _create_stat_card(self, parent, title, value, color, side):
        card_frame = tk.Frame(parent, bg=COLORS['white'], relief='flat', borderwidth=0)
        card_frame.pack(side=side, fill='both', expand=True, padx=5, pady=5)
        
        shadow = tk.Frame(parent, bg='#BDC3C7')
        shadow.place(in_=card_frame, x=2, y=2, relwidth=1, relheight=1)
        card_frame.lift()
        
        color_bar = tk.Frame(card_frame, bg=color, height=5)
        color_bar.pack(fill='x')
        
        content = tk.Frame(card_frame, bg=COLORS['white'])
        content.pack(fill='both', expand=True, padx=15, pady=15)
        
        title_label = tk.Label(content, text=title, 
                              font=('Segoe UI', 11),
                              bg=COLORS['white'], fg=COLORS['dark'])
        title_label.pack(anchor='w')
        
        value_label = tk.Label(content, text=value, 
                              font=('Segoe UI', 28, 'bold'),
                              bg=COLORS['white'], fg=color)
        value_label.pack(anchor='w', pady=(5, 0))
        
        if "Students" in title:
            self.dash_total_students = value_label
        elif "Present" in title:
            self.dash_today_attendance = value_label
        elif "Absent" in title:
            self.dash_absent_students = value_label
        elif "Records" in title:
            self.dash_total_records = value_label
    
    def refresh_dashboard(self):
        total_students = len(db)
        today_present = get_today_attendance()
        today_attendance = len(today_present)
        absent_count = max(0, total_students - today_attendance)

        
        total_records = 0
        if os.path.exists(DB_ATTENDANCE):
            try:
                with open(DB_ATTENDANCE, 'r', encoding='utf-8') as f:
                    total_records = sum(1 for line in f) - 1
            except:
                total_records = 0
        
        self.dash_total_students.config(text=str(total_students))
        self.dash_today_attendance.config(text=str(today_attendance))
        self.dash_absent_students.config(text=str(absent_count))
        self.dash_total_records.config(text=str(total_records))
        
        # Calculate attendance rate
        if total_students > 0:
            rate = (today_attendance / total_students) * 100 if total_students > 0 else 0
            self.dash_attendance_rate.config(text=f"{rate:.1f}%")
            
            # Update color based on rate
            if rate >= 80:
                self.dash_attendance_rate.config(fg=COLORS['success'])
            elif rate >= 60:
                self.dash_attendance_rate.config(fg=COLORS['warning'])
            else:
                self.dash_attendance_rate.config(fg=COLORS['danger'])
            
            # Update progress bar
            self._update_progress_bar(rate)
        else:
            self.dash_attendance_rate.config(text="N/A")
            self._update_progress_bar(0)
        
        # Update recent activity
        for item in self.dashboard_tree.get_children():
            self.dashboard_tree.delete(item)
        
        if os.path.exists(DB_ATTENDANCE):
            try:
                with open(DB_ATTENDANCE, 'r', encoding='utf-8') as f:
                    reader = csv.reader(f)
                    next(reader)
                    records = list(reader)
                    
                    for record in reversed(records[-10:]):
                        if len(record) == 5:
                            self.dashboard_tree.insert('', 'end', values=record)
            except:
                pass
    
    def _update_progress_bar(self, percentage):
        """Update the progress bar visualization"""
        self.progress_canvas.delete("all")
        
        canvas_width = self.progress_canvas.winfo_width()
        if canvas_width <= 1:
            canvas_width = 500
        
        canvas_height = 30
        
        # Background
        self.progress_canvas.create_rectangle(0, 0, canvas_width, canvas_height, 
                                             fill='#ECF0F1', outline='')
        
        # Progress fill
        fill_width = (percentage / 100) * canvas_width
        
        if percentage >= 80:
            color = COLORS['success']
        elif percentage >= 60:
            color = COLORS['warning']
        else:
            color = COLORS['danger']
        
        if fill_width > 0:
            self.progress_canvas.create_rectangle(0, 0, fill_width, canvas_height, 
                                                 fill=color, outline='')
        
        # Text
        text = f"{int(percentage)}% Complete"
        self.progress_canvas.create_text(canvas_width/2, canvas_height/2, 
                                        text=text, fill=COLORS['dark'],
                                        font=('Segoe UI', 10, 'bold'))

    def _build_settings_tab(self):
        """Build comprehensive settings tab"""
        frame = self.tab_settings
        
        # Camera Settings Card
        camera_card = self._create_card(frame, "📹 Camera Settings", compact=True)
        camera_card.master.pack(fill='x', padx=5, pady=5)
        
        # Camera Index
        cam_index_frame = tk.Frame(camera_card, bg=COLORS['white'])
        cam_index_frame.pack(fill='x', pady=8)
        
        tk.Label(cam_index_frame, text="Camera Device:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=10)
        
        self.camera_index_var = tk.StringVar(value=str(app_settings['camera_index']))
        camera_combo = ttk.Combobox(cam_index_frame, textvariable=self.camera_index_var,
                                   values=['0', '1', '2', '3'], width=10, state='readonly')
        camera_combo.pack(side='left', padx=5)
        
        tk.Label(cam_index_frame, text="(0 = Default camera)", 
                font=('Segoe UI', 9, 'italic'),
                bg=COLORS['white'], fg='#7F8C8D').pack(side='left', padx=5)
        
        # Resolution
        res_frame = tk.Frame(camera_card, bg=COLORS['white'])
        res_frame.pack(fill='x', pady=8)
        
        tk.Label(res_frame, text="Resolution:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=10)
        
        self.resolution_var = tk.StringVar(value=app_settings['camera_resolution'])
        res_combo = ttk.Combobox(res_frame, textvariable=self.resolution_var,
                                values=['320x240', '640x480', '800x600', '1280x720', '1920x1080'],
                                width=15, state='readonly')
        res_combo.pack(side='left', padx=5)
        
        # FPS Limit
        fps_frame = tk.Frame(camera_card, bg=COLORS['white'])
        fps_frame.pack(fill='x', pady=8)
        
        tk.Label(fps_frame, text="FPS Limit:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=10)
        
        self.fps_var = tk.StringVar(value=str(app_settings['fps_limit']))
        fps_spin = tk.Spinbox(fps_frame, from_=10, to=60, textvariable=self.fps_var,
                             width=10, font=('Segoe UI', 10))
        fps_spin.pack(side='left', padx=5)
        
        tk.Label(fps_frame, text="frames per second", 
                font=('Segoe UI', 9, 'italic'),
                bg=COLORS['white'], fg='#7F8C8D').pack(side='left', padx=5)
        
        # Recognition Settings Card
        recog_card = self._create_card(frame, "🔍 Recognition Settings", compact=True)
        recog_card.master.pack(fill='x', padx=5, pady=5)
        
        # Recognition Threshold
        thresh_frame = tk.Frame(recog_card, bg=COLORS['white'])
        thresh_frame.pack(fill='x', pady=8)
        
        tk.Label(thresh_frame, text="Recognition Threshold:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=10)
        
        self.threshold_var = tk.DoubleVar(value=app_settings['recognition_threshold'])
        
        thresh_scale = tk.Scale(thresh_frame, from_=0.3, to=0.9, resolution=0.05,
                               orient='horizontal', variable=self.threshold_var,
                               length=300, bg=COLORS['white'])
        thresh_scale.pack(side='left', padx=5)
        
        self.threshold_label = tk.Label(thresh_frame, text=f"{app_settings['recognition_threshold']:.2f}",
                                       font=('Segoe UI', 10, 'bold'),
                                       bg=COLORS['white'], fg=COLORS['secondary'])
        self.threshold_label.pack(side='left', padx=10)
        
        thresh_scale.config(command=lambda v: self.threshold_label.config(text=f"{float(v):.2f}"))
        
        tk.Label(recog_card, text="Lower = More sensitive (may increase false positives)\nHigher = Less sensitive (may miss faces)",
                font=('Segoe UI', 9, 'italic'),
                bg=COLORS['white'], fg='#7F8C8D',
                justify='left').pack(anchor='w', padx=10, pady=5)
        
        # Face Detection Confidence
        conf_frame = tk.Frame(recog_card, bg=COLORS['white'])
        conf_frame.pack(fill='x', pady=8)
        
        tk.Label(conf_frame, text="Face Detection Confidence:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=10)
        
        self.confidence_var = tk.DoubleVar(value=app_settings['face_detection_confidence'])
        
        conf_scale = tk.Scale(conf_frame, from_=0.3, to=0.9, resolution=0.05,
                             orient='horizontal', variable=self.confidence_var,
                             length=300, bg=COLORS['white'])
        conf_scale.pack(side='left', padx=5)
        
        self.confidence_label = tk.Label(conf_frame, text=f"{app_settings['face_detection_confidence']:.2f}",
                                        font=('Segoe UI', 10, 'bold'),
                                        bg=COLORS['white'], fg=COLORS['secondary'])
        self.confidence_label.pack(side='left', padx=10)
        
        conf_scale.config(command=lambda v: self.confidence_label.config(text=f"{float(v):.2f}"))
        
        # Time Settings Card
        time_card = self._create_card(frame, "⏰ Attendance Time Settings", compact=True)
        time_card.master.pack(fill='x', padx=5, pady=5)
        
        # Start Time
        start_frame = tk.Frame(time_card, bg=COLORS['white'])
        start_frame.pack(fill='x', pady=8)
        
        tk.Label(start_frame, text="Attendance Start Time:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=10)
        
        self.start_time_var = tk.StringVar(value=app_settings['attendance_start_time'])
        start_entry = tk.Entry(start_frame, textvariable=self.start_time_var,
                              font=('Segoe UI', 10), width=10)
        start_entry.pack(side='left', padx=5)
        
        tk.Label(start_frame, text="(Format: HH:MM, e.g., 08:00)", 
                font=('Segoe UI', 9, 'italic'),
                bg=COLORS['white'], fg='#7F8C8D').pack(side='left', padx=5)
        
        # End Time
        end_frame = tk.Frame(time_card, bg=COLORS['white'])
        end_frame.pack(fill='x', pady=8)
        
        tk.Label(end_frame, text="Attendance End Time:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=10)
        
        self.end_time_var = tk.StringVar(value=app_settings['attendance_end_time'])
        end_entry = tk.Entry(end_frame, textvariable=self.end_time_var,
                            font=('Segoe UI', 10), width=10)
        end_entry.pack(side='left', padx=5)
        
        tk.Label(end_frame, text="(Format: HH:MM, e.g., 18:00)", 
                font=('Segoe UI', 9, 'italic'),
                bg=COLORS['white'], fg='#7F8C8D').pack(side='left', padx=5)
        
        # Notification Settings Card
        notif_card = self._create_card(frame, "🔔 Notifications & Alerts", compact=True)
        notif_card.master.pack(fill='x', padx=5, pady=5)
        
        # Enable Sound
        self.sound_var = tk.BooleanVar(value=app_settings['enable_sound'])
        sound_check = tk.Checkbutton(notif_card, text="Enable Sound Alerts",
                                    variable=self.sound_var,
                                    font=('Segoe UI', 10, 'bold'),
                                    bg=COLORS['white'], activebackground=COLORS['white'])
        sound_check.pack(anchor='w', padx=10, pady=5)
        
        # Enable Notifications
        self.notif_var = tk.BooleanVar(value=app_settings['enable_notifications'])
        notif_check = tk.Checkbutton(notif_card, text="Enable Desktop Notifications",
                                    variable=self.notif_var,
                                    font=('Segoe UI', 10, 'bold'),
                                    bg=COLORS['white'], activebackground=COLORS['white'])
        notif_check.pack(anchor='w', padx=10, pady=5)
        
        # Auto Export
        self.auto_export_var = tk.BooleanVar(value=app_settings['auto_export'])
        export_check = tk.Checkbutton(notif_card, text="Auto-Export Attendance Daily",
                                     variable=self.auto_export_var,
                                     font=('Segoe UI', 10, 'bold'),
                                     bg=COLORS['white'], activebackground=COLORS['white'])
        export_check.pack(anchor='w', padx=10, pady=5)
        
        # Save Button
        save_btn_frame = tk.Frame(frame, bg=COLORS['light'])
        save_btn_frame.pack(fill='x', padx=5, pady=10)
        
        ModernButton(save_btn_frame, "💾 Save Settings", 
                    self.save_settings,
                    width=250, height=45,
                    bg_color=COLORS['success']).pack(pady=10)
        
        # Reset Button
        ModernButton(save_btn_frame, "🔄 Reset to Defaults", 
                    self.reset_settings,
                    width=250, height=40,
                    bg_color=COLORS['warning']).pack(pady=5)
    
    def save_settings(self):
        """Save all settings"""
        global app_settings
        
        try:
            # Validate time format
            start_time = self.start_time_var.get().strip()
            end_time = self.end_time_var.get().strip()
            
            # Simple time format validation
            if not (len(start_time) == 5 and start_time[2] == ':'):
                raise ValueError("Invalid start time format")
            if not (len(end_time) == 5 and end_time[2] == ':'):
                raise ValueError("Invalid end time format")
            
            # Update settings
            app_settings['camera_index'] = int(self.camera_index_var.get())
            app_settings['camera_resolution'] = self.resolution_var.get()
            app_settings['recognition_threshold'] = self.threshold_var.get()
            app_settings['attendance_start_time'] = start_time
            app_settings['attendance_end_time'] = end_time
            app_settings['enable_sound'] = self.sound_var.get()
            app_settings['enable_notifications'] = self.notif_var.get()
            app_settings['auto_export'] = self.auto_export_var.get()
            app_settings['fps_limit'] = int(self.fps_var.get())
            app_settings['face_detection_confidence'] = self.confidence_var.get()
            
            save_settings(app_settings)
            
            messagebox.showinfo("Success", 
                              "Settings saved successfully!\n\n"
                              "Note: Some settings (like camera changes) will take effect "
                              "after restarting the camera.")
        
        except ValueError as e:
            messagebox.showerror("Invalid Input", 
                               f"Please check your settings:\n{str(e)}")
    
    def reset_settings(self):
        """Reset all settings to defaults"""
        confirm = messagebox.askyesno("Confirm Reset",
                                     "Are you sure you want to reset all settings to defaults?")
        
        if confirm:
            global app_settings
            
            # Reset to defaults
            app_settings = {
                'camera_index': 0,
                'camera_resolution': '640x480',
                'recognition_threshold': 0.6,
                'attendance_start_time': '08:00',
                'attendance_end_time': '18:00',
                'enable_sound': True,
                'enable_notifications': True,
                'auto_export': False,
                'fps_limit': 30,
                'face_detection_confidence': 0.5
            }
            
            save_settings(app_settings)
            
            # Update UI
            self.camera_index_var.set('0')
            self.resolution_var.set('640x480')
            self.threshold_var.set(0.6)
            self.start_time_var.set('08:00')
            self.end_time_var.set('18:00')
            self.sound_var.set(True)
            self.notif_var.set(True)
            self.auto_export_var.set(False)
            self.fps_var.set('30')
            self.confidence_var.set(0.5)
            
            messagebox.showinfo("Success", "Settings have been reset to defaults.")

    def _build_students_tab(self):
        frame = self.tab_students
        
        top_card = self._create_card(frame, "🔍 Search & Manage Students", compact=True)
        top_card.master.pack(fill='x', padx=5, pady=5)
        
        search_frame = tk.Frame(top_card, bg=COLORS['white'])
        search_frame.pack(fill='x', pady=5)
        
        tk.Label(search_frame, text="Search:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=5)
        
        self.student_search_var = tk.StringVar()
        self.student_search_var.trace('w', lambda *args: self.search_students())
        
        search_entry = tk.Entry(search_frame, textvariable=self.student_search_var,
                               font=('Segoe UI', 10), width=40,
                               relief='solid', borderwidth=1)
        search_entry.pack(side='left', padx=5, ipady=5)
        
        tk.Label(search_frame, text="(Search by ID or Name)", 
                font=('Segoe UI', 9, 'italic'),
                bg=COLORS['white'], fg='#7F8C8D').pack(side='left', padx=5)
        
        btn_frame = tk.Frame(top_card, bg=COLORS['white'])
        btn_frame.pack(fill='x', pady=5)
        
        self.btn_delete_student = ModernButton(btn_frame, "🗑️ Delete Selected", 
                                               self.delete_selected_student,
                                               width=180,
                                               bg_color=COLORS['danger'])
        self.btn_delete_student.pack(side='left', padx=5)
        
        self.btn_refresh_students = ModernButton(btn_frame, "🔄 Refresh", 
                                                 self.refresh_students_view,
                                                 width=150,
                                                 bg_color=COLORS['secondary'])
        self.btn_refresh_students.pack(side='left', padx=5)
        
        self.student_count_label = tk.Label(btn_frame, text="",
                                           font=('Segoe UI', 10, 'bold'),
                                           bg=COLORS['white'], fg=COLORS['dark'])
        self.student_count_label.pack(side='right', padx=20)
        
        table_card = self._create_card(frame, "👥 Registered Students")
        
        tree_frame = tk.Frame(table_card, bg=COLORS['white'])
        tree_frame.pack(fill='both', expand=True, padx=5, pady=5)
        
        vsb = ttk.Scrollbar(tree_frame, orient="vertical")
        hsb = ttk.Scrollbar(tree_frame, orient="horizontal")
        
        self.students_tree = ttk.Treeview(tree_frame, 
                                         columns=("ID", "Name","Room","Key"),
                                         show='headings',
                                         yscrollcommand=vsb.set,
                                         xscrollcommand=hsb.set,
                                         height=20)
        
        vsb.config(command=self.students_tree.yview)
        hsb.config(command=self.students_tree.xview)
        
        self.students_tree.heading("ID", text="Student ID")
        self.students_tree.heading("Name", text="Student Name")
        self.students_tree.heading("Room", text="Room")
        self.students_tree.heading("Key", text="Database Key")
        
        self.students_tree.column("ID", width=200, anchor='center')
        self.students_tree.column("Name", width=300, anchor='w')
        self.students_tree.column("Room", width=120, anchor='center')
        self.students_tree.column("Key", width=0, stretch=False)
        
        vsb.pack(side='right', fill='y')
        hsb.pack(side='bottom', fill='x')
        self.students_tree.pack(side='left', fill='both', expand=True)
        
        self.refresh_students_view()
    
    def refresh_students_view(self):
        global db
        
        for item in self.students_tree.get_children():
            self.students_tree.delete(item)
        
        for key in sorted(db.keys()):
            student_id = get_id_from_key(key)
            student_name = get_name_from_key(key)
            room = rooms_db.get(key, "N/A")
            self.students_tree.insert('', 'end', values=(student_id, student_name, room, key))
        
        self.student_count_label.config(text=f"Total Students: {len(db)}")
        self.student_search_var.set("")
    
    def search_students(self):
        global db
        search_term = self.student_search_var.get().strip().lower()
        
        for item in self.students_tree.get_children():
            self.students_tree.delete(item)
        
        count = 0
        for key in sorted(db.keys()):
            student_id = get_id_from_key(key)
            student_name = get_name_from_key(key)
            room = rooms_db.get(key, "N/A")
            
            if (search_term in student_id.lower() or 
                search_term in student_name.lower()):
                self.students_tree.insert('', 'end', values=(student_id, student_name,room, key))
                count += 1
        
        if search_term:
            self.student_count_label.config(text=f"Found: {count} of {len(db)} students")
        else:
            self.student_count_label.config(text=f"Total Students: {len(db)}")
    
    def delete_selected_student(self):
        global db
        
        selected = self.students_tree.selection()
        if not selected:
            messagebox.showwarning("No Selection", "Please select a student to delete.")
            return
        
        item = self.students_tree.item(selected[0])
        values = item['values']
        student_id   = values[0]
        student_name = values[1]
        room         = values[2]
        student_key  = values[3]
        
        confirm = messagebox.askyesno(
            "Confirm Deletion",
            f"Are you sure you want to delete this student?\n\n"
            f"ID: {student_id}\n"
            f"Name: {student_name}\n\n"
            f"This action cannot be undone!"
        )
        if student_key in rooms_db:
            del rooms_db[student_key]
            save_rooms(rooms_db)

        
        if confirm:
            try:
                if student_key in db:
                    del db[student_key]
                    save_db(db)
                    
                    if student_key in session_marked:
                        session_marked.remove(student_key)
                    
                    self.refresh_students_view()
                    self.update_stats()
                    
                    messagebox.showinfo("Success", 
                                       f"Student {student_name} (ID: {student_id}) has been deleted.")
                else:
                    messagebox.showerror("Error", "Student not found in database.")
            except Exception as e:
                messagebox.showerror("Error", f"Failed to delete student: {str(e)}")

    # ======================== NEW TAB: ABSENT STUDENTS ========================
    def _build_absent_tab(self):
        """Build absent students tab"""
        frame = self.tab_absent
        
        # Top Control Card
        top_card = self._create_card(frame, "📅 Absent Students Management", compact=True)
        top_card.master.pack(fill='x', padx=5, pady=5)
        
        # Info Frame
        info_frame = tk.Frame(top_card, bg=COLORS['white'])
        info_frame.pack(fill='x', pady=5)
        
        # Date Display
        today = datetime.now().strftime("%A, %B %d, %Y")
        tk.Label(info_frame, text=f"📆 Date: {today}",
                font=('Segoe UI', 11, 'bold'),
                bg=COLORS['white'], fg=COLORS['dark']).pack(side='left', padx=10)
        
        # Absent Count
        self.absent_count_label = tk.Label(info_frame, text="",
                                          font=('Segoe UI', 11, 'bold'),
                                          bg=COLORS['white'], fg=COLORS['danger'])
        self.absent_count_label.pack(side='left', padx=20)
        
        # Buttons Frame
        btn_frame = tk.Frame(top_card, bg=COLORS['white'])
        btn_frame.pack(fill='x', pady=5)
        
        self.btn_refresh_absent = ModernButton(btn_frame, "🔄 Refresh", 
                                              self.refresh_absent_view,
                                              width=150,
                                              bg_color=COLORS['secondary'])
        self.btn_refresh_absent.pack(side='left', padx=5)
        
        self.btn_export_absent = ModernButton(btn_frame, "📥 Export Absent List", 
                                             self.export_absent_list,
                                             width=180,
                                             bg_color=COLORS['warning'])
        self.btn_export_absent.pack(side='left', padx=5)
        
        self.btn_notify_absent = ModernButton(btn_frame, "✉️ Send Notification", 
                                             self.notify_absent_students,
                                             width=180,
                                             bg_color=COLORS['accent'])
        self.btn_notify_absent.pack(side='left', padx=5)
        
        # Table Card
        table_card = self._create_card(frame, "❌ Students Absent Today")
        
        tree_frame = tk.Frame(table_card, bg=COLORS['white'])
        tree_frame.pack(fill='both', expand=True, padx=5, pady=5)
        
        # Scrollbars
        vsb = ttk.Scrollbar(tree_frame, orient="vertical")
        hsb = ttk.Scrollbar(tree_frame, orient="horizontal")
        
        # Treeview
        self.absent_tree = ttk.Treeview(tree_frame, 
                                       columns=("No", "ID", "Name", "Room"),
                                       show='headings',
                                       yscrollcommand=vsb.set,
                                       xscrollcommand=hsb.set,
                                       height=15)
        
        vsb.config(command=self.absent_tree.yview)
        hsb.config(command=self.absent_tree.xview)
        
        # Headers
        self.absent_tree.heading("No", text="#")
        self.absent_tree.heading("ID", text="Student ID")
        self.absent_tree.heading("Name", text="Student Name")
        self.absent_tree.heading("Room", text="Room")
        
        # Columns
        self.absent_tree.column("No", width=50, anchor='center')
        self.absent_tree.column("ID", width=200, anchor='center')
        self.absent_tree.column("Name", width=300, anchor='w')
        self.absent_tree.column("Room", width=150, anchor='center')
        
        vsb.pack(side='right', fill='y')
        hsb.pack(side='bottom', fill='x')
        self.absent_tree.pack(side='left', fill='both', expand=True)
        
        # Statistics Card
        stats_card = self._create_card(frame, "📊 Absence Statistics", compact=True)
        stats_card.master.pack(fill='x', padx=5, pady=5)
        
        stats_grid = tk.Frame(stats_card, bg=COLORS['white'])
        stats_grid.pack(fill='x', pady=10)
        
        # Total Students
        total_frame = tk.Frame(stats_grid, bg=COLORS['white'])
        total_frame.pack(side='left', padx=20, fill='x', expand=True)
        
        tk.Label(total_frame, text="Total Registered:",
                font=('Segoe UI', 10),
                bg=COLORS['white']).pack(anchor='w')
        
        self.absent_total_students = tk.Label(total_frame, text="0",
                                             font=('Segoe UI', 20, 'bold'),
                                             bg=COLORS['white'], fg=COLORS['secondary'])
        self.absent_total_students.pack(anchor='w')
        
        # Present
        present_frame = tk.Frame(stats_grid, bg=COLORS['white'])
        present_frame.pack(side='left', padx=20, fill='x', expand=True)
        
        tk.Label(present_frame, text="Present Today:",
                font=('Segoe UI', 10),
                bg=COLORS['white']).pack(anchor='w')
        
        self.absent_present_count = tk.Label(present_frame, text="0",
                                            font=('Segoe UI', 20, 'bold'),
                                            bg=COLORS['white'], fg=COLORS['success'])
        self.absent_present_count.pack(anchor='w')
        
        # Absent
        absent_frame = tk.Frame(stats_grid, bg=COLORS['white'])
        absent_frame.pack(side='left', padx=20, fill='x', expand=True)
        
        tk.Label(absent_frame, text="Absent Today:",
                font=('Segoe UI', 10),
                bg=COLORS['white']).pack(anchor='w')
        
        self.absent_absent_count = tk.Label(absent_frame, text="0",
                                           font=('Segoe UI', 20, 'bold'),
                                           bg=COLORS['white'], fg=COLORS['danger'])
        self.absent_absent_count.pack(anchor='w')
        
        # Absence Rate
        rate_frame = tk.Frame(stats_grid, bg=COLORS['white'])
        rate_frame.pack(side='left', padx=20, fill='x', expand=True)
        
        tk.Label(rate_frame, text="Absence Rate:",
                font=('Segoe UI', 10),
                bg=COLORS['white']).pack(anchor='w')
        
        self.absent_rate = tk.Label(rate_frame, text="0%",
                                   font=('Segoe UI', 20, 'bold'),
                                   bg=COLORS['white'], fg=COLORS['warning'])
        self.absent_rate.pack(anchor='w')
        
        # Load initial data
        self.refresh_absent_view()
    
    def refresh_absent_view(self):
        """Refresh absent students view"""
        
        # Clear table
        for item in self.absent_tree.get_children():
            self.absent_tree.delete(item)
        
        # Get absent students
        absent_students = get_absent_students_today()
        
        # Fill table
        for idx, student in enumerate(absent_students, 1):
            self.absent_tree.insert('', 'end', values=(
                idx,
                student['id'],
                student['name'],
                student['room']
            ))
        
        # Update statistics
        total_students = len(db)
        present_count = len(get_today_attendance())
        absent_count = len(absent_students)
        
        self.absent_total_students.config(text=str(total_students))
        self.absent_present_count.config(text=str(present_count))
        self.absent_absent_count.config(text=str(absent_count))
        
        # Calculate absence rate
        if total_students > 0:
            absence_rate = (absent_count / total_students) * 100
            self.absent_rate.config(text=f"{absence_rate:.1f}%")
            
            # Color based on rate
            if absence_rate <= 10:
                self.absent_rate.config(fg=COLORS['success'])
            elif absence_rate <= 25:
                self.absent_rate.config(fg=COLORS['warning'])
            else:
                self.absent_rate.config(fg=COLORS['danger'])
        else:
            self.absent_rate.config(text="N/A")
        
        # Update count label
        self.absent_count_label.config(
            text=f"❌ {absent_count} Student{'s' if absent_count != 1 else ''} Absent"
        )
    
    def export_absent_list(self):
        """Export absent students list to CSV"""
        
        absent_students = get_absent_students_today()
        
        if not absent_students:
            messagebox.showinfo("No Absent Students", 
                               "All students are present today! 🎉")
            return
        
        today = datetime.now().strftime("%Y-%m-%d")
        file_path = filedialog.asksaveasfilename(
            defaultextension=".csv",
            filetypes=[("CSV files", "*.csv"), ("All files", "*.*")],
            initialfile=f"absent_students_{today}.csv"
        )
        
        if file_path:
            try:
                with open(file_path, 'w', newline='', encoding='utf-8') as f:
                    writer = csv.writer(f)
                    writer.writerow(["#", "Student ID", "Student Name", "Room", "Date"])
                    
                    for idx, student in enumerate(absent_students, 1):
                        writer.writerow([
                            idx,
                            student['id'],
                            student['name'],
                            student['room'],
                            today
                        ])
                
                messagebox.showinfo("Success", 
                                  f"Absent list exported successfully!\n"
                                  f"File: {file_path}\n"
                                  f"Total absent: {len(absent_students)}")
            except Exception as e:
                messagebox.showerror("Error", f"Failed to export: {str(e)}")
    
    def notify_absent_students(self):
        """Send notification about absent students"""
        
        absent_students = get_absent_students_today()
        
        if not absent_students:
            messagebox.showinfo("No Absent Students", 
                               "All students are present today! 🎉")
            return
        
        # Build notification message
        message = f"Found {len(absent_students)} absent student(s) today:\n\n"
        for student in absent_students[:10]:  # Show first 10 only
            message += f"• {student['name']} (ID: {student['id']}) - Room: {student['room']}\n"
        
        if len(absent_students) > 10:
            message += f"\n... and {len(absent_students) - 10} more"
        
        messagebox.showinfo("Absent Students Notification", message)
    
    # ======================== END OF ABSENT TAB ========================

    def _build_register_tab(self):
        frame = self.tab_register
        
        left_card = self._create_card(frame, "Student Registration")
        left_card.master.pack(side="left", fill="both", expand=False, padx=5, pady=5)
        left_card.master.config(width=450)
        
        tk.Label(left_card, text="Student ID:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white'], fg=COLORS['dark']).pack(anchor="w", pady=(3, 2))
        
        id_frame = tk.Frame(left_card, bg=COLORS['white'])
        id_frame.pack(fill='x', pady=(0, 8))
        
        self.reg_id = tk.Entry(id_frame, font=('Segoe UI', 10),
                              relief='solid', borderwidth=1)
        self.reg_id.pack(fill='x', ipady=5)
        
        tk.Label(left_card, text="Student Name:", 
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white'], fg=COLORS['dark']).pack(anchor="w", pady=(8, 2))
        
        name_frame = tk.Frame(left_card, bg=COLORS['white'])
        name_frame.pack(fill='x', pady=(0, 8))
        tk.Label(left_card, text="Room Number:", 
        font=('Segoe UI', 10, 'bold'),
        bg=COLORS['white'], fg=COLORS['dark']).pack(anchor="w", pady=(8, 2))

        room_frame = tk.Frame(left_card, bg=COLORS['white'])
        room_frame.pack(fill='x', pady=(0, 8))

        self.reg_room = tk.Entry(
            room_frame,
            font=('Segoe UI', 10),
            relief='solid',
            borderwidth=1
        )
        self.reg_room.pack(fill='x', ipady=5)

        
        self.reg_name = tk.Entry(name_frame, font=('Segoe UI', 10),
                                relief='solid', borderwidth=1)
        self.reg_name.pack(fill='x', ipady=5)
        
        self.btn_open_cam = ModernButton(left_card, "📷 Open Camera", 
                                        self.reg_open_camera,
                                        height=38,
                                        bg_color=COLORS['secondary'])
        self.btn_open_cam.pack(pady=3, fill='x')
        
        self.btn_capture = ModernButton(left_card, "📸 Capture Photo", 
                                       self.reg_capture,
                                       height=38,
                                       bg_color=COLORS['warning'])
        self.btn_capture.set_enabled(False)
        self.btn_capture.pack(pady=3, fill='x')
        
        self.btn_register = ModernButton(left_card, "✅ Register Student", 
                                        self.register_student,
                                        height=38,
                                        bg_color=COLORS['success'])
        self.btn_register.set_enabled(False)
        self.btn_register.pack(pady=8, fill='x')
        
        status_frame = tk.Frame(left_card, bg=COLORS['light'], relief='solid', borderwidth=1)
        status_frame.pack(fill='x', pady=6)
        
        self.reg_status = tk.Label(status_frame, text="Ready to register new student",
                                  font=('Segoe UI', 9),
                                  bg=COLORS['light'], fg=COLORS['dark'],
                                  wraplength=320, justify='left')
        self.reg_status.pack(padx=6, pady=6)
        
        right = tk.Frame(frame, bg=COLORS['light'])
        right.pack(side="left", fill="both", expand=True, padx=5, pady=5)
        
        cam_card = self._create_card(right, "📹 Live Camera")
        self.reg_cam_preview = tk.Label(cam_card, bg=COLORS['light'],
                                       text="Camera not started",
                                       font=('Segoe UI', 11),
                                       fg='#7F8C8D')
        self.reg_cam_preview.pack(pady=5, padx=5, expand=True, fill='both')
        
        cap_card = self._create_card(right, "📸 Captured Image")
        self.reg_captured_preview = tk.Label(cap_card, bg=COLORS['light'],
                                            text="No image captured",
                                            font=('Segoe UI', 11),
                                            fg='#7F8C8D')
        self.reg_captured_preview.pack(pady=5, padx=5, expand=True, fill='both')
        
        self.reg_cam_running = False
        self.reg_cam = None
        self.reg_captured_image = None

    def reg_open_camera(self):
        if self.reg_cam_running:
            return

        cam_index = app_settings['camera_index']
        self.reg_cam = cv2.VideoCapture(cam_index)
        
        if not self.reg_cam.isOpened():
            messagebox.showerror("Error", f"Camera {cam_index} not found.\nPlease check settings.")
            return

        self.reg_cam_running = True
        self.btn_capture.set_enabled(True)
        self._update_reg_status("✓ Camera started successfully", COLORS['success'])
        self._reg_camera_loop()
    
    def _update_reg_status(self, text, color):
        self.reg_status.config(text=text, fg=color)
        original_bg = self.reg_status.cget('bg')
        self.reg_status.config(bg='#D5F4E6' if color == COLORS['success'] else '#FADBD8' if color == COLORS['danger'] else '#FEF9E7')
        self.after(200, lambda: self.reg_status.config(bg=original_bg))

    def _reg_camera_loop(self):
        if not self.reg_cam_running:
            return

        ret, frame = self.reg_cam.read()
        if ret:
            frame = cv2.resize(frame, (640, 480))
            self.show_image(frame, self.reg_cam_preview, (450, 280))

        self.after(30, self._reg_camera_loop)

    def reg_capture(self):
        if not self.reg_cam_running:
            return

        ret, frame = self.reg_cam.read()
        if not ret:
            return

        frame = cv2.resize(frame, (640, 480))
        self.reg_captured_image = frame.copy()
        self.show_image(self.reg_captured_image, self.reg_captured_preview, (450, 280))

        self.btn_register.set_enabled(True)
        self._update_reg_status("✓ Photo captured! Click 'Register Student' to save.", COLORS['success'])

    def register_student(self):
        global db
        
        student_id = self.reg_id.get().strip()
        name = self.reg_name.get().strip()
        
        if not student_id:
            messagebox.showerror("Error", "Student ID cannot be empty.")
            return

        if not name:
            messagebox.showerror("Error", "Student name cannot be empty.")
            return

        if self.reg_captured_image is None:
            messagebox.showerror("Error", "Please capture a photo first.")
            return

        features = extract_all_features(self.reg_captured_image)
        
        if not features:
            messagebox.showerror("Error", "Could not detect a face in the captured image.")
            self._update_reg_status("❌ No face detected. Please try again.", COLORS['danger'])
            return

        new_face_feature = features[0][0]
        matched_name, score, matched = match_with_db(new_face_feature, db)
        
        if matched:
            self._update_reg_status(f"⚠ WARNING: Face already registered under {matched_name}!", COLORS['warning'])
            messagebox.showwarning("Duplicate Face", 
                                  f"This face is already registered under: {matched_name}")
            return
        
        student_key = f"{student_id}_{name}"
        
        if student_key in db:
            overwrite = messagebox.askyesno("Student Exists", 
                f"Student ID '{student_id}' with name '{name}' exists. Overwrite?")
            if not overwrite:
                return
        room = self.reg_room.get().strip()

        if not room:
            messagebox.showerror("Error", "Room number cannot be empty.")
            return


        db[student_key] = new_face_feature
        save_db(db)
        rooms_db[student_key] = room
        save_rooms(rooms_db)

        self._update_reg_status(f"✅ SUCCESS: {name} (ID: {student_id}) registered!", COLORS['success'])
        self.update_stats()
        messagebox.showinfo("Success", f"Student {name} (ID: {student_id}) registered successfully!")
        
        self.clear_registration_form()

    def clear_registration_form(self):
        self.reg_id.delete(0, 'end')
        self.reg_name.delete(0, 'end')
        self.reg_room.delete(0, 'end')

        
        self.reg_captured_image = None
        self.reg_captured_preview.config(image="", text="No image captured", bg=COLORS['light'])
        
        self.btn_register.set_enabled(False)
        
        self._update_reg_status("Ready to register new student", COLORS['dark'])

    def _build_realtime_tab(self):
        frame = self.tab_realtime
        
        container = tk.Frame(frame, bg=COLORS['light'])
        container.pack(fill='both', expand=True, padx=5, pady=(0, 5))
        
        top_card = self._create_card(container, "Real-Time Recognition Controls", compact=True)
        top_card.master.pack(fill='x', padx=5, pady=(0, 0))
        top_card.master.config(height=65)
        
        btn_frame = tk.Frame(top_card, bg=COLORS['white'])
        btn_frame.pack(side='left', expand=True, pady=2)
        
        self.btn_start = ModernButton(btn_frame, "▶ Start Camera", 
                                     self.start_realtime,
                                     width=180,
                                     bg_color=COLORS['success'])
        self.btn_start.pack(side='left', padx=10)
        
        self.btn_stop = ModernButton(btn_frame, "⏹ Stop", 
                                    self.stop_realtime,
                                    width=180,
                                    bg_color=COLORS['danger'])
        self.btn_stop.set_enabled(False)
        self.btn_stop.pack(side='left', padx=10)
        
        status_container = tk.Frame(top_card, bg=COLORS['white'])
        status_container.pack(side='left', padx=20)
        
        tk.Label(status_container, text="Status:", 
                font=('Segoe UI', 11, 'bold'),
                bg=COLORS['white']).pack(side='left', padx=5)
        
        self.realtime_status = tk.Label(status_container, text="● Stopped",
                                       font=('Segoe UI', 11, 'bold'),
                                       bg=COLORS['white'], fg=COLORS['danger'])
        self.realtime_status.pack(side='left')
        
        self.attendance_message = tk.Label(top_card, text="",
                                          font=('Segoe UI', 13, 'bold'),
                                          bg=COLORS['white'])
        self.attendance_message.pack(side='right', padx=20)
        
        preview_card = self._create_card(container, "📹 Live Camera Feed", compact=True)
        preview_card.master.pack(fill='both', expand=True, padx=5, pady=(2, 5))
        
        self.real_preview = tk.Label(preview_card, bg=COLORS['dark'],
                                    text="Camera not started\nClick 'Start Camera' to begin",
                                    font=('Segoe UI', 12),
                                    fg=COLORS['light'])
        self.real_preview.pack(expand=True, fill='both', pady=8, padx=8)
        
        self.real_cam_running = False
        self.real_cam = None
        self.last_unknown_time = 0
        self.status_pulse_state = 0
        self.message_slide_position = 0

    def start_realtime(self):
        if self.real_cam_running:
            return

        cam_index = app_settings['camera_index']
        self.real_cam = cv2.VideoCapture(cam_index)
        
        if not self.real_cam.isOpened():
            messagebox.showerror("Error", f"Camera {cam_index} not found.\nPlease check settings.")
            return

        self.real_cam_running = True
        self.btn_start.set_enabled(False)
        self.btn_stop.set_enabled(True)
        self.realtime_status.config(text="● Running", fg=COLORS['success'])
        self._pulse_status()
        self._realtime_loop()
    
    def _pulse_status(self):
        if not self.real_cam_running:
            return
        
        self.status_pulse_state = (self.status_pulse_state + 1) % 60
        alpha = abs((self.status_pulse_state % 30) - 15) / 15.0
        
        base_color = self._hex_to_rgb(COLORS['success'])
        bright_color = tuple(min(255, int(c * 1.3)) for c in base_color)
        
        interpolated = tuple(
            int(base_color[i] + (bright_color[i] - base_color[i]) * alpha)
            for i in range(3)
        )
        
        pulse_color = '#%02x%02x%02x' % interpolated
        self.realtime_status.config(fg=pulse_color)
        
        self.after(50, self._pulse_status)
    
    def _hex_to_rgb(self, hex_color):
        hex_color = hex_color.lstrip('#')
        return tuple(int(hex_color[i:i+2], 16) for i in (0, 2, 4))

    def stop_realtime(self):
        self.real_cam_running = False
        if self.real_cam:
            self.real_cam.release()

        self.btn_start.set_enabled(True)
        self.btn_stop.set_enabled(False)
        self.realtime_status.config(text="● Stopped", fg=COLORS['danger'])
        self.real_preview.config(image="", text="Camera stopped", bg=COLORS['dark'])
        self.attendance_message.config(text="")
        self.current_attendance_status = ""

    def _realtime_loop(self):
        if not self.real_cam_running:
            return

        ret, frame = self.real_cam.read()
        if ret:
            frame = cv2.resize(frame, (640, 480))
            faces = extract_all_features(frame)
            display = frame.copy()
            
            if not faces:
                if self.current_attendance_status != "":
                    pass
            
            for feature, face in faces:
                x, y, w, h, _ = map(int, face[:5])
                matched_key, score, matched = match_with_db(feature, db)

                color = (0, 255, 0) if matched else (0, 0, 255)
                cv2.rectangle(display, (x, y), (x+w, y+h), color, 2)

                if matched:
                    display_name = get_name_from_key(matched_key)
                    label = f"{display_name}"
                else:
                    display_name = "Unknown"
                    label = "Unknown"
                
                (text_width, text_height), baseline = cv2.getTextSize(
                    label, cv2.FONT_HERSHEY_SIMPLEX, 0.7, 2
                )
                cv2.rectangle(display, (x, y-text_height-10), 
                            (x+text_width, y), color, -1)
                cv2.putText(display, label, (x, y-5),
                           cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)

                if matched:
                    self.record_attendance(matched_key, display_name)
                else:
                    unknown_text = "❌ Unknown face detected"
                    if self.current_attendance_status != unknown_text:
                        self._animate_message(unknown_text, COLORS['danger'])
                        self.current_attendance_status = unknown_text
            
            self.show_image(display, self.real_preview, (1100, 750))

        fps_delay = int(1000 / app_settings['fps_limit'])
        self.after(fps_delay, self._realtime_loop)

    def record_attendance(self, student_key, display_name):
        global session_marked
        
        if student_key in session_marked:
            warning_text = f"⚠ WARNING: {display_name} already marked"
            if self.current_attendance_status.startswith("✓ SUCCESS:") and display_name in self.current_attendance_status:
                return
            
            if self.current_attendance_status != warning_text:
                self._animate_message(warning_text, COLORS['warning'])
                self.current_attendance_status = warning_text
            return
        
        session_marked.add(student_key)
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        
        student_id = get_id_from_key(student_key)
        student_name = display_name
        
        file_exists = os.path.exists(DB_ATTENDANCE)
        with open(DB_ATTENDANCE, "a", newline="", encoding='utf-8') as f:
            writer = csv.writer(f)
            if not file_exists:
                writer.writerow(["Student ID", "Student Name","Room", "Date", "Time"])
            
            date_part = timestamp.split(' ')[0]
            time_part = timestamp.split(' ')[1]
            room = rooms_db.get(student_key, "N/A")
            writer.writerow([student_id, student_name, room, date_part, time_part])


        time_only = time_part
        success_text = f"✓ SUCCESS: {display_name} marked at {time_only}"
        
        self._animate_message(success_text, COLORS['success'])
        self.current_attendance_status = success_text
        self.update_stats()
    
    def _animate_message(self, text, color):
        self.attendance_message.config(text=text, fg=color)
        
        self.message_slide_position = 20
        self._slide_message()
    
    def _slide_message(self):
        if self.message_slide_position > 0:
            self.message_slide_position -= 2
            self.after(16, self._slide_message)

    def _build_database_tab(self):
            frame = self.tab_database
            
            top_card = self._create_card(frame, "Attendance Database")
            top_card.master.pack(fill='x', padx=5, pady=5)
            top_card.master.config(height=60)
            
            btn_frame = tk.Frame(top_card, bg=COLORS['white'])
            btn_frame.pack(side='left', expand=True, pady=5)
            
            self.btn_refresh_db = ModernButton(btn_frame, "🔄 Refresh", 
                                            self.refresh_database_view,
                                            width=150,
                                            bg_color=COLORS['secondary'])
            self.btn_refresh_db.pack(side='left', padx=5)
            
            self.btn_export_db = ModernButton(btn_frame, "📥 Export CSV", 
                                            self.export_database,
                                            width=150,
                                            bg_color=COLORS['success'])
            self.btn_export_db.pack(side='left', padx=5)
            
            self.btn_clear_db = ModernButton(btn_frame, "🗑️ Clear All", 
                                            self.clear_database,
                                            width=150,
                                            bg_color=COLORS['danger'])
            self.btn_clear_db.pack(side='left', padx=5)
            
            # ---- Date Filter ----
            filter_frame = tk.Frame(top_card, bg=COLORS['white'])
            filter_frame.pack(side='right', padx=10)

            tk.Label(
                filter_frame,
                text="Filter by Date (YYYY-MM-DD):",
                font=('Segoe UI', 10, 'bold'),
                bg=COLORS['white']
            ).pack(side='left', padx=5)
            
            self.filter_date = DateEntry(
                filter_frame,
                width=12,
                background=COLORS['secondary'],
                foreground='white',
                borderwidth=2,
                date_pattern='yyyy-mm-dd',
                font=('Segoe UI', 10)
            )
            self.filter_date.pack(side='left', padx=5)

            ModernButton(
                filter_frame,
                "🔍 Filter",
                self.filter_database_by_date,
                width=120,
                height=32,
                bg_color=COLORS['accent']
            ).pack(side='left', padx=5)

            
            self.db_info_label = tk.Label(top_card, text="",
                                        font=('Segoe UI', 10, 'bold'),
                                        bg=COLORS['white'], fg=COLORS['dark'])
            self.db_info_label.pack(side='right', padx=20)
            
            table_card = self._create_card(frame, "📋 Attendance Records")
            
            tree_frame = tk.Frame(table_card, bg=COLORS['white'])
            tree_frame.pack(fill='both', expand=True, padx=5, pady=5)
            
            vsb = ttk.Scrollbar(tree_frame, orient="vertical")
            hsb = ttk.Scrollbar(tree_frame, orient="horizontal")
            
            self.db_tree = ttk.Treeview(tree_frame, 
                                        columns=("ID", "Name","Room", "Date", "Time"),
                                        show='headings',
                                        yscrollcommand=vsb.set,
                                        xscrollcommand=hsb.set,
                                        height=20)
            
            vsb.config(command=self.db_tree.yview)
            hsb.config(command=self.db_tree.xview)
            
            self.db_tree.heading("ID", text="Student ID")
            self.db_tree.heading("Name", text="Student Name")
            self.db_tree.heading("Date", text="Date")
            self.db_tree.heading("Room", text="Room")
            self.db_tree.heading("Time", text="Time")

            
            self.db_tree.column("ID", width=150, anchor='center')
            self.db_tree.column("Name", width=250, anchor='w')
            self.db_tree.column("Date", width=150, anchor='center')
            self.db_tree.column("Room", width=100, anchor='center')
            self.db_tree.column("Time", width=150, anchor='center')
            
            style = ttk.Style()
            style.configure("Treeview",
                        background=COLORS['white'],
                        foreground=COLORS['dark'],
                        rowheight=30,
                        fieldbackground=COLORS['white'],
                        font=('Segoe UI', 10))
            style.configure("Treeview.Heading",
                        font=('Segoe UI', 11, 'bold'),
                        background=COLORS['secondary'],
                        foreground=COLORS['white'])
            style.map('Treeview',
                    background=[('selected', COLORS['secondary'])],
                    foreground=[('selected', COLORS['white'])])
            
            vsb.pack(side='right', fill='y')
            hsb.pack(side='bottom', fill='x')
            self.db_tree.pack(side='left', fill='both', expand=True)
            
            self.refresh_database_view()

    def filter_database_by_date(self):
            selected_date = self.filter_date.get_date().strftime("%Y-%m-%d")

            # Clear table
            for item in self.db_tree.get_children():
                self.db_tree.delete(item)

            if not os.path.exists(DB_ATTENDANCE):
                self.db_info_label.config(text="No records found")
                return

            try:
                with open(DB_ATTENDANCE, 'r', encoding='utf-8') as f:
                    reader = csv.reader(f)
                    next(reader)

                    filtered_records = [
                        row for row in reader
                        if len(row) >= 5 and row[3] == selected_date
                    ]

                    for record in reversed(filtered_records):
                        self.db_tree.insert('', 'end', values=record)

                    self.db_info_label.config(
                        text=f"Records on {selected_date}: {len(filtered_records)}"
                    )

            except Exception as e:
                messagebox.showerror("Error", f"Failed to filter records:\n{str(e)}")

    def refresh_database_view(self):
            for item in self.db_tree.get_children():
                self.db_tree.delete(item)
            
            if os.path.exists(DB_ATTENDANCE):
                try:
                    with open(DB_ATTENDANCE, 'r', encoding='utf-8') as f:
                        reader = csv.reader(f)
                        next(reader)
                        
                        records = list(reader)
                        
                        for record in reversed(records):
                            if len(record) >= 4:
                                self.db_tree.insert('', 'end', values=record)
                        
                        self.db_info_label.config(
                            text=f"Total Records: {len(records)}"
                        )
                except Exception as e:
                    messagebox.showerror("Error", f"Failed to load database: {str(e)}")
                    self.db_info_label.config(text="Error loading records")
            else:
                self.db_info_label.config(text="No records found")

    def export_database(self):
            if not os.path.exists(DB_ATTENDANCE):
                messagebox.showwarning("No Data", "No attendance records to export.")
                return
            
            file_path = filedialog.asksaveasfilename(
                defaultextension=".csv",
                filetypes=[("CSV files", "*.csv"), ("All files", "*.*")],
                initialfile=f"attendance_export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.csv"
            )
            
            if file_path:
                try:
                    import shutil
                    shutil.copy(DB_ATTENDANCE, file_path)
                    messagebox.showinfo("Success", f"Database exported to:\n{file_path}")
                except Exception as e:
                    messagebox.showerror("Error", f"Failed to export: {str(e)}")

    def clear_database(self):
            confirm = messagebox.askyesno(
                "Confirm Clear",
                "Are you sure you want to delete ALL attendance records?\nThis action cannot be undone!"
            )
            
            if confirm:
                try:
                    if os.path.exists(DB_ATTENDANCE):
                        os.remove(DB_ATTENDANCE)
                    
                    global session_marked
                    session_marked.clear()
                    
                    self.refresh_database_view()
                    self.update_stats()
                    messagebox.showinfo("Success", "All attendance records have been cleared.")
                except Exception as e:
                    messagebox.showerror("Error", f"Failed to clear database: {str(e)}")

    def show_image(self, cv_img, widget, size):
            rgb = cv2.cvtColor(cv_img, cv2.COLOR_BGR2RGB)
            pil = Image.fromarray(rgb)
            pil.thumbnail(size, Image.Resampling.LANCZOS)
            imgtk = ImageTk.PhotoImage(pil)
            widget.image = imgtk
            widget.config(image=imgtk)


if __name__ == "__main__":
    FaceApp().mainloop()