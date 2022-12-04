/*
 *   File:   main.c
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
#include "ltfsreg.h"
#include "fusesvc.h"
#include "util.h"
#include "getopt.h"

#define DEFAULT_LOG_DIR    "C:\\ProgramData\\HPE\\LTFS"
#define DEFAULT_WORK_DIR   "C:\\tmp\\LTFS"

typedef enum
{
    None,
    ListDrives,
    ListMappings,
    MapDrive,
    UnmapDrive,
    Start,
    Stop,
    Load,
    Remap,
    LoadOnly,
    Mount,
    Eject,
    CheckMedia
} Operation;

static int ListTapeDrives();
static int ListDriveMappings();
static int StartLtfsService();
static int StopLtfsService();
static int RemapTapeDrives();
static int MapTapeDrive(CHAR driveLetter, LPCSTR tapeDrive, BYTE tapeIndex, LPCSTR logDir, LPCSTR workDir, BOOL showOffline);
static int UnmapTapeDrive(CHAR driveLetter);
static int LoadTapeDrive(CHAR driveLetter, BOOL mount);
static int EjectTapeDrive(CHAR driveLetter);
static int MountTapeDrive(CHAR driveLetter);
static int CheckTapeMedia(CHAR driveLetter);
LPSTR _GetTapeDriveList();
LPSTR _GetDriveMappings();
LPSTR _StartLtfsService();
LPSTR _StopLtfsService();
LPSTR _RemapTapeDrives();
LPSTR _MapTapeDrive(CHAR driveLetter, LPCSTR tapeDrive, BYTE tapeIndex, LPCSTR logDir, LPCSTR workDir, BOOL showOffline);
LPSTR _UnmapTapeDrive(CHAR driveLetter);
LPSTR _LoadTapeDrive(CHAR driveLetter, BOOL mount);
LPSTR _EjectTapeDrive(CHAR driveLetter);
LPSTR _MountTapeDrive(CHAR driveLetter);
LPSTR _CheckTapeMedia(CHAR driveLetter);
LPSTR _Test(CHAR a)
{
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "Test%cTest", a);

	return (LPSTR)Buffer;
}

int main(int argc, char *argv[])
{
    int opt = 0;
    Operation operation = None;
    BOOL showOffline = TRUE;
    BOOL driveLetterArgFound = FALSE;
    BOOL tapeDriveArgFound = FALSE;
    BYTE tapeIndex;
    CHAR driveName[8];
    CHAR driveLetter;
    LPCSTR logDir = DEFAULT_LOG_DIR;
    LPCSTR workDir = DEFAULT_WORK_DIR;

    if (!IsElevated())
    {
        fprintf(stderr, "\r\nThis process requires elevation.\r\n");
        return EXIT_FAILURE;
    }

    while ((opt = getopt(argc, argv, "o:d:t:l:w:nh?")) != -1)
    {
        switch (opt)
        {
        case 'o':
        {
            if (!_stricmp(optarg, "listdrives"))
                operation = ListDrives;
            else if (!_stricmp(optarg, "listmappings"))
                operation = ListMappings;
            else if (!_stricmp(optarg, "map"))
                operation = MapDrive;
            else if (!_stricmp(optarg, "unmap"))
                operation = UnmapDrive;
            else if (!_stricmp(optarg, "remap"))
                operation = Remap;
            else if (!_stricmp(optarg, "start"))
                operation = Start;
            else if (!_stricmp(optarg, "stop"))
                operation = Stop;
            else if (!_stricmp(optarg, "load"))
                operation = Load;
            else if (!_stricmp(optarg, "loadonly"))
                operation = LoadOnly;
            else if (!_stricmp(optarg, "mount"))
                operation = Mount;
            else if (!_stricmp(optarg, "eject"))
                operation = Eject;
            else if (!_stricmp(optarg, "checkmedia"))
                operation = CheckMedia;
            else
            {
                fprintf(stderr, "\r\nInvalid operation.\r\n");
                return EXIT_FAILURE;
            }
            break;
        }
        case 'd':
        {
            if (strlen(optarg) != 2 || optarg[1] != ':')
            {
                fprintf(stderr, "\r\nInvalid format for drive letter argument.\r\n");
                return EXIT_FAILURE;
            }

            driveLetter = toupper(optarg[0]);

            if (optarg[0] < 'D' || optarg[1] > 'Z')
            {
                fprintf(stderr, "\r\nInvalid drive letter.\r\n");
                return EXIT_FAILURE;
            }

            driveLetterArgFound = TRUE;
            break;
        }
        case 'n':
        {
            showOffline = FALSE;
            break;
        }
        case 'l':
        {
            logDir = optarg;
            break;
        }
        case 'w':
        {
            workDir = optarg;
            break;
        }
        case 't':
        {
            strcpy_s(driveName, sizeof(driveName), optarg);
            _strupr_s(driveName, sizeof(driveName));

            if (strlen(driveName) != 5 || strncmp(driveName, "TAPE", 4) != 0)
            {
                fprintf(stderr, "\r\nInvalid format for tape drive argument.\r\n");
                return EXIT_FAILURE;
            }

            tapeIndex = driveName[4];

            if (tapeIndex < '0' || tapeIndex > '9')
            {
                fprintf(stderr, "\r\nInvalid tape drive index\r\n");
                return EXIT_FAILURE;
            }

            tapeIndex -= '0';

            tapeDriveArgFound = TRUE;
            break;
        }
        default:
            {
			    fprintf(stderr, "\r\nUsage: %s -o operation [options]\r\n\r\n"
                    "List tape drives:\r\n\r\n"
                    "\t%s -o listdrives\r\n\r\n"
                    "List mappings:\r\n\r\n"
                    "\t%s -o listmappings\r\n\r\n"
                    "Map tape drive:\r\n\r\n"
                    "\t%s -o map -d DRIVE: -t TAPEn [-n]\r\n"
                    "\t\t[-l logdir] [-w workdir]\r\n\r\n"
                    "\tReplace DRIVE: with your intended drive letter i.e. T:\r\n"
                    "\tReplace TAPEn with the tape device name returned from the list\r\n"
                    "\tdrives operation i.e. TAPE0.\r\n\r\n"
                    "\tPass -n to show all files as 'online'. Not recommended.\r\n"
                    "\tPass -l and/or -w to override default log and working\r\n"
                    "\tdirectories.\r\n\r\n"
                    "Unmap tape drive:\r\n\r\n"
                    "\t%s -o unmap -d DRIVE:\r\n\r\n"
                    "Fix existing mappings:\r\n\r\n"
                    "\t%s -o remap\r\n\r\n"
                    "\tIn some cases, particularly when drives are hot-plugged, the\r\n"
                    "\tdevice index may change i.e. from TAPE0 to TAPE1, breaking an\r\n"
                    "\texisting mapping. This operation will repair existing mappings.\r\n\r\n"
                    "Start FUSE/LTFS service:\r\n\r\n"
                    "\t%s -o start\r\n\r\n"
                    "\tIf the operating system was booted with the tape drive powered\r\n"
                    "\toff or disconnected, filesystem services will not have started.\r\n"
                    "\tUse this operation to start them.\r\n\r\n"
                    "Stop FUSE/LTFS service:\r\n\r\n"
                    "\t%s -o stop\r\n\r\n"
                    "Physically load tape and mount filesystem:\r\n\r\n"
                    "\t%s -o load -d DRIVE:\r\n\r\n"
                    "Physically load tape without mounting filesystem:\r\n\r\n"
                    "\t%s -o loadonly -d DRIVE:\r\n\r\n"
                    "\tUse this if you intend to format the tape immediately.\r\n\r\n"
                    "Mount filesystem:\r\n\r\n"
                    "\t%s -o mount -d DRIVE:\r\n\r\n"
                    "\tNote that 'mounting' is a vague concept under Windows.\r\n"
                    "\tThis operation is equivalent double clicking the drive icon in\r\n"
                    "\tWindows explorer, which will cause LTFS to read the inserted\r\n"
                    "\ttape and report size/usage/label information back to the \r\n"
                    "\toperating system.\r\n\r\n"
                    "Unmount filesystem and physically eject tape:\r\n\r\n"
                    "\t%s -o eject -d DRIVE:\r\n\r\n"
                    "Check if a tape is loaded and report type:\r\n\r\n"
                    "\t%s -o checkmedia -d DRIVE:\r\n\r\n"
                    , argv[0], argv[0], argv[0], argv[0], argv[0], argv[0], argv[0], argv[0], argv[0], argv[0], argv[0], argv[0], argv[0]);
                return EXIT_FAILURE;
            }
        }
    }

    if (operation == None)
    {
        fprintf(stderr, "\r\nNo operation specified.\r\n");
        return EXIT_FAILURE;
    }

    if (operation == MapDrive ||
        operation == UnmapDrive ||
        operation == Load ||
        operation == LoadOnly ||
        operation == Mount ||
        operation == Eject ||
        operation == CheckMedia)
    {
        if (!driveLetterArgFound)
        {
            fprintf(stderr, "\r\nDrive letter not specified.\r\n");
            return EXIT_FAILURE;
        }
    }

    if (operation == MapDrive)
    {
        if (!tapeDriveArgFound)
        {
            fprintf(stderr, "\r\nTape drive not specified.\r\n");
            return EXIT_FAILURE;
        }
    }

    switch (operation)
    {
    case ListDrives:
        return ListTapeDrives();

    case ListMappings:
        return ListDriveMappings();

    case Start:
        return StartLtfsService();

    case Stop:
        return StopLtfsService();

    case MapDrive:
        return MapTapeDrive(driveLetter, driveName, tapeIndex, logDir, workDir, showOffline);

    case UnmapDrive:
        return UnmapTapeDrive(driveLetter);

    case Remap:
        return RemapTapeDrives();

    case Load:
        return LoadTapeDrive(driveLetter, TRUE);

    case LoadOnly:
        return LoadTapeDrive(driveLetter, FALSE);

    case Mount:
        return MountTapeDrive(driveLetter);

    case Eject:
        return EjectTapeDrive(driveLetter);

    case CheckMedia:
        return CheckTapeMedia(driveLetter);
    }
}

static int ListTapeDrives()
{
    PTAPE_DRIVE driveList;
    DWORD numDrivesFound;

    if (TapeGetDriveList(&driveList, &numDrivesFound))
    {
        printf("\r\nCurrently attached tape drives:\r\n\r\n");

        PTAPE_DRIVE drive = driveList;

        while (drive != NULL)
        {
            printf("TAPE%d: [%s] %s %s\r\n", drive->DevIndex, drive->SerialNumber, drive->VendorId, drive->ProductId);
            drive = drive->Next;
        }

        TapeDestroyDriveList(driveList);

        return EXIT_SUCCESS;
    }

    printf("\r\nNo tape drives found.\r\n");
    return EXIT_SUCCESS;
}

LPSTR _GetTapeDriveList()
{
	PTAPE_DRIVE driveList;
	DWORD numDrivesFound;
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
	if (TapeGetDriveList(&driveList, &numDrivesFound))
	{
		PTAPE_DRIVE drive = driveList;
		while (drive != NULL)
		{
			sprintf(Buffer + strlen(Buffer), "%d|%s|%s|%s\r\n", drive->DevIndex, drive->SerialNumber, drive->VendorId, drive->ProductId);
			drive = drive->Next;
		}

		TapeDestroyDriveList(driveList);
	}
	return Buffer;
}

static int ListDriveMappings()
{
    CHAR driveLetter;
    BYTE numMappings;

    if (!LtfsRegGetMappingCount(&numMappings))
    {
        fprintf(stderr, "\r\nFailed to get mappings from registry.\r\n");
        return EXIT_FAILURE;
    }

    if (!numMappings)
    {
        printf("\r\nNo mappings found.\r\n");
        return EXIT_SUCCESS;
    }

    printf("\r\nCurrent drive mappings:\r\n\r\n");

    for (driveLetter = MIN_DRIVE_LETTER; driveLetter <= MAX_DRIVE_LETTER; driveLetter++)
    {
        char serialNumber[MAX_SERIAL_NUMBER];
        char devName[MAX_DEVICE_NAME];

        if (LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), serialNumber, _countof(serialNumber)))
        {
            printf("%c: %s [%s]\r\n", driveLetter, devName, serialNumber);
        }
    }

    return EXIT_SUCCESS;
}

LPSTR _GetDriveMappings()
{
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
	CHAR driveLetter;
	BYTE numMappings;

	if (!LtfsRegGetMappingCount(&numMappings))
	{
		return Buffer;
		//fprintf(stderr, "\r\nFailed to get mappings from registry.\r\n");
		//return EXIT_FAILURE;
	}

	if (!numMappings)
	{
		return Buffer;
		//printf("\r\nNo mappings found.\r\n");
		//return EXIT_SUCCESS;
	}

	//printf("\r\nCurrent drive mappings:\r\n\r\n");

	for (driveLetter = MIN_DRIVE_LETTER; driveLetter <= MAX_DRIVE_LETTER; driveLetter++)
	{
		char serialNumber[MAX_SERIAL_NUMBER];
		char devName[MAX_DEVICE_NAME];

		if (LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), serialNumber, _countof(serialNumber)))
		{
			sprintf(Buffer + strlen(Buffer), "%c|%s|%s\r\n", driveLetter, devName, serialNumber);
		}
	}
	return Buffer;

}

static int StartLtfsService()
{
    BOOL success = FuseStartService();

    if (!success)
    {
        fprintf(stderr, "\r\nFailed to start service.\r\n");
        return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}

LPSTR _StartLtfsService() {

	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");

	BOOL success = FuseStartService();
	if (!success)
	{
		sprintf(Buffer + strlen(Buffer),"Failed to start service.");
	}

	return Buffer;
}

static int StopLtfsService()
{
    BOOL success = FuseStopService();

    if (!success)
    {
        fprintf(stderr, "\r\nFailed to stop service.\r\n");
        return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}

LPSTR _StopLtfsService() {

	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");

	BOOL success = FuseStopService();
	if (!success)
	{
		sprintf(Buffer + strlen(Buffer), "Failed to stop service.");
	}

	return Buffer;
}

static int MapTapeDrive(CHAR driveLetter, LPCSTR tapeDrive, BYTE tapeIndex, LPCSTR logDir, LPCSTR workDir, BOOL showOffline)
{
    PTAPE_DRIVE driveList;
    DWORD numDrivesFound;
    BOOL success = FALSE;
    BOOL driveFound = FALSE;

    if (PollFileSystem(driveLetter))
    {
        fprintf(stderr, "\r\nDrive letter %c: already in use.\r\n", driveLetter);
        return EXIT_FAILURE;
    }

    if (TapeGetDriveList(&driveList, &numDrivesFound))
    {
        PTAPE_DRIVE drive = driveList;

        while (drive != NULL)
        {
            if (drive->DevIndex == tapeIndex)
            {
                driveFound = TRUE;

                if (LtfsRegGetMappingProperties(driveLetter, NULL, 0, NULL, 0))
                {
                    fprintf(stderr, "\r\nMapping for %c: arleady exists.\r\n", driveLetter);
                }
                else
                {
                    success = LtfsRegCreateMapping(driveLetter, tapeDrive, drive->SerialNumber, logDir, workDir, showOffline);

                    if (!success)
                        fprintf(stderr, "\r\nFailed to create registry entries.\r\n");
                }

                break;
            }

            drive = drive->Next;
        }

        TapeDestroyDriveList(driveList);

        if (!driveFound)
        {
            fprintf(stderr, "\r\nDrive %s not found.\r\n", tapeDrive);
            return EXIT_FAILURE;
        }

        if (!success)
        {
            return EXIT_FAILURE;
        }

        success = FuseStopService();

        if (!success)
        {
            fprintf(stderr, "\r\nFailed to stop LTFS service.\r\n");
            return EXIT_FAILURE;
        }

        success = FuseStartService();

        if (!success)
        {
            fprintf(stderr, "\r\nFailed to start LTFS service.\r\n");
            return EXIT_FAILURE;
        }

        // Need to check file system here

        return EXIT_SUCCESS;
    }

    fprintf(stderr, "\r\nNo tape drives found.\r\n");
    return EXIT_FAILURE;
}

LPSTR _MapTapeDrive(CHAR driveLetter, LPCSTR tapeDrive, BYTE tapeIndex, LPCSTR logDir, LPCSTR workDir, BOOL showOffline) {
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
    sprintf(Buffer + strlen(Buffer), "");

	PTAPE_DRIVE driveList;
	DWORD numDrivesFound;
	BOOL success = FALSE;
	BOOL driveFound = FALSE;

	if (PollFileSystem(driveLetter))
	{
		sprintf(Buffer + strlen(Buffer), "Drive letter %c: already in use.\r\n", driveLetter);
		return Buffer;
	}

	if (TapeGetDriveList(&driveList, &numDrivesFound))
	{
		PTAPE_DRIVE drive = driveList;

		while (drive != NULL)
		{
			if (drive->DevIndex == tapeIndex)
			{
				driveFound = TRUE;

				if (LtfsRegGetMappingProperties(driveLetter, NULL, 0, NULL, 0))
				{
					sprintf(Buffer + strlen(Buffer), "Mapping for %c: arleady exists.\r\n", driveLetter);
				}
				else
				{
					success = LtfsRegCreateMapping(driveLetter, tapeDrive, drive->SerialNumber, logDir, workDir, showOffline);

					if (!success)
						sprintf(Buffer + strlen(Buffer), "Failed to create registry entries.\r\n");
				}

				break;
			}

			drive = drive->Next;
		}

		TapeDestroyDriveList(driveList);

		if (!driveFound)
		{
			sprintf(Buffer + strlen(Buffer), "Drive %s not found.\r\n", tapeDrive);
			return Buffer;
		}

		if (!success)
		{
			return Buffer;
		}

		success = FuseStopService();

		if (!success)
		{
			sprintf(Buffer + strlen(Buffer), "Failed to stop LTFS service.\r\n");
			return Buffer;
		}

		success = FuseStartService();

		if (!success)
		{
			sprintf(Buffer + strlen(Buffer), "Failed to start LTFS service.\r\n");
			return Buffer;
		}

		// Need to check file system here

		return Buffer;
	}

	sprintf(Buffer + strlen(Buffer), "No tape drives found.\r\n");
	return Buffer;
}

static int UnmapTapeDrive(CHAR driveLetter)
{
    BYTE numMappings;
    BOOL success;

    success = LtfsRegGetMappingCount(&numMappings);

    if (!success)
    {
        fprintf(stderr, "\r\nFailed to get mappings from registry.\r\n");
        return EXIT_FAILURE;
    }

    if (!numMappings)
    {
        fprintf(stderr, "\r\nNo drives currently mapped.\r\n");
        return EXIT_FAILURE;
    }

    success = LtfsRegRemoveMapping(driveLetter);

    if (!success)
    {
        fprintf(stderr, "\r\nFailed to remove mapping from registry.\r\n");
        return EXIT_FAILURE;
    }

    numMappings--;

    success = FuseStopService();
    if (!success)
    {
        fprintf(stderr, "\r\nFailed to stop LTFS service.\r\n");
        return EXIT_FAILURE;
    }

    if (numMappings > 0)
    {
        success = FuseStartService();

        if (!success)
        {
            fprintf(stderr, "\r\nFailed to start LTFS service.\r\n");
            return EXIT_FAILURE;
        }
    }

    return success;
}

LPSTR _UnmapTapeDrive(CHAR driveLetter) {
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
	sprintf(Buffer + strlen(Buffer), "");
	BYTE numMappings;
	BOOL success;

	success = LtfsRegGetMappingCount(&numMappings);

	if (!success)
	{
		sprintf(Buffer + strlen(Buffer), "Failed to get mappings from registry.\r\n");
		return Buffer;
	}

	if (!numMappings)
	{
		sprintf(Buffer + strlen(Buffer), "No drives currently mapped.\r\n");
		return Buffer;
	}

	success = LtfsRegRemoveMapping(driveLetter);

	if (!success)
	{
		sprintf(Buffer + strlen(Buffer), "Failed to remove mapping from registry.\r\n");
		return Buffer;
	}

	numMappings--;

	success = FuseStopService();
	if (!success)
	{
		sprintf(Buffer + strlen(Buffer), "Failed to stop LTFS service.\r\n");
		return Buffer;
	}

	if (numMappings > 0)
	{
		success = FuseStartService();

		if (!success)
		{
			sprintf(Buffer + strlen(Buffer), "Failed to start LTFS service.\r\n");
			return Buffer;
		}
	}

	return Buffer;
}

static int RemapTapeDrives()
{
    PTAPE_DRIVE driveList;
    
    BYTE changesMade = 0;
    DWORD numDrivesFound;
    BOOL success = TRUE;
    BOOL firstOutput = TRUE;

    if (TapeGetDriveList(&driveList, &numDrivesFound))
    {
        PTAPE_DRIVE drive = driveList;

        while (drive != NULL)
        {
            CHAR driveLetter;
            char devName[MAX_DEVICE_NAME];
            _snprintf_s(devName, _countof(devName), _TRUNCATE, "TAPE%d", drive->DevIndex);

            for (driveLetter = MIN_DRIVE_LETTER; driveLetter <= MAX_DRIVE_LETTER; driveLetter++)
            {
                CHAR regSerialNumber[MAX_SERIAL_NUMBER];
                CHAR regDevName[MAX_DEVICE_NAME];

                if (LtfsRegGetMappingProperties(driveLetter, regDevName, _countof(regDevName), regSerialNumber, _countof(regSerialNumber)))
                {
                    if (strcmp(regSerialNumber, drive->SerialNumber) == 0 && strcmp(regDevName, devName) != 0)
                    {
                        success = LtfsRegUpdateMapping(driveLetter, devName);

                        if (!success)
                        {
                            fprintf(stderr, "\r\nFailed to update existing mapping for %c:\r\n", driveLetter);
                        }

                        if (success)
                        {
                            if (firstOutput)
                            {
                                firstOutput = FALSE;
                                printf("\r\n");
                            }

                            printf("%c: %s [%s] -> %s\r\n", driveLetter, regDevName, regSerialNumber, devName);
                            changesMade++;
                        }
                    }
                }
            }

            drive = drive->Next;
        }

        printf("\r\n%d mapping(s) updated.\r\n", changesMade);

        if (success)
        {
            if (changesMade)
            {
                success = FuseStopService();

                if (!success)
                {
                    fprintf(stderr, "\r\nFailed to stop LTFS service.\r\n");
                    return EXIT_FAILURE;
                }
            }

            success = FuseStartService();

            if (!success)
            {
                fprintf(stderr, "\r\nFailed to start LTFS service.\r\n");
                return EXIT_FAILURE;
            }

            // Need to check file system here
        }

        TapeDestroyDriveList(driveList);
        return EXIT_SUCCESS;
    }

    // No point in the service running if there are no drives.
    FuseStopService();

    fprintf(stderr, "\r\nNo tape drives found.\r\n");
    return EXIT_FAILURE;
}

LPSTR _RemapTapeDrives() {

	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");

	PTAPE_DRIVE driveList;

	BYTE changesMade = 0;
	DWORD numDrivesFound;
	BOOL success = TRUE;
	BOOL firstOutput = TRUE;

	if (TapeGetDriveList(&driveList, &numDrivesFound))
	{
		PTAPE_DRIVE drive = driveList;

		while (drive != NULL)
		{
			CHAR driveLetter;
			char devName[MAX_DEVICE_NAME];
			_snprintf_s(devName, _countof(devName), _TRUNCATE, "TAPE%d", drive->DevIndex);

			for (driveLetter = MIN_DRIVE_LETTER; driveLetter <= MAX_DRIVE_LETTER; driveLetter++)
			{
				CHAR regSerialNumber[MAX_SERIAL_NUMBER];
				CHAR regDevName[MAX_DEVICE_NAME];

				if (LtfsRegGetMappingProperties(driveLetter, regDevName, _countof(regDevName), regSerialNumber, _countof(regSerialNumber)))
				{
					if (strcmp(regSerialNumber, drive->SerialNumber) == 0 && strcmp(regDevName, devName) != 0)
					{
						success = LtfsRegUpdateMapping(driveLetter, devName);

						if (!success)
						{
							sprintf(Buffer + strlen(Buffer), "Failed to update existing mapping for %c:\r\n", driveLetter);
						}

						if (success)
						{
							if (firstOutput)
							{
								firstOutput = FALSE;
								//printf("\r\n");
							}

							sprintf(Buffer + strlen(Buffer), "%c: %s [%s] -> %s\r\n", driveLetter, regDevName, regSerialNumber, devName);
							changesMade++;
						}
					}
				}
			}

			drive = drive->Next;
		}

		sprintf(Buffer + strlen(Buffer), "%d mapping(s) updated.\r\n", changesMade);

		if (success)
		{
			if (changesMade)
			{
				success = FuseStopService();

				if (!success)
				{
					sprintf(Buffer + strlen(Buffer), "Failed to stop LTFS service.\r\n");
					return Buffer;
				}
			}

			success = FuseStartService();

			if (!success)
			{
				sprintf(Buffer + strlen(Buffer), "Failed to start LTFS service.\r\n");
				return Buffer;
			}

			// Need to check file system here
		}

		TapeDestroyDriveList(driveList);
		return Buffer;
	}

	// No point in the service running if there are no drives.
	FuseStopService();

	sprintf(Buffer + strlen(Buffer), "No tape drives found.\r\n");
	return Buffer;
}

static int LoadTapeDrive(CHAR driveLetter, BOOL mount)
{
    char devName[MAX_DEVICE_NAME];
    BOOL result = FALSE;

    result = LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), NULL, 0);

    if (!result)
    {
        fprintf(stderr, "\r\nMapping for %c: does not exist.\r\n", driveLetter);
        return EXIT_FAILURE;
    }

    result = TapeLoad(devName);

    if (!result)
    {
        return EXIT_FAILURE;
    }

    if (mount)
    {
        result = PollFileSystem(driveLetter);

        if (!result)
        {
            fprintf(stderr, "\r\nCannot start file system. LTFS not running.\r\n");
            return EXIT_FAILURE;
        }
    }

    return EXIT_SUCCESS;
}

LPSTR _LoadTapeDrive(CHAR driveLetter, BOOL mount) {
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
	sprintf(Buffer + strlen(Buffer), "");
	char devName[MAX_DEVICE_NAME];
	BOOL result = FALSE;

	result = LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), NULL, 0);

	if (!result)
	{
		sprintf(Buffer + strlen(Buffer), "Mapping for %c: does not exist.\r\n", driveLetter);
		return Buffer;
	}

	result = TapeLoad(devName);

	if (!result)
	{
		return Buffer;
	}

	if (mount)
	{
		result = PollFileSystem(driveLetter);

		if (!result)
		{
			sprintf(Buffer + strlen(Buffer), "Cannot start file system. LTFS not running.\r\n");
			return Buffer;
		}
	}
	return Buffer;
}

static int MountTapeDrive(CHAR driveLetter)
{
    BOOL result = PollFileSystem(driveLetter);

    if (!result)
    {
        fprintf(stderr, "\r\nCannot start file system. LTFS not running.\r\n");
        return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}

LPSTR _MountTapeDrive(CHAR driveLetter) {
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
	sprintf(Buffer + strlen(Buffer), "");
	BOOL result = PollFileSystem(driveLetter);

	if (!result)
	{
		sprintf(Buffer + strlen(Buffer), "Cannot start file system. LTFS not running.\r\n");
		return Buffer;
	}

	return Buffer;
}

static int EjectTapeDrive(CHAR driveLetter)
{
    CHAR devName[MAX_DEVICE_NAME];
    BOOL result = FALSE;

    result = LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), NULL, 0);

    if (!result)
    {
        fprintf(stderr, "\r\nMapping for %c: does not exist.\r\n", driveLetter);
        return EXIT_FAILURE;
    }

    // This could do more detailed error reporting, and perhaps the ability to force dismount if files are still open.
    result = TapeEject(devName);

    if (!result)
    {
        fprintf(stderr, "\r\nFailed to eject tape. Ensure no files are open on the target volume.\r\n");
        return EXIT_FAILURE;
    }

    // Not sure why LTFSConfigurator.exe does this, but we'll do it too.
    result = PollFileSystem(driveLetter);

    if (!result)
    {
        return EXIT_FAILURE;
    }

    return EXIT_SUCCESS;
}

LPSTR _EjectTapeDrive(CHAR driveLetter) {
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
	sprintf(Buffer + strlen(Buffer), "");
	CHAR devName[MAX_DEVICE_NAME];
	BOOL result = FALSE;

	result = LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), NULL, 0);

	if (!result)
	{
		sprintf(Buffer + strlen(Buffer), "Mapping for %c: does not exist.\r\n", driveLetter);
		return Buffer;
	}

	// This could do more detailed error reporting, and perhaps the ability to force dismount if files are still open.
	result = TapeEject(devName);

	if (!result)
	{
		sprintf(Buffer + strlen(Buffer), "Failed to eject tape. Ensure no files are open on the target volume.\r\n");
		return Buffer;
	}

	// Not sure why LTFSConfigurator.exe does this, but we'll do it too.
	result = PollFileSystem(driveLetter);

	if (!result)
	{
		return Buffer;
	}

	return Buffer;
}

static int CheckTapeMedia(CHAR driveLetter)
{
    CHAR mediaDesc[128];
    CHAR devName[MAX_DEVICE_NAME];

    BOOL result = FALSE;

    result = LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), NULL, 0);

    if (!result)
    {
        fprintf(stderr, "\r\nMapping for %c: does not exist.\r\n", driveLetter);
        return EXIT_FAILURE;
    }

    result = TapeCheckMedia(devName, mediaDesc, _countof(mediaDesc));

    if (!result)
    {
        fprintf(stderr, "\r\nMedia check failed.\r\n");
        return EXIT_FAILURE;
    }

    printf("\r\n%s: %s\r\n", devName, mediaDesc);
    return EXIT_SUCCESS;
}

LPSTR _CheckTapeMedia(CHAR driveLetter) {
	LPSTR Buffer = (LPSTR)malloc(32768);
	sprintf(Buffer, "");
	sprintf(Buffer + strlen(Buffer), "");
	CHAR mediaDesc[128];
	CHAR devName[MAX_DEVICE_NAME];

	BOOL result = FALSE;

	result = LtfsRegGetMappingProperties(driveLetter, devName, _countof(devName), NULL, 0);

	if (!result)
	{
		sprintf(Buffer + strlen(Buffer), "Mapping for %c: does not exist.\r\n", driveLetter);
		return Buffer;
	}

	result = TapeCheckMedia(devName, mediaDesc, _countof(mediaDesc));

	if (!result)
	{
		sprintf(Buffer + strlen(Buffer), "Media check failed.\r\n");
		return Buffer;
	}

	sprintf(Buffer + strlen(Buffer), "%s: %s\r\n", devName, mediaDesc);
	return Buffer;
}