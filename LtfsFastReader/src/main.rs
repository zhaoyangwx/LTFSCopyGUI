#![allow(non_snake_case)]

use md5::Md5;
use sha1::Sha1;
use sha2::{Digest, Sha256, Sha512};
use std::collections::HashMap;
use std::ffi::OsStr;
use std::io::{self, BufRead, Write};
use std::os::windows::ffi::OsStrExt;
use std::ptr::{null, null_mut};
use windows_sys::Win32::Foundation::{
    CloseHandle, ERROR_IO_PENDING, ERROR_NOT_FOUND, GENERIC_READ, HANDLE, INVALID_HANDLE_VALUE,
    WAIT_OBJECT_0,
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
    CreateEventW, SetEvent, WaitForSingleObject, INFINITE,
};
use windows_sys::Win32::System::IO::{
    CancelIoEx, CreateIoCompletionPort, GetOverlappedResult, GetQueuedCompletionStatusEx,
    OVERLAPPED, OVERLAPPED_ENTRY,
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

    unsafe fn wait_free_slot(&self, space_event: HANDLE) -> io::Result<u64> {
        loop {
            let write_idx = read_u64(self.base, 32);
            let meta = self.slot_meta(write_idx);
            if read_u32(meta, 0) == STATUS_EMPTY {
                return Ok(write_idx);
            }
            if WaitForSingleObject(space_event, INFINITE) != WAIT_OBJECT_0 {
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
}

fn completed_request_at(requests: &[ReadRequest], offset: u64) -> Option<usize> {
    requests.iter().position(|request| {
        request.offset == offset && matches!(request.state, RequestState::Completed(_))
    })
}

impl AsyncSequentialReader {
    unsafe fn open(path: &str, expected_len: u64, chunk_size: usize) -> io::Result<Self> {
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
                .min(IO_QUEUE_DEPTH as u64) as usize
        };
        let requests = (0..request_count)
            .map(|_| ReadRequest::new(chunk_size))
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
        })
    }

    unsafe fn submit(&mut self, request_index: usize) -> io::Result<()> {
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
        for index in 0..self.requests.len() {
            self.submit(index)?;
        }
        Ok(())
    }

    unsafe fn receive_completions(&mut self) -> io::Result<()> {
        let mut entries = [OVERLAPPED_ENTRY::default(); IO_QUEUE_DEPTH];
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
            return Err(io::Error::last_os_error());
        }

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

    unsafe fn run<F>(&mut self, mut consume: F) -> io::Result<()>
    where
        F: FnMut(u64, &[u8]) -> io::Result<()>,
    {
        self.prime()?;
        while self.next_consume < self.file_len {
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
    consume: F,
) -> io::Result<()>
where
    F: FnMut(u64, &[u8]) -> io::Result<()>,
{
    let mut reader = AsyncSequentialReader::open(path, expected_len, chunk_size)?;
    reader.run(consume)
}

unsafe fn fill_file(
    mapping: &Mapping,
    data_event: HANDLE,
    space_event: HANDLE,
    file_index: u64,
    expected_len: u64,
    path: &str,
    enabled: &HashMap<String, bool>,
) -> io::Result<()> {
    let mut hashes = HashSet::new(enabled)?;
    let mut file_offset = 0u64;
    let read_result =
        read_file_overlapped(path, expected_len, mapping.slot_size, |offset, slice| {
            if offset != file_offset {
                return Err(io::Error::new(
                    io::ErrorKind::InvalidData,
                    "asynchronous read order mismatch",
                ));
            }
            let idx = mapping.wait_free_slot(space_event)?;
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
            SetEvent(data_event);
            Ok(())
        });
    if let Err(error) = read_result {
        let idx = mapping.wait_free_slot(space_event)?;
        let meta = mapping.slot_meta(idx);
        write_u32(meta, 4, FLAG_ERROR);
        write_u64(meta, 8, file_index);
        write_u64(meta, 16, file_offset);
        write_u32(meta, 24, 0);
        write_u32(meta, 0, STATUS_FULL);
        write_u64(mapping.base, 32, idx + 1);
        SetEvent(data_event);
        return Err(error);
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

unsafe fn hash_file(
    file_index: u64,
    expected_len: u64,
    path: &str,
    enabled: &HashMap<String, bool>,
) -> io::Result<()> {
    let mut hashes = HashSet::new(enabled)?;
    read_file_overlapped(path, expected_len, HASH_CHUNK_SIZE, |_offset, slice| {
        hashes.update(slice)
    })?;
    let digest = hashes.finish()?;
    println!("FILE_DONE\t{}\t{}", file_index, digest);
    io::stdout().flush().ok();
    Ok(())
}

fn main() -> io::Result<()> {
    let stdin = io::stdin();
    let mut lines = stdin.lock().lines();
    let init = lines
        .next()
        .transpose()?
        .ok_or_else(|| io::Error::new(io::ErrorKind::UnexpectedEof, "missing INIT"))?;
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
    let capacity: u64 = cfg
        .get("capacity")
        .and_then(|s| s.parse().ok())
        .unwrap_or(268435456);
    let slot_size: u64 = cfg
        .get("slot_size")
        .and_then(|s| s.parse().ok())
        .unwrap_or(1048576);
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
        println!(
            "READY\tslot_count={}\tdata_offset={}\tmap_size={}",
            mapping.slot_count, mapping.data_offset, mapping.size
        );
        io::stdout().flush().ok();

        for line in lines {
            let line = line?;
            if line == "DONE" {
                break;
            }
            if let Some(rest) = line.strip_prefix("FILE\t") {
                let mut parts = rest.splitn(3, '\t');
                let idx: u64 = parts
                    .next()
                    .and_then(|s| s.parse().ok())
                    .ok_or_else(|| io::Error::new(io::ErrorKind::InvalidInput, "bad file index"))?;
                let len: u64 = parts.next().and_then(|s| s.parse().ok()).ok_or_else(|| {
                    io::Error::new(io::ErrorKind::InvalidInput, "bad file length")
                })?;
                let path =
                    decode_path_hex(parts.next().ok_or_else(|| {
                        io::Error::new(io::ErrorKind::InvalidInput, "missing path")
                    })?)?;
                if let Err(e) =
                    fill_file(&mapping, data_event, space_event, idx, len, &path, &enabled)
                {
                    eprintln!("FILE_ERROR\t{}\t{}", idx, e);
                    write_u32(mapping.base, 52, 1);
                    SetEvent(data_event);
                    break;
                }
            } else if let Some(rest) = line.strip_prefix("HASH\t") {
                let mut parts = rest.splitn(3, '\t');
                let idx: u64 = parts.next().and_then(|s| s.parse().ok()).ok_or_else(|| {
                    io::Error::new(io::ErrorKind::InvalidInput, "bad hash file index")
                })?;
                let len: u64 = parts.next().and_then(|s| s.parse().ok()).ok_or_else(|| {
                    io::Error::new(io::ErrorKind::InvalidInput, "bad hash file length")
                })?;
                let path = decode_path_hex(parts.next().ok_or_else(|| {
                    io::Error::new(io::ErrorKind::InvalidInput, "missing hash path")
                })?)?;
                if let Err(e) = hash_file(idx, len, &path, &enabled) {
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
            read_file_overlapped(file.0.to_str().unwrap(), 5, 4096, |_offset, _slice| Ok(()))
        };
        assert_eq!(result.unwrap_err().kind(), io::ErrorKind::InvalidData);
        Ok(())
    }
}
