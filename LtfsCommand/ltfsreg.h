/*
 *   File:   ltfsreg.h
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

#pragma once

#include "pch.h"

#define MIN_DRIVE_LETTER    'D'
#define MAX_DRIVE_LETTER    'Z'

#define MAX_DEVICE_NAME     128
#define MAX_SERIAL_NUMBER   128
#define MAX_TRACE_TARGET    128
#define MAX_COMMAND_LINE    1024

BOOL LtfsRegCreateMapping(CHAR driveLetter, LPCSTR tapeDrive, LPCSTR serialNumber, LPCSTR logDir, LPCSTR workDir, BOOL showOffline);
BOOL LtfsRegUpdateMapping(CHAR driveLetter, LPCSTR newDevName);
BOOL LtfsRegRemoveMapping(CHAR driveLetter);
BOOL LtfsRegGetMappingCount(BYTE *numMappings);
BOOL LtfsRegGetMappingProperties(CHAR driveLetter, LPSTR deviceName, USHORT deviceNameLength, LPSTR serialNumber, USHORT serialNumberLength);