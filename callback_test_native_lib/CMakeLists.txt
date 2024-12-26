cmake_minimum_required(VERSION 3.30)
project(callback_test_native_lib)

set(CMAKE_CXX_STANDARD 26)

add_library(callback_test_native_lib SHARED library.cpp)
