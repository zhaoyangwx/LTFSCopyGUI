#![allow(non_snake_case)]

use std::collections::HashMap;
use std::ffi::{c_void, OsStr};
use std::io::{self, BufRead, Write};
use std::os::windows::ffi::OsStrExt;
use std::ptr::null_mut;
use md5::Md5;
use sha1::Sha1;
use sha2::{Digest, Sha256, Sha512};

type HANDLE = *mut c_void;
type DWORD = u32;
type BOOL = i32;
type LPCWSTR = *const u16;
type LPVOID = *mut c_void;

const INVALID_HANDLE_VALUE: HANDLE = -1isize as HANDLE;
const PAGE_READWRITE: DWORD = 0x04;
const FILE_MAP_ALL_ACCESS: DWORD = 0x001f;
const INFINITE: DWORD = 0xffff_ffff;
const FILE_FLAG_SEQUENTIAL_SCAN: DWORD = 0x0800_0000;
const OPEN_EXISTING: DWORD = 3;
const GENERIC_READ: DWORD = 0x8000_0000;
const FILE_SHARE_READ: DWORD = 0x0000_0001;
const FILE_SHARE_WRITE: DWORD = 0x0000_0002;
const FILE_SHARE_DELETE: DWORD = 0x0000_0004;

const HEADER_SIZE: usize = 4096;
const SLOT_META_SIZE: usize = 64;
const MAGIC: u64 = 0x4c544653_46525354; // LTFSFRST
const VERSION: u32 = 1;
const STATUS_EMPTY: u32 = 0;
const STATUS_FULL: u32 = 1;
const FLAG_EOF: u32 = 1;
const FLAG_ERROR: u32 = 2;

#[link(name = "kernel32")]
extern "system" {
    fn CreateFileMappingW(hFile: HANDLE, lpAttributes: LPVOID, flProtect: DWORD, dwMaximumSizeHigh: DWORD, dwMaximumSizeLow: DWORD, lpName: LPCWSTR) -> HANDLE;
    fn MapViewOfFile(hFileMappingObject: HANDLE, dwDesiredAccess: DWORD, dwFileOffsetHigh: DWORD, dwFileOffsetLow: DWORD, dwNumberOfBytesToMap: usize) -> LPVOID;
    fn UnmapViewOfFile(lpBaseAddress: LPVOID) -> BOOL;
    fn CloseHandle(hObject: HANDLE) -> BOOL;
    fn CreateEventW(lpEventAttributes: LPVOID, bManualReset: BOOL, bInitialState: BOOL, lpName: LPCWSTR) -> HANDLE;
    fn SetEvent(hEvent: HANDLE) -> BOOL;
    fn WaitForSingleObject(hHandle: HANDLE, dwMilliseconds: DWORD) -> DWORD;
    fn CreateFileW(lpFileName: LPCWSTR, dwDesiredAccess: DWORD, dwShareMode: DWORD, lpSecurityAttributes: LPVOID, dwCreationDisposition: DWORD, dwFlagsAndAttributes: DWORD, hTemplateFile: HANDLE) -> HANDLE;
    fn ReadFile(hFile: HANDLE, lpBuffer: LPVOID, nNumberOfBytesToRead: DWORD, lpNumberOfBytesRead: *mut DWORD, lpOverlapped: LPVOID) -> BOOL;
}

fn wide(s: &str) -> Vec<u16> {
    OsStr::new(s).encode_wide().chain(Some(0)).collect()
}

unsafe fn write_u32(base: *mut u8, off: usize, v: u32) {
    std::ptr::write_unaligned(base.add(off) as *mut u32, v);
}
unsafe fn write_u64(base: *mut u8, off: usize, v: u64) {
    std::ptr::write_unaligned(base.add(off) as *mut u64, v);
}
unsafe fn read_u32(base: *mut u8, off: usize) -> u32 {
    std::ptr::read_unaligned(base.add(off) as *const u32)
}
unsafe fn read_u64(base: *mut u8, off: usize) -> u64 {
    std::ptr::read_unaligned(base.add(off) as *const u64)
}

struct Handle(HANDLE);
impl Drop for Handle {
    fn drop(&mut self) {
        unsafe {
            if !self.0.is_null() && self.0 != INVALID_HANDLE_VALUE {
                CloseHandle(self.0);
            }
        }
    }
}

struct Mapping {
    _handle: Handle,
    base: *mut u8,
    size: usize,
    slot_count: u64,
    slot_size: usize,
    data_offset: usize,
}

impl Drop for Mapping {
    fn drop(&mut self) {
        unsafe {
            if !self.base.is_null() {
                UnmapViewOfFile(self.base as LPVOID);
            }
        }
    }
}

impl Mapping {
    unsafe fn create(name: &str, capacity: u64, slot_size: u64) -> io::Result<Self> {
        let slot_count = std::cmp::max(2, capacity / slot_size);
        let meta_bytes = slot_count as usize * SLOT_META_SIZE;
        let data_offset = (HEADER_SIZE + meta_bytes + 4095) & !4095usize;
        let size = data_offset + slot_count as usize * slot_size as usize;
        let name_w = wide(name);
        let handle = CreateFileMappingW(INVALID_HANDLE_VALUE, null_mut(), PAGE_READWRITE, (size as u64 >> 32) as u32, size as u32, name_w.as_ptr());
        if handle.is_null() {
            return Err(io::Error::last_os_error());
        }
        let base = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, size) as *mut u8;
        if base.is_null() {
            CloseHandle(handle);
            return Err(io::Error::last_os_error());
        }
        std::ptr::write_bytes(base, 0, size);
        write_u64(base, 0, MAGIC);
        write_u32(base, 8, VERSION);
        write_u32(base, 12, HEADER_SIZE as u32);
        write_u64(base, 16, slot_size);
        write_u64(base, 24, slot_count);
        write_u64(base, 32, 0);
        write_u64(base, 40, 0);
        write_u32(base, 48, 0);
        write_u32(base, 52, 0);
        write_u64(base, 56, data_offset as u64);
        Ok(Self { _handle: Handle(handle), base, size, slot_count, slot_size: slot_size as usize, data_offset })
    }

    unsafe fn slot_meta(&self, idx: u64) -> *mut u8 {
        self.base.add(HEADER_SIZE + (idx as usize % self.slot_count as usize) * SLOT_META_SIZE)
    }

    unsafe fn slot_data(&self, idx: u64) -> *mut u8 {
        self.base.add(self.data_offset + (idx as usize % self.slot_count as usize) * self.slot_size)
    }

    unsafe fn wait_free_slot(&self, space_event: HANDLE) -> io::Result<u64> {
        loop {
            let write_idx = read_u64(self.base, 32);
            let meta = self.slot_meta(write_idx);
            if read_u32(meta, 0) == STATUS_EMPTY {
                return Ok(write_idx);
            }
            WaitForSingleObject(space_event, INFINITE);
        }
    }
}

struct Xxh3_64 {
    h: xxhash_rust::xxh3::Xxh3,
}
impl Xxh3_64 {
    fn new() -> Self { Self { h: xxhash_rust::xxh3::Xxh3::new() } }
    fn update(&mut self, data: &[u8]) { self.h.update(data); }
    fn finish(&self) -> [u8; 8] { self.h.digest().to_be_bytes() }
}

struct Xxh3_128 {
    h: xxhash_rust::xxh3::Xxh3,
}
impl Xxh3_128 {
    fn new() -> Self { Self { h: xxhash_rust::xxh3::Xxh3::new() } }
    fn update(&mut self, data: &[u8]) { self.h.update(data); }
    fn finish(&self) -> [u8; 16] { self.h.digest128().to_be_bytes() }
}

struct HashSet {
    sha1: Option<Sha1>,
    sha256: Option<Sha256>,
    sha512: Option<Sha512>,
    md5: Option<Md5>,
    crc32: Option<crc32fast::Hasher>,
    blake3: Option<blake3::Hasher>,
    xxh3: Option<Xxh3_64>,
    xxh128: Option<Xxh3_128>,
}

fn hex(bytes: &[u8]) -> String {
    let mut s = String::with_capacity(bytes.len() * 2);
    for b in bytes {
        s.push_str(&format!("{:02X}", b));
    }
    s
}

impl HashSet {
    fn new(enabled: &HashMap<String, bool>) -> io::Result<Self> {
        Ok(Self {
            sha1: if *enabled.get("SHA1").unwrap_or(&false) { Some(Sha1::new()) } else { None },
            sha256: if *enabled.get("SHA256").unwrap_or(&false) { Some(Sha256::new()) } else { None },
            sha512: if *enabled.get("SHA512").unwrap_or(&false) { Some(Sha512::new()) } else { None },
            md5: if *enabled.get("MD5").unwrap_or(&false) { Some(Md5::new()) } else { None },
            crc32: if *enabled.get("CRC32").unwrap_or(&false) { Some(crc32fast::Hasher::new()) } else { None },
            blake3: if *enabled.get("BLAKE3").unwrap_or(&false) { Some(blake3::Hasher::new()) } else { None },
            xxh3: if *enabled.get("XxHash3").unwrap_or(&false) { Some(Xxh3_64::new()) } else { None },
            xxh128: if *enabled.get("XxHash128").unwrap_or(&false) { Some(Xxh3_128::new()) } else { None },
        })
    }

    fn update(&mut self, slice: &[u8]) -> io::Result<()> {
        if let Some(h) = self.sha1.as_mut() { h.update(slice); }
        if let Some(h) = self.sha256.as_mut() { h.update(slice); }
        if let Some(h) = self.sha512.as_mut() { h.update(slice); }
        if let Some(h) = self.md5.as_mut() { h.update(slice); }
        if let Some(c) = self.crc32.as_mut() { c.update(slice); }
        if let Some(h) = self.blake3.as_mut() { h.update(slice); }
        if let Some(h) = self.xxh3.as_mut() { h.update(slice); }
        if let Some(h) = self.xxh128.as_mut() { h.update(slice); }
        Ok(())
    }

    fn finish(&mut self) -> io::Result<String> {
        let mut parts = Vec::new();
        if let Some(h) = self.sha1.take() { parts.push(format!("SHA1={}", hex(&h.finalize()))); }
        if let Some(h) = self.sha256.take() { parts.push(format!("SHA256={}", hex(&h.finalize()))); }
        if let Some(h) = self.sha512.take() { parts.push(format!("SHA512={}", hex(&h.finalize()))); }
        if let Some(h) = self.md5.take() { parts.push(format!("MD5={}", hex(&h.finalize()))); }
        if let Some(c) = self.crc32.take() { parts.push(format!("CRC32={}", hex(&c.finalize().to_be_bytes()))); }
        if let Some(h) = self.blake3.as_ref() { parts.push(format!("BLAKE3={}", h.finalize().to_hex().to_string().to_uppercase())); }
        if let Some(h) = self.xxh3.as_ref() { parts.push(format!("XxHash3={}", hex(&h.finish()))); }
        if let Some(h) = self.xxh128.as_ref() { parts.push(format!("XxHash128={}", hex(&h.finish()))); }
        Ok(parts.join("\t"))
    }
}

fn parse_bool(v: Option<&String>) -> bool {
    matches!(v.map(|s| s.as_str()), Some("1") | Some("true") | Some("True"))
}

fn parse_init(line: &str) -> HashMap<String, String> {
    let mut result = HashMap::new();
    for part in line.trim_end().split('\t').skip(1) {
        if let Some((k, v)) = part.split_once('=') {
            result.insert(k.to_string(), v.to_string());
        }
    }
    result
}

fn decode_path_hex(value: &str) -> io::Result<String> {
    if value.len() % 4 != 0 {
        return Err(io::Error::new(io::ErrorKind::InvalidInput, "bad path encoding length"));
    }
    let mut units = Vec::with_capacity(value.len() / 4);
    let bytes = value.as_bytes();
    let mut i = 0usize;
    while i < bytes.len() {
        let lo = hex_byte(bytes[i], bytes[i + 1])?;
        let hi = hex_byte(bytes[i + 2], bytes[i + 3])?;
        units.push(u16::from_le_bytes([lo, hi]));
        i += 4;
    }
    String::from_utf16(&units).map_err(|e| io::Error::new(io::ErrorKind::InvalidInput, e))
}

fn hex_byte(hi: u8, lo: u8) -> io::Result<u8> {
    Ok((hex_nibble(hi)? << 4) | hex_nibble(lo)?)
}

fn hex_nibble(v: u8) -> io::Result<u8> {
    match v {
        b'0'..=b'9' => Ok(v - b'0'),
        b'a'..=b'f' => Ok(v - b'a' + 10),
        b'A'..=b'F' => Ok(v - b'A' + 10),
        _ => Err(io::Error::new(io::ErrorKind::InvalidInput, "bad hex path encoding")),
    }
}

unsafe fn fill_file(mapping: &Mapping, data_event: HANDLE, space_event: HANDLE, file_index: u64, path: &str, enabled: &HashMap<String, bool>) -> io::Result<()> {
    let path_w = wide(path);
    let fh = CreateFileW(path_w.as_ptr(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, null_mut(), OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, null_mut());
    if fh == INVALID_HANDLE_VALUE {
        return Err(io::Error::last_os_error());
    }
    let _file = Handle(fh);
    let mut hashes = HashSet::new(enabled)?;
    let mut file_offset = 0u64;
    loop {
        let idx = mapping.wait_free_slot(space_event)?;
        let meta = mapping.slot_meta(idx);
        let data = mapping.slot_data(idx);
        let mut read = 0u32;
        let ok = ReadFile(fh, data as LPVOID, mapping.slot_size as u32, &mut read, null_mut());
        if ok == 0 {
            write_u32(meta, 4, FLAG_ERROR);
            write_u64(meta, 8, file_index);
            write_u64(meta, 16, file_offset);
            write_u32(meta, 24, 0);
            write_u32(meta, 0, STATUS_FULL);
            write_u64(mapping.base, 32, idx + 1);
            SetEvent(data_event);
            return Err(io::Error::last_os_error());
        }
        if read == 0 {
            break;
        }
        hashes.update(std::slice::from_raw_parts(data, read as usize))?;
        write_u32(meta, 4, 0);
        write_u64(meta, 8, file_index);
        write_u64(meta, 16, file_offset);
        write_u32(meta, 24, read);
        write_u32(meta, 0, STATUS_FULL);
        file_offset += read as u64;
        write_u64(mapping.base, 32, idx + 1);
        SetEvent(data_event);
    }
    let digest = hashes.finish()?;
    println!("FILE_DONE\t{}\t{}", file_index, digest);
    io::stdout().flush().ok();
    let idx = mapping.wait_free_slot(space_event)?;
    let meta = mapping.slot_meta(idx);
    write_u32(meta, 4, FLAG_EOF);
    write_u64(meta, 8, file_index);
    write_u64(meta, 16, file_offset);
    write_u32(meta, 24, 0);
    write_u32(meta, 0, STATUS_FULL);
    write_u64(mapping.base, 32, idx + 1);
    SetEvent(data_event);
    Ok(())
}

unsafe fn hash_file(file_index: u64, path: &str, enabled: &HashMap<String, bool>) -> io::Result<()> {
    let path_w = wide(path);
    let fh = CreateFileW(path_w.as_ptr(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, null_mut(), OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, null_mut());
    if fh == INVALID_HANDLE_VALUE {
        return Err(io::Error::last_os_error());
    }
    let _file = Handle(fh);
    let mut hashes = HashSet::new(enabled)?;
    let mut buf = vec![0u8; 4 * 1024 * 1024];
    loop {
        let mut read = 0u32;
        let ok = ReadFile(fh, buf.as_mut_ptr() as LPVOID, buf.len() as u32, &mut read, null_mut());
        if ok == 0 {
            return Err(io::Error::last_os_error());
        }
        if read == 0 {
            break;
        }
        hashes.update(&buf[..read as usize])?;
    }
    let digest = hashes.finish()?;
    println!("FILE_DONE\t{}\t{}", file_index, digest);
    io::stdout().flush().ok();
    Ok(())
}

fn main() -> io::Result<()> {
    let stdin = io::stdin();
    let mut lines = stdin.lock().lines();
    let init = lines.next().transpose()?.ok_or_else(|| io::Error::new(io::ErrorKind::UnexpectedEof, "missing INIT"))?;
    if !init.starts_with("INIT\t") {
        return Err(io::Error::new(io::ErrorKind::InvalidInput, "expected INIT"));
    }
    let cfg = parse_init(&init);
    let shm_name = cfg.get("shm").ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing shm"))?.clone();
    let data_event_name = cfg.get("data_event").ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing data_event"))?.clone();
    let space_event_name = cfg.get("space_event").ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing space_event"))?.clone();
    let capacity: u64 = cfg.get("capacity").and_then(|s| s.parse().ok()).unwrap_or(268435456);
    let slot_size: u64 = cfg.get("slot_size").and_then(|s| s.parse().ok()).unwrap_or(1048576);
    let mut enabled = HashMap::new();
    for name in ["SHA1", "SHA256", "SHA512", "MD5", "CRC32", "BLAKE3", "XxHash3", "XxHash128"] {
        enabled.insert(name.to_string(), parse_bool(cfg.get(name)));
    }

    unsafe {
        let mapping = Mapping::create(&shm_name, capacity, slot_size)?;
        let data_event = CreateEventW(null_mut(), 0, 0, wide(&data_event_name).as_ptr());
        if data_event.is_null() {
            return Err(io::Error::last_os_error());
        }
        let _data_event = Handle(data_event);
        let space_event = CreateEventW(null_mut(), 0, 0, wide(&space_event_name).as_ptr());
        if space_event.is_null() {
            return Err(io::Error::last_os_error());
        }
        let _space_event = Handle(space_event);
        println!("READY\tslot_count={}\tdata_offset={}\tmap_size={}", mapping.slot_count, mapping.data_offset, mapping.size);
        io::stdout().flush().ok();

        for line in lines {
            let line = line?;
            if line == "DONE" {
                break;
            }
            if let Some(rest) = line.strip_prefix("FILE\t") {
                let mut parts = rest.splitn(3, '\t');
                let idx: u64 = parts.next().and_then(|s| s.parse().ok()).ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "bad file index"))?;
                let _len = parts.next();
                let path = decode_path_hex(parts.next().ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing path"))?)?;
                if let Err(e) = fill_file(&mapping, data_event, space_event, idx, &path, &enabled) {
                    eprintln!("FILE_ERROR\t{}\t{}", idx, e);
                    write_u32(mapping.base, 52, 1);
                    SetEvent(data_event);
                    break;
                }
            } else if let Some(rest) = line.strip_prefix("HASH\t") {
                let mut parts = rest.splitn(3, '\t');
                let idx: u64 = parts.next().and_then(|s| s.parse().ok()).ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "bad hash file index"))?;
                let _len = parts.next();
                let path = decode_path_hex(parts.next().ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing hash path"))?)?;
                if let Err(e) = hash_file(idx, &path, &enabled) {
                    eprintln!("FILE_ERROR\t{}\t{}", idx, e);
                    write_u32(mapping.base, 52, 1);
                    SetEvent(data_event);
                    break;
                }
            }
        }
        write_u32(mapping.base, 48, 1);
        SetEvent(data_event);
    }
    Ok(())
}
