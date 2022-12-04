/*
 *   File:   fusesvc.c
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
#include "fusesvc.h"

BOOL FuseStartService()
{
    SC_HANDLE smHandle;
    SC_HANDLE serviceHandle;
    DWORD bytesNeeded;
    LPQUERY_SERVICE_CONFIG serviceConfig = NULL;
    BOOL success = FALSE;

    smHandle = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);

    if (!smHandle)
        return FALSE;

    serviceHandle = OpenService(smHandle, "fuse4winsvc", SERVICE_ALL_ACCESS);

    if (!serviceHandle)
    {
        CloseServiceHandle(smHandle);
        return FALSE;
    }

    QueryServiceConfig(serviceHandle, NULL, 0, &bytesNeeded);
    success = (bytesNeeded > 0 && GetLastError() == ERROR_INSUFFICIENT_BUFFER);

    if (success)
    {
        // Ensure service is set to automatic start
        serviceConfig = (LPQUERY_SERVICE_CONFIG)LocalAlloc(LMEM_FIXED, bytesNeeded);
        success = QueryServiceConfig(serviceHandle, serviceConfig, bytesNeeded, &bytesNeeded);

        if (success)
        {
            if (serviceConfig->dwStartType != SERVICE_AUTO_START)
            {
                serviceConfig->dwStartType = SERVICE_AUTO_START;
                success = ChangeServiceConfig(serviceHandle, SERVICE_NO_CHANGE, serviceConfig->dwStartType, SERVICE_NO_CHANGE, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
            }
        }
    }

    if (success)
    {
        // Start service
        SERVICE_STATUS_PROCESS serviceData;
        success = QueryServiceStatusEx(serviceHandle, SC_STATUS_PROCESS_INFO, (LPBYTE)&serviceData, sizeof(serviceData), &bytesNeeded);

        if (serviceData.dwCurrentState == SERVICE_STOPPED)
        {
            success = StartService(serviceHandle, 0, NULL);

            if (success)
            {
                do
                {
                    success = QueryServiceStatusEx(serviceHandle, SC_STATUS_PROCESS_INFO, (LPBYTE)&serviceData, sizeof(serviceData), &bytesNeeded);
                    Sleep(50);
                } while (serviceData.dwCurrentState == SERVICE_START_PENDING);
            }

            if (serviceData.dwCurrentState != SERVICE_RUNNING)
                success = FALSE;

        }
        else if (serviceData.dwCurrentState != SERVICE_RUNNING)
        {
            success = FALSE;
        }
    }

    if (serviceConfig)
        LocalFree(serviceConfig);

    CloseServiceHandle(serviceHandle);
    CloseServiceHandle(smHandle);

    return success;
}

BOOL FuseStopService()
{
    SC_HANDLE smHandle;
    SC_HANDLE serviceHandle;
    DWORD bytesNeeded;
    BOOL success = FALSE;

    smHandle = OpenSCManager(NULL, NULL, SC_MANAGER_ALL_ACCESS);

    if (!smHandle)
        return FALSE;

    serviceHandle = OpenService(smHandle, "fuse4winsvc", SERVICE_ALL_ACCESS);

    if (!serviceHandle)
    {
        CloseServiceHandle(smHandle);
        return FALSE;
    }

    // Stop service
    SERVICE_STATUS_PROCESS serviceData;
    success = QueryServiceStatusEx(serviceHandle, SC_STATUS_PROCESS_INFO, (LPBYTE)&serviceData, sizeof(serviceData), &bytesNeeded);

    if (serviceData.dwCurrentState == SERVICE_RUNNING)
    {
        SERVICE_STATUS serviceStatus;
        success = ControlService(serviceHandle, SERVICE_CONTROL_STOP, &serviceStatus);

        if (success)
        {
            do
            {
                success = QueryServiceStatusEx(serviceHandle, SC_STATUS_PROCESS_INFO, (LPBYTE)&serviceData, sizeof(serviceData), &bytesNeeded);
                Sleep(50);
            } while (serviceData.dwCurrentState == SERVICE_STOP_PENDING);
        }

        if (serviceData.dwCurrentState != SERVICE_STOPPED)
            success = FALSE;

    }
    else if (serviceData.dwCurrentState != SERVICE_STOPPED)
    {
        success = FALSE;
    }

    CloseServiceHandle(serviceHandle);
    CloseServiceHandle(smHandle);

    return success;
}
