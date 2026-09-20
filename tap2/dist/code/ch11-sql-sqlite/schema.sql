-- Luoc do co so du lieu "cua hang" dung xuyen suot Tap 2. Chay duoc tren SQLite (sqlite3, DB Browser, VS Code SQLite...).
PRAGMA foreign_keys = ON;

CREATE TABLE nhom (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    ten  TEXT NOT NULL UNIQUE
);

CREATE TABLE san_pham (
    id       INTEGER PRIMARY KEY AUTOINCREMENT,
    ma       TEXT    NOT NULL UNIQUE,
    ten      TEXT    NOT NULL,
    gia      INTEGER NOT NULL CHECK (gia >= 0),          -- dong (VND), khong dung so thuc cho tien
    ton      INTEGER NOT NULL DEFAULT 0 CHECK (ton >= 0),
    nhom_id  INTEGER NOT NULL REFERENCES nhom(id)
);
CREATE INDEX ix_san_pham_nhom ON san_pham(nhom_id);

CREATE TABLE khach_hang (
    id     INTEGER PRIMARY KEY AUTOINCREMENT,
    ten    TEXT NOT NULL,
    email  TEXT NOT NULL UNIQUE
);

CREATE TABLE don_hang (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    khach_hang_id  INTEGER NOT NULL REFERENCES khach_hang(id),
    ngay           TEXT    NOT NULL                       -- ISO 8601: 'YYYY-MM-DD'
);

CREATE TABLE chi_tiet_don (
    don_hang_id  INTEGER NOT NULL REFERENCES don_hang(id) ON DELETE CASCADE,
    san_pham_id  INTEGER NOT NULL REFERENCES san_pham(id),
    so_luong     INTEGER NOT NULL CHECK (so_luong > 0),
    don_gia      INTEGER NOT NULL,                        -- gia tai thoi diem dat (khong doi khi gia san pham doi)
    PRIMARY KEY (don_hang_id, san_pham_id)
);

-- Du lieu mau
INSERT INTO nhom (ten) VALUES ('Phu kien'), ('Thiet bi'), ('Sach');

INSERT INTO san_pham (ma, ten, gia, ton, nhom_id) VALUES
    ('CH001', 'Chuot khong day',   150000, 30, 1),
    ('BP001', 'Ban phim co',       500000, 12, 1),
    ('TN001', 'Tai nghe',          200000,  0, 1),
    ('MH001', 'Man hinh 24 inch', 3500000,  5, 2),
    ('LT001', 'Laptop Dell',     18000000,  3, 2);

INSERT INTO khach_hang (ten, email) VALUES
    ('Nguyen An', 'an@example.com'),
    ('Tran Binh', 'binh@example.com'),
    ('Le Chi',    'chi@example.com');

INSERT INTO don_hang (khach_hang_id, ngay) VALUES (1, '2026-09-01'), (1, '2026-09-10'), (2, '2026-09-12');

INSERT INTO chi_tiet_don (don_hang_id, san_pham_id, so_luong, don_gia) VALUES
    (1, 1, 2,   150000),
    (1, 2, 1,   500000),
    (2, 5, 1, 18000000),
    (3, 4, 2,  3500000),
    (3, 1, 1,   150000);
