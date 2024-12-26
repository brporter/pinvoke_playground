#include "library.h"

#include <iostream>

__attribute__((visibility("default")))
extern "C" int32_t hello(int32_t in) {
    return in * in + in;
}

__attribute__((visibility("default")))
extern "C" int invokeCallback(void (*callback)(int32_t input, void* state), void* state, bool async) {
    if (async) {
        // std::wcout << L"Invoked Async..." << std::endl;
        std::thread([callback, state] {
            callback(42, state);
        }).detach();

        return IO_PENDING;
    } else {
        // std::wcout << L"Invoked Sync..." << std::endl;
        callback(24, state);

        return IO_COMPLETE;
    }
}