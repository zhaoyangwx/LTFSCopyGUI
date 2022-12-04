/*
 *   File:   tape.h
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

#define MEMBER_SIZE(type, member) sizeof(((type *)NULL)->member)

typedef struct TAPE_DRIVE
{
    UCHAR VendorId[MEMBER_SIZE(INQUIRYDATA, VendorId) + 1];
    UCHAR ProductId[MEMBER_SIZE(INQUIRYDATA, ProductId) + 1];
    UCHAR SerialNumber[128];
    DWORD DevIndex;
    struct TAPE_DRIVE * Next;
} TAPE_DRIVE, *PTAPE_DRIVE;

BOOL TapeGetDriveList(PTAPE_DRIVE *driveList, PDWORD numDrivesFound);
void TapeDestroyDriveList(PTAPE_DRIVE driveList);
BOOL TapeLoad(LPCSTR tapeDrive);
BOOL TapeEject(LPCSTR tapeDrive);
BOOL TapeCheckMedia(LPCSTR tapeDrive, LPSTR mediaDesc, size_t len);