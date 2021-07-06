#include "pch.h"
#include <versionhelpers.h>
#include "MixedStackTrace.h"

extern HMODULE hInstance;

static PLDRREGISTERDLLNOTIFICATION LdrRegisterDllNotification = (PLDRREGISTERDLLNOTIFICATION) GetProcAddress(GetModuleHandle(_T("NTDLL")), "LdrRegisterDllNotification");
static PLDRUNREGISTERDLLNOTIFICATION LdrUnregisterDllNotification = (PLDRUNREGISTERDLLNOTIFICATION) GetProcAddress(GetModuleHandle(_T("NTDLL")), "LdrUnregisterDllNotification");

class LdrDllTracker
{
  LPVOID mCookie = nullptr;
  std::wstring mStackTraceFilePath;
  std::set<std::wstring> mModuleSet;

  static VOID CALLBACK LdrDllNotification
  (
    ULONG                       NotificationReason,
    LDR_DLL_NOTIFICATION_DATA   const* NotificationData,
    PVOID                       Context
  )
  {
    if (NotificationReason == LDR_DLL_NOTIFICATION_REASON_LOADED)
    {
      if(Instance.ReportOnLoad(NotificationData->Loaded.BaseDllName->Buffer))
      {
        std::wstringstream buffer;
        buffer.imbue(std::locale::classic());

        // Timestamp
        {
          auto now = std::chrono::system_clock::now();
          auto epoch_seconds = std::chrono::system_clock::to_time_t(now);
          auto truncated = std::chrono::system_clock::from_time_t(epoch_seconds);
          auto delta_us = std::chrono::duration_cast<std::chrono::microseconds>(now - truncated).count();

          buffer << "**";
          buffer << std::put_time(gmtime(&epoch_seconds), L"%FT%T");
          buffer << "." << std::fixed << std::setw(6) << std::setfill(L'0') << delta_us << 'Z';
          buffer << "**:";
          buffer << " Loaded '" << NotificationData->Loaded.FullDllName->Buffer << "'.  " << std::endl;
        }

        buffer << L"> Dumping stack backtrace…" << std::endl;
        buffer << "> ```" << std::endl;
        for (const auto &trace : StackBackTrace(1))
          buffer << "> " << trace;
        buffer << "> ```" << std::endl;

        Instance.ReportOnLoad(NotificationData->Loaded.BaseDllName->Buffer, false);

        // Dump stack trace to file
        auto& stack_trace_file_path = Instance.StackTraceFilePath();
        if (stack_trace_file_path.empty())
        {
#ifdef _DEBUG
          OutputDebugString(buffer.str().c_str());
#endif
        }
        else
        {
          std::ofstream log_file(stack_trace_file_path, std::fstream::app | std::fstream::out);
          log_file.imbue(std::locale::classic());

          if (log_file.is_open())
          {
            std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>, wchar_t> conversion;
            log_file << conversion.to_bytes(buffer.str());
          }
        }

        //HANDLE hFile = CreateFile
        //(
        //  Instance.StackTraceFilePath().c_str(),
        //  GENERIC_WRITE,
        //  0,
        //  nullptr,
        //  CREATE_ALWAYS,
        //  FILE_ATTRIBUTE_NORMAL,
        //  0
        //);

        //if (hFile != INVALID_HANDLE_VALUE)
        //{
        //  // Fake an exception to call MiniDumpWriteDump.
        //  CONTEXT ContextRecord {};
        //  RtlCaptureContext(&ContextRecord);

        //  EXCEPTION_RECORD ExceptionRecord
        //  {
        //    (DWORD) STATUS_DLL_MIGHT_BE_INCOMPATIBLE,
        //    EXCEPTION_NONCONTINUABLE,
        //    nullptr,
        //    _ReturnAddress(),
        //    0,
        //    {}
        //  };

        //  EXCEPTION_POINTERS ExceptionPointers
        //  {
        //    &ExceptionRecord,
        //    &ContextRecord
        //  };

        //  MINIDUMP_EXCEPTION_INFORMATION ExceptionInformation
        //  {
        //    GetCurrentThreadId(),
        //    &ExceptionPointers,
        //    TRUE
        //  };

        //  MiniDumpWriteDump
        //  (
        //    GetCurrentProcess(),
        //    GetCurrentProcessId(),
        //    hFile,
        //    MiniDumpWithFullMemory,
        //    &ExceptionInformation,
        //    nullptr,
        //    nullptr
        //  );

        //  CloseHandle(hFile);
        //}
      }
    }
    else if (NotificationReason == LDR_DLL_NOTIFICATION_REASON_UNLOADED) { }
  }

  static std::vector<std::wstring> StackBackTrace(unsigned FramesToSkip)
  {
    std::vector<std::wstring> stack_trace;
    PVOID CallStack[128]{};
    auto count = MixedCaptureStackBackTrace(FramesToSkip + 1, _countof(CallStack), CallStack, nullptr);

    for (WORD num = 0; num < count; num++)
    {
      std::wstringstream stream;
      stream.imbue(std::locale::classic());

      TCHAR ModuleName[2048]{};
      if (MixedGetModuleFileName(CallStack[num], ModuleName, _countof(ModuleName)))
        stream << ModuleName;

      stream << "!" << CallStack[num] << "()" << std::endl;

      stack_trace.push_back(stream.str());
    }

    return stack_trace;
  }

public:

  LdrDllTracker() { }

  ~LdrDllTracker()
  {
    if (mCookie != nullptr)
    {
      LdrUnregisterDllNotification(mCookie);
      MixedStackTraceFinalize();
    }
  }

  const std::wstring& StackTraceFilePath(const std::wstring& reportFolder)
  {
    return mStackTraceFilePath = reportFolder;
  }

  const std::wstring& StackTraceFilePath() const
  {
    return mStackTraceFilePath;
  }

  bool ReportOnLoad(const std::wstring& moduleName, bool enable)
  {
    std::wstring module_name(moduleName.size(), wchar_t());
    std::transform(moduleName.begin(), moduleName.end(), module_name.begin(), tolower);
    bool success = false;

    if (enable)
    {
      if (mModuleSet.size() == 0)
      {
        MixedStackTraceInitialize();
        LdrRegisterDllNotification(0, LdrDllNotification, nullptr, &mCookie);
      }

      success = mModuleSet.insert(module_name).second;
    }
    else
    {
      success = mModuleSet.erase(module_name) == 1;

      if (mModuleSet.size() == 0)
      {
        LdrUnregisterDllNotification(mCookie); mCookie = nullptr;
        MixedStackTraceFinalize();
      }
    }

    return success;
  }

  bool ReportOnLoad(const std::wstring& moduleName) const
  {
    std::wstring module_name(moduleName.size(), wchar_t());
    std::transform(moduleName.begin(), moduleName.end(), module_name.begin(), tolower);

    return mModuleSet.find(module_name) != mModuleSet.cend();
  }

  static LdrDllTracker Instance;
};

LdrDllTracker LdrDllTracker::Instance;

RIR_EXPORT
LPCWSTR STDAPICALLTYPE LdrGetStackTraceFilePath()
{
  return LdrDllTracker::Instance.StackTraceFilePath().c_str();
}

RIR_EXPORT
void STDAPICALLTYPE LdrSetStackTraceFilePath(LPCWSTR pReportFilePath)
{
  LdrDllTracker::Instance.StackTraceFilePath(pReportFilePath ? pReportFilePath : L"");
}

RIR_EXPORT
BOOL STDAPICALLTYPE LdrGetReportOnLoad(LPCWSTR pModuleName)
{
  if (pModuleName == nullptr)
    return false;

  LdrDllTracker::Instance.ReportOnLoad(pModuleName);
  return true;
}

RIR_EXPORT
BOOL STDAPICALLTYPE LdrSetReportOnLoad(LPCWSTR pModuleName, BOOL bEnable)
{
  if (pModuleName == nullptr)
    return false;

  LdrDllTracker::Instance.ReportOnLoad(pModuleName, bEnable);
  return true;
}

BOOL EnsureOpenNurbsPrivateManifest(HWND hWnd, LPCTSTR ManifestFileName)
{
  if (GetFileAttributes(ManifestFileName) != INVALID_FILE_ATTRIBUTES)
    return TRUE;

  if
  (
    IDOK == MessageBox
    (
      hWnd,
      _T("Failed to find 'opennurbs_private.manifest' file in Revit folder.\r\n\r\n")
      _T("This file is necessary to avoid OpenNURBS conflicts with Revit builtin 3dm importer.\r\n\r\n")
      _T("Do you want to install it now? The copy operation will ask for admin access to copy this file to Revit folder"),
      _T("Rhino.Inside - opennurbs_private.manifest"),
      MB_ICONWARNING | MB_OK
    )
  )
  {
    TCHAR ModuleFileName[MAX_PATH]{};
    const DWORD ModuleFileNameLength = GetModuleFileName(hInstance, ModuleFileName, (DWORD)std::size(ModuleFileName));
    if (ModuleFileNameLength && ModuleFileNameLength < std::size(ModuleFileName))
    {
      if (LPTSTR FileName = std::max(_tcsrchr(ModuleFileName, '/'), _tcsrchr(ModuleFileName, '\\')))
      {
        auto size = ModuleFileName + std::size(ModuleFileName) - FileName - 1;
        _tcscpy_s(++FileName, size, _T("opennurbs_private.manifest"));

        std::wstring From = ModuleFileName, To = ManifestFileName;
        From.push_back(_T('\0'));
        To.push_back(_T('\0'));

        SHFILEOPSTRUCT Operation{ hWnd, FO_COPY, From.c_str(), To.c_str() };

        auto hwnd_enabled = IsWindowEnabled(Operation.hwnd);
        EnableWindow(Operation.hwnd, FALSE);
        int result = SHFileOperation(&Operation);
        EnableWindow(Operation.hwnd, hwnd_enabled);
        return result == 0;
      }
    }
  }

  return FALSE;
}

RIR_EXPORT
BOOL STDAPICALLTYPE LdrIsolateOpenNurbs()
{
  if (IsWindowsServer())
    return TRUE;

  TCHAR ManifestFileName[MAX_PATH] {};
  const DWORD ModuleFileNameLength = GetModuleFileName(NULL, ManifestFileName, (DWORD)std::size(ManifestFileName));
  if (ModuleFileNameLength && ModuleFileNameLength < std::size(ManifestFileName))
  {
    if (LPTSTR FileName = std::max(_tcsrchr(ManifestFileName, '/'), _tcsrchr(ManifestFileName, '\\')))
    {
      auto size = ManifestFileName + std::size(ManifestFileName) - FileName - 1;
      _tcscpy_s(++FileName, size, _T("opennurbs_private.manifest"));

      if (!EnsureOpenNurbsPrivateManifest(GetActiveWindow(), ManifestFileName))
        return FALSE;

      ACTCTX ActCtx {sizeof(ActCtx)};
      ActCtx.lpSource = ManifestFileName;

      auto hActCtx = CreateActCtx(&ActCtx);
      if (hActCtx != INVALID_HANDLE_VALUE)
      {
        ULONG_PTR Cookie {};
        if (ActivateActCtx(hActCtx, &Cookie))
        {
          LoadLibrary(_T("opennurbs"));
          LoadLibrary(_T("atf_rhino_producer"));
          DeactivateActCtx(0, Cookie);
        }

        ReleaseActCtx(hActCtx);
        return TRUE;
      }
    }
  }

  return FALSE;
}
