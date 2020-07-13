#pragma once

EXTERN_C
BOOL STDAPICALLTYPE
MixedStackTraceInitialize();

EXTERN_C
void STDAPICALLTYPE
MixedStackTraceFinalize();

EXTERN_C
DWORD STDAPICALLTYPE
MixedGetModuleFileName(
  _In_ LPVOID lpv,
  _Out_writes_(nSize) LPTSTR lpFilename,
  _In_ DWORD nSize
);

EXTERN_C
WORD STDAPICALLTYPE
MixedCaptureStackBackTrace
(
  DWORD FramesToSkip,
  DWORD nFrames,
  PVOID* BackTrace,
  PDWORD pBackTraceHash
);


