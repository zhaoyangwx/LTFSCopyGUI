#![allow(non_snake_case)]

use md5::Md5;
use sha1::Sha1;
use sha2::{Digest, Sha256, Sha512};
use std::collections::{HashMap, VecDeque};
use std::ffi::OsStr;
use std::io::{self, BufRead, Write};
use std::os::windows::ffi::OsStrExt;
use std::ptr::{null, null_mut};
use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::{mpsc, Arc, Condvar, Mutex};
use std::thread::{self, JoinHandle};
use std::time::{Duration, Instant};
use windows_sys::Win32::Foundation::{
    CloseHandle, ERROR_IO_PENDING, ERROR_NOT_FOUND, GENERIC_READ, HANDLE, INVALID_HANDLE_VALUE,
    WAIT_OBJECT_0, WAIT_TIMEOUT,
};
use windows_sys::Win32::Storage::FileSystem::{
    CreateFileW, GetFileSizeEx, ReadFile, FILE_ATTRIBUTE_NORMAL, FILE_FLAG_OVERLAPPED,
    FILE_FLAG_SEQUENTIAL_SCAN, FILE_SHARE_DELETE, FILE_SHARE_READ, FILE_SHARE_WRITE, OPEN_EXISTING,
};
use windows_sys::Win32::System::Memory::{
    CreateFileMappingW, MapViewOfFile, UnmapViewOfFile, FILE_MAP_ALL_ACCESS,
    MEMORY_MAPPED_VIEW_ADDRESS, PAGE_READWRITE,
};
use windows_sys::Win32::System::Threading::{
    CreateEventW, SetEvent, WaitForMultipleObjects, WaitForSingleObject, INFINITE,
};
use windows_sys::Win32::System::IO::{
    CancelIoEx, CreateIoCompletionPort, GetOverlappedResult, GetQueuedCompletionStatusEx,
    PostQueuedCompletionStatus, OVERLAPPED, OVERLAPPED_ENTRY,
};

const HEADER_SIZE: usize = 4096;
const SLOT_META_SIZE: usize = 64;
const MAGIC: u64 = 0x4c544653_46525354; // LTFSFRST
const VERSION: u32 = 1;
const STATUS_EMPTY: u32 = 0;
const STATUS_FULL: u32 = 1;
const FLAG_EOF: u32 = 1;
const FLAG_ERROR: u32 = 2;
const IO_QUEUE_DEPTH: usize = 16;
const HASH_CHUNK_SIZE: usize = 4 * 1024 * 1024;
const CANCEL_POLL_MS: u32 = 100;

// C-FFI
pub const LFR_ABI_VERSION: u32 = 2;
pub const LFR_OK: i32 = 0;
pub const LFR_TIMEOUT: i32 = 1;
pub const LFR_DONE: i32 = 2;
pub const LFR_INVALID: i32 = -1;
pub const LFR_ERROR: i32 = -2;
pub const LFR_CANCELLED: i32 = -3;
pub const LFR_HASH_SHA1: u32 = 1 << 0;
pub const LFR_HASH_SHA256: u32 = 1 << 1;
pub const LFR_HASH_SHA512: u32 = 1 << 2;
pub const LFR_HASH_MD5: u32 = 1 << 3;
pub const LFR_HASH_CRC32: u32 = 1 << 4;
pub const LFR_HASH_BLAKE3: u32 = 1 << 5;
pub const LFR_HASH_XXH3: u32 = 1 << 6;
pub const LFR_HASH_XXH128: u32 = 1 << 7;

#[repr(C)]
pub struct LfrConfig {
    pub struct_size: u32,
    pub abi_version: u32,
    pub slot_size: u32,
    pub read_chunk_size: u32,
    pub queue_depth: u32,
    pub capacity_bytes: u64,
    pub small_open_concurrency: u32,
    pub small_active_files: u32,
    pub small_inflight_bytes: u64,
    pub small_threshold: u64,
    pub hash_mask: u32,
    pub next_file_prime_depth: u32,
}

#[repr(C)]
pub struct LfrSlot {
    pub token: u64,
    pub file_index: i64,
    pub file_offset: u64,
    pub data: *const u8,
    pub length: u32,
    pub flags: u32,
}

#[repr(C)]
pub struct LfrStats {
    pub struct_size: u32,
    pub abi_version: u32,
    pub bytes_read: u64,
    pub bytes_published: u64,
    pub buffered_bytes: u64,
    pub occupied_slots: u64,
    pub read_wait_ns: u64,
    pub hash_ns: u64,
    pub publish_wait_ns: u64,
}

#[derive(Clone)]
struct NativeFileTask {
    index: u64,
    len: u64,
    path: String,
    selected: bool,
}

struct NativeSlot {
    buffer: Box<[u8]>,
    token: u64,
    file_index: u64,
    file_offset: u64,
    length: u32,
    flags: u32,
    full: bool,
}

struct NativeState {
    slots: Vec<NativeSlot>,
    files: Vec<NativeFileTask>,
    write_index: u64,
    read_index: u64,
    buffered_bytes: u64,
    occupied_slots: usize,
    started: bool,
    done: bool,
    cancelled: bool,
    error: String,
    results: HashMap<u64, String>,
}

#[derive(Default)]
struct NativeTelemetry {
    bytes_read: AtomicU64,
    bytes_published: AtomicU64,
    read_wait_ns: Arc<AtomicU64>,
    hash_ns: AtomicU64,
    publish_wait_ns: AtomicU64,
}

struct NativeShared {
    state: Mutex<NativeState>,
    changed: Condvar,
    telemetry: NativeTelemetry,
}

struct NativeConfig {
    slot_size: usize,
    read_chunk_size: usize,
    queue_depth: usize,
    capacity_bytes: u64,
    small_open_concurrency: usize,
    small_active_files: usize,
    small_inflight_bytes: usize,
    small_threshold: u64,
    hash_mask: u32,
    next_file_prime_depth: usize,
}

pub struct LfrContext {
    shared: Arc<NativeShared>,
    config: NativeConfig,
    cancel_event: Handle,
    worker: Mutex<Option<JoinHandle<()>>>,
}

fn cancelled_error() -> io::Error {
    io::Error::new(io::ErrorKind::Interrupted, "fastreader operation cancelled")
}

unsafe fn is_cancelled(cancel_event: HANDLE) -> bool {
    !cancel_event.is_null() && WaitForSingleObject(cancel_event, 0) == WAIT_OBJECT_0
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
unsafe impl Send for Handle {}

#[derive(Clone, Copy)]
struct SessionEvents {
    data: HANDLE,
    space: HANDLE,
    cancel: HANDLE,
}

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
                UnmapViewOfFile(MEMORY_MAPPED_VIEW_ADDRESS {
                    Value: self.base.cast(),
                });
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
        let handle = CreateFileMappingW(
            INVALID_HANDLE_VALUE,
            null(),
            PAGE_READWRITE,
            (size as u64 >> 32) as u32,
            size as u32,
            name_w.as_ptr(),
        );
        if handle.is_null() {
            return Err(io::Error::last_os_error());
        }
        let base = MapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, size).Value as *mut u8;
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
        Ok(Self {
            _handle: Handle(handle),
            base,
            size,
            slot_count,
            slot_size: slot_size as usize,
            data_offset,
        })
    }

    unsafe fn slot_meta(&self, idx: u64) -> *mut u8 {
        self.base
            .add(HEADER_SIZE + (idx as usize % self.slot_count as usize) * SLOT_META_SIZE)
    }

    unsafe fn slot_data(&self, idx: u64) -> *mut u8 {
        self.base
            .add(self.data_offset + (idx as usize % self.slot_count as usize) * self.slot_size)
    }

    unsafe fn wait_free_slot(&self, space_event: HANDLE, cancel_event: HANDLE) -> io::Result<u64> {
        loop {
            if is_cancelled(cancel_event) {
                return Err(cancelled_error());
            }
            let write_idx = read_u64(self.base, 32);
            let meta = self.slot_meta(write_idx);
            if read_u32(meta, 0) == STATUS_EMPTY {
                return Ok(write_idx);
            }
            let handles = [cancel_event, space_event];
            let wait = WaitForMultipleObjects(handles.len() as u32, handles.as_ptr(), 0, INFINITE);
            if wait == WAIT_OBJECT_0 {
                return Err(cancelled_error());
            }
            if wait != WAIT_OBJECT_0 + 1 {
                return Err(io::Error::last_os_error());
            }
        }
    }
}

struct Xxh3_64 {
    h: xxhash_rust::xxh3::Xxh3,
}
impl Xxh3_64 {
    fn new() -> Self {
        Self {
            h: xxhash_rust::xxh3::Xxh3::new(),
        }
    }
    fn update(&mut self, data: &[u8]) {
        self.h.update(data);
    }
    fn finish(&self) -> [u8; 8] {
        self.h.digest().to_be_bytes()
    }
}

struct Xxh3_128 {
    h: xxhash_rust::xxh3::Xxh3,
}
impl Xxh3_128 {
    fn new() -> Self {
        Self {
            h: xxhash_rust::xxh3::Xxh3::new(),
        }
    }
    fn update(&mut self, data: &[u8]) {
        self.h.update(data);
    }
    fn finish(&self) -> [u8; 16] {
        self.h.digest128().to_be_bytes()
    }
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
            sha1: if *enabled.get("SHA1").unwrap_or(&false) {
                Some(Sha1::new())
            } else {
                None
            },
            sha256: if *enabled.get("SHA256").unwrap_or(&false) {
                Some(Sha256::new())
            } else {
                None
            },
            sha512: if *enabled.get("SHA512").unwrap_or(&false) {
                Some(Sha512::new())
            } else {
                None
            },
            md5: if *enabled.get("MD5").unwrap_or(&false) {
                Some(Md5::new())
            } else {
                None
            },
            crc32: if *enabled.get("CRC32").unwrap_or(&false) {
                Some(crc32fast::Hasher::new())
            } else {
                None
            },
            blake3: if *enabled.get("BLAKE3").unwrap_or(&false) {
                Some(blake3::Hasher::new())
            } else {
                None
            },
            xxh3: if *enabled.get("XxHash3").unwrap_or(&false) {
                Some(Xxh3_64::new())
            } else {
                None
            },
            xxh128: if *enabled.get("XxHash128").unwrap_or(&false) {
                Some(Xxh3_128::new())
            } else {
                None
            },
        })
    }

    fn update(&mut self, slice: &[u8]) -> io::Result<()> {
        if let Some(h) = self.sha1.as_mut() {
            h.update(slice);
        }
        if let Some(h) = self.sha256.as_mut() {
            h.update(slice);
        }
        if let Some(h) = self.sha512.as_mut() {
            h.update(slice);
        }
        if let Some(h) = self.md5.as_mut() {
            h.update(slice);
        }
        if let Some(c) = self.crc32.as_mut() {
            c.update(slice);
        }
        if let Some(h) = self.blake3.as_mut() {
            h.update(slice);
        }
        if let Some(h) = self.xxh3.as_mut() {
            h.update(slice);
        }
        if let Some(h) = self.xxh128.as_mut() {
            h.update(slice);
        }
        Ok(())
    }

    fn finish(&mut self) -> io::Result<String> {
        let mut parts = Vec::new();
        if let Some(h) = self.sha1.take() {
            parts.push(format!("SHA1={}", hex(&h.finalize())));
        }
        if let Some(h) = self.sha256.take() {
            parts.push(format!("SHA256={}", hex(&h.finalize())));
        }
        if let Some(h) = self.sha512.take() {
            parts.push(format!("SHA512={}", hex(&h.finalize())));
        }
        if let Some(h) = self.md5.take() {
            parts.push(format!("MD5={}", hex(&h.finalize())));
        }
        if let Some(c) = self.crc32.take() {
            parts.push(format!("CRC32={}", hex(&c.finalize().to_be_bytes())));
        }
        if let Some(h) = self.blake3.as_ref() {
            parts.push(format!(
                "BLAKE3={}",
                h.finalize().to_hex().to_string().to_uppercase()
            ));
        }
        if let Some(h) = self.xxh3.as_ref() {
            parts.push(format!("XxHash3={}", hex(&h.finish())));
        }
        if let Some(h) = self.xxh128.as_ref() {
            parts.push(format!("XxHash128={}", hex(&h.finish())));
        }
        Ok(parts.join("\t"))
    }
}

fn parse_bool(v: Option<&String>) -> bool {
    matches!(
        v.map(|s| s.as_str()),
        Some("1") | Some("true") | Some("True")
    )
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
    if !value.len().is_multiple_of(4) {
        return Err(io::Error::new(
            io::ErrorKind::InvalidInput,
            "bad path encoding length",
        ));
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
        _ => Err(io::Error::new(
            io::ErrorKind::InvalidInput,
            "bad hex path encoding",
        )),
    }
}

#[derive(Clone, Copy)]
enum RequestState {
    Idle,
    InFlight,
    Completed(Result<u32, u32>),
}

struct ReadRequest {
    overlapped: OVERLAPPED,
    buffer: Vec<u8>,
    offset: u64,
    requested: u32,
    state: RequestState,
}

impl ReadRequest {
    fn new(chunk_size: usize) -> Self {
        Self {
            overlapped: OVERLAPPED::default(),
            buffer: vec![0u8; chunk_size],
            offset: 0,
            requested: 0,
            state: RequestState::Idle,
        }
    }
}

struct AsyncSequentialReader {
    file: Handle,
    completion_port: Handle,
    requests: Vec<ReadRequest>,
    file_len: u64,
    chunk_size: usize,
    next_submit: u64,
    next_consume: u64,
    outstanding: usize,
    cancel_event: HANDLE,
    read_wait_counter: Option<Arc<AtomicU64>>,
}

fn completed_request_at(requests: &[ReadRequest], offset: u64) -> Option<usize> {
    requests.iter().position(|request| {
        request.offset == offset && matches!(request.state, RequestState::Completed(_))
    })
}

impl AsyncSequentialReader {
    unsafe fn open(
        path: &str,
        expected_len: u64,
        chunk_size: usize,
        cancel_event: HANDLE,
    ) -> io::Result<Self> {
        Self::open_with_depth(path, expected_len, chunk_size, cancel_event, IO_QUEUE_DEPTH)
    }

    unsafe fn open_with_depth(
        path: &str,
        expected_len: u64,
        chunk_size: usize,
        cancel_event: HANDLE,
        queue_depth: usize,
    ) -> io::Result<Self> {
        if is_cancelled(cancel_event) {
            return Err(cancelled_error());
        }
        if chunk_size == 0 || chunk_size > u32::MAX as usize {
            return Err(io::Error::new(
                io::ErrorKind::InvalidInput,
                "invalid asynchronous read chunk size",
            ));
        }

        let path_w = wide(path);
        let file = CreateFileW(
            path_w.as_ptr(),
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            null(),
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED | FILE_FLAG_SEQUENTIAL_SCAN,
            null_mut(),
        );
        if file == INVALID_HANDLE_VALUE {
            return Err(io::Error::last_os_error());
        }
        let file = Handle(file);

        let mut actual_len = 0i64;
        if GetFileSizeEx(file.0, &mut actual_len) == 0 {
            return Err(io::Error::last_os_error());
        }
        if actual_len < 0 || actual_len as u64 != expected_len {
            return Err(io::Error::new(
                io::ErrorKind::InvalidData,
                format!("file length changed: expected={expected_len} actual={actual_len}"),
            ));
        }

        let completion_port = CreateIoCompletionPort(file.0, null_mut(), 0, 0);
        if completion_port.is_null() {
            return Err(io::Error::last_os_error());
        }
        let completion_port = Handle(completion_port);
        let request_count = if expected_len == 0 {
            0
        } else {
            expected_len
                .div_ceil(chunk_size as u64)
                .min(queue_depth.clamp(1, 128) as u64) as usize
        };
        let request_buffer_size = expected_len.min(chunk_size as u64) as usize;
        let requests = (0..request_count)
            .map(|_| ReadRequest::new(request_buffer_size))
            .collect();

        Ok(Self {
            file,
            completion_port,
            requests,
            file_len: expected_len,
            chunk_size,
            next_submit: 0,
            next_consume: 0,
            outstanding: 0,
            cancel_event,
            read_wait_counter: None,
        })
    }

    unsafe fn submit(&mut self, request_index: usize) -> io::Result<()> {
        if is_cancelled(self.cancel_event) {
            return Err(cancelled_error());
        }
        if self.next_submit >= self.file_len {
            return Ok(());
        }
        let request = &mut self.requests[request_index];
        debug_assert!(matches!(request.state, RequestState::Idle));
        let offset = self.next_submit;
        let requested = (self.file_len - offset).min(self.chunk_size as u64) as u32;
        request.overlapped = OVERLAPPED::default();
        request.overlapped.Anonymous.Anonymous.Offset = offset as u32;
        request.overlapped.Anonymous.Anonymous.OffsetHigh = (offset >> 32) as u32;
        request.offset = offset;
        request.requested = requested;
        request.state = RequestState::InFlight;

        let ok = ReadFile(
            self.file.0,
            request.buffer.as_mut_ptr(),
            requested,
            null_mut(),
            &mut request.overlapped,
        );
        if ok == 0 {
            let error = io::Error::last_os_error();
            if error.raw_os_error() != Some(ERROR_IO_PENDING as i32) {
                request.state = RequestState::Idle;
                return Err(error);
            }
        }
        self.next_submit += requested as u64;
        self.outstanding += 1;
        Ok(())
    }

    unsafe fn prime(&mut self) -> io::Result<()> {
        self.prime_limit(self.requests.len())
    }

    unsafe fn prime_limit(&mut self, limit: usize) -> io::Result<()> {
        let mut submitted = 0usize;
        for index in 0..self.requests.len() {
            if submitted >= limit || self.next_submit >= self.file_len {
                break;
            }
            if !matches!(self.requests[index].state, RequestState::Idle) {
                continue;
            }
            self.submit(index)?;
            submitted += 1;
        }
        Ok(())
    }

    unsafe fn receive_completions(&mut self) -> io::Result<()> {
        let wait_started = Instant::now();
        let mut entries = [OVERLAPPED_ENTRY::default(); IO_QUEUE_DEPTH];
        let mut removed = 0u32;
        if GetQueuedCompletionStatusEx(
            self.completion_port.0,
            entries.as_mut_ptr(),
            entries.len() as u32,
            &mut removed,
            CANCEL_POLL_MS,
            0,
        ) == 0
        {
            self.record_read_wait(wait_started.elapsed());
            let error = io::Error::last_os_error();
            if error.raw_os_error() == Some(WAIT_TIMEOUT as i32) {
                if is_cancelled(self.cancel_event) {
                    return Err(cancelled_error());
                }
                return Ok(());
            }
            return Err(error);
        }
        self.record_read_wait(wait_started.elapsed());

        for entry in &entries[..removed as usize] {
            let Some(request) = self
                .requests
                .iter_mut()
                .find(|request| std::ptr::eq(&request.overlapped, entry.lpOverlapped))
            else {
                return Err(io::Error::new(
                    io::ErrorKind::InvalidData,
                    "unknown IOCP completion",
                ));
            };
            if !matches!(request.state, RequestState::InFlight) {
                return Err(io::Error::new(
                    io::ErrorKind::InvalidData,
                    "duplicate IOCP completion",
                ));
            }
            let mut transferred = 0u32;
            let completion = if GetOverlappedResult(
                self.file.0,
                &request.overlapped,
                &mut transferred,
                0,
            ) != 0
            {
                Ok(transferred)
            } else {
                Err(io::Error::last_os_error().raw_os_error().unwrap_or(1) as u32)
            };
            request.state = RequestState::Completed(completion);
            self.outstanding -= 1;
        }
        Ok(())
    }

    fn set_read_wait_counter(&mut self, counter: Arc<AtomicU64>) {
        self.read_wait_counter = Some(counter);
    }

    fn record_read_wait(&self, elapsed: Duration) {
        if let Some(counter) = self.read_wait_counter.as_ref() {
            counter.fetch_add(
                elapsed.as_nanos().min(u64::MAX as u128) as u64,
                Ordering::Relaxed,
            );
        }
    }

    unsafe fn run<F>(&mut self, mut consume: F) -> io::Result<()>
    where
        F: FnMut(u64, &[u8]) -> io::Result<()>,
    {
        // A reader may already contain a few requests submitted while the
        // preceding file is being consumed. Fill the rest of the queue here.
        self.prime()?;
        while self.next_consume < self.file_len {
            if is_cancelled(self.cancel_event) {
                return Err(cancelled_error());
            }
            let next_ready = completed_request_at(&self.requests, self.next_consume);
            let Some(index) = next_ready else {
                self.receive_completions()?;
                continue;
            };

            let result = match self.requests[index].state {
                RequestState::Completed(result) => result,
                _ => unreachable!(),
            };
            let transferred = result.map_err(|code| io::Error::from_raw_os_error(code as i32))?;
            let requested = self.requests[index].requested;
            if transferred != requested {
                return Err(io::Error::new(
                    io::ErrorKind::UnexpectedEof,
                    format!(
                        "short asynchronous read at offset {}: expected={} actual={}",
                        self.next_consume, requested, transferred
                    ),
                ));
            }
            consume(
                self.requests[index].offset,
                &self.requests[index].buffer[..transferred as usize],
            )?;
            self.next_consume += transferred as u64;
            self.requests[index].state = RequestState::Idle;
            self.submit(index)?;
        }
        Ok(())
    }

    unsafe fn cancel_and_drain(&mut self) {
        if self.outstanding == 0 {
            return;
        }
        if CancelIoEx(self.file.0, null()) == 0 {
            let code = io::Error::last_os_error().raw_os_error();
            if code != Some(ERROR_NOT_FOUND as i32) {
                // Continue draining: already completed requests may still have queued packets.
            }
        }
        let mut entries = [OVERLAPPED_ENTRY::default(); IO_QUEUE_DEPTH];
        while self.outstanding > 0 {
            let mut removed = 0u32;
            if GetQueuedCompletionStatusEx(
                self.completion_port.0,
                entries.as_mut_ptr(),
                entries.len() as u32,
                &mut removed,
                INFINITE,
                0,
            ) == 0
            {
                // Preserve the request storage if Windows cannot confirm cancellation.
                let leaked = std::mem::take(&mut self.requests);
                std::mem::forget(leaked);
                return;
            }
            self.outstanding = self.outstanding.saturating_sub(removed as usize);
        }
    }
}

impl Drop for AsyncSequentialReader {
    fn drop(&mut self) {
        unsafe {
            self.cancel_and_drain();
        }
    }
}

unsafe fn read_file_overlapped<F>(
    path: &str,
    expected_len: u64,
    chunk_size: usize,
    cancel_event: HANDLE,
    consume: F,
) -> io::Result<()>
where
    F: FnMut(u64, &[u8]) -> io::Result<()>,
{
    let mut reader = AsyncSequentialReader::open(path, expected_len, chunk_size, cancel_event)?;
    reader.run(consume)
}

const SMALL_BUFFER_CLASSES: [usize; 7] = [
    4 * 1024,
    16 * 1024,
    64 * 1024,
    256 * 1024,
    1024 * 1024,
    2 * 1024 * 1024,
    4 * 1024 * 1024,
];

#[derive(Clone)]
struct SmallFileTask {
    index: u64,
    len: u64,
    path: String,
}

enum SmallFileStatus {
    Pending,
    Opening,
    InFlight,
    Ready {
        data: Vec<u8>,
        digest: Option<String>,
        reserved: usize,
    },
    Failed(String),
    Borrowed,
}

struct SmallFileEntry {
    task: SmallFileTask,
    status: SmallFileStatus,
    attempts: u8,
    discard: bool,
}

struct SmallFileState {
    entries: HashMap<u64, SmallFileEntry>,
    queue: VecDeque<u64>,
    buffers: HashMap<usize, Vec<Vec<u8>>>,
    active_files: usize,
    reserved_bytes: usize,
    max_active_files: usize,
    max_reserved_bytes: usize,
    shutdown: bool,
    fatal_error: Option<String>,
}

impl SmallFileState {
    fn new() -> Self {
        Self {
            entries: HashMap::new(),
            queue: VecDeque::new(),
            buffers: HashMap::new(),
            active_files: 0,
            reserved_bytes: 0,
            max_active_files: 0,
            max_reserved_bytes: 0,
            shutdown: false,
            fatal_error: None,
        }
    }

    fn take_buffer(&mut self, capacity: usize) -> Vec<u8> {
        if capacity == 0 {
            return Vec::new();
        }
        let mut buffer = self
            .buffers
            .get_mut(&capacity)
            .and_then(Vec::pop)
            .unwrap_or_else(|| vec![0u8; capacity]);
        buffer.resize(capacity, 0);
        buffer
    }

    fn return_buffer(&mut self, mut buffer: Vec<u8>, capacity: usize) {
        if capacity == 0 {
            return;
        }
        buffer.clear();
        self.buffers.entry(capacity).or_default().push(buffer);
    }
}

#[derive(Clone, Copy)]
struct SharedHandle(HANDLE);

unsafe impl Send for SharedHandle {}
unsafe impl Sync for SharedHandle {}

struct SmallOperation {
    overlapped: OVERLAPPED,
    file: Handle,
    buffer: Vec<u8>,
    index: u64,
    expected: u32,
    reserved: usize,
}

unsafe impl Send for SmallOperation {}

struct SmallShared {
    state: Mutex<SmallFileState>,
    changed: Condvar,
    operations: Mutex<HashMap<usize, Box<SmallOperation>>>,
    completion_port: SharedHandle,
    active_limit: usize,
    inflight_byte_limit: usize,
    completion_batch: usize,
    cancel_event: SharedHandle,
}

struct CachedSmallFile {
    data: Vec<u8>,
    digest: Option<String>,
    reserved: usize,
}

struct SmallFilePool {
    shared: Arc<SmallShared>,
    completion_port: Handle,
    workers: Vec<JoinHandle<()>>,
    completion_thread: Option<JoinHandle<()>>,
}

#[derive(Clone)]
struct SmallFilePoolControl {
    shared: Arc<SmallShared>,
}

impl SmallFilePoolControl {
    fn enqueue(&self, task: SmallFileTask, priority: bool) {
        if small_buffer_capacity(task.len).is_none() {
            return;
        }
        let mut state = self.shared.state.lock().unwrap();
        if state.shutdown || state.fatal_error.is_some() || state.entries.contains_key(&task.index)
        {
            return;
        }
        let index = task.index;
        state.entries.insert(
            index,
            SmallFileEntry {
                task,
                status: SmallFileStatus::Pending,
                attempts: 0,
                discard: false,
            },
        );
        if priority {
            state.queue.push_front(index);
        } else {
            state.queue.push_back(index);
        }
        self.shared.changed.notify_all();
    }
}

fn small_buffer_capacity(len: u64) -> Option<usize> {
    if len == 0 {
        return Some(0);
    }
    SMALL_BUFFER_CLASSES
        .iter()
        .copied()
        .find(|capacity| len <= *capacity as u64)
}

fn small_failure(
    shared: &SmallShared,
    index: u64,
    buffer: Vec<u8>,
    reserved: usize,
    message: String,
) {
    let mut state = shared.state.lock().unwrap();
    state.active_files = state.active_files.saturating_sub(1);
    state.reserved_bytes = state.reserved_bytes.saturating_sub(reserved);
    state.return_buffer(buffer, reserved);
    let discard = state.shutdown
        || state
            .entries
            .get(&index)
            .map(|entry| entry.discard)
            .unwrap_or(true);
    if discard {
        state.entries.remove(&index);
    } else if let Some(entry) = state.entries.get_mut(&index) {
        entry.status = SmallFileStatus::Failed(message);
    }
    shared.changed.notify_all();
}

fn small_open_worker(shared: Arc<SmallShared>) {
    loop {
        let (task, reserved, buffer) = {
            let mut state = shared.state.lock().unwrap();
            loop {
                if state.shutdown {
                    return;
                }
                if state.fatal_error.is_some() {
                    state = shared.changed.wait(state).unwrap();
                    continue;
                }
                if state.active_files < shared.active_limit {
                    let position = state.queue.iter().position(|index| {
                        let Some(entry) = state.entries.get(index) else {
                            return false;
                        };
                        let Some(capacity) = small_buffer_capacity(entry.task.len) else {
                            return false;
                        };
                        matches!(entry.status, SmallFileStatus::Pending)
                            && state.reserved_bytes + capacity <= shared.inflight_byte_limit
                    });
                    if let Some(position) = position {
                        let index = state.queue.remove(position).unwrap();
                        let (task, reserved) = {
                            let entry = state.entries.get_mut(&index).unwrap();
                            entry.status = SmallFileStatus::Opening;
                            entry.attempts += 1;
                            (
                                entry.task.clone(),
                                small_buffer_capacity(entry.task.len).unwrap(),
                            )
                        };
                        state.active_files += 1;
                        state.reserved_bytes += reserved;
                        state.max_active_files = state.max_active_files.max(state.active_files);
                        state.max_reserved_bytes =
                            state.max_reserved_bytes.max(state.reserved_bytes);
                        let buffer = state.take_buffer(reserved);
                        break (task, reserved, buffer);
                    }
                }
                state = shared.changed.wait(state).unwrap();
            }
        };

        let path_w = wide(&task.path);
        let raw_file = unsafe {
            CreateFileW(
                path_w.as_ptr(),
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                null(),
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
                null_mut(),
            )
        };
        if raw_file == INVALID_HANDLE_VALUE {
            small_failure(
                &shared,
                task.index,
                buffer,
                reserved,
                io::Error::last_os_error().to_string(),
            );
            continue;
        }
        let file = Handle(raw_file);

        if shared.state.lock().unwrap().shutdown {
            small_failure(
                &shared,
                task.index,
                buffer,
                reserved,
                "reader shutting down".into(),
            );
            drop(file);
            continue;
        }

        if task.len == 0 {
            let mut state = shared.state.lock().unwrap();
            state.active_files = state.active_files.saturating_sub(1);
            if let Some(entry) = state.entries.get_mut(&task.index) {
                if entry.discard {
                    state.entries.remove(&task.index);
                } else {
                    entry.status = SmallFileStatus::Ready {
                        data: buffer,
                        digest: None,
                        reserved,
                    };
                }
            }
            shared.changed.notify_all();
            continue;
        }

        let associated = unsafe {
            CreateIoCompletionPort(raw_file, shared.completion_port.0, task.index as usize, 0)
        };
        if associated.is_null() {
            small_failure(
                &shared,
                task.index,
                buffer,
                reserved,
                io::Error::last_os_error().to_string(),
            );
            continue;
        }

        let mut operation = Box::new(SmallOperation {
            overlapped: OVERLAPPED::default(),
            file,
            buffer,
            index: task.index,
            expected: task.len as u32,
            reserved,
        });
        let operation_key = (&mut operation.overlapped as *mut OVERLAPPED) as usize;
        {
            let mut state = shared.state.lock().unwrap();
            if let Some(entry) = state.entries.get_mut(&task.index) {
                entry.status = SmallFileStatus::InFlight;
            }
        }
        let mut operations = shared.operations.lock().unwrap();
        operations.insert(operation_key, operation);
        let operation = operations.get_mut(&operation_key).unwrap();
        let ok = unsafe {
            ReadFile(
                operation.file.0,
                operation.buffer.as_mut_ptr(),
                operation.expected,
                null_mut(),
                &mut operation.overlapped,
            )
        };
        if ok == 0 {
            let error = io::Error::last_os_error();
            if error.raw_os_error() != Some(ERROR_IO_PENDING as i32) {
                let operation = operations.remove(&operation_key).unwrap();
                drop(operations);
                small_failure(
                    &shared,
                    task.index,
                    operation.buffer,
                    reserved,
                    error.to_string(),
                );
            }
        }
    }
}

fn small_completion_worker(shared: Arc<SmallShared>) {
    let mut entries = vec![OVERLAPPED_ENTRY::default(); shared.completion_batch];
    loop {
        let mut removed = 0u32;
        let ok = unsafe {
            GetQueuedCompletionStatusEx(
                shared.completion_port.0,
                entries.as_mut_ptr(),
                entries.len() as u32,
                &mut removed,
                INFINITE,
                0,
            )
        };
        if ok == 0 {
            let error = io::Error::last_os_error();
            let mut report_error = false;
            {
                let mut state = shared.state.lock().unwrap();
                if state.fatal_error.is_none() {
                    state.fatal_error = Some(format!("small-file IOCP failed: {error}"));
                    report_error = true;
                }
                shared.changed.notify_all();
            }
            if report_error {
                eprintln!("IOCP_ERROR\t{error}");
                io::stderr().flush().ok();
                let operations = shared.operations.lock().unwrap();
                for operation in operations.values() {
                    unsafe {
                        CancelIoEx(operation.file.0, null());
                    }
                }
            }
            if shared.state.lock().unwrap().shutdown && shared.operations.lock().unwrap().is_empty()
            {
                return;
            }
            thread::sleep(Duration::from_millis(10));
            continue;
        }

        for completion in &entries[..removed as usize] {
            if completion.lpOverlapped.is_null() {
                continue;
            }
            let key = completion.lpOverlapped as usize;
            let mut operation = {
                let mut operations = shared.operations.lock().unwrap();
                let Some(operation) = operations.remove(&key) else {
                    continue;
                };
                operation
            };
            let mut transferred = 0u32;
            let result = if unsafe {
                GetOverlappedResult(operation.file.0, &operation.overlapped, &mut transferred, 0)
            } != 0
            {
                if transferred == operation.expected {
                    Ok(())
                } else {
                    Err(format!(
                        "short asynchronous read: expected={} actual={transferred}",
                        operation.expected
                    ))
                }
            } else {
                Err(io::Error::last_os_error().to_string())
            };
            operation.buffer.truncate(transferred as usize);

            let mut state = shared.state.lock().unwrap();
            state.active_files = state.active_files.saturating_sub(1);
            let discard = state.shutdown
                || state
                    .entries
                    .get(&operation.index)
                    .map(|entry| entry.discard)
                    .unwrap_or(true);
            if discard {
                state.reserved_bytes = state.reserved_bytes.saturating_sub(operation.reserved);
                state.return_buffer(operation.buffer, operation.reserved);
                state.entries.remove(&operation.index);
            } else {
                match result {
                    Ok(()) => {
                        if let Some(entry) = state.entries.get_mut(&operation.index) {
                            entry.status = SmallFileStatus::Ready {
                                data: operation.buffer,
                                digest: None,
                                reserved: operation.reserved,
                            };
                        }
                    }
                    Err(message) => {
                        state.reserved_bytes =
                            state.reserved_bytes.saturating_sub(operation.reserved);
                        state.return_buffer(operation.buffer, operation.reserved);
                        if let Some(entry) = state.entries.get_mut(&operation.index) {
                            entry.status = SmallFileStatus::Failed(message);
                        }
                    }
                }
            }
            shared.changed.notify_all();
        }

        if shared.state.lock().unwrap().shutdown && shared.operations.lock().unwrap().is_empty() {
            return;
        }
    }
}

impl SmallFilePool {
    unsafe fn new(
        open_concurrency: usize,
        active_limit: usize,
        inflight_byte_limit: usize,
        completion_batch: usize,
        cancel_event: HANDLE,
    ) -> io::Result<Self> {
        let raw_port = CreateIoCompletionPort(INVALID_HANDLE_VALUE, null_mut(), 0, 0);
        if raw_port.is_null() {
            return Err(io::Error::last_os_error());
        }
        let completion_port = Handle(raw_port);
        let shared = Arc::new(SmallShared {
            state: Mutex::new(SmallFileState::new()),
            changed: Condvar::new(),
            operations: Mutex::new(HashMap::new()),
            completion_port: SharedHandle(raw_port),
            active_limit: active_limit.max(1),
            inflight_byte_limit: inflight_byte_limit.max(64 * 1024),
            completion_batch: completion_batch.clamp(1, 128),
            cancel_event: SharedHandle(cancel_event),
        });
        let workers = (0..open_concurrency.max(1))
            .map(|_| {
                let shared = Arc::clone(&shared);
                thread::spawn(move || small_open_worker(shared))
            })
            .collect();
        let completion_shared = Arc::clone(&shared);
        let completion_thread = Some(thread::spawn(move || {
            small_completion_worker(completion_shared)
        }));
        Ok(Self {
            shared,
            completion_port,
            workers,
            completion_thread,
        })
    }

    fn enqueue(&self, task: SmallFileTask, priority: bool) {
        self.control().enqueue(task, priority);
    }

    fn control(&self) -> SmallFilePoolControl {
        SmallFilePoolControl {
            shared: Arc::clone(&self.shared),
        }
    }

    fn discard_before(&self, index: u64) {
        let mut state = self.shared.state.lock().unwrap();
        let keys = state
            .entries
            .keys()
            .copied()
            .filter(|existing| *existing < index)
            .collect::<Vec<_>>();
        for key in keys {
            let removable = matches!(
                state.entries.get(&key).map(|entry| &entry.status),
                Some(SmallFileStatus::Pending)
                    | Some(SmallFileStatus::Ready { .. })
                    | Some(SmallFileStatus::Failed(_))
            );
            if removable {
                if let Some(entry) = state.entries.remove(&key) {
                    if let SmallFileStatus::Ready { data, reserved, .. } = entry.status {
                        state.reserved_bytes = state.reserved_bytes.saturating_sub(reserved);
                        state.return_buffer(data, reserved);
                    }
                }
                state.queue.retain(|queued| *queued != key);
            } else if let Some(entry) = state.entries.get_mut(&key) {
                entry.discard = true;
            }
        }
        self.shared.changed.notify_all();
    }

    fn wait_take(&self, task: SmallFileTask) -> io::Result<CachedSmallFile> {
        self.enqueue(task.clone(), true);
        let mut state = self.shared.state.lock().unwrap();
        loop {
            if unsafe { is_cancelled(self.shared.cancel_event.0) } {
                return Err(cancelled_error());
            }
            if let Some(message) = state.fatal_error.as_ref() {
                return Err(io::Error::other(message.clone()));
            }
            let Some(entry) = state.entries.get_mut(&task.index) else {
                drop(state);
                self.enqueue(task.clone(), true);
                state = self.shared.state.lock().unwrap();
                continue;
            };
            match &entry.status {
                SmallFileStatus::Ready { .. } => {
                    let status = std::mem::replace(&mut entry.status, SmallFileStatus::Borrowed);
                    if let SmallFileStatus::Ready {
                        data,
                        digest,
                        reserved,
                    } = status
                    {
                        return Ok(CachedSmallFile {
                            data,
                            digest,
                            reserved,
                        });
                    }
                }
                SmallFileStatus::Failed(message) if entry.attempts >= 2 => {
                    return Err(io::Error::other(message.clone()));
                }
                SmallFileStatus::Failed(_) => {
                    entry.status = SmallFileStatus::Pending;
                    state.queue.push_front(task.index);
                    self.shared.changed.notify_all();
                }
                _ => {
                    state = self
                        .shared
                        .changed
                        .wait_timeout(state, Duration::from_millis(CANCEL_POLL_MS as u64))
                        .unwrap()
                        .0;
                }
            }
        }
    }

    fn put_back(&self, index: u64, cached: CachedSmallFile) {
        let mut state = self.shared.state.lock().unwrap();
        if let Some(entry) = state.entries.get_mut(&index) {
            entry.status = SmallFileStatus::Ready {
                data: cached.data,
                digest: cached.digest,
                reserved: cached.reserved,
            };
        }
        self.shared.changed.notify_all();
    }

    fn release(&self, index: u64, cached: CachedSmallFile) {
        let mut state = self.shared.state.lock().unwrap();
        state.entries.remove(&index);
        state.reserved_bytes = state.reserved_bytes.saturating_sub(cached.reserved);
        state.return_buffer(cached.data, cached.reserved);
        self.shared.changed.notify_all();
    }

    unsafe fn shutdown(&mut self) {
        {
            let mut state = self.shared.state.lock().unwrap();
            state.shutdown = true;
            self.shared.changed.notify_all();
        }
        for worker in self.workers.drain(..) {
            let _ = worker.join();
        }
        {
            let operations = self.shared.operations.lock().unwrap();
            for operation in operations.values() {
                CancelIoEx(operation.file.0, null());
            }
        }
        PostQueuedCompletionStatus(self.completion_port.0, 0, 0, null());
        if let Some(completion_thread) = self.completion_thread.take() {
            let join_result = completion_thread.join();
            let mut operations = self
                .shared
                .operations
                .lock()
                .unwrap_or_else(|poisoned| poisoned.into_inner());
            if join_result.is_err() || !operations.is_empty() {
                for (_, operation) in operations.drain() {
                    std::mem::forget(operation);
                }
            }
        }
    }
}

impl Drop for SmallFilePool {
    fn drop(&mut self) {
        unsafe { self.shutdown() }
    }
}

fn parse_file_task(rest: &str, kind: &str) -> io::Result<SmallFileTask> {
    let mut parts = rest.splitn(3, '\t');
    let index = parts
        .next()
        .and_then(|value| value.parse().ok())
        .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, format!("bad {kind} index")))?;
    let len = parts
        .next()
        .and_then(|value| value.parse().ok())
        .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, format!("bad {kind} length")))?;
    let path = decode_path_hex(parts.next().ok_or_else(|| {
        io::Error::new(io::ErrorKind::InvalidInput, format!("missing {kind} path"))
    })?)?;
    Ok(SmallFileTask { index, len, path })
}

enum SessionCommand {
    File(SmallFileTask),
    Hash(SmallFileTask),
    Done,
    ProtocolError(String),
}

fn receive_commands(
    sender: mpsc::Sender<SessionCommand>,
    small_pool: SmallFilePoolControl,
    small_threshold: u64,
) {
    for line in io::stdin().lock().lines() {
        let line = match line {
            Ok(line) => line,
            Err(error) => {
                let _ = sender.send(SessionCommand::ProtocolError(error.to_string()));
                return;
            }
        };
        if line == "DONE" {
            let _ = sender.send(SessionCommand::Done);
            return;
        }
        let parsed = if let Some(rest) = line.strip_prefix("PREFETCH\t") {
            parse_file_task(rest, "prefetch").map(|task| {
                if task.len <= small_threshold {
                    small_pool.enqueue(task, false);
                }
                None
            })
        } else if let Some(rest) = line.strip_prefix("FILE\t") {
            parse_file_task(rest, "file").map(|task| {
                if task.len <= small_threshold {
                    small_pool.enqueue(task.clone(), false);
                }
                Some(SessionCommand::File(task))
            })
        } else if let Some(rest) = line.strip_prefix("HASH\t") {
            parse_file_task(rest, "hash").map(|task| {
                if task.len <= small_threshold {
                    small_pool.enqueue(task.clone(), false);
                }
                Some(SessionCommand::Hash(task))
            })
        } else {
            Err(io::Error::new(
                io::ErrorKind::InvalidInput,
                format!("unknown command: {line}"),
            ))
        };
        match parsed {
            Ok(Some(command)) => {
                if sender.send(command).is_err() {
                    return;
                }
            }
            Ok(None) => {}
            Err(error) => {
                let _ = sender.send(SessionCommand::ProtocolError(error.to_string()));
                return;
            }
        }
    }
    let _ = sender.send(SessionCommand::Done);
}

fn digest_bytes(data: &[u8], enabled: &HashMap<String, bool>) -> io::Result<String> {
    let mut hashes = HashSet::new(enabled)?;
    hashes.update(data)?;
    hashes.finish()
}

unsafe fn publish_error_slot(
    mapping: &Mapping,
    events: SessionEvents,
    file_index: u64,
    file_offset: u64,
) -> io::Result<()> {
    let idx = mapping.wait_free_slot(events.space, events.cancel)?;
    let meta = mapping.slot_meta(idx);
    write_u32(meta, 4, FLAG_ERROR);
    write_u64(meta, 8, file_index);
    write_u64(meta, 16, file_offset);
    write_u32(meta, 24, 0);
    write_u32(meta, 0, STATUS_FULL);
    write_u64(mapping.base, 32, idx + 1);
    SetEvent(events.data);
    Ok(())
}

unsafe fn fill_cached_file(
    pool: &SmallFilePool,
    mapping: &Mapping,
    events: SessionEvents,
    task: SmallFileTask,
    enabled: &HashMap<String, bool>,
) -> io::Result<()> {
    let mut cached = match pool.wait_take(task.clone()) {
        Ok(cached) => cached,
        Err(error) => {
            if error.kind() != io::ErrorKind::Interrupted {
                publish_error_slot(mapping, events, task.index, 0)?;
            }
            return Err(error);
        }
    };
    let result = (|| -> io::Result<()> {
        let digest = match cached.digest.take() {
            Some(digest) => digest,
            None => digest_bytes(&cached.data, enabled)?,
        };
        let mut file_offset = 0u64;
        for slice in cached.data.chunks(mapping.slot_size) {
            let idx = mapping.wait_free_slot(events.space, events.cancel)?;
            let meta = mapping.slot_meta(idx);
            let data = mapping.slot_data(idx);
            std::ptr::copy_nonoverlapping(slice.as_ptr(), data, slice.len());
            write_u32(meta, 4, 0);
            write_u64(meta, 8, task.index);
            write_u64(meta, 16, file_offset);
            write_u32(meta, 24, slice.len() as u32);
            write_u32(meta, 0, STATUS_FULL);
            file_offset += slice.len() as u64;
            write_u64(mapping.base, 32, idx + 1);
            SetEvent(events.data);
        }
        println!("FILE_DONE\t{}\t{}", task.index, digest);
        io::stdout().flush().ok();
        let idx = mapping.wait_free_slot(events.space, events.cancel)?;
        let meta = mapping.slot_meta(idx);
        write_u32(meta, 4, FLAG_EOF);
        write_u64(meta, 8, task.index);
        write_u64(meta, 16, task.len);
        write_u32(meta, 24, 0);
        write_u32(meta, 0, STATUS_FULL);
        write_u64(mapping.base, 32, idx + 1);
        SetEvent(events.data);
        Ok(())
    })();
    pool.release(task.index, cached);
    result
}

fn hash_cached_file(
    pool: &SmallFilePool,
    task: SmallFileTask,
    enabled: &HashMap<String, bool>,
) -> io::Result<()> {
    let mut cached = pool.wait_take(task.clone())?;
    let digest = match cached.digest.clone() {
        Some(digest) => digest,
        None => {
            let digest = digest_bytes(&cached.data, enabled)?;
            cached.digest = Some(digest.clone());
            digest
        }
    };
    println!("HASH_DONE\t{}\t{}", task.index, digest);
    io::stdout().flush().ok();
    pool.put_back(task.index, cached);
    Ok(())
}

unsafe fn fill_file(
    mapping: &Mapping,
    events: SessionEvents,
    file_index: u64,
    expected_len: u64,
    path: &str,
    enabled: &HashMap<String, bool>,
) -> io::Result<()> {
    let mut hashes = HashSet::new(enabled)?;
    let mut file_offset = 0u64;
    let read_result = read_file_overlapped(
        path,
        expected_len,
        mapping.slot_size,
        events.cancel,
        |offset, slice| {
            if offset != file_offset {
                return Err(io::Error::new(
                    io::ErrorKind::InvalidData,
                    "asynchronous read order mismatch",
                ));
            }
            let idx = mapping.wait_free_slot(events.space, events.cancel)?;
            let meta = mapping.slot_meta(idx);
            let data = mapping.slot_data(idx);
            std::ptr::copy_nonoverlapping(slice.as_ptr(), data, slice.len());
            hashes.update(slice)?;
            write_u32(meta, 4, 0);
            write_u64(meta, 8, file_index);
            write_u64(meta, 16, file_offset);
            write_u32(meta, 24, slice.len() as u32);
            write_u32(meta, 0, STATUS_FULL);
            file_offset += slice.len() as u64;
            write_u64(mapping.base, 32, idx + 1);
            SetEvent(events.data);
            Ok(())
        },
    );
    if let Err(error) = read_result {
        if error.kind() != io::ErrorKind::Interrupted {
            publish_error_slot(mapping, events, file_index, file_offset)?;
        }
        return Err(error);
    }
    let digest = hashes.finish()?;
    println!("FILE_DONE\t{}\t{}", file_index, digest);
    io::stdout().flush().ok();
    let idx = mapping.wait_free_slot(events.space, events.cancel)?;
    let meta = mapping.slot_meta(idx);
    write_u32(meta, 4, FLAG_EOF);
    write_u64(meta, 8, file_index);
    write_u64(meta, 16, file_offset);
    write_u32(meta, 24, 0);
    write_u32(meta, 0, STATUS_FULL);
    write_u64(mapping.base, 32, idx + 1);
    SetEvent(events.data);
    Ok(())
}

unsafe fn hash_file(
    file_index: u64,
    expected_len: u64,
    path: &str,
    cancel_event: HANDLE,
    enabled: &HashMap<String, bool>,
) -> io::Result<()> {
    let mut hashes = HashSet::new(enabled)?;
    read_file_overlapped(
        path,
        expected_len,
        HASH_CHUNK_SIZE,
        cancel_event,
        |_offset, slice| hashes.update(slice),
    )?;
    let digest = hashes.finish()?;
    println!("HASH_DONE\t{}\t{}", file_index, digest);
    io::stdout().flush().ok();
    Ok(())
}

fn native_lock(shared: &NativeShared) -> std::sync::MutexGuard<'_, NativeState> {
    shared
        .state
        .lock()
        .unwrap_or_else(|poisoned| poisoned.into_inner())
}

fn native_hash_options(mask: u32) -> HashMap<String, bool> {
    let mut enabled = HashMap::new();
    enabled.insert("SHA1".to_string(), mask & LFR_HASH_SHA1 != 0);
    enabled.insert("SHA256".to_string(), mask & LFR_HASH_SHA256 != 0);
    enabled.insert("SHA512".to_string(), mask & LFR_HASH_SHA512 != 0);
    enabled.insert("MD5".to_string(), mask & LFR_HASH_MD5 != 0);
    enabled.insert("CRC32".to_string(), mask & LFR_HASH_CRC32 != 0);
    enabled.insert("BLAKE3".to_string(), mask & LFR_HASH_BLAKE3 != 0);
    enabled.insert("XxHash3".to_string(), mask & LFR_HASH_XXH3 != 0);
    enabled.insert("XxHash128".to_string(), mask & LFR_HASH_XXH128 != 0);
    enabled
}

fn native_set_error(shared: &NativeShared, error: impl ToString) {
    let mut state = native_lock(shared);
    if state.error.is_empty() {
        state.error = error.to_string();
    }
    state.done = true;
    shared.changed.notify_all();
}

fn native_publish(
    shared: &NativeShared,
    file_index: u64,
    file_offset: u64,
    data: &[u8],
    flags: u32,
) -> io::Result<()> {
    let mut state = native_lock(shared);
    loop {
        if state.cancelled {
            return Err(cancelled_error());
        }
        let slot_index = state.write_index as usize % state.slots.len();
        if !state.slots[slot_index].full {
            if data.len() > state.slots[slot_index].buffer.len() {
                return Err(io::Error::new(
                    io::ErrorKind::InvalidData,
                    "native slot is too small",
                ));
            }
            let token = state.write_index + 1;
            let slot = &mut state.slots[slot_index];
            if !data.is_empty() {
                slot.buffer[..data.len()].copy_from_slice(data);
            }
            slot.token = token;
            slot.file_index = file_index;
            slot.file_offset = file_offset;
            slot.length = data.len() as u32;
            slot.flags = flags;
            slot.full = true;
            state.write_index += 1;
            state.buffered_bytes += data.len() as u64;
            state.occupied_slots += 1;
            if !data.is_empty() {
                shared
                    .telemetry
                    .bytes_published
                    .fetch_add(data.len() as u64, Ordering::Relaxed);
            }
            shared.changed.notify_all();
            return Ok(());
        }
        let wait_started = Instant::now();
        state = shared
            .changed
            .wait(state)
            .unwrap_or_else(|poisoned| poisoned.into_inner());
        shared.telemetry.publish_wait_ns.fetch_add(
            wait_started.elapsed().as_nanos().min(u64::MAX as u128) as u64,
            Ordering::Relaxed,
        );
    }
}

fn native_publish_batch(
    shared: &NativeShared,
    file_index: u64,
    file_offset: u64,
    data: &[u8],
    slot_size: usize,
) -> io::Result<()> {
    let mut consumed = 0usize;
    while consumed < data.len() {
        let mut state = native_lock(shared);
        while state.occupied_slots == state.slots.len() {
            if state.cancelled {
                return Err(cancelled_error());
            }
            let wait_started = Instant::now();
            state = shared
                .changed
                .wait(state)
                .unwrap_or_else(|poisoned| poisoned.into_inner());
            shared.telemetry.publish_wait_ns.fetch_add(
                wait_started.elapsed().as_nanos().min(u64::MAX as u128) as u64,
                Ordering::Relaxed,
            );
        }
        if state.cancelled {
            return Err(cancelled_error());
        }

        let remaining_slots = data[consumed..].len().div_ceil(slot_size);
        let batch_slots = remaining_slots.min(state.slots.len() - state.occupied_slots);
        let first_write_index = state.write_index;
        let mut batch_bytes = 0usize;
        for batch_index in 0..batch_slots {
            let start = consumed + batch_bytes;
            let end = (start + slot_size).min(data.len());
            let slice = &data[start..end];
            let slot_index = (first_write_index as usize + batch_index) % state.slots.len();
            let token = first_write_index + batch_index as u64 + 1;
            let slot = &mut state.slots[slot_index];
            debug_assert!(!slot.full);
            slot.buffer[..slice.len()].copy_from_slice(slice);
            slot.token = token;
            slot.file_index = file_index;
            slot.file_offset = file_offset + start as u64;
            slot.length = slice.len() as u32;
            slot.flags = 0;
            slot.full = true;
            batch_bytes += slice.len();
        }
        state.write_index += batch_slots as u64;
        state.buffered_bytes += batch_bytes as u64;
        state.occupied_slots += batch_slots;
        shared
            .telemetry
            .bytes_published
            .fetch_add(batch_bytes as u64, Ordering::Relaxed);
        consumed += batch_bytes;
        shared.changed.notify_all();
    }
    Ok(())
}

unsafe fn native_run_worker(
    shared: Arc<NativeShared>,
    config: NativeConfig,
    cancel_event: HANDLE,
    files: Vec<NativeFileTask>,
) {
    let enabled = native_hash_options(config.hash_mask);
    let mut small_pool = match SmallFilePool::new(
        config.small_open_concurrency,
        config.small_active_files,
        config.small_inflight_bytes,
        64,
        cancel_event,
    ) {
        Ok(pool) => pool,
        Err(error) => {
            native_set_error(&shared, error);
            return;
        }
    };

    for file in &files {
        if file.len <= config.small_threshold {
            small_pool.enqueue(
                SmallFileTask {
                    index: file.index,
                    len: file.len,
                    path: file.path.clone(),
                },
                false,
            );
        }
    }

    let run_result = (|| -> io::Result<()> {
        let mut prepared_large: Option<(u64, AsyncSequentialReader)> = None;
        for position in 0..files.len() {
            let file = &files[position];
            if is_cancelled(cancel_event) {
                return Err(cancelled_error());
            }

            let mut current_reader = if file.len > config.small_threshold {
                match prepared_large.take() {
                    Some((index, reader)) if index == file.index => Some(reader),
                    Some(_) => {
                        return Err(io::Error::new(
                            io::ErrorKind::InvalidData,
                            "native next-file reader order mismatch",
                        ));
                    }
                    None => {
                        let mut reader = AsyncSequentialReader::open_with_depth(
                            &file.path,
                            file.len,
                            config.read_chunk_size,
                            cancel_event,
                            config.queue_depth,
                        )?;
                        reader.set_read_wait_counter(Arc::clone(&shared.telemetry.read_wait_ns));
                        Some(reader)
                    }
                }
            } else {
                None
            };

            // Give the current file the full IOCP depth first, then submit a
            // small head start for the next large file. Its I/O can complete
            // while the current file is being hashed/published to tape.
            if let Some(reader) = current_reader.as_mut() {
                reader.prime()?;
            }
            let mut next_prepared = None;
            if let Some(next) = files.get(position + 1) {
                if next.len > config.small_threshold {
                    let mut reader = AsyncSequentialReader::open_with_depth(
                        &next.path,
                        next.len,
                        config.read_chunk_size,
                        cancel_event,
                        config.queue_depth,
                    )?;
                    reader.set_read_wait_counter(Arc::clone(&shared.telemetry.read_wait_ns));
                    reader.prime_limit(config.next_file_prime_depth)?;
                    next_prepared = Some((next.index, reader));
                }
            }

            let mut hashes = HashSet::new(&enabled)?;
            if file.len <= config.small_threshold {
                let task = SmallFileTask {
                    index: file.index,
                    len: file.len,
                    path: file.path.clone(),
                };
                let cached = small_pool.wait_take(task)?;
                let hash_started = Instant::now();
                hashes.update(&cached.data)?;
                shared.telemetry.hash_ns.fetch_add(
                    hash_started.elapsed().as_nanos().min(u64::MAX as u128) as u64,
                    Ordering::Relaxed,
                );
                shared
                    .telemetry
                    .bytes_read
                    .fetch_add(cached.data.len() as u64, Ordering::Relaxed);
                native_publish_batch(&shared, file.index, 0, &cached.data, config.slot_size)?;
                small_pool.release(file.index, cached);
            } else {
                let reader = current_reader.as_mut().unwrap();
                reader.run(|offset, slice| {
                    shared
                        .telemetry
                        .bytes_read
                        .fetch_add(slice.len() as u64, Ordering::Relaxed);
                    let hash_started = Instant::now();
                    hashes.update(slice)?;
                    shared.telemetry.hash_ns.fetch_add(
                        hash_started.elapsed().as_nanos().min(u64::MAX as u128) as u64,
                        Ordering::Relaxed,
                    );
                    native_publish_batch(&shared, file.index, offset, slice, config.slot_size)
                })?;
            }
            let result = hashes.finish()?;
            {
                let mut state = native_lock(&shared);
                state.results.insert(file.index, result);
                shared.changed.notify_all();
            }
            native_publish(&shared, file.index, file.len, &[], FLAG_EOF)?;
            prepared_large = next_prepared;
        }
        Ok(())
    })();

    small_pool.shutdown();
    match run_result {
        Ok(()) => {
            let mut state = native_lock(&shared);
            state.done = true;
            shared.changed.notify_all();
        }
        Err(error) if error.kind() == io::ErrorKind::Interrupted => {
            let mut state = native_lock(&shared);
            state.cancelled = true;
            state.done = true;
            shared.changed.notify_all();
        }
        Err(error) => native_set_error(&shared, error),
    }
}

unsafe fn native_context<'a>(context: *mut LfrContext) -> Result<&'a LfrContext, i32> {
    context.as_ref().ok_or(LFR_INVALID)
}

fn native_copy_text(text: &str, buffer: *mut u8, capacity: u32, written: *mut u32) -> i32 {
    unsafe {
        if written.is_null() {
            return LFR_INVALID;
        }
        *written = text.len() as u32;
        if buffer.is_null() || capacity < text.len() as u32 + 1 {
            return LFR_INVALID;
        }
        std::ptr::copy_nonoverlapping(text.as_ptr(), buffer, text.len());
        *buffer.add(text.len()) = 0;
    }
    LFR_OK
}

#[unsafe(no_mangle)]
pub extern "system" fn lfr_abi_version() -> u32 {
    LFR_ABI_VERSION
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_create(
    config: *const LfrConfig,
    output: *mut *mut LfrContext,
) -> i32 {
    if config.is_null() || output.is_null() {
        return LFR_INVALID;
    }
    let config = &*config;
    if config.struct_size as usize != std::mem::size_of::<LfrConfig>()
        || config.abi_version != LFR_ABI_VERSION
        || config.slot_size == 0
        || config.read_chunk_size < config.slot_size
        || config.read_chunk_size > 64 * 1024 * 1024
        || config.read_chunk_size % config.slot_size != 0
    {
        return LFR_INVALID;
    }
    let slot_size = config.slot_size as usize;
    let slot_count = (config.capacity_bytes / config.slot_size as u64).max(2) as usize;
    let slots = (0..slot_count)
        .map(|_| NativeSlot {
            buffer: vec![0u8; slot_size].into_boxed_slice(),
            token: 0,
            file_index: 0,
            file_offset: 0,
            length: 0,
            flags: 0,
            full: false,
        })
        .collect();
    let cancel_event = CreateEventW(null(), 1, 0, null());
    if cancel_event.is_null() {
        return LFR_ERROR;
    }
    let context = Box::new(LfrContext {
        shared: Arc::new(NativeShared {
            state: Mutex::new(NativeState {
                slots,
                files: Vec::new(),
                write_index: 0,
                read_index: 0,
                buffered_bytes: 0,
                occupied_slots: 0,
                started: false,
                done: false,
                cancelled: false,
                error: String::new(),
                results: HashMap::new(),
            }),
            changed: Condvar::new(),
            telemetry: NativeTelemetry::default(),
        }),
        config: NativeConfig {
            slot_size,
            read_chunk_size: config.read_chunk_size as usize,
            queue_depth: (config.queue_depth as usize).clamp(1, 128),
            capacity_bytes: slot_count as u64 * slot_size as u64,
            small_open_concurrency: (config.small_open_concurrency as usize).clamp(1, 128),
            small_active_files: (config.small_active_files as usize).clamp(1, 1024),
            small_inflight_bytes: config.small_inflight_bytes.max(config.slot_size as u64) as usize,
            small_threshold: config.small_threshold.clamp(64 * 1024, 4 * 1024 * 1024),
            hash_mask: config.hash_mask,
            next_file_prime_depth: (config.next_file_prime_depth as usize).clamp(1, 16),
        },
        cancel_event: Handle(cancel_event),
        worker: Mutex::new(None),
    });
    *output = Box::into_raw(context);
    LFR_OK
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_add_file(
    context: *mut LfrContext,
    index: i64,
    path: *const u16,
    path_len: u32,
    expected_len: u64,
) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    if index < 0 || path.is_null() {
        return LFR_INVALID;
    }
    let path = match String::from_utf16(std::slice::from_raw_parts(path, path_len as usize)) {
        Ok(value) => value,
        Err(_) => return LFR_INVALID,
    };
    let mut state = native_lock(&context.shared);
    if state.started || state.files.iter().any(|file| file.index == index as u64) {
        return LFR_INVALID;
    }
    state.files.push(NativeFileTask {
        index: index as u64,
        len: expected_len,
        path,
        selected: false,
    });
    LFR_OK
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_select_file(context: *mut LfrContext, index: i64) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let mut state = native_lock(&context.shared);
    if state.started {
        return LFR_INVALID;
    }
    match state
        .files
        .iter_mut()
        .find(|file| file.index == index as u64)
    {
        Some(file) => {
            file.selected = true;
            LFR_OK
        }
        None => LFR_INVALID,
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_start(context: *mut LfrContext) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let files = {
        let mut state = native_lock(&context.shared);
        if state.started {
            return LFR_INVALID;
        }
        state.started = true;
        state
            .files
            .iter()
            .filter(|file| file.selected)
            .cloned()
            .collect::<Vec<_>>()
    };
    let shared = Arc::clone(&context.shared);
    let config = NativeConfig {
        slot_size: context.config.slot_size,
        read_chunk_size: context.config.read_chunk_size,
        queue_depth: context.config.queue_depth,
        capacity_bytes: context.config.capacity_bytes,
        small_open_concurrency: context.config.small_open_concurrency,
        small_active_files: context.config.small_active_files,
        small_inflight_bytes: context.config.small_inflight_bytes,
        small_threshold: context.config.small_threshold,
        hash_mask: context.config.hash_mask,
        next_file_prime_depth: context.config.next_file_prime_depth,
    };
    let cancel_event = context.cancel_event.0 as usize;
    let worker = thread::spawn(move || unsafe {
        native_run_worker(shared, config, cancel_event as HANDLE, files)
    });
    let mut worker_slot = context.worker.lock().unwrap_or_else(|p| p.into_inner());
    *worker_slot = Some(worker);
    LFR_OK
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_buffered_bytes(context: *mut LfrContext) -> u64 {
    match native_context(context) {
        Ok(context) => native_lock(&context.shared).buffered_bytes,
        Err(_) => 0,
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_buffer_capacity(context: *mut LfrContext) -> u64 {
    match native_context(context) {
        Ok(context) => context.config.capacity_bytes,
        Err(_) => 0,
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_occupied_slots(context: *mut LfrContext) -> u64 {
    match native_context(context) {
        Ok(context) => native_lock(&context.shared).occupied_slots as u64,
        Err(_) => 0,
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_get_stats(
    context: *mut LfrContext,
    output: *mut LfrStats,
) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    if output.is_null() || (*output).struct_size as usize != std::mem::size_of::<LfrStats>() {
        return LFR_INVALID;
    }
    let state = native_lock(&context.shared);
    *output = LfrStats {
        struct_size: std::mem::size_of::<LfrStats>() as u32,
        abi_version: LFR_ABI_VERSION,
        bytes_read: context.shared.telemetry.bytes_read.load(Ordering::Relaxed),
        bytes_published: context
            .shared
            .telemetry
            .bytes_published
            .load(Ordering::Relaxed),
        buffered_bytes: state.buffered_bytes,
        occupied_slots: state.occupied_slots as u64,
        read_wait_ns: context
            .shared
            .telemetry
            .read_wait_ns
            .load(Ordering::Relaxed),
        hash_ns: context.shared.telemetry.hash_ns.load(Ordering::Relaxed),
        publish_wait_ns: context
            .shared
            .telemetry
            .publish_wait_ns
            .load(Ordering::Relaxed),
    };
    LFR_OK
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_is_done(context: *mut LfrContext) -> i32 {
    match native_context(context) {
        Ok(context) => i32::from(native_lock(&context.shared).done),
        Err(_) => 1,
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_wait_until_buffered(
    context: *mut LfrContext,
    target: u64,
    timeout_ms: u32,
) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let start = std::time::Instant::now();
    let mut state = native_lock(&context.shared);
    loop {
        if state.cancelled {
            return LFR_CANCELLED;
        }
        if !state.error.is_empty() {
            return LFR_ERROR;
        }
        if state.buffered_bytes >= target || state.done || state.occupied_slots == state.slots.len()
        {
            return LFR_OK;
        }
        if timeout_ms == 0 {
            return LFR_TIMEOUT;
        }
        if timeout_ms == u32::MAX {
            state = context
                .shared
                .changed
                .wait(state)
                .unwrap_or_else(|p| p.into_inner());
        } else {
            let timeout = Duration::from_millis(timeout_ms as u64).saturating_sub(start.elapsed());
            if timeout.is_zero() {
                return LFR_TIMEOUT;
            }
            let (next, result) = context
                .shared
                .changed
                .wait_timeout(state, timeout)
                .unwrap_or_else(|p| p.into_inner());
            state = next;
            if result.timed_out() {
                return LFR_TIMEOUT;
            }
        }
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_acquire_slot(
    context: *mut LfrContext,
    expected_file_index: i64,
    timeout_ms: u32,
    output: *mut LfrSlot,
) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    if output.is_null() {
        return LFR_INVALID;
    }
    let start = std::time::Instant::now();
    let mut state = native_lock(&context.shared);
    loop {
        let slot_index = state.read_index as usize % state.slots.len();
        let slot = &state.slots[slot_index];
        if slot.full {
            if expected_file_index >= 0 && slot.file_index != expected_file_index as u64 {
                return LFR_ERROR;
            }
            *output = LfrSlot {
                token: slot.token,
                file_index: slot.file_index as i64,
                file_offset: slot.file_offset,
                data: slot.buffer.as_ptr(),
                length: slot.length,
                flags: slot.flags,
            };
            return LFR_OK;
        }
        if state.cancelled {
            return LFR_CANCELLED;
        }
        if !state.error.is_empty() {
            return LFR_ERROR;
        }
        if state.done {
            return LFR_DONE;
        }
        if timeout_ms == 0 {
            return LFR_TIMEOUT;
        }
        if timeout_ms == u32::MAX {
            state = context
                .shared
                .changed
                .wait(state)
                .unwrap_or_else(|p| p.into_inner());
        } else {
            let timeout = Duration::from_millis(timeout_ms as u64).saturating_sub(start.elapsed());
            if timeout.is_zero() {
                return LFR_TIMEOUT;
            }
            let (next, result) = context
                .shared
                .changed
                .wait_timeout(state, timeout)
                .unwrap_or_else(|p| p.into_inner());
            state = next;
            if result.timed_out() {
                return LFR_TIMEOUT;
            }
        }
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_release_slot(context: *mut LfrContext, token: u64) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let mut state = native_lock(&context.shared);
    let slot_index = state.read_index as usize % state.slots.len();
    let slot = &state.slots[slot_index];
    if !slot.full || slot.token != token {
        return LFR_INVALID;
    }
    let length = slot.length as u64;
    let slot = &mut state.slots[slot_index];
    slot.full = false;
    slot.length = 0;
    slot.flags = 0;
    state.read_index += 1;
    state.buffered_bytes = state.buffered_bytes.saturating_sub(length);
    state.occupied_slots = state.occupied_slots.saturating_sub(1);
    context.shared.changed.notify_all();
    LFR_OK
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_hash_file(
    context: *mut LfrContext,
    index: i64,
    buffer: *mut u8,
    capacity: u32,
    written: *mut u32,
) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let file = {
        let state = native_lock(&context.shared);
        match state.files.iter().find(|file| file.index == index as u64) {
            Some(file) => file.clone(),
            None => return LFR_INVALID,
        }
    };
    let enabled = native_hash_options(context.config.hash_mask);
    let mut hashes = match HashSet::new(&enabled) {
        Ok(value) => value,
        Err(error) => {
            native_set_error(&context.shared, error);
            return LFR_ERROR;
        }
    };
    let result = read_file_overlapped(
        &file.path,
        file.len,
        HASH_CHUNK_SIZE,
        context.cancel_event.0,
        |_offset, slice| hashes.update(slice),
    )
    .and_then(|_| hashes.finish());
    match result {
        Ok(text) => native_copy_text(&text, buffer, capacity, written),
        Err(error) if error.kind() == io::ErrorKind::Interrupted => LFR_CANCELLED,
        Err(error) => {
            native_set_error(&context.shared, error);
            LFR_ERROR
        }
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_get_file_hashes(
    context: *mut LfrContext,
    index: i64,
    buffer: *mut u8,
    capacity: u32,
    written: *mut u32,
) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let state = native_lock(&context.shared);
    match state.results.get(&(index as u64)) {
        Some(text) => native_copy_text(text, buffer, capacity, written),
        None if state.cancelled => LFR_CANCELLED,
        None if !state.error.is_empty() => LFR_ERROR,
        None => LFR_TIMEOUT,
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_last_error(
    context: *mut LfrContext,
    buffer: *mut u8,
    capacity: u32,
    written: *mut u32,
) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    let state = native_lock(&context.shared);
    native_copy_text(&state.error, buffer, capacity, written)
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_cancel(context: *mut LfrContext) -> i32 {
    let context = match native_context(context) {
        Ok(value) => value,
        Err(code) => return code,
    };
    {
        let mut state = native_lock(&context.shared);
        state.cancelled = true;
        context.shared.changed.notify_all();
    }
    SetEvent(context.cancel_event.0);
    LFR_OK
}

#[unsafe(no_mangle)]
pub unsafe extern "system" fn lfr_destroy(context: *mut LfrContext) {
    if context.is_null() {
        return;
    }
    let context = Box::from_raw(context);
    {
        let mut state = native_lock(&context.shared);
        state.cancelled = true;
        context.shared.changed.notify_all();
    }
    SetEvent(context.cancel_event.0);
    let worker = context
        .worker
        .lock()
        .unwrap_or_else(|p| p.into_inner())
        .take();
    if let Some(worker) = worker {
        let _ = worker.join();
    }
}

fn main() -> io::Result<()> {
    let mut init = String::new();
    if io::stdin().read_line(&mut init)? == 0 {
        return Err(io::Error::new(io::ErrorKind::UnexpectedEof, "missing INIT"));
    }
    if !init.starts_with("INIT\t") {
        return Err(io::Error::new(io::ErrorKind::InvalidInput, "expected INIT"));
    }
    let cfg = parse_init(&init);
    let shm_name = cfg
        .get("shm")
        .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing shm"))?
        .clone();
    let data_event_name = cfg
        .get("data_event")
        .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing data_event"))?
        .clone();
    let space_event_name = cfg
        .get("space_event")
        .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing space_event"))?
        .clone();
    let cancel_event_name = cfg
        .get("cancel_event")
        .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "missing cancel_event"))?
        .clone();
    let capacity: u64 = cfg
        .get("capacity")
        .and_then(|s| s.parse().ok())
        .unwrap_or(268435456);
    let slot_size: u64 = cfg
        .get("slot_size")
        .and_then(|s| s.parse().ok())
        .unwrap_or(1048576);
    let default_small_inflight = capacity.min(128 * 1024 * 1024);
    let small_inflight_bytes: usize = cfg
        .get("small_inflight_bytes")
        .and_then(|value| value.parse().ok())
        .unwrap_or(default_small_inflight as usize);
    let small_threshold: u64 = cfg
        .get("small_threshold")
        .and_then(|value| value.parse().ok())
        .unwrap_or_else(|| (default_small_inflight / 64).clamp(64 * 1024, 4 * 1024 * 1024))
        .clamp(64 * 1024, 4 * 1024 * 1024);
    let small_open_concurrency: usize = cfg
        .get("small_open_concurrency")
        .and_then(|value| value.parse().ok())
        .unwrap_or(32);
    let small_active_files: usize = cfg
        .get("small_active_files")
        .and_then(|value| value.parse().ok())
        .unwrap_or(64);
    let small_iocp_batch: usize = cfg
        .get("small_iocp_batch")
        .and_then(|value| value.parse().ok())
        .unwrap_or(64);
    let mut enabled = HashMap::new();
    for name in [
        "SHA1",
        "SHA256",
        "SHA512",
        "MD5",
        "CRC32",
        "BLAKE3",
        "XxHash3",
        "XxHash128",
    ] {
        enabled.insert(name.to_string(), parse_bool(cfg.get(name)));
    }

    unsafe {
        let mapping = Mapping::create(&shm_name, capacity, slot_size)?;
        let data_event = CreateEventW(null(), 0, 0, wide(&data_event_name).as_ptr());
        if data_event.is_null() {
            return Err(io::Error::last_os_error());
        }
        let _data_event = Handle(data_event);
        let space_event = CreateEventW(null(), 0, 0, wide(&space_event_name).as_ptr());
        if space_event.is_null() {
            return Err(io::Error::last_os_error());
        }
        let _space_event = Handle(space_event);
        let cancel_event = CreateEventW(null(), 1, 0, wide(&cancel_event_name).as_ptr());
        if cancel_event.is_null() {
            return Err(io::Error::last_os_error());
        }
        let _cancel_event = Handle(cancel_event);
        let events = SessionEvents {
            data: data_event,
            space: space_event,
            cancel: cancel_event,
        };
        let small_pool = SmallFilePool::new(
            small_open_concurrency,
            small_active_files,
            small_inflight_bytes,
            small_iocp_batch,
            cancel_event,
        )?;
        println!(
            "READY\tslot_count={}\tdata_offset={}\tmap_size={}\tsmall_threshold={}\tsmall_inflight_bytes={}",
            mapping.slot_count,
            mapping.data_offset,
            mapping.size,
            small_threshold,
            small_inflight_bytes
        );
        io::stdout().flush().ok();

        let (command_sender, command_receiver) = mpsc::channel();
        let command_pool = small_pool.control();
        let command_thread =
            thread::spawn(move || receive_commands(command_sender, command_pool, small_threshold));
        let session_result = (|| -> io::Result<()> {
            loop {
                match command_receiver.recv() {
                    Ok(SessionCommand::Done) | Err(_) => break,
                    Ok(SessionCommand::ProtocolError(message)) => {
                        return Err(io::Error::new(io::ErrorKind::InvalidInput, message));
                    }
                    Ok(SessionCommand::File(task)) => {
                        small_pool.discard_before(task.index);
                        let result = if task.len <= small_threshold {
                            fill_cached_file(&small_pool, &mapping, events, task.clone(), &enabled)
                        } else {
                            fill_file(&mapping, events, task.index, task.len, &task.path, &enabled)
                        };
                        result.map_err(|error| {
                            io::Error::new(error.kind(), format!("file {}: {error}", task.index))
                        })?;
                    }
                    Ok(SessionCommand::Hash(task)) => {
                        small_pool.discard_before(task.index);
                        let result = if task.len <= small_threshold {
                            hash_cached_file(&small_pool, task.clone(), &enabled)
                        } else {
                            hash_file(task.index, task.len, &task.path, cancel_event, &enabled)
                        };
                        result.map_err(|error| {
                            io::Error::new(error.kind(), format!("hash {}: {error}", task.index))
                        })?;
                    }
                }
            }
            Ok(())
        })();
        if session_result.is_ok() {
            let _ = command_thread.join();
        }
        if let Err(error) = session_result {
            if error.kind() != io::ErrorKind::Interrupted || !is_cancelled(cancel_event) {
                eprintln!("FILE_ERROR\t{error}");
                io::stderr().flush().ok();
                write_u32(mapping.base, 52, 1);
            }
        }
        write_u32(mapping.base, 48, 1);
        SetEvent(data_event);
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;
    use std::path::PathBuf;
    use std::time::{SystemTime, UNIX_EPOCH};

    struct TempFile(PathBuf);

    impl TempFile {
        fn create(label: &str, data: &[u8]) -> io::Result<Self> {
            let unique = SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .unwrap()
                .as_nanos();
            let path = std::env::temp_dir().join(format!(
                "ltfscopy-fastreader-{label}-{}-{unique}.bin",
                std::process::id()
            ));
            fs::write(&path, data)?;
            Ok(Self(path))
        }
    }

    impl Drop for TempFile {
        fn drop(&mut self) {
            let _ = fs::remove_file(&self.0);
        }
    }

    #[test]
    fn completed_requests_are_selected_by_offset() {
        let mut requests = vec![
            ReadRequest::new(1),
            ReadRequest::new(1),
            ReadRequest::new(1),
        ];
        for (request, offset) in requests.iter_mut().zip([8192, 0, 4096]) {
            request.offset = offset;
            request.state = RequestState::Completed(Ok(1));
        }

        assert_eq!(completed_request_at(&requests, 0), Some(1));
        assert_eq!(completed_request_at(&requests, 4096), Some(2));
        assert_eq!(completed_request_at(&requests, 12288), None);
    }

    #[test]
    fn overlapped_reader_preserves_bytes_and_order() -> io::Result<()> {
        const CHUNK: usize = 4096;
        for (case, len) in [
            ("empty", 0usize),
            ("small", 173usize),
            ("partial", CHUNK * 2 + 37),
            ("queue16", CHUNK * (IO_QUEUE_DEPTH + 3) + 211),
        ] {
            let expected = (0..len)
                .map(|index| ((index * 31 + 7) % 251) as u8)
                .collect::<Vec<_>>();
            let file = TempFile::create(case, &expected)?;
            let mut actual = Vec::with_capacity(len);
            unsafe {
                read_file_overlapped(
                    file.0.to_str().unwrap(),
                    len as u64,
                    CHUNK,
                    null_mut(),
                    |offset, slice| {
                        assert_eq!(offset, actual.len() as u64);
                        actual.extend_from_slice(slice);
                        Ok(())
                    },
                )?;
            }
            assert_eq!(actual, expected, "failed case {case}");
        }
        Ok(())
    }

    #[test]
    fn overlapped_reader_rejects_length_changes() -> io::Result<()> {
        let file = TempFile::create("length", b"abcdef")?;
        let result = unsafe {
            read_file_overlapped(
                file.0.to_str().unwrap(),
                5,
                4096,
                null_mut(),
                |_offset, _slice| Ok(()),
            )
        };
        assert_eq!(result.unwrap_err().kind(), io::ErrorKind::InvalidData);
        Ok(())
    }

    #[test]
    fn small_file_pool_prefetches_once_within_limits() -> io::Result<()> {
        let mut pool = unsafe { SmallFilePool::new(8, 12, 2 * 1024 * 1024, 16, null_mut())? };
        let mut files = Vec::new();
        let mut tasks = Vec::new();
        let mut expected = Vec::new();
        for index in 0..24u64 {
            let len = 1000 + index as usize * 317;
            let data = (0..len)
                .map(|offset| ((offset * 17 + index as usize) % 251) as u8)
                .collect::<Vec<_>>();
            let file = TempFile::create(&format!("small-pool-文件-{index}"), &data)?;
            let task = SmallFileTask {
                index,
                len: len as u64,
                path: file.0.to_str().unwrap().to_string(),
            };
            pool.enqueue(task.clone(), false);
            files.push(file);
            tasks.push(task);
            expected.push(data);
        }

        for index in 0..tasks.len() {
            let cached = pool.wait_take(tasks[index].clone())?;
            assert_eq!(cached.data, expected[index]);
            if index == 0 {
                pool.put_back(tasks[index].index, cached);
                fs::remove_file(&files[index].0)?;
                let cached_again = pool.wait_take(tasks[index].clone())?;
                assert_eq!(cached_again.data, expected[index]);
                pool.release(tasks[index].index, cached_again);
            } else {
                pool.release(tasks[index].index, cached);
            }
        }

        {
            let state = pool.shared.state.lock().unwrap();
            assert!(state.max_active_files <= 12);
            assert!(state.max_reserved_bytes <= 2 * 1024 * 1024);
        }
        unsafe { pool.shutdown() };
        Ok(())
    }

    #[test]
    fn native_abi_streams_ordered_files_from_stable_native_slots() -> io::Result<()> {
        let first_data = (0..(96 * 1024 + 37))
            .map(|index| ((index * 13 + 5) % 251) as u8)
            .collect::<Vec<_>>();
        let second_data = (0..(80 * 1024 + 11))
            .map(|index| ((index * 29 + 3) % 251) as u8)
            .collect::<Vec<_>>();
        let first = TempFile::create("native-abi-first-文件", &first_data)?;
        let second = TempFile::create("native-abi-second-文件", &second_data)?;
        let first_path = first.0.to_str().unwrap().encode_utf16().collect::<Vec<_>>();
        let second_path = second
            .0
            .to_str()
            .unwrap()
            .encode_utf16()
            .collect::<Vec<_>>();
        let config = LfrConfig {
            struct_size: std::mem::size_of::<LfrConfig>() as u32,
            abi_version: LFR_ABI_VERSION,
            slot_size: 4096,
            read_chunk_size: 16 * 1024,
            queue_depth: 16,
            capacity_bytes: 32 * 1024,
            small_open_concurrency: 4,
            small_active_files: 8,
            small_inflight_bytes: 2 * 1024 * 1024,
            small_threshold: 64 * 1024,
            hash_mask: LFR_HASH_CRC32,
            next_file_prime_depth: 8,
        };

        unsafe {
            let mut context = null_mut();
            assert_eq!(lfr_create(&config, &mut context), LFR_OK);
            assert!(!context.is_null());
            assert_eq!(
                lfr_add_file(
                    context,
                    0,
                    first_path.as_ptr(),
                    first_path.len() as u32,
                    first_data.len() as u64,
                ),
                LFR_OK
            );
            assert_eq!(
                lfr_add_file(
                    context,
                    1,
                    second_path.as_ptr(),
                    second_path.len() as u32,
                    second_data.len() as u64,
                ),
                LFR_OK
            );
            assert_eq!(lfr_select_file(context, 0), LFR_OK);
            assert_eq!(lfr_select_file(context, 1), LFR_OK);
            assert_eq!(lfr_start(context), LFR_OK);

            for (index, expected) in [(0i64, &first_data), (1i64, &second_data)] {
                let mut actual = Vec::with_capacity(expected.len());
                loop {
                    let mut slot = LfrSlot {
                        token: 0,
                        file_index: -1,
                        file_offset: 0,
                        data: null(),
                        length: 0,
                        flags: 0,
                    };
                    assert_eq!(
                        lfr_acquire_slot(context, index, u32::MAX, &mut slot),
                        LFR_OK
                    );
                    assert_eq!(slot.file_index, index);
                    if slot.flags & FLAG_EOF != 0 {
                        assert_eq!(slot.file_offset, expected.len() as u64);
                        assert_eq!(slot.length, 0);
                        assert_eq!(lfr_release_slot(context, slot.token), LFR_OK);
                        break;
                    }
                    assert!(!slot.data.is_null());
                    assert_eq!(slot.file_offset, actual.len() as u64);
                    actual.extend_from_slice(std::slice::from_raw_parts(
                        slot.data,
                        slot.length as usize,
                    ));
                    assert_eq!(lfr_release_slot(context, slot.token), LFR_OK);
                }
                assert_eq!(&actual, expected);

                let mut hashes = [0u8; 256];
                let mut written = 0u32;
                assert_eq!(
                    lfr_get_file_hashes(
                        context,
                        index,
                        hashes.as_mut_ptr(),
                        hashes.len() as u32,
                        &mut written,
                    ),
                    LFR_OK
                );
                let hashes = std::str::from_utf8(&hashes[..written as usize]).unwrap();
                assert!(hashes.starts_with("CRC32="));
            }
            assert_eq!(lfr_wait_until_buffered(context, u64::MAX, 1000), LFR_OK);
            assert_eq!(lfr_is_done(context), 1);
            let mut stats = LfrStats {
                struct_size: std::mem::size_of::<LfrStats>() as u32,
                abi_version: 0,
                bytes_read: 0,
                bytes_published: 0,
                buffered_bytes: 0,
                occupied_slots: u64::MAX,
                read_wait_ns: 0,
                hash_ns: 0,
                publish_wait_ns: 0,
            };
            assert_eq!(lfr_get_stats(context, &mut stats), LFR_OK);
            assert_eq!(stats.abi_version, LFR_ABI_VERSION);
            assert_eq!(
                stats.bytes_read,
                (first_data.len() + second_data.len()) as u64
            );
            assert_eq!(
                stats.bytes_published,
                (first_data.len() + second_data.len()) as u64
            );
            assert_eq!(stats.buffered_bytes, 0);
            assert_eq!(stats.occupied_slots, 0);
            lfr_destroy(context);
        }
        Ok(())
    }
}
