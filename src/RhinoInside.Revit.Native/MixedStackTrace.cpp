#include "pch.h"
#include <CorHdr.h>
#include <clrdata.h>
#include "xclrdata_h.h"

#pragma comment(lib, "CorGuids.lib")

struct FreeLibraryDeleter { void operator ()(HINSTANCE hInstance) { FreeLibrary(hInstance); } };
static std::unique_ptr<HINSTANCE__, FreeLibraryDeleter> clrData;

EXTERN_C
BOOL STDAPICALLTYPE
MixedStackTraceInitialize()
{
  if (!clrData)
  {
    auto clr = GetModuleHandle(L"CLR");

    std::wstring ClrFileName; ClrFileName.resize(2048);
    ClrFileName.resize(GetModuleFileName(clr, ClrFileName.data(), (DWORD)ClrFileName.size()));

    std::filesystem::path ClrFilePath = ClrFileName;
    ClrFilePath = ClrFilePath.remove_filename() / "mscordacwks.dll";

    clrData.reset(LoadLibrary(ClrFilePath.c_str()));
  }

  return clrData != nullptr;
}

EXTERN_C
void STDAPICALLTYPE
MixedStackTraceFinalize()
{
  clrData.reset();
}

STDAPI CLRDataCreateInstance(REFIID iid, ICLRDataTarget* target, void** iface)
{
  if (!clrData)
    return E_FAIL;

  PFN_CLRDataCreateInstance pCLRDataCreateInstance = (PFN_CLRDataCreateInstance) GetProcAddress(clrData.get(), "CLRDataCreateInstance");
  if (pCLRDataCreateInstance == nullptr)
    return E_FAIL;

  return pCLRDataCreateInstance(iid, target, iface);  
}

class ClrDataTarget : public ICLRDataTarget
{
  HANDLE m_hProcess;
public:
  ClrDataTarget(HANDLE hProcess) : m_hProcess(hProcess) {}

  HRESULT STDMETHODCALLTYPE QueryInterface(
    /* [in] */ REFIID riid,
    /* [iid_is][out] */ PVOID* ppvObject)
  {
    if (
      IsEqualIID(riid, IID_IUnknown) ||
      IsEqualIID(riid, IID_ICLRDataTarget)
      )
    {
      this->AddRef();
      *ppvObject = this;
      return S_OK;
    }
    else
    {
      *ppvObject = nullptr;
      return E_NOINTERFACE;
    }
  }

  ULONG STDMETHODCALLTYPE AddRef(void)
  {
    return 1;
  }

  ULONG STDMETHODCALLTYPE Release(void)
  {
    return 0;
  }

public:
  virtual HRESULT STDMETHODCALLTYPE GetMachineType(
    /* [out] */ ULONG32* machineType)
  {
    *machineType = IMAGE_FILE_MACHINE_AMD64;
    return S_OK;
  }

  virtual HRESULT STDMETHODCALLTYPE GetPointerSize(
    /* [out] */ ULONG32* pointerSize)
  {
    *pointerSize = sizeof(PVOID);
    return S_OK;
  }

  virtual HRESULT STDMETHODCALLTYPE GetImageBase(
    /* [string][in] */ LPCWSTR imagePath,
    /* [out] */ CLRDATA_ADDRESS* baseAddress)
  {
    *baseAddress = (CLRDATA_ADDRESS) GetModuleHandle(imagePath);

    return *baseAddress ? S_OK : HRESULT_FROM_WIN32(GetLastError());
  }

  virtual HRESULT STDMETHODCALLTYPE ReadVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [length_is][size_is][out] */ BYTE* buffer,
    /* [in] */ ULONG32 bytesRequested,
    /* [out] */ ULONG32* bytesRead)
  {
    SIZE_T NumberOfBytesRead = 0;
    if(!ReadProcessMemory(m_hProcess, (LPCVOID)address, buffer, bytesRequested, &NumberOfBytesRead))
      return HRESULT_FROM_WIN32(GetLastError());

    *bytesRead = (ULONG32)NumberOfBytesRead;
    return S_OK;    
  }

  virtual HRESULT STDMETHODCALLTYPE WriteVirtual(
    /* [in] */ CLRDATA_ADDRESS address,
    /* [size_is][in] */ BYTE* buffer,
    /* [in] */ ULONG32 bytesRequested,
    /* [out] */ ULONG32* bytesWritten)
  {
    return E_NOTIMPL;
  }

  virtual HRESULT STDMETHODCALLTYPE GetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [out] */ CLRDATA_ADDRESS* value)
  {
    return E_NOTIMPL;
  }

  virtual HRESULT STDMETHODCALLTYPE SetTLSValue(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 index,
    /* [in] */ CLRDATA_ADDRESS value)
  {
    return E_NOTIMPL;
  }

  virtual HRESULT STDMETHODCALLTYPE GetCurrentThreadID(
    /* [out] */ ULONG32* threadID)
  {
    return E_NOTIMPL;
  }

  virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextFlags,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][out] */ BYTE* context)
  {
    return E_NOTIMPL;
  }

  virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
    /* [in] */ ULONG32 threadID,
    /* [in] */ ULONG32 contextSize,
    /* [size_is][in] */ BYTE* context)
  {
    return E_NOTIMPL;
  }

  virtual HRESULT STDMETHODCALLTYPE Request(
    /* [in] */ ULONG32 reqCode,
    /* [in] */ ULONG32 inBufferSize,
    /* [size_is][in] */ BYTE* inBuffer,
    /* [in] */ ULONG32 outBufferSize,
    /* [size_is][out] */ BYTE* outBuffer)
  {
    return E_NOTIMPL;
  }
};

EXTERN_C
DWORD STDAPICALLTYPE
MixedGetModuleFileName(
  _In_ LPVOID lpv,
  _Out_writes_(nSize) LPTSTR lpFilename,
  _In_ DWORD nSize
)
{
  HMODULE hModule{};
  if
  (
    GetModuleHandleEx
    (
      GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
      (LPCTSTR)lpv,
      &hModule
    )
  )
  {
    return GetModuleFileName(hModule, lpFilename, nSize);
  }

  ClrDataTarget data_target(GetCurrentProcess());

  ULONG32 nLen = 0;
  IXCLRDataProcess* pIXCLRDataProcess = nullptr;
  if (SUCCEEDED(CLRDataCreateInstance(__uuidof(IXCLRDataProcess), &data_target, (LPVOID*)&pIXCLRDataProcess)))
  {
    IXCLRDataMethodInstance* pIXCLRDataMethodInstance = nullptr;
    {
      CLRDATA_ENUM handle{};
      if (SUCCEEDED(pIXCLRDataProcess->StartEnumMethodInstancesByAddress((CLRDATA_ADDRESS)lpv, nullptr, &handle)))
      {
        pIXCLRDataProcess->EnumMethodInstanceByAddress(&handle, &pIXCLRDataMethodInstance);
        pIXCLRDataProcess->EndEnumMethodInstancesByAddress(handle);
      }
    }

    if (pIXCLRDataMethodInstance)
    {
      IXCLRDataMethodDefinition* pIXCLRDataMethodDefinition = nullptr;
      if (SUCCEEDED(pIXCLRDataMethodInstance->GetDefinition(&pIXCLRDataMethodDefinition)) && pIXCLRDataMethodDefinition)
      {
        IXCLRDataTypeDefinition* pIXCLRDataTypeDefinition = nullptr;
        if (SUCCEEDED(pIXCLRDataMethodDefinition->GetTypeDefinition(&pIXCLRDataTypeDefinition)) && pIXCLRDataTypeDefinition)
        {
          IXCLRDataModule* pIXCLRDataModule = nullptr;
          if (SUCCEEDED(pIXCLRDataTypeDefinition->GetModule(&pIXCLRDataModule)) && pIXCLRDataModule)
          {
            pIXCLRDataModule->GetFileName(nSize, &nLen, lpFilename);
            pIXCLRDataModule->Release();
          }

          pIXCLRDataTypeDefinition->Release();
        }

        pIXCLRDataMethodDefinition->Release();
      }

      pIXCLRDataMethodInstance->Release();
    }

    pIXCLRDataProcess->Release();
  }

  return nLen;
}

EXTERN_C
WORD STDAPICALLTYPE
MixedCaptureStackBackTrace
(
  DWORD FramesToSkip,
  DWORD nFrames,
  PVOID* BackTrace,
  PDWORD pBackTraceHash
)
{
  CONTEXT ContextRecord{};
  RtlCaptureContext(&ContextRecord);

  UINT iFrame;
  for (iFrame = 0; iFrame < nFrames + FramesToSkip; iFrame++)
  {
    DWORD64 ImageBase;
    PRUNTIME_FUNCTION pFunctionEntry = RtlLookupFunctionEntry(ContextRecord.Rip, &ImageBase, nullptr);

    if (pFunctionEntry == nullptr)
      break;

    PVOID HandlerData;
    DWORD64 EstablisherFrame;
    RtlVirtualUnwind
    (
      UNW_FLAG_NHANDLER,
      ImageBase,
      ContextRecord.Rip,
      pFunctionEntry,
      &ContextRecord,
      &HandlerData,
      &EstablisherFrame,
      nullptr
    );

    if (iFrame >= FramesToSkip)
      BackTrace[iFrame - FramesToSkip] = (PVOID)ContextRecord.Rip;
  }

  return iFrame - (WORD) FramesToSkip;
}
