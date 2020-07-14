#pragma once

#include <ntstatus.h>

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
#define WIN32_NO_STATUS

// Windows Header Files
#include <windows.h>
#include <tchar.h>
#include <Ntsecapi.h>

typedef struct _LDR_DLL_LOADED_NOTIFICATION_DATA {
  ULONG Flags;                          //Reserved.
  UNICODE_STRING const* FullDllName;    //The full path name of the DLL module.
  UNICODE_STRING const* BaseDllName;    //The base file name of the DLL module.
  PVOID DllBase;                        //A pointer to the base address for the DLL in memory.
  ULONG SizeOfImage;                    //The size of the DLL image, in bytes.
} LDR_DLL_LOADED_NOTIFICATION_DATA, * PLDR_DLL_LOADED_NOTIFICATION_DATA;

typedef struct _LDR_DLL_UNLOADED_NOTIFICATION_DATA {
  ULONG Flags;                          //Reserved.
  UNICODE_STRING const* FullDllName;    //The full path name of the DLL module.
  UNICODE_STRING const* BaseDllName;    //The base file name of the DLL module.
  PVOID DllBase;                        //A pointer to the base address for the DLL in memory.
  ULONG SizeOfImage;                    //The size of the DLL image, in bytes.
} LDR_DLL_UNLOADED_NOTIFICATION_DATA, * PLDR_DLL_UNLOADED_NOTIFICATION_DATA;

typedef union _LDR_DLL_NOTIFICATION_DATA {
  LDR_DLL_LOADED_NOTIFICATION_DATA Loaded;
  LDR_DLL_UNLOADED_NOTIFICATION_DATA Unloaded;
} LDR_DLL_NOTIFICATION_DATA, * PLDR_DLL_NOTIFICATION_DATA, * PLDR_DLL_NOTIFICATION_DATA;

typedef VOID(CALLBACK* PLDR_DLL_NOTIFICATION_FUNCTION)(
  ULONG                            NotificationReason,
  LDR_DLL_NOTIFICATION_DATA const* NotificationData,
  PVOID                            Context
  );

typedef NTSTATUS(NTAPI* PLDRREGISTERDLLNOTIFICATION)(
  ULONG                          Flags,
  PLDR_DLL_NOTIFICATION_FUNCTION NotificationFunction,
  PVOID                          Context,
  PVOID* Cookie
  );

typedef NTSTATUS(NTAPI* PLDRUNREGISTERDLLNOTIFICATION)(
  PVOID Cookie
  );

#define LDR_DLL_NOTIFICATION_REASON_LOADED   1
#define LDR_DLL_NOTIFICATION_REASON_UNLOADED 2
