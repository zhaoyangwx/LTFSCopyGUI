/*
 *   File:   ltfsreg.c
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
#include "ltfsreg.h"
#include "util.h"

static BOOL LtfsRegGetInstallDir(LPSTR buffer, USHORT bufferLen);

BOOL LtfsRegCreateMapping(CHAR driveLetter, LPCSTR tapeDrive, LPCSTR serialNumber, LPCSTR logDir, LPCSTR workDir, BOOL showOffline)
{
    HKEY key;
    DWORD disposition;
    CHAR regKey[128];
    BOOL success = FALSE;

    _snprintf_s(regKey, _countof(regKey), _TRUNCATE, "Software\\HPE\\LTFS\\Mappings\\%c", driveLetter);

    // These registry values are read directly by FUSE4WinSvc.exe
    if (RegCreateKeyEx(HKEY_LOCAL_MACHINE, regKey, 0, NULL, 0, KEY_READ | KEY_CREATE_SUB_KEY | KEY_SET_VALUE | KEY_WOW64_64KEY, NULL, &key, &disposition) == ERROR_SUCCESS)
    {
        success = RegSetKeyValue(key, NULL, "SerialNumber", REG_SZ, serialNumber, (DWORD)(strlen(serialNumber) + 1)) == ERROR_SUCCESS;

        if (success)
        {
            success = RegSetKeyValue(key, NULL, "DeviceName", REG_SZ, tapeDrive, (DWORD)(strlen(tapeDrive) + 1)) == ERROR_SUCCESS;
        }

        if (success)
        {
            CHAR installDir[MAX_COMMAND_LINE];

            success = LtfsRegGetInstallDir(installDir, _countof(installDir));

            if (success)
            {
                char commandLine[MAX_COMMAND_LINE];
                _snprintf_s(commandLine, _countof(commandLine), _TRUNCATE,
                    "%s%sltfs.exe %c: -o devname=%s -d -o log_directory=%s -o work_directory=%s%s",
                    installDir, installDir[strlen(installDir) - 1] == '\\' ? "" : "\\", driveLetter, tapeDrive, logDir, workDir, showOffline ? " -o show_offline" : "");
                success = RegSetKeyValue(key, NULL, "CommandLine", REG_SZ, commandLine, (DWORD)(strlen(commandLine) + 1)) == ERROR_SUCCESS;
            }
        }

        if (success)
        {
            char traceTarget[MAX_TRACE_TARGET];
            _snprintf_s(traceTarget, _countof(traceTarget), _TRUNCATE, "\\\\.\\pipe\\%c", driveLetter);
            success = RegSetKeyValue(key, NULL, "TraceTarget", REG_SZ, traceTarget, (DWORD)(strlen(traceTarget) + 1)) == ERROR_SUCCESS;
        }

        if (success)
        {
            DWORD traceType = 0x00000101;
            success = RegSetKeyValue(key, NULL, "TraceType", REG_DWORD, &traceType, sizeof(traceType)) == ERROR_SUCCESS;
        }

        RegCloseKey(key);
    }

    return success;
}

BOOL LtfsRegUpdateMapping(CHAR driveLetter, LPCSTR newDevName)
{
    HKEY key;
    DWORD disposition;
    CHAR regKey[128];
    BOOL success = FALSE;

    _snprintf_s(regKey, _countof(regKey), _TRUNCATE, "Software\\HPE\\LTFS\\Mappings\\%c", driveLetter);

    if (RegCreateKeyEx(HKEY_LOCAL_MACHINE, regKey, 0, NULL, 0, KEY_READ | KEY_CREATE_SUB_KEY | KEY_SET_VALUE | KEY_WOW64_64KEY, NULL, &key, &disposition) == ERROR_SUCCESS)
    {
        CHAR oldDevName[MAX_DEVICE_NAME];
        DWORD type = REG_SZ;
        DWORD valueLen;

        valueLen = _countof(oldDevName);
        success = RegQueryValueEx(key, "DeviceName", NULL, &type, oldDevName, &valueLen) == ERROR_SUCCESS;

        if (success)
        {
            success = RegSetKeyValue(key, NULL, "DeviceName", REG_SZ, newDevName, (DWORD)(strlen(newDevName) + 1)) == ERROR_SUCCESS;
        }

        if (success)
        {
            CHAR commandLine[MAX_COMMAND_LINE];

            valueLen = _countof(commandLine);
            success = RegQueryValueEx(key, "CommandLine", NULL, &type, commandLine, &valueLen) == ERROR_SUCCESS;

            if (success)
            {
                // Now fix up the LTFS command line. Let's just sub the devname argument for now, otherwise we have to rebuild this string 
                // which would be a lot more complicated.

                CHAR oldDevArg[MAX_DEVICE_NAME];
                CHAR newDevArg[MAX_DEVICE_NAME];

                _snprintf_s(oldDevArg, _countof(regKey), _TRUNCATE, "devname=%s", oldDevName);
                _snprintf_s(newDevArg, _countof(regKey), _TRUNCATE, "devname=%s", newDevName);

                success = StringReplace(commandLine, oldDevArg, newDevArg, _countof(commandLine)) > 0;

                if (success)
                {
                    success = RegSetKeyValue(key, NULL, "CommandLine", REG_SZ, commandLine, (DWORD)(strlen(commandLine) + 1)) == ERROR_SUCCESS;
                }
            }
        }

        RegCloseKey(key);
    }

    return success;
}

BOOL LtfsRegRemoveMapping(CHAR driveLetter)
{
    char regKey[128];

    _snprintf_s(regKey, _countof(regKey), _TRUNCATE, "Software\\HPE\\LTFS\\Mappings\\%c", driveLetter);

    return RegDeleteKeyEx(HKEY_LOCAL_MACHINE, regKey, KEY_WOW64_64KEY, 0) == ERROR_SUCCESS;
}

BOOL LtfsRegGetMappingCount(BYTE *numMappings)
{
    HKEY key;
    BYTE count = 0;
    char driveLetter;
    char regKey[128];

    for (driveLetter = MIN_DRIVE_LETTER; driveLetter <= MAX_DRIVE_LETTER; driveLetter++)
    {
        _snprintf_s(regKey, _countof(regKey), _TRUNCATE, "SOFTWARE\\HPE\\LTFS\\Mappings\\%c", driveLetter);

        LRESULT result = RegOpenKeyEx(HKEY_LOCAL_MACHINE, regKey, 0, KEY_READ | KEY_WOW64_64KEY, &key);
		if (result == ERROR_SUCCESS)
        {
            count++;
            RegCloseKey(key);
        }
        else if (result != ERROR_FILE_NOT_FOUND)
        {
            return FALSE;
        }
    }

    *numMappings = count;
    return TRUE;
}

BOOL LtfsRegGetMappingProperties(CHAR driveLetter, LPSTR deviceName, USHORT deviceNameLength, LPSTR serialNumber, USHORT serialNumberLength)
{
    HKEY key;
    BYTE count = 0;
    BOOL result = FALSE;
    char regKey[128];

    _snprintf_s(regKey, _countof(regKey), _TRUNCATE, "Software\\HPE\\LTFS\\Mappings\\%c", driveLetter);

    if ((result = (RegOpenKeyEx(HKEY_LOCAL_MACHINE, regKey, 0, KEY_READ | KEY_WOW64_64KEY, &key) == ERROR_SUCCESS)))
    {
        if (deviceName && deviceNameLength)
        {
            DWORD value = deviceNameLength;
            DWORD type = REG_SZ;
            result = RegQueryValueEx(key, "DeviceName", NULL, &type, deviceName, &value) == ERROR_SUCCESS;
        }

        if (serialNumber && serialNumberLength)
        {
            DWORD value = serialNumberLength;
            DWORD type = REG_SZ;
            result = RegQueryValueEx(key, "SerialNumber", NULL, &type, serialNumber, &value) == ERROR_SUCCESS;
        }

        RegCloseKey(key);
    }

    return result;
}

static BOOL LtfsRegGetInstallDir(LPSTR buffer, USHORT bufferLen)
{
    HKEY key;
    BYTE count = 0;
    BOOL result = FALSE;
    char regKey[128];

    strcpy_s(regKey, _countof(regKey), "Software\\HPE\\LTFS");

    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, regKey, 0, KEY_READ | KEY_WOW64_64KEY, &key) == ERROR_SUCCESS)
    {
        DWORD value = bufferLen;
        DWORD type = REG_SZ;
        result = RegQueryValueEx(key, "InstallDir", NULL, &type, buffer, &value) == ERROR_SUCCESS;

        RegCloseKey(key);
    }

    return result;
}
