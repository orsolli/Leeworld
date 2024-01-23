# Set the minimum required version of CMake
cmake_minimum_required(VERSION 3.12)

# Set the project name
project(LeeworldCpp)

# Add the executable target for main.cpp
add_executable(LeeworldEngine main.cpp engine.cpp)
