// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

#define RIR_EXPORT extern "C" __declspec(dllexport)

#define _CRT_SECURE_NO_WARNINGS
#define _SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING
#define NOMINMAX

// add headers that you want to pre-compile here
#include "framework.h"
#include <shellapi.h>

#include <memory>
#include <algorithm>
#include <string>
#include <set>
#include <filesystem>
#include <chrono>

#include <sstream>
#include <fstream>
#include <codecvt>

#endif //PCH_H
