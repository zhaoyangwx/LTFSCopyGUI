/*
 *   File:   tape.c
 *   Author: Matthew Millman (inaxeon@hotmail.com)
 *
 *   Command line LTFS Configurator for Windows
 *
 *   This is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 2 of the License, or
 *   (at your option) any later version.
 *   This software is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *   You should have received a copy of the GNU General Public License
 *   along with this software.  If not, see <http://www.gnu.org/licenses/>.
 */

#include "pch.h"
#include "tape.h"

#define TC_MP_PC_CURRENT                 0x00
#define TC_MP_PC_CHANGEABLE              0x40

#define TC_MP_MEDIUM_CONFIGURATION       0x1D
#define TC_MP_MEDIUM_PARTITION           0x11
#define TC_MP_MEDIUM_PARTITION_SIZE      28

#define SENSE_INFO_LEN                   64

static BOOL ScsiIoControl(HANDLE hFile, DWORD deviceNumber, PVOID cdb, UCHAR cdbLength, PVOID dataBuffer, USHORT bufferLength, BYTE dataIn, ULONG timeoutValue, PVOID senseBuffer);

BOOL TapeGetDriveList(PTAPE_DRIVE *driveList, PDWORD numDrivesFound)
{
    HDEVINFO devInfo = SetupDiGetClassDevs(&GUID_DEVINTERFACE_TAPE, NULL, NULL, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);
    SP_DEVICE_INTERFACE_DATA devData;
    PTAPE_DRIVE listHead = NULL;
    PTAPE_DRIVE listLast = NULL;
    DWORD devIndex = 0;
    DWORD devsFound = 0;
    BOOL lastRet = FALSE;

    do
    {
        devData.cbSize = sizeof(SP_DEVICE_INTERFACE_DATA);
        lastRet = SetupDiEnumDeviceInterfaces(devInfo, NULL, &GUID_DEVINTERFACE_TAPE, devIndex, &devData);

        if (lastRet == TRUE)
        {
            DWORD dwRequiredSize = 0;
            SetupDiGetDeviceInterfaceDetail(devInfo, &devData, NULL, 0, &dwRequiredSize, NULL);
            if (dwRequiredSize > 0 && GetLastError() == ERROR_INSUFFICIENT_BUFFER)
            {
                PSP_DEVICE_INTERFACE_DETAIL_DATA devDetail = (PSP_DEVICE_INTERFACE_DETAIL_DATA)LocalAlloc(LMEM_FIXED, dwRequiredSize);
                devDetail->cbSize = sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA);

                if (SetupDiGetDeviceInterfaceDetail(devInfo, &devData, devDetail, dwRequiredSize, &dwRequiredSize, NULL) == TRUE)
                {
                    HANDLE handle = CreateFile(devDetail->DevicePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_DELETE | FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);
                    if (handle != INVALID_HANDLE_VALUE)
                    {
                        BOOL result = FALSE;
                        BYTE dataBuffer[1024];
                        BYTE cdb[6];
                        STORAGE_DEVICE_NUMBER devNum;

                        PTAPE_DRIVE driveData = (PTAPE_DRIVE)LocalAlloc(LMEM_FIXED, sizeof(TAPE_DRIVE));
                        driveData->Next = NULL;

                        DWORD lpBytesReturned;
                        result = DeviceIoControl(handle, IOCTL_STORAGE_GET_DEVICE_NUMBER, NULL, 0, &devNum, sizeof(STORAGE_DEVICE_NUMBER), &lpBytesReturned, NULL);

                        driveData->DevIndex = devNum.DeviceNumber;

                        if (result)
                        {
                            memset(dataBuffer, 0, sizeof(dataBuffer));
                            memset(cdb, 0, sizeof(cdb));

                            ((PCDB)(cdb))->CDB6INQUIRY.OperationCode = SCSIOP_INQUIRY;
                            ((PCDB)(cdb))->CDB6INQUIRY.IReserved = 4;

                            result = ScsiIoControl(handle, devNum.DeviceNumber, cdb, sizeof(cdb), dataBuffer, sizeof(dataBuffer), SCSI_IOCTL_DATA_IN, 10, NULL);

                            if (result)
                            {
                                PINQUIRYDATA inquiryResult = (PINQUIRYDATA)dataBuffer;
                                strncpy_s((char *)driveData->VendorId, sizeof(driveData->VendorId), (char *)inquiryResult->VendorId, MEMBER_SIZE(INQUIRYDATA, VendorId));
                                strncpy_s((char *)driveData->ProductId, sizeof(driveData->ProductId), (char *)inquiryResult->ProductId, MEMBER_SIZE(INQUIRYDATA, ProductId));
                            }
                        }

                        if (result)
                        {
                            memset(dataBuffer, 0, sizeof(dataBuffer));
                            memset(cdb, 0, sizeof(cdb));

                            ((PCDB)(cdb))->CDB6INQUIRY.OperationCode = SCSIOP_INQUIRY;
                            ((PCDB)(cdb))->CDB6INQUIRY.IReserved = 4;
                            ((PCDB)(cdb))->CDB6INQUIRY.PageCode = 0x80;
                            ((PCDB)(cdb))->CDB6INQUIRY.Reserved1 = 1;

                            BOOL result = ScsiIoControl(handle, devNum.DeviceNumber, cdb, sizeof(cdb), dataBuffer, sizeof(dataBuffer), SCSI_IOCTL_DATA_IN, 10, NULL);

                            if (result)
                            {
                                PVPD_SERIAL_NUMBER_PAGE inquiryResult = (PVPD_SERIAL_NUMBER_PAGE)dataBuffer;
                                strncpy_s((char *)driveData->SerialNumber, sizeof(driveData->SerialNumber), (char *)inquiryResult->SerialNumber, inquiryResult->PageLength);
                            }
                        }

                        if (result)
                        {
                            memset(dataBuffer, 0, sizeof(dataBuffer));
                            memset(cdb, 0, sizeof(cdb));

                            ((PCDB)(cdb))->MODE_SENSE.OperationCode = SCSIOP_MODE_SENSE;
                            ((PCDB)(cdb))->MODE_SENSE.PageCode = TC_MP_MEDIUM_PARTITION;
                            ((PCDB)(cdb))->MODE_SENSE.AllocationLength = 255;

                            BOOL result = ScsiIoControl(handle, devNum.DeviceNumber, cdb, sizeof(cdb), dataBuffer, sizeof(dataBuffer), SCSI_IOCTL_DATA_IN, 10, NULL);

                            // Fuck knows. LTFSConfigurator.exe performs this operation (and others), which it appears may be able to tell us whether or not the 
                            // drive is compatible with LTFS. I have yet to figure out how to parse this data to perform this test, so we're not doing it at present.
                        }

                        if (result)
                        {
                            if (listLast)
                                listLast->Next = driveData;

                            if (listHead == NULL)
                                listHead = driveData;

                            listLast = driveData;
                            devsFound++;
                        }
                        else
                        {
                            LocalFree(driveData);
                        }
                    }

                    CloseHandle(handle);
                }

                LocalFree(devDetail);
            }
        }

        devIndex++;

    } while (lastRet == TRUE);

    SetupDiDestroyDeviceInfoList(devInfo);

    *driveList = listHead;
    *numDrivesFound = devsFound;

    return devsFound > 0;
}

void TapeDestroyDriveList(PTAPE_DRIVE driveList)
{
    PTAPE_DRIVE drive = driveList;

    while (drive != NULL)
    {
        PTAPE_DRIVE toFree = drive;
        drive = drive->Next;
        LocalFree(toFree);
    }
}

BOOL TapeCheckMedia(LPCSTR tapeDrive, LPSTR mediaDesc, size_t len)
{
    CHAR drivePath[64];
    HANDLE handle;
    BOOL result = FALSE;
    BYTE cdb[10];
    BYTE dataBuffer[64];
    BYTE senseBuffer[SENSE_INFO_LEN];

    _snprintf_s(drivePath, _countof(drivePath), _TRUNCATE, "\\\\.\\%s", tapeDrive);

    handle = CreateFile(drivePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_DELETE | FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);

    if (handle == INVALID_HANDLE_VALUE)
        return FALSE;

    memset(cdb, 0, sizeof(cdb));
    memset(dataBuffer, 0, sizeof(dataBuffer));
    memset(senseBuffer, 0, sizeof(senseBuffer));

    // There doesn't appear to be a direct way to tell if there's anything in the drive, so instead we just try and read the position
    // which won't fuck up a mounted LTFS volume. If there's no tape, the drive will tell us in the sense data.

    ((PCDB)(cdb))->READ_POSITION.Operation = SCSIOP_READ_POSITION;
    ((PCDB)(cdb))->READ_POSITION.Reserved1 = 0x03;

    result = ScsiIoControl(handle, 0, cdb, sizeof(cdb), dataBuffer, sizeof(dataBuffer), SCSI_IOCTL_DATA_IN, 300, senseBuffer);

    if (!result)
    {
        CloseHandle(handle);
        return FALSE;
    }

    if (((senseBuffer[2] & 0x0F) == 0x02) && (senseBuffer[12] == 0x3A) && (senseBuffer[13] == 0x00))
    {
        strcpy_s(mediaDesc, len, "No tape loaded");
        CloseHandle(handle);
        return TRUE;
    }

    memset(cdb, 0, sizeof(cdb));
    memset(dataBuffer, 0, sizeof(dataBuffer));

    // This will only tell us the *last* tape that was in the drive, which is why we have to do the above check first

    ((PCDB)(cdb))->MODE_SENSE10.OperationCode = SCSIOP_MODE_SENSE10;
    ((PCDB)(cdb))->MODE_SENSE10.PageCode = TC_MP_MEDIUM_CONFIGURATION;
    ((PCDB)(cdb))->MODE_SENSE10.Pc = TC_MP_PC_CURRENT;
    ((PCDB)(cdb))->MODE_SENSE10.AllocationLength[0] = sizeof(dataBuffer) >> 8;
    ((PCDB)(cdb))->MODE_SENSE10.AllocationLength[1] = sizeof(dataBuffer) & 0xFF;

    result = ScsiIoControl(handle, 0, cdb, sizeof(cdb), dataBuffer, sizeof(dataBuffer), SCSI_IOCTL_DATA_IN, 300, NULL);

    if (result)
    {
        USHORT mediaType = (USHORT)dataBuffer[8] + ((USHORT)(dataBuffer[18] & 0x01) << 8);

        // I don't have a WORM cartridge to test, so only set the below bit if it's definitely not WORM.
        if (!(mediaType & 0x100))
            mediaType |= ((USHORT)(dataBuffer[3] & 0x80) << 2);

        switch (mediaType)
        {
        case 0x005E:
            strcpy_s(mediaDesc, len, "LTO8 RW");
            break;
        case 0x015E:
            strcpy_s(mediaDesc, len, "LTO8 WORM");
            break;
        case 0x025E:
            strcpy_s(mediaDesc, len, "LTO8 RO");
            break;
        case 0x005D:
            strcpy_s(mediaDesc, len, "LTOM8 RW");
            break;
        case 0x015D:
            strcpy_s(mediaDesc, len, "LTOM8 WORM");
            break;
        case 0x025D:
            strcpy_s(mediaDesc, len, "LTOM8 RO");
            break;
        case 0x005C:
            strcpy_s(mediaDesc, len, "LTO7 RW");
            break;
        case 0x015C:
            strcpy_s(mediaDesc, len, "LTO7 WORM");
            break;
        case 0x025C:
            strcpy_s(mediaDesc, len, "LTO7 RO");
            break;
        case 0x005A:
            strcpy_s(mediaDesc, len, "LTO6 RW");
            break;
        case 0x015A:
            strcpy_s(mediaDesc, len, "LTO6 WORM");
            break;
        case 0x025A:
            strcpy_s(mediaDesc, len, "LTO6 RO");
            break;
        case 0x0058:
            strcpy_s(mediaDesc, len, "LTO5 RW");
            break;
        case 0x0158:
            strcpy_s(mediaDesc, len, "LTO5 WORM");
            break;
        case 0x0258:
            strcpy_s(mediaDesc, len, "LTO5 RO");
            break;
        case 0x0046:
            strcpy_s(mediaDesc, len, "LTO4 RW");
            break;
        case 0x0146:
            strcpy_s(mediaDesc, len, "LTO4 WORM");
            break;
        case 0x0246:
            strcpy_s(mediaDesc, len, "LTO4 RO");
            break;
        case 0x0044:
            strcpy_s(mediaDesc, len, "LTO3 RW");
            break;
        case 0x0144:
            strcpy_s(mediaDesc, len, "LTO3 WORM");
            break;
        case 0x0244:
            strcpy_s(mediaDesc, len, "LTO3 RO");
            break;
        default:
            _snprintf_s(mediaDesc, len, _TRUNCATE, "Unknown media type 0x%X", mediaType);
        }
    }

    CloseHandle(handle);

    return result;
}

BOOL TapeLoad(LPCSTR tapeDrive)
{
    CHAR drivePath[64];
    HANDLE handle;
    BOOL result = FALSE;
    BYTE cdb[6];

    _snprintf_s(drivePath, _countof(drivePath), _TRUNCATE, "\\\\.\\%s", tapeDrive);

    handle = CreateFile(drivePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_DELETE | FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);

    if (handle == INVALID_HANDLE_VALUE)
        return FALSE;

    memset(cdb, 0, sizeof(cdb));

    ((PCDB)(cdb))->START_STOP.OperationCode = SCSIOP_LOAD_UNLOAD;
    ((PCDB)(cdb))->START_STOP.Start = 1;

    result = ScsiIoControl(handle, 0, cdb, sizeof(cdb), NULL, 0, SCSI_IOCTL_DATA_UNSPECIFIED, 300, NULL);
    
    CloseHandle(handle);

    return result;
}

BOOL TapeEject(LPCSTR tapeDrive)
{
    DWORD bytesReturned;
    CHAR drivePath[64];
    HANDLE handle;
    BOOL result = FALSE;

    _snprintf_s(drivePath, _countof(drivePath), _TRUNCATE, "\\\\.\\%s", tapeDrive);

    handle = CreateFile(drivePath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_DELETE | FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, 0, NULL);

    if (handle == INVALID_HANDLE_VALUE)
        return FALSE;

    result = DeviceIoControl(handle, FSCTL_LOCK_VOLUME, NULL, 0, NULL, 0, &bytesReturned, NULL);

    if (result)
    {
        result = DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, NULL, 0, NULL, 0, &bytesReturned, NULL);
    }

    if (result)
    {
        result = DeviceIoControl(handle, IOCTL_DISK_EJECT_MEDIA, NULL, 0, NULL, 0, &bytesReturned, NULL);
    }

    CloseHandle(handle);

    return result;
}


static BOOL ScsiIoControl(HANDLE hFile, DWORD deviceNumber, PVOID cdb, UCHAR cdbLength, PVOID dataBuffer, USHORT bufferLength, BYTE dataIn, ULONG timeoutValue, PVOID senseBuffer)
{
    DWORD bytesReturned;
    BOOL result = FALSE;
    BYTE scsiBuffer[sizeof(SCSI_PASS_THROUGH_DIRECT) + SENSE_INFO_LEN];

    PSCSI_PASS_THROUGH_DIRECT scsiDirect = (PSCSI_PASS_THROUGH_DIRECT)scsiBuffer;
    memset(scsiDirect, 0, sizeof(scsiBuffer));

    scsiDirect->Length = sizeof(SCSI_PASS_THROUGH_DIRECT);
    scsiDirect->CdbLength = cdbLength;
    scsiDirect->DataBuffer = dataBuffer;
    scsiDirect->SenseInfoLength = SENSE_INFO_LEN;
    scsiDirect->SenseInfoOffset = sizeof(SCSI_PASS_THROUGH_DIRECT);
    scsiDirect->DataTransferLength = bufferLength;
    scsiDirect->TimeOutValue = timeoutValue;
    scsiDirect->DataIn = dataIn;

    memcpy(scsiDirect->Cdb, cdb, cdbLength);

    result = DeviceIoControl(hFile, IOCTL_SCSI_PASS_THROUGH_DIRECT, scsiDirect, sizeof(scsiBuffer), scsiDirect, sizeof(scsiBuffer), &bytesReturned, NULL);

    if (senseBuffer)
        memcpy(senseBuffer, scsiBuffer + sizeof(SCSI_PASS_THROUGH_DIRECT), SENSE_INFO_LEN);

    return result;
}
