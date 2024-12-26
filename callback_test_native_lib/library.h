#ifndef CALLBACK_TEST_NATIVE_LIB_LIBRARY_H
#define CALLBACK_TEST_NATIVE_LIB_LIBRARY_H
#include <cstdlib>
#include <thread>

#define IO_COMPLETE 0
#define IO_PENDING 1

extern "C" int32_t hello(int32_t in);
extern "C" int32_t invokeCallback(void (*callback)(int32_t input, void* state), void* state, bool async);

#endif //CALLBACK_TEST_NATIVE_LIB_LIBRARY_H