#pragma once

#ifdef SIMPLERAWHID_EXPORTS
#define SIMPLERAWHID_API __declspec(dllexport)
#else
#define SIMPLERAWHID_API __declspec(dllimport)
#endif

extern "C" SIMPLERAWHID_API int rawhid_open(int max, int vid, int pid, int usage_page, int usage);
extern "C" SIMPLERAWHID_API int rawhid_recv(int num, void* buf, int len, int timeout);
extern "C" SIMPLERAWHID_API int rawhid_send(int num, void* buf, int len, int timeout);
extern "C" SIMPLERAWHID_API void rawhid_close(int num);
