#include "pch.h"
#include "MixedStackTrace.h"

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
        auto stack_trace_file_path = Instance.StackTraceFilePath();
        if (stack_trace_file_path.empty())
        {
#ifdef _DEBUG
          OutputDebugString(buffer.str().c_str());
#endif
        }
        else
        {
          std::ofstream trace_file(stack_trace_file_path, std::fstream::app | std::fstream::out);
          if (trace_file.is_open())
          {
            std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>, wchar_t> conversion;
            trace_file << conversion.to_bytes(buffer.str());
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
        //  CONTEXT ContextRecord { };
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
      //stream << "[" << std::setw(3) << std::setfill(L'0') << num << "] ";

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
    return mModuleSet.find(moduleName) != mModuleSet.cend();
  }

  static LdrDllTracker Instance;
};

LdrDllTracker LdrDllTracker::Instance;

RIR_EXPORT
void STDAPICALLTYPE LdrSetStackTraceFilePath(LPCWSTR pReportFilePath)
{
  LdrDllTracker::Instance.StackTraceFilePath(pReportFilePath);
}

RIR_EXPORT
BOOL STDAPICALLTYPE LdrReportOnLoad(LPCWSTR pModuleName, BOOL bEnable)
{
  if (pModuleName == nullptr)
    return false;

  LdrDllTracker::Instance.ReportOnLoad(pModuleName, bEnable);
  return true;
}
